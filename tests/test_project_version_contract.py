"""
Tests for Project/Version Contract and Persistence.
"""

import tempfile
import pytest
from src.models import Project, ProjectVersion
from src.services.project_version_service import ProjectVersionService
from src.utils.postgres_db_manager import PostgresDBManager


@pytest.fixture
def temp_db():
    with tempfile.NamedTemporaryFile(suffix=".db") as tmp:
        db = PostgresDBManager(f"sqlite:///{tmp.name}")
        db.init_schema()
        yield db
        db.close()


def test_project_registration_and_immutability(temp_db):
    service = ProjectVersionService(temp_db)
    project = service.register_project(
        remote_url="https://github.com/example/repo",
        default_branch="main",
        owner_scope="user-1",
    )
    assert project.provider == "github"
    assert project.remote_url == "https://github.com/example/repo"
    assert project.default_branch == "main"
    assert project.owner_scope == "user-1"

    # Fetch project
    fetched = service.get_project(project.id, owner_scope="user-1")
    assert fetched is not None
    assert fetched.id == project.id

    # Unauthorized fetch returns None
    assert service.get_project(project.id, owner_scope="other-user") is None


def test_version_creation_and_deduplication(temp_db):
    service = ProjectVersionService(temp_db)
    project = service.register_project(
        remote_url="https://github.com/example/repo",
        default_branch="main",
        owner_scope="user-1",
    )

    commit_sha = "a" * 40
    version1, status1 = service.create_or_get_version(
        project_id=project.id,
        commit_sha=commit_sha,
        branch="main",
        content_digest="digest-123",
        build_config={"lang": "c"},
        manifest={"files": 10},
        owner_scope="user-1",
    )
    assert status1 == "created"
    assert version1.commit_sha == commit_sha
    assert version1.content_digest == "digest-123"

    # Duplicate call returns existing version with 'unchanged'
    version2, status2 = service.create_or_get_version(
        project_id=project.id,
        commit_sha=commit_sha,
        branch="main",
        content_digest="digest-123",
        build_config={"lang": "c"},
        manifest={"files": 10},
        owner_scope="user-1",
    )
    assert status2 == "unchanged"
    assert version2.id == version1.id


def test_version_list_and_ownership(temp_db):
    service = ProjectVersionService(temp_db)
    project = service.register_project(
        remote_url="https://gitlab.com/example/repo",
        default_branch="main",
        owner_scope="user-1",
    )

    sha1 = "1" * 40
    sha2 = "2" * 40
    service.create_or_get_version(project.id, sha1, "main", "d1", owner_scope="user-1")
    service.create_or_get_version(project.id, sha2, "main", "d2", owner_scope="user-1")

    versions = service.list_versions(project.id, owner_scope="user-1")
    assert len(versions) == 2
    assert service.list_versions(project.id, owner_scope="other-user") == []
