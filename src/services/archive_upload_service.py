import hashlib
import json
import logging
import os
import shutil
import tarfile
import tempfile
import zipfile
from typing import Any, Dict, Optional, Tuple

from .project_version_service import ProjectVersionService

logger = logging.getLogger(__name__)

MAX_UNCOMPRESSED_BYTES = 500 * 1024 * 1024  # 500 MB limit
MAX_FILE_COUNT = 10_000


class ArchiveUploadService:
    """Service handling safe ingestion of uploaded source archives with ZipSlip and size bomb protection."""

    def __init__(self, version_service: ProjectVersionService, cpg_queue: Optional[Any] = None):
        self.version_service = version_service
        self.cpg_queue = cpg_queue

    def process_archive_upload(
        self,
        project_id: str,
        archive_bytes: bytes,
        filename: str,
        build_config: Optional[Dict[str, Any]] = None,
        owner_scope: str = "default",
    ) -> Tuple[Dict[str, Any], str]:
        """Safely extract archive bytes, compute content digest, register version and auto-enqueue build.

        Returns (version_dict, status) where status is 'created' or 'unchanged'.
        """
        temp_dir = tempfile.mkdtemp(prefix="cb_archive_")
        try:
            filename_lower = filename.lower()
            if filename_lower.endswith(".zip"):
                self._extract_zip(archive_bytes, temp_dir)
            elif filename_lower.endswith((".tar.gz", ".tgz", ".tar")):
                self._extract_tar(archive_bytes, temp_dir)
            else:
                raise ValueError(f"Unsupported archive format: {filename}. Supported formats are .zip, .tar.gz, .tgz, .tar")

            digest = self._compute_dir_digest(temp_dir)
            commit_sha = f"archive:{digest[:32]}"
            branch = "upload"

            cfg = build_config or {}
            version, status = self.version_service.create_or_get_version(
                project_id=project_id,
                commit_sha=commit_sha,
                branch=branch,
                content_digest=digest,
                build_config=cfg,
                source_snapshot_ref=temp_dir,
                owner_scope=owner_scope,
            )

            version_dict = version.to_dict()

            if status == "created" and self.cpg_queue:
                job_payload = {
                    "version_id": version.id,
                    "project_id": project_id,
                    "codebase_hash": version.id,
                    "source_type": "local",
                    "source_path": temp_dir,
                    "build_config": cfg,
                }
                # Submit build job to cpg_queue if present
                if hasattr(self.cpg_queue, "submit_job"):
                    self.cpg_queue.submit_job(job_payload)
                elif hasattr(self.cpg_queue, "enqueue_job"):
                    self.cpg_queue.enqueue_job(version.id, "generate_cpg", job_payload)

            return version_dict, status

        except Exception:
            shutil.rmtree(temp_dir, ignore_errors=True)
            raise

    def _extract_zip(self, archive_bytes: bytes, target_dir: str):
        target_dir_abs = os.path.abspath(target_dir)
        total_uncompressed = 0
        file_count = 0

        bio = tempfile.NamedTemporaryFile(delete=False)
        bio.write(archive_bytes)
        bio.close()

        try:
            with zipfile.ZipFile(bio.name, "r") as zf:
                for member in zf.infolist():
                    file_count += 1
                    if file_count > MAX_FILE_COUNT:
                        raise ValueError(f"Archive exceeds maximum file count limit ({MAX_FILE_COUNT})")

                    total_uncompressed += member.file_size
                    if total_uncompressed > MAX_UNCOMPRESSED_BYTES:
                        raise ValueError("Archive exceeds maximum uncompressed size limit (500 MB)")

                    dest_path = os.path.abspath(os.path.join(target_dir, member.filename))
                    if not dest_path.startswith(target_dir_abs + os.sep) and dest_path != target_dir_abs:
                        raise ValueError("Directory traversal attempt detected in archive")

                    # Check for symlink/hardlink flags in external_attr if present
                    mode = member.external_attr >> 16
                    if (mode & 0o170000) == 0o120000:
                        raise ValueError("Symlinks and hardlinks in archives are not permitted")

                    zf.extract(member, target_dir)
        finally:
            if os.path.exists(bio.name):
                os.remove(bio.name)

    def _extract_tar(self, archive_bytes: bytes, target_dir: str):
        target_dir_abs = os.path.abspath(target_dir)
        total_uncompressed = 0
        file_count = 0

        bio = tempfile.NamedTemporaryFile(delete=False)
        bio.write(archive_bytes)
        bio.close()

        try:
            with tarfile.open(bio.name, "r:*") as tf:
                for member in tf.getmembers():
                    file_count += 1
                    if file_count > MAX_FILE_COUNT:
                        raise ValueError(f"Archive exceeds maximum file count limit ({MAX_FILE_COUNT})")

                    if member.issym() or member.islnk():
                        raise ValueError("Symlinks and hardlinks in archives are not permitted")

                    total_uncompressed += member.size
                    if total_uncompressed > MAX_UNCOMPRESSED_BYTES:
                        raise ValueError("Archive exceeds maximum uncompressed size limit (500 MB)")

                    dest_path = os.path.abspath(os.path.join(target_dir, member.name))
                    if not dest_path.startswith(target_dir_abs + os.sep) and dest_path != target_dir_abs:
                        raise ValueError("Directory traversal attempt detected in archive")

                    tf.extract(member, target_dir)
        finally:
            if os.path.exists(bio.name):
                os.remove(bio.name)

    def _compute_dir_digest(self, target_dir: str) -> str:
        hasher = hashlib.sha256()
        for root, dirs, files in os.walk(target_dir):
            dirs.sort()
            for file in sorted(files):
                full_path = os.path.join(root, file)
                rel_path = os.path.relpath(full_path, target_dir)
                hasher.update(rel_path.encode("utf-8"))
                try:
                    with open(full_path, "rb") as f:
                        while chunk := f.read(65536):
                            hasher.update(chunk)
                except OSError:
                    pass
        return hasher.hexdigest()
