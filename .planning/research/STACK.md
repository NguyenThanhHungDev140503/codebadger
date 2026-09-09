# Technology Stack

**Project:** CodeBadger v0.7 — Codebase Context Backend  
**Researched:** 2026-08-09  
**Scope:** Backend stack additions only; this recommendation preserves the deployed Python/FastMCP/Joern/Postgres/Redis/Docker Compose architecture.

## Recommended Stack

### Core Framework

| Technology | Version | Purpose | Why |
|---|---:|---|---|
| Python | 3.13 (already deployed) | Application runtime | Supports the existing service code and current standard-library archive safety APIs. Do not lower the project’s actual runtime to the `>=3.10` packaging floor. |
| FastMCP | `>=3.4.2` (existing) | MCP tool surface | Keep the established MCP contracts and tool registration. FastMCP’s ASGI app can be mounted in a FastAPI application. |
| FastAPI | `>=0.115,<1` | Authenticated REST facade and OpenAPI contract | Make FastAPI the outer ASGI app; mount FastMCP at `/mcp`, while REST owns `/v1/*`. This gives REST proper dependencies/authentication, request models, responses, and documentation without a second server process. Pin the exact compatible release in the lockfile after validating it with the installed FastMCP version. |
| Uvicorn | `>=0.49.0` (existing) | ASGI server | Already ships with CodeBadger; serve the combined FastAPI + FastMCP app rather than running separate listeners. |
| Pydantic | `>=2.13.4` (existing) | REST request/response schemas | Reuse for project/version/job/context DTOs and explicit, bounded query parameters. |
| `python-multipart` | `>=0.0.20,<1` | Multipart parser required by FastAPI file endpoints | Required for `UploadFile`-based archive submission. Stream bounded chunks to a staging file; never call `read()` with no size. |

### Database

| Technology | Version | Purpose | Why |
|---|---:|---|---|
| PostgreSQL | 16 (existing) | Catalog, version lifecycle, jobs, citation/index records, lexical retrieval | Extend the current shared store rather than introduce Elasticsearch or a vector database. Add migrations for normalized `projects`, `project_versions`, `source_files`, `symbols`, and `context_documents`/`context_edges`; retain the existing CPG hash as the build artifact identity. |
| PostgreSQL full-text search | Built into PostgreSQL 16 | Ranked lexical retrieval over extracted source/context chunks | Store a generated or maintained `tsvector`, query with `websearch_to_tsquery`/`plainto_tsquery`, and index it with GIN. It is explainable, transactional with version metadata, and meets the explicitly non-embedding v0.7 direction. |
| `pg_trgm` | PostgreSQL 16 contrib extension | Symbol/file-name substring and typo-tolerant lookup | Add a GIN/GiST trigram index only on bounded name/path fields. It complements FTS, which is weak for identifiers such as `parseHTTP2Frame`. |
| psycopg + psycopg_pool | `psycopg[binary,pool]>=3.3.4` (existing) | Postgres access and pool | Continue using the existing pooled DB manager. Add transaction-scoped repository methods and numbered SQL migrations; do not add an ORM in this milestone. |
| Redis | 7 (existing) | Cross-process locks and Joern worker ledger | Keep it out of the new source-of-record path. PostgreSQL remains authoritative for projects, versions, jobs, and retrieval metadata. |

### Infrastructure

| Technology | Version | Purpose | Why |
|---|---:|---|---|
| Docker Compose | v2 (existing) | Single-host deployment | Extend the current service rather than split REST and MCP into containers. Continue mounting persistent `playground/`, `pgdata/`, and `logs/` as documented. |
| Joern | Existing pinned image/build | CPG build and graph relationship retrieval | Preserve the memory-aware, cgroup-capped worker pool. Context retrieval calls a narrow `ContextService` over the existing query executor; it must not expose raw CPGQL to REST callers. |
| Filesystem staging volume | Existing `playground/` volume, with new `uploads/` and `snapshots/` subtrees | Archive quarantine and immutable source snapshots | Stage outside any directory directly visible to a Joern worker until validation completes; then atomically promote a validated, symlink-free snapshot. Future isolation should mount only that version’s snapshot into its worker. |

### Supporting Libraries

| Library | Version | Purpose | When to Use |
|---|---:|---|---|
| `zipfile`, `tarfile`, `pathlib`, `hashlib`, `secrets` (stdlib) | Python 3.13 | Archive inspection, safe manual extraction, SHA-256, constant-time API-token comparison | Use instead of an archive-extraction dependency. v0.7 should accept **ZIP only** initially; if TAR support is added later, use `tarfile` with `filter="data"` and the same member/size/path policy. |
| `tempfile` + `os.replace` (stdlib) | Python 3.13 | Private staging directory and atomic promotion | Write the upload to a random staging path; validate before extracting; promote only after all checks and manifest creation succeed. |
| `asyncio` + existing `DurableCPGQueue` | Python 3.13 / existing | Asynchronous CPG builds, retry/status behavior | Reuse the current Postgres-backed queue (`FOR UPDATE SKIP LOCKED`, dedup, bounded depth). Add `index_source` as a job type or make it a durable post-build step. Do **not** add Celery, RQ, Dramatiq, or a second Redis queue. |
| Existing `CodeBrowsingService` + `QueryExecutor` | existing | Symbol and graph retrieval | Reuse their bounded Joern calls. Build a new `ContextService` that merges PostgreSQL lexical hits, symbol metadata, and explicitly whitelisted graph-neighbor queries into compact cited passages. |

## Recommended Integration Shape

```text
Client
  ├─ HTTPS + Bearer/API token ──> FastAPI /v1/projects, /versions, /jobs, /context
  │                                ├─ archive staging + manifest
  │                                ├─ Postgres catalog / lexical indexes
  │                                └─ existing DurableCPGQueue ──> Joern build/index work
  └─ MCP ───────────────────────> mounted FastMCP /mcp ──> same service layer

ContextService = lexical candidates (Postgres FTS + trigrams)
                 + symbol lookup (Postgres)
                 + bounded CPG neighbor enrichment (Joern)
                 -> ranked, size-capped citations {project, version, path, line_start, line_end, symbol}
```

Make the service layer—not REST handlers or MCP tools—the sole owner of lifecycle and context logic. Both facades must call the same authentication/authorization policy and return the same stable project-version identifiers.

## Exact Additions to `requirements.txt`

```bash
# REST facade and multipart uploads
pip install "fastapi>=0.115,<1" "python-multipart>=0.0.20,<1"
```

No queue, ORM, archive, search-engine, embedding, or vector-database dependency is justified for v0.7. Before implementation, resolve and lock the FastAPI/FastMCP compatible versions together in a reproducible constraints/lock file; the repository currently records ranges, not a lockfile.

## Security and Archive-Handling Requirements

1. Support ZIP only for the first contract. Reject encrypted archives, unsupported compression methods, duplicate normalized paths, absolute paths, `..` traversal, device/FIFO entries, symlinks/hardlinks, and ambiguous Unicode/control-character names.
2. Apply limits before and during extraction: request `Content-Length` if present, streamed compressed-byte cap, maximum member count, per-member uncompressed cap, total uncompressed cap, path-depth cap, and compression-ratio cap. Do not trust the filename or MIME type.
3. Extract member-by-member to a mode-`0700` staging directory using canonical destination checks; do not use `extractall()`. Hash the received archive and produce a deterministic source manifest before enqueueing work.
4. Make upload idempotency explicit: a client idempotency key and/or `(project_id, archive_sha256)` unique constraint must return the existing version/job rather than enqueueing another CPG build.
5. Authenticate all `/v1/*` endpoints with a FastAPI dependency. For v0.7, use one configured opaque bearer token checked with `secrets.compare_digest` (or an already-managed reverse-proxy identity); do not claim user/tenant authorization before a real identity model exists. Put the mounted MCP path behind the same perimeter/auth policy. Keep `/health` unauthenticated only if it exposes no sensitive details.

## Alternatives Considered

| Category | Recommended | Alternative | Why Not |
|---|---|---|---|
| REST facade | FastAPI outer app + mounted FastMCP | FastMCP custom routes only | FastMCP documents custom health routes as deliberately outside its authentication middleware; authenticated REST endpoints belong in FastAPI’s dependency model. |
| Background jobs | Existing Postgres durable queue | Celery/RQ/Dramatiq | The current queue already has DB durability, deduplication, restart recovery, backpressure, and multi-worker-safe claims. Another queue creates split status and retry truth. |
| Archive format | ZIP-only + stdlib validation | ZIP + TAR + 7z from day one | More parsers and link semantics multiply the attack surface. Add TAR only after archive policy tests cover it; do not accept 7z in v0.7. |
| Indexing | PostgreSQL FTS + `pg_trgm` + Joern | Elasticsearch/OpenSearch | A separate search cluster is unjustified before corpus scale or relevance needs demonstrate it; PostgreSQL keeps version/citation consistency transactional. |
| Semantic retrieval | Lexical + symbols + bounded CPG links | Embeddings/vector database | The milestone explicitly defers embedding-first retrieval. Graph relationships supply structural context while lexical ranking remains inspectable. |
| Data layer | psycopg repositories + SQL migrations | SQLAlchemy/Alembic introduction | The project is already psycopg-based. A thin migration runner and focused repositories minimize a broad persistence rewrite during a contract-establishing milestone. |

## Implementation Notes and Version Risks

- `main.py` currently calls `mcp.run_http_async()` and exposes only FastMCP custom routes. Replace that boot path with a combined ASGI app: create `mcp.http_app(path="/mcp")`, create a FastAPI app with the combined lifespan, and mount/include the MCP routes. Preserve the existing concurrency limiter around both publicly reachable facades, with a separate upload byte/concurrency limit if necessary.
- The current jobs schema has only `queued/running/done/failed`, JSON stored as `TEXT`, and requeues every `running` job at startup. It is sufficient for builds but needs an explicit retry policy (`max_attempts`, retryable error classification, next-at/lease metadata) before exposing retries as an API guarantee. That is a schema migration, not a queue-framework switch.
- Current codebase catalog keys only by hash. v0.7 needs a stable `project_id` and immutable `version_id`, so two projects can intentionally reference identical content without collapsing their catalog history. Keep content SHA/CPG cache keys as deduplication artifacts, not as the public version identity.
- Source indexing must run only on the validated promoted snapshot and must store file/line ranges that remain correct for that immutable version. Citation payloads should be first-class structured data, never inferred from Joern text after the fact.

## Sources

- [FastMCP + FastAPI integration](https://gofastmcp.com/integrations/fastapi) — **HIGH**: documents creating `mcp.http_app()` and combining/mounting it with a FastAPI application and lifespan.
- [FastMCP HTTP deployment / custom routes](https://gofastmcp.com/deployment/http) — **HIGH**: states custom routes such as health checks are intentionally excluded from FastMCP authentication middleware; use FastAPI for authenticated HTTP endpoints.
- [FastAPI request files](https://fastapi.tiangolo.com/tutorial/request-files/) — **HIGH**: `UploadFile` is the supported multipart-upload type; multipart support is required.
- [FastAPI `UploadFile.read`](https://fastapi.tiangolo.com/reference/uploadfile/) — **HIGH**: asynchronous `read(size)` API; bounded chunk reads support streaming to staging.
- [FastAPI security reference](https://fastapi.tiangolo.com/reference/security/) — **HIGH**: `HTTPBearer` dependency support for bearer-token extraction.
- [Python `zipfile` documentation](https://docs.python.org/3/library/zipfile.html) and [Python `tarfile` extraction filters](https://docs.python.org/3/library/tarfile.html#extraction-filters) — **HIGH**: standard-library archive APIs and Python’s archive-extraction safety guidance.
- [PostgreSQL full-text search](https://www.postgresql.org/docs/16/textsearch.html) and [`pg_trgm`](https://www.postgresql.org/docs/16/pgtrgm.html) — **HIGH**: PostgreSQL-native lexical search and trigram indexing.
- Local evidence: `requirements.txt`, `main.py`, `src/utils/postgres_job_store.py`, `src/utils/postgres_db_manager.py`, `src/tools/core_tools.py`, `src/services/code_browsing_service.py`, and `docs/architecture.md` — **HIGH** for current-codebase observations.
