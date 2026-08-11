import logging
from typing import Any, Dict

from ..api.rest_routes import format_version_response

logger = logging.getLogger(__name__)


def register_lifecycle_tools(mcp: Any, services: Dict[str, Any]):
    version_service = services.get("version_service")
    git_sync_service = services.get("git_sync_service")
    archive_service = services.get("archive_service")
    cpg_queue = services.get("cpg_queue")

    @mcp.tool()
    def project_create(
        remote_url: str,
        default_branch: str = "main",
        owner_scope: str = "default",
        credential: str = None,
    ) -> Dict[str, Any]:
        """Register a new Git project repository in CodeBadger catalog."""
        p = version_service.register_project(remote_url, default_branch, owner_scope, credential)
        return p.to_dict()

    @mcp.tool()
    def project_list(owner_scope: str = "default") -> list:
        """List registered projects for an owner scope."""
        projects = version_service.list_projects(owner_scope=owner_scope)
        return [p.to_dict() for p in projects]

    @mcp.tool()
    def project_delete(project_id: str, owner_scope: str = "default") -> Dict[str, Any]:
        """Delete a project and its credentials."""
        ok = version_service.delete_project(project_id, owner_scope)
        if not ok:
            raise ValueError("Project not found")
        return {"status": "deleted", "id": project_id}

    @mcp.tool()
    async def version_sync(
        project_id: str,
        branch: str = None,
        build_config: dict = None,
    ) -> Dict[str, Any]:
        """Fetch remote branch updates, create version, and trigger CPG build."""
        v_dict, status = await git_sync_service.sync_project_branch(project_id, branch, build_config)
        v = version_service.get_version(v_dict["id"])
        return format_version_response(v)

    @mcp.tool()
    def version_list(project_id: str, owner_scope: str = "default") -> list:
        """List all version build states for a project."""
        versions = version_service.list_versions(project_id, owner_scope)
        return [format_version_response(v) for v in versions]

    @mcp.tool()
    def version_get(version_id: str, owner_scope: str = "default") -> Dict[str, Any]:
        """Get build status details and observability metadata for a version."""
        v = version_service.get_version(version_id, owner_scope)
        if not v:
            raise ValueError("Version not found")
        return format_version_response(v)

    @mcp.tool()
    def version_retry(version_id: str, owner_scope: str = "default") -> Dict[str, Any]:
        """Retry a failed or cancelled version build."""
        v, status = version_service.retry_version_build(version_id, queue=cpg_queue, owner_scope=owner_scope)
        return format_version_response(v)

    @mcp.tool()
    def version_cancel(version_id: str, owner_scope: str = "default") -> Dict[str, Any]:
        """Cancel an active version build and purge partial artifacts."""
        v, ok = version_service.cancel_version_build(version_id, owner_scope=owner_scope)
        return format_version_response(v)
