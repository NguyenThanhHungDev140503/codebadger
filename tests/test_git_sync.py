"""
Integration and unit tests for GitSyncService.
"""

import os
import json
import shutil
import subprocess
import tempfile
from unittest.mock import patch
import pytest

from src.services.git_sync_service import GitSyncService
from src.services.project_version_service import ProjectVersionService
from src.utils.postgres_db_manager import PostgresDBManager


@pytest.fixture
def temp_env():
    tmp_dir = tempfile.mkdtemp()
    db_path = os.path.join(tmp_dir, "test.db")
    db = PostgresDBManager(f"sqlite:///{db_path}")
    db.init_schema()

    version_service = ProjectVersionService(db)
    sync_service = GitSyncService(tmp_dir, version_service)

    # Create dummy git repository fixture
    repo_dir = os.path.join(tmp_dir, "fixture_repo")
    os.makedirs(repo_dir, exist_ok=True)
    subprocess.run(["git", "init"], cwd=repo_dir, check=True, capture_output=True)
    subprocess.run(["git", "config", "user.name", "Test"], cwd=repo_dir, check=True)
    subprocess.run(["git", "config", "user.email", "test@example.com"], cwd=repo_dir, check=True)

    with open(os.path.join(repo_dir, "README.md"), "w") as f:
        f.write("# Fixture Repo\n")
    subprocess.run(["git", "add", "."], cwd=repo_dir, check=True)
    subprocess.run(["git", "commit", "-m", "Initial commit"], cwd=repo_dir, check=True)

    # Rename default branch to main if needed
    subprocess.run(["git", "branch", "-M", "main"], cwd=repo_dir, check=True)

    commit_sha = subprocess.run(
        ["git", "rev-parse", "HEAD"], cwd=repo_dir, check=True, capture_output=True, text=True
    ).stdout.strip()

    yield {
        "root": tmp_dir,
        "db": db,
        "version_service": version_service,
        "sync_service": sync_service,
        "repo_dir": repo_dir,
        "commit_sha": commit_sha,
    }

    db.close()
    shutil.rmtree(tmp_dir, ignore_errors=True)


@pytest.mark.asyncio
async def test_git_sync_flow(temp_env):
    v_service = temp_env["version_service"]
    s_service = temp_env["sync_service"]
    repo_url = "https://github.com/example/test-repo"

    project = v_service.register_project(remote_url=repo_url, default_branch="main", owner_scope="user-1")

    repo_dir = temp_env["repo_dir"]
    commit_sha = temp_env["commit_sha"]

    # Keep URL validation in the production path, but substitute the local
    # fixture URL after registration so the actual Git fetch/checkout/promotion
    # flow is exercised without a network dependency.
    with patch("src.services.git_sync_service.canonicalize_repo_url", return_value=repo_dir):
        v1, status1 = await s_service.sync_project_branch(
            project.id, branch="main", build_config={"lang": "c"}, owner_scope="user-1"
        )
    assert status1 == "created"
    assert v1["commit_sha"] == commit_sha
    snapshot_ref = v1["source_snapshot_ref"]
    assert os.path.isdir(snapshot_ref)
    manifest = json.loads(v1["manifest"])
    assert manifest["files"][0]["sha256"]

    with patch("src.services.git_sync_service.canonicalize_repo_url", return_value=repo_dir):
        v2, status2 = await s_service.sync_project_branch(
            project.id, branch="main", build_config={"lang": "c"}, owner_scope="user-1"
        )
    assert status2 == "unchanged"
    assert v2["id"] == v1["id"]
    assert os.path.isdir(snapshot_ref)


def test_snapshot_digest_includes_file_contents(temp_env):
    service = temp_env["sync_service"]
    root = os.path.join(temp_env["root"], "digest")
    os.makedirs(root)
    path = os.path.join(root, "same-size.txt")
    with open(path, "wb") as output:
        output.write(b"aaaa")
    first, _ = service._compute_snapshot_metadata(root)
    with open(path, "wb") as output:
        output.write(b"bbbb")
    second, _ = service._compute_snapshot_metadata(root)
    assert first != second
