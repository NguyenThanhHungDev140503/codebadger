"""
Git synchronization service using safe subprocess execution with ephemeral credential handling.
"""

import asyncio
import hashlib
import logging
import os
import re
import shutil
import subprocess
from typing import Dict, Optional, Tuple

from ..exceptions import GitOperationError, ValidationError
from ..utils.validators import (
    canonicalize_repo_url,
    validate_git_branch,
    validate_github_token,
    validate_repo_url,
)
from .project_version_service import ProjectVersionService

logger = logging.getLogger(__name__)

# Lock per project_id to serialize concurrent sync operations
_PROJECT_LOCKS: Dict[str, asyncio.Lock] = {}


def _get_project_lock(project_id: str) -> asyncio.Lock:
    if project_id not in _PROJECT_LOCKS:
        _PROJECT_LOCKS[project_id] = asyncio.Lock()
    return _PROJECT_LOCKS[project_id]


def _mask_text(text: str, token: Optional[str] = None) -> str:
    """Mask credentials in logs, URLs, and subprocess output."""
    if not text:
        return ""
    masked = re.sub(r"(https?://)[^@\s]+@", r"\1***@", text)
    if token and token in masked:
        masked = masked.replace(token, "***")
    return masked


class GitSyncService:
    """Service to safely sync Git repositories using safe CLI subprocess calls."""

    def __init__(self, workspace_root: str, version_service: ProjectVersionService):
        self.workspace_root = workspace_root
        self.version_service = version_service
        self.sync_dir = os.path.join(workspace_root, "sync_workspaces")
        self.snapshot_dir = os.path.join(workspace_root, "snapshots")
        os.makedirs(self.sync_dir, exist_ok=True)
        os.makedirs(self.snapshot_dir, exist_ok=True)

    def _run_git_cmd(
        self,
        args: list[str],
        cwd: str,
        env: Optional[Dict[str, str]] = None,
        timeout: int = 120,
        token: Optional[str] = None,
    ) -> str:
        """Run a git CLI command safely without shell interpolation."""
        clean_env = os.environ.copy()
        if env:
            clean_env.update(env)

        # Prevent interactive prompts
        clean_env["GIT_TERMINAL_PROMPT"] = "0"
        clean_env["GIT_ASKPASS"] = "echo"

        try:
            res = subprocess.run(
                ["git"] + args,
                cwd=cwd,
                env=clean_env,
                capture_output=True,
                text=True,
                timeout=timeout,
                shell=False,
                check=True,
            )
            return res.stdout.strip()
        except subprocess.TimeoutExpired as e:
            logger.error(f"Git command timed out: {args[0] if args else ''}")
            raise GitOperationError("Git command timed out")
        except subprocess.CalledProcessError as e:
            safe_stderr = _mask_text(e.stderr, token)
            logger.error(f"Git command failed: {safe_stderr}")
            raise GitOperationError(f"Git operation failed: {safe_stderr}")
        except Exception as e:
            safe_err = _mask_text(str(e), token)
            raise GitOperationError(f"Git error: {safe_err}")

    async def sync_project_branch(
        self,
        project_id: str,
        branch: Optional[str] = None,
        build_config: Optional[Dict] = None,
        owner_scope: str = "default",
        timeout: int = 120,
    ) -> Tuple[Dict, str]:
        """Fetch remote branch, resolve SHA, update snapshot, return (version_dict, status)."""
        lock = _get_project_lock(project_id)
        async with lock:
            loop = asyncio.get_event_loop()
            return await loop.run_in_executor(
                None,
                self._do_sync,
                project_id,
                branch,
                build_config,
                owner_scope,
                timeout,
            )

    def _do_sync(
        self,
        project_id: str,
        target_branch: Optional[str],
        build_config: Optional[Dict],
        owner_scope: str,
        timeout: int,
    ) -> Tuple[Dict, str]:
        project = self.version_service.get_project(project_id, owner_scope)
        if not project:
            raise ValidationError(f"Project {project_id} not found or unauthorized")

        branch = target_branch or project.default_branch
        validate_git_branch(branch)

        credential = self.version_service.get_project_credential(project_id, owner_scope)
        if credential:
            validate_github_token(credential)

        # Isolated temporary workspace for fetching
        ws_dir = os.path.join(self.sync_dir, f"ws_{project_id}")
        if os.path.exists(ws_dir):
            shutil.rmtree(ws_dir, ignore_errors=True)
        os.makedirs(ws_dir, exist_ok=True)

        try:
            # Construct clean remote URL (never embed credential in URL/argv)
            clean_url = canonicalize_repo_url(project.remote_url)

            # git init
            self._run_git_cmd(["init"], cwd=ws_dir, timeout=timeout, token=credential)

            # Ephemeral header override for HTTP auth to avoid putting token in argv URL
            git_config_env = {}
            if credential:
                import base64
                b64_cred = base64.b64encode(f":{credential}".encode("utf-8")).decode("utf-8")
                git_config_env["GIT_CONFIG_COUNT"] = "1"
                git_config_env["GIT_CONFIG_KEY_0"] = "http.extraHeader"
                git_config_env["GIT_CONFIG_VALUE_0"] = f"Authorization: Basic {b64_cred}"

            # git fetch --depth=1 origin branch
            self._run_git_cmd(
                ["fetch", "--depth=1", clean_url, branch],
                cwd=ws_dir,
                env=git_config_env,
                timeout=timeout,
                token=credential,
            )

            # Resolve FETCH_HEAD SHA
            commit_sha = self._run_git_cmd(
                ["rev-parse", "FETCH_HEAD"],
                cwd=ws_dir,
                timeout=timeout,
                token=credential,
            )

            if len(commit_sha) != 40:
                raise GitOperationError(f"Invalid commit SHA resolved: {commit_sha}")

            # Check if existing version matches
            cfg = build_config or {}
            existing_ver = self.version_service.get_version(
                self.version_service.compute_version_id(project_id, commit_sha, cfg),
                owner_scope,
            )
            if existing_ver:
                # Cleanup workspace
                shutil.rmtree(ws_dir, ignore_errors=True)
                return existing_ver.to_dict(), "unchanged"

            # Detached checkout for snapshotting
            self._run_git_cmd(
                ["checkout", "--detach", "FETCH_HEAD"],
                cwd=ws_dir,
                timeout=timeout,
                token=credential,
            )

            # Remove .git dir before hashing snapshot
            git_dir = os.path.join(ws_dir, ".git")
            if os.path.exists(git_dir):
                shutil.rmtree(git_dir, ignore_errors=True)

            # Compute content digest and manifest
            digest, manifest = self._compute_snapshot_metadata(ws_dir)

            # Promote snapshot
            snapshot_path = os.path.join(self.snapshot_dir, f"{project_id}_{commit_sha[:12]}")
            if os.path.exists(snapshot_path):
                shutil.rmtree(snapshot_path, ignore_errors=True)

            shutil.move(ws_dir, snapshot_path)

            version, status = self.version_service.create_or_get_version(
                project_id=project_id,
                commit_sha=commit_sha,
                branch=branch,
                content_digest=digest,
                build_config=cfg,
                manifest=manifest,
                source_snapshot_ref=snapshot_path,
                owner_scope=owner_scope,
            )

            return version.to_dict(), status

        except Exception as e:
            if os.path.exists(ws_dir):
                shutil.rmtree(ws_dir, ignore_errors=True)
            raise

    def _compute_snapshot_metadata(self, root_dir: str) -> Tuple[str, Dict]:
        hasher = hashlib.sha256()
        file_count = 0
        total_size = 0

        for dirpath, _, filenames in sorted(os.walk(root_dir)):
            for filename in sorted(filenames):
                filepath = os.path.join(dirpath, filename)
                relpath = os.path.relpath(filepath, root_dir)
                hasher.update(relpath.encode("utf-8"))
                file_count += 1
                try:
                    size = os.path.getsize(filepath)
                    total_size += size
                    hasher.update(str(size).encode("utf-8"))
                except OSError:
                    pass

        manifest = {
            "file_count": file_count,
            "total_bytes": total_size,
        }
        return hasher.hexdigest(), manifest
