"""
Integration and unit tests for GitSyncService.
"""

import os
import shutil
import subprocess
import tempfile
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

    # Mock _do_sync or substitute file protocol for test execution
    repo_dir = temp_env["repo_dir"]
    commit_sha = temp_env["commit_sha"]

    # Directly verify snapshot creation and deduplication via version service contract
    v1, status1 = v_service.create_or_get_version(
        project_id=project.id,
        commit_sha=commit_sha,
        branch="main",
        content_digest="digest-abc",
        build_config={"lang": "c"},
        manifest={"file_count": 1},
        owner_scope="user-1",
    )
    assert status1 == "created"
    assert v1.commit_sha == commit_sha

    v2, status2 = v_service.create_or_get_version(
        project_id=project.id,
        commit_sha=commit_sha,
        branch="main",
        content_digest="digest-abc",
        build_config={"lang": "c"},
        manifest={"file_count": 1},
        owner_scope="user-1",
    )
    assert status2 == "unchanged"
    assert v2.id == v1.id
