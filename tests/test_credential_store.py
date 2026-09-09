"""
Tests for Encrypted Project Credential Store.
"""

import tempfile
import pytest
from src.services.credential_store import (
    FernetCredentialEncryptionAdapter,
    InMemoryCredentialEncryptionAdapter,
)
from src.services.project_version_service import ProjectVersionService
from src.utils.postgres_db_manager import PostgresDBManager


@pytest.fixture
def temp_db():
    with tempfile.NamedTemporaryFile(suffix=".db") as tmp:
        db = PostgresDBManager(f"sqlite:///{tmp.name}")
        db.init_schema()
        yield db
        db.close()


def test_in_memory_credential_adapter():
    adapter = InMemoryCredentialEncryptionAdapter("secret")
    plaintext = "ghp_1234567890abcdef"
    ciphertext = adapter.encrypt(plaintext)
    assert ciphertext != plaintext
    decrypted = adapter.decrypt(ciphertext)
    assert decrypted == plaintext


def test_fernet_credential_adapter():
    adapter = FernetCredentialEncryptionAdapter()
    plaintext = "glpat-secret-token"
    ciphertext = adapter.encrypt(plaintext)
    assert ciphertext != plaintext
    decrypted = adapter.decrypt(ciphertext)
    assert decrypted == plaintext


def test_project_credential_lifecycle(temp_db):
    service = ProjectVersionService(temp_db)
    project = service.register_project(
        remote_url="https://github.com/example/private-repo",
        default_branch="main",
        owner_scope="user-1",
        credential="ghp_initialtoken123",
    )

    # Read credential back
    token = service.get_project_credential(project.id, owner_scope="user-1")
    assert token == "ghp_initialtoken123"

    # Unauthorized read returns None
    assert service.get_project_credential(project.id, owner_scope="other-user") is None

    # Replace credential
    service.set_project_credential(project.id, "ghp_updatedtoken456", owner_scope="user-1")
    updated = service.get_project_credential(project.id, owner_scope="user-1")
    assert updated == "ghp_updatedtoken456"

    # Revoke credential
    assert service.revoke_project_credential(project.id, owner_scope="user-1") is True
    assert service.get_project_credential(project.id, owner_scope="user-1") is None
