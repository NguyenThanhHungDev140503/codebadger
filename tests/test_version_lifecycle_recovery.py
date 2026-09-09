"""
Tests for version lifecycle recovery: cancellation, retry, and startup reconciliation.
"""

import os
import tempfile
import pytest
from src.models import Project, ProjectVersion
from src.services.project_version_service import ProjectVersionService
from src.utils.postgres_db_manager import PostgresDBManager
from src.tools.core_tools import DurableCPGQueue


@pytest.fixture
def temp_db():
    with tempfile.NamedTemporaryFile(suffix=".db") as tmp:
        db = PostgresDBManager(f"sqlite:///{tmp.name}")
        db.init_schema()
        yield db
        db.close()


@pytest.fixture
def setup_project(temp_db):
    service = ProjectVersionService(temp_db)
    project = service.register_project(
        remote_url="https://github.com/example/repo",
        default_branch="main",
        owner_scope="user-1",
    )
    commit_sha = "a" * 40
    version, _ = service.create_or_get_version(
        project_id=project.id,
        commit_sha=commit_sha,
        branch="main",
        content_digest="digest-123",
        owner_scope="user-1",
    )
    return service, project, version


# Task 1 Tests: Cancellation
def test_cancel_success(temp_db, setup_project, tmp_path):
    service, project, version = setup_project

    # Create temporary partial artifact
    codebase_hash = "hash_cancel_test"
    tmp_file = tmp_path / f"{codebase_hash}.cpg.bin.tmp"
    tmp_file.write_text("partial cpg data")

    # Set version build_status to queued, building, or loading
    temp_db.update_version_status(version.id, "building", {})

    version_res, cancelled = service.cancel_version_build(
        version_id=version.id,
        codebase_hash=codebase_hash,
        partial_artifacts=[str(tmp_file)],
        owner_scope="user-1",
    )

    assert cancelled is True
    assert version_res.build_status == "cancelled"
    assert not tmp_file.exists()

    # Verify DB row is kept and status updated
    updated_ver = service.get_version(version.id, owner_scope="user-1")
    assert updated_ver is not None
    assert updated_ver.build_status == "cancelled"


def test_cancel_ready_guard(temp_db, setup_project):
    service, project, version = setup_project
    temp_db.update_version_status(version.id, "ready", {})

    with pytest.raises(ValueError, match="cannot cancel a ready build"):
        service.cancel_version_build(version.id, owner_scope="user-1")


def test_cancel_failed_guard(temp_db, setup_project):
    service, project, version = setup_project
    temp_db.update_version_status(version.id, "failed", {})

    with pytest.raises(ValueError, match="cannot cancel a failed build"):
        service.cancel_version_build(version.id, owner_scope="user-1")


def test_cancel_cleanup(temp_db, setup_project, tmp_path):
    service, project, version = setup_project
    temp_db.update_version_status(version.id, "queued", {})

    snapshot_dir = tmp_path / "snapshot_workspace"
    snapshot_dir.mkdir()
    (snapshot_dir / "file.txt").write_text("code")

    service.cancel_version_build(
        version.id,
        partial_artifacts=[str(snapshot_dir)],
        owner_scope="user-1",
    )

    assert not snapshot_dir.exists()


# Task 2 Tests: Retry
def test_retry_failed(temp_db, setup_project):
    service, project, version = setup_project
    temp_db.update_version_status(version.id, "failed", {"error": "some error"})

    queue = DurableCPGQueue(temp_db, services={"db_manager": temp_db})
    retried_version, status = service.retry_version_build(
        version.id, queue=queue, owner_scope="user-1"
    )

    assert status == "queued"
    assert retried_version.build_status == "queued"
    assert retried_version.build_metadata.get("retry_count") == 1
    assert temp_db.count_jobs("queued") == 1


def test_retry_cancelled(temp_db, setup_project):
    service, project, version = setup_project
    temp_db.update_version_status(version.id, "cancelled", {})

    queue = DurableCPGQueue(temp_db, services={"db_manager": temp_db})
    retried_version, status = service.retry_version_build(
        version.id, queue=queue, owner_scope="user-1"
    )

    assert status == "queued"
    assert retried_version.build_status == "queued"
    assert retried_version.build_metadata.get("retry_count") == 1
    assert temp_db.count_jobs("queued") == 1


def test_retry_idempotent(temp_db, setup_project):
    service, project, version = setup_project
    temp_db.update_version_status(version.id, "building", {})

    queue = DurableCPGQueue(temp_db, services={"db_manager": temp_db})
    retried_version, status = service.retry_version_build(
        version.id, queue=queue, owner_scope="user-1"
    )

    assert status == "already_active"
    assert retried_version.build_status == "building"
    assert temp_db.count_jobs("queued") == 0


def test_retry_ready_guard(temp_db, setup_project):
    service, project, version = setup_project
    temp_db.update_version_status(version.id, "ready", {})

    queue = DurableCPGQueue(temp_db, services={"db_manager": temp_db})
    with pytest.raises(ValueError, match="cannot retry a ready build"):
        service.retry_version_build(version.id, queue=queue, owner_scope="user-1")


# Task 3 Tests: Startup Reconciliation
def test_reconciliation_requeue(temp_db, setup_project):
    service, project, version = setup_project
    temp_db.update_version_status(version.id, "building", {})

    # Submit a job directly
    job_id, status = temp_db.enqueue_job("hash1", "generate_cpg", {"version_id": version.id})
    assert status == "submitted"

    # Set job status to running via DB manager _connect using raw execute
    with temp_db._connect() as conn:
        conn.execute("UPDATE jobs SET status = 'running', attempts = 1 WHERE id = %s", (job_id,))
        conn.commit()

    j = temp_db.get_job(job_id)
    assert j is not None
    assert j["status"] == "running"

    # Call requeue_running_jobs(max_retries=3)
    requeued_count = temp_db.requeue_running_jobs(max_retries=3)
    assert requeued_count == 1

    # Check job status is queued
    job_after = temp_db.get_job(job_id)
    assert job_after["status"] == "queued"

    # Check version status is queued
    updated_ver = service.get_version(version.id, owner_scope="user-1")
    assert updated_ver.build_status == "queued"


def test_reconciliation_max_retries_cap(temp_db, setup_project):
    service, project, version = setup_project
    temp_db.update_version_status(version.id, "building", {})

    # Submit a job and simulate multiple crash recoveries (attempts >= 3)
    job_id, status = temp_db.enqueue_job("hash2", "generate_cpg", {"version_id": version.id})
    assert status == "submitted"

    # Set attempts = 3 and status = running in DB
    with temp_db._connect() as conn:
        conn.execute("UPDATE jobs SET status = 'running', attempts = 3 WHERE id = %s", (job_id,))
        conn.commit()

    requeued_count = temp_db.requeue_running_jobs(max_retries=3)
    assert requeued_count == 1

    # Job should now be failed
    job_after = temp_db.get_job(job_id)
    assert job_after["status"] == "failed"
    assert "EXCEEDED_MAX_RETRIES" in job_after["error"]

    # Version status should now be failed with EXCEEDED_MAX_RETRIES error code
    updated_ver = service.get_version(version.id, owner_scope="user-1")
    assert updated_ver.build_status == "failed"
    assert updated_ver.build_metadata.get("error", {}).get("error_code") == "EXCEEDED_MAX_RETRIES"
