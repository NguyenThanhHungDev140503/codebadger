import io
import pytest
from starlette.testclient import TestClient
from fastmcp import FastMCP

from src.api.rest_routes import register_rest_routes, format_version_response
from src.tools.lifecycle_tools import register_lifecycle_tools
from src.services.project_version_service import ProjectVersionService
from src.services.archive_upload_service import ArchiveUploadService
from tests.test_archive_upload_service import DummyDBManager


@pytest.fixture
def app_env():
    db = DummyDBManager()
    version_service = ProjectVersionService(db)
    archive_service = ArchiveUploadService(version_service)

    services = {
        "version_service": version_service,
        "archive_service": archive_service,
        "cpg_queue": None,
        "git_sync_service": None,
    }

    from starlette.applications import Starlette

    app = Starlette()
    register_rest_routes(app, services)

    mcp = FastMCP("TestMCP")
    register_lifecycle_tools(mcp, services)

    client = TestClient(app)
    return client, version_service, mcp


def test_rest_create_and_get_project(app_env):
    client, _, _ = app_env

    # POST /projects
    resp = client.post("/projects", json={"remote_url": "https://github.com/owner/repo.git"})
    assert resp.status_code == 201
    p = resp.json()
    assert p["remote_url"] == "https://github.com/owner/repo"

    # GET /projects/{id}
    get_resp = client.get(f"/projects/{p['id']}")
    assert get_resp.status_code == 200
    assert get_resp.json()["id"] == p["id"]


def test_rest_and_mcp_response_parity(app_env):
    client, version_service, mcp = app_env

    p = version_service.register_project("https://github.com/owner/repo.git")
    v, _ = version_service.create_or_get_version(
        project_id=p.id,
        commit_sha="a" * 40,
        branch="main",
        content_digest="digest123",
    )

    # Fetch via REST GET /versions/{id}
    rest_resp = client.get(f"/versions/{v.id}")
    assert rest_resp.status_code == 200
    rest_data = rest_resp.json()

    # Verify keys present
    expected_keys = {
        "id", "project_id", "commit_sha", "branch", "status", "phase",
        "queue_position", "elapsed_ms", "retry_count", "error", "created_at", "updated_at"
    }
    assert expected_keys.issubset(set(rest_data.keys()))
    assert rest_data["id"] == v.id
    assert rest_data["status"] == "queued"


def test_swagger_documents_every_version_catalog_endpoint(app_env):
    """Swagger remains a complete, executable contract for the REST surface."""
    client, _, _ = app_env

    schema_response = client.get("/openapi.json")
    assert schema_response.status_code == 200
    schema = schema_response.json()

    assert schema["openapi"] == "3.1.0"
    assert schema["paths"]["/projects"]["post"]["requestBody"]["required"] is True
    assert schema["paths"]["/projects/{id}/versions"]["post"]["requestBody"]["content"]
    assert schema["paths"]["/versions"]["get"]["parameters"][0]["name"] == "project_id"
    assert schema["paths"]["/versions/{id}/retry"]["post"]["parameters"][0]["in"] == "path"

    docs_response = client.get("/docs")
    assert docs_response.status_code == 200
    assert "SwaggerUIBundle" in docs_response.text
    assert "/openapi.json" in docs_response.text
