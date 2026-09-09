import io
import tarfile
import zipfile
import pytest

from src.models import Project
from src.services.archive_upload_service import ArchiveUploadService, MAX_UNCOMPRESSED_BYTES, MAX_FILE_COUNT
from src.services.project_version_service import ProjectVersionService
from src.utils.postgres_db_manager import PostgresDBManager


class DummyDBManager(PostgresDBManager):
    def __init__(self):
        import sqlite3
        self.conn = sqlite3.connect(":memory:", check_same_thread=False)
        self.conn.row_factory = sqlite3.Row
        self._init_sqlite_schema()

    def _init_sqlite_schema(self):
        with self.conn:
            self.conn.execute("""
                CREATE TABLE projects (
                    id TEXT PRIMARY KEY,
                    provider TEXT NOT NULL,
                    remote_url TEXT NOT NULL,
                    default_branch TEXT NOT NULL,
                    owner_scope TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL
                )
            """)
            self.conn.execute("""
                CREATE TABLE project_versions (
                    id TEXT PRIMARY KEY,
                    project_id TEXT NOT NULL,
                    commit_sha TEXT NOT NULL,
                    branch TEXT NOT NULL,
                    content_digest TEXT NOT NULL,
                    build_config TEXT NOT NULL,
                    manifest TEXT NOT NULL,
                    source_snapshot_ref TEXT,
                    build_status TEXT NOT NULL DEFAULT 'queued',
                    build_metadata TEXT DEFAULT '{}',
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL
                )
            """)
            self.conn.execute("""
                CREATE TABLE project_credentials (
                    project_id TEXT PRIMARY KEY,
                    ciphertext TEXT NOT NULL,
                    key_version TEXT NOT NULL,
                    updated_at TEXT NOT NULL
                )
            """)
            self.conn.execute("""
                CREATE TABLE jobs (
                    job_id TEXT PRIMARY KEY,
                    codebase_hash TEXT NOT NULL,
                    job_type TEXT NOT NULL,
                    version_id TEXT,
                    status TEXT NOT NULL,
                    payload TEXT,
                    error TEXT,
                    attempts INTEGER DEFAULT 0,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL
                )
            """)

    def execute(self, sql, params=()):
        sql = sql.replace("%s", "?")
        return self.conn.execute(sql, params)

    def commit(self):
        self.conn.commit()

    def rollback(self):
        self.conn.rollback()

    def _connect(self):
        class ConnContext:
            def __init__(ctx_self, conn_obj):
                ctx_self.conn_obj = conn_obj
            def __enter__(ctx_self):
                return ctx_self.conn_obj
            def __exit__(ctx_self, exc_type, exc_val, exc_tb):
                pass
        return ConnContext(self)

    def enqueue_job(self, codebase_hash: str, job_type: str, payload: dict, version_id: str = None) -> str:
        return "job_123"


@pytest.fixture
def service_env():
    db = DummyDBManager()
    version_service = ProjectVersionService(db)
    project = version_service.register_project("https://github.com/owner/repo.git")
    archive_service = ArchiveUploadService(version_service)
    return archive_service, version_service, project.id


def test_zip_traversal_protection(service_env):
    archive_service, _, project_id = service_env

    buf = io.BytesIO()
    with zipfile.ZipFile(buf, "w") as zf:
        zf.writestr("../etc/passwd", "root:x:0:0")

    with pytest.raises(ValueError, match="Directory traversal attempt detected"):
        archive_service.process_archive_upload(project_id, buf.getvalue(), "bad.zip")


def test_zip_symlink_protection(service_env):
    archive_service, _, project_id = service_env

    buf = io.BytesIO()
    with zipfile.ZipFile(buf, "w") as zf:
        zi = zipfile.ZipInfo("symlink.txt")
        zi.external_attr = 0o120755 << 16  # S_IFLNK
        zf.writestr(zi, "/target")

    with pytest.raises(ValueError, match="Symlinks and hardlinks in archives are not permitted"):
        archive_service.process_archive_upload(project_id, buf.getvalue(), "link.zip")


def test_valid_zip_upload_and_deduplication(service_env):
    archive_service, version_service, project_id = service_env

    buf = io.BytesIO()
    with zipfile.ZipFile(buf, "w") as zf:
        zf.writestr("src/main.c", "int main() { return 0; }")
        zf.writestr("README.md", "# Hello World")

    archive_bytes = buf.getvalue()
    v1, status1 = archive_service.process_archive_upload(project_id, archive_bytes, "source.zip")

    assert status1 == "created"
    assert v1["project_id"] == project_id
    assert v1["build_status"] == "queued"

    # Second upload of identical archive
    v2, status2 = archive_service.process_archive_upload(project_id, archive_bytes, "source.zip")
    assert status2 == "unchanged"
    assert v2["id"] == v1["id"]
