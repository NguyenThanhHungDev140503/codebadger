# Phase 5: Secure Ingestion & Version Catalog - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-08-09
**Phase:** 5-Secure Ingestion & Version Catalog
**Areas discussed:** Source delivery, provider support, version deduplication, credentials, update trigger

---

## Source delivery

| Option | Description | Selected |
|--------|-------------|----------|
| ZIP upload | Client archives and uploads code for every version. | |
| Git remote synchronization | Server fetches the selected remote branch on demand and snapshots its commit. | ✓ |

**User's choice:** Git remote synchronization.
**Notes:** Code changes continuously, so repeatedly uploading source archives is unsuitable.

---

## Provider support and version behavior

| Option | Description | Selected |
|--------|-------------|----------|
| GitHub/Azure only | Match the initially mentioned providers. | |
| All existing MCP providers | GitHub, GitLab, and Azure DevOps. | ✓ |
| Always create a version | Keep an event history even when no commit changed. | |
| Return existing version | Report `unchanged` when the resolved commit/config already exists. | ✓ |

**User's choice:** Support all existing MCP providers and return the existing version when unchanged.
**Notes:** Branch selection and switching are required; immutable versions are commit-based.

---

## Credentials and trigger

| Option | Description | Selected |
|--------|-------------|----------|
| Request-supplied token | Client sends a token on each update. | |
| Encrypted project credential | Server stores a project credential for subsequent updates. | ✓ |
| Webhook-driven sync | Provider pushes trigger updates automatically. | |
| Explicit update endpoint | Client calls `POST /versions:update` to synchronize. | ✓ |

**User's choice:** Encrypted per-project credentials and an explicit update endpoint.
**Notes:** Credentials remain secret from URLs, Git config, logs, and API responses.

---

## the agent's Discretion

- Concrete endpoint field names, encrypted credential adapter, Git CLI command details, retention, and a possible future archive adapter.

## Deferred Ideas

- Provider webhooks, archive upload adapter, and full multi-tenant worker/key isolation.
