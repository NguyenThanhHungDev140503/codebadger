# Research Summary: Codebase Context Backend

**Milestone:** v0.7
**Researched:** 2026-08-09

## Recommendation

Add a FastAPI REST facade around the existing FastMCP ASGI app and keep both
adapters on shared application services. Use the current Postgres durable queue,
Redis coordination, Joern worker pool, and content-addressed CPG cache. Add a
project/version catalog and a secure ZIP-only staging pipeline. After a CPG is
ready, index symbols and source spans in Postgres and implement a bounded hybrid
ContextService: exact symbol lookup, PostgreSQL full-text/trigram search, then
capped Joern graph expansion. Defer embeddings, vector infrastructure, and
multi-host scheduling.

## Build order

1. Project/version/artifact schema and service interfaces.
2. Authenticated upload, archive validation, immutable promotion and manifest.
3. Version-to-existing durable CPG queue integration, status, retry and cleanup.
4. REST lifecycle endpoints and thin MCP parity tools.
5. Symbol/source indexing and cited hybrid context retrieval.
6. Quotas, audit, observability, isolation and contract/load/security tests.

## Stack decisions

- FastAPI + `python-multipart` for bounded uploads; mount FastMCP under one ASGI app.
- Standard-library ZIP validation/extraction for v0.7; reject traversal, symlink,
  special files, duplicate canonical paths and zip bombs.
- PostgreSQL FTS + `pg_trgm` plus Joern queries; no Celery/RQ or vector DB yet.
- Bearer authentication and project/version authorization at the application seam.

## Non-negotiable safeguards

Archive/resource quotas, immutable content digests, durable state reconciliation,
tenant-scoped lookups, no public raw CPGQL, bounded graph expansion, and citations
(`project_id`, `version_id`, digest, relative path, line range, selection reason).

## Research gaps to resolve during planning

- Exact FastMCP mounting/route composition for the pinned dependency version.
- Migration strategy compatible with the current hand-created Postgres schema.
- Source span and symbol index extraction details across supported Joern languages.
- Authentication deployment contract (reverse proxy versus in-process bearer tokens).
