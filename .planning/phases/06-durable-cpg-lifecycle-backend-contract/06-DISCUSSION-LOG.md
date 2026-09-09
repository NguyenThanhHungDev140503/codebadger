# Phase 6: Durable CPG Lifecycle & Backend Contract - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-08-10
**Phase:** 6-Durable CPG Lifecycle & Backend Contract
**Areas discussed:** Build trigger & job binding, Lifecycle state model, Cancel, retry & recovery, REST surface & archive upload

---

## Build trigger & job binding

| Option | Description | Selected |
|--------|-------------|----------|
| Auto-enqueue on sync | Syncs, then instantly enqueues a build job for the new version | ✓ |
| Separate explicit build step | Sync only creates version; client calls separate build/status endpoint | |
| Enqueue only when 'created' | Sync enqueues build but unchanged returns ready, no new job | |
| build_status on version row | Single source of truth per version | ✓ |
| Separate build table | Cleaner history, more joins | |
| Dedup by version_id | DB partial unique index on (job_type, version_id) | ✓ |
| Reuse codebase_hash dedup | Keeps legacy dedup only | |
| Register mapping | version_id↔codebase_hash+cpg_path mapping on build start | ✓ |
| Carry both IDs in payload | Job payload carries both from start | |

**User's choice:** Auto-enqueue on sync, build_status on version row, dedup by version_id, register mapping on build start.

**Notes:** Unchanged branch returns existing version, never a duplicate job.

---

## Lifecycle state model

| Option | Description | Selected |
|--------|-------------|----------|
| version.build_status authoritative | Only public status field; queue writes back | ✓ |
| Derive from jobs table | Status via DB join at read time | |
| 6-state model | queued→building→loading→ready + failed + cancelled | ✓ |
| 5-state model | Merge loading into building | |
| Extend metadata | queue_position/elapsed_ms/retry_count/error in JSON metadata | ✓ |
| Status enum only | Defer detail to Phase 8 | |
| Sanitized error_code + message | Reuse Phase 5 masking; store sanitized only | ✓ |
| Store raw, strip on read | Keep raw error string, strip at boundary | |

**User's choice:** version.build_status authoritative, 6-state model, extended metadata, sanitized error_code + message.

---

## Cancel, retry & recovery

| Option | Description | Selected |
|--------|-------------|----------|
| Explicit cancel request only | Client-initiated; DB guard vs final states | ✓ |
| No explicit cancel | Only startup reconciliation | |
| Delete partial, keep version | Remove partial artifacts; keep row w/ status=cancelled | ✓ |
| Keep artifacts | Operator inspection retained | |
| Re-enqueue reset to queued | Retry re-enqueues one job, resets to queued | ✓ |
| Delete+recreate version | Destructive, loses provenance | |
| Requeue + retry cap | requeue_running_jobs + capped retry_count | ✓ |
| Unlimited requeue | Requeue forever | |

**User's choice:** Explicit cancel, delete partial keep version, idempotent re-enqueue retry, requeue + retry cap.

---

## REST surface & archive upload

| Option | Description | Selected |
|--------|-------------|----------|
| Same FastMCP process + routes | Starlette routes in same app/process/port | ✓ |
| Separate REST app | Own port, own lifecycle | |
| Include archive upload | Secondary source adapter via tarball | ✓ |
| Exclude archive upload | Only Git-synced versions | |
| RESTful + action verbs | projects + versions/update, retry, cancel | ✓ |
| Status as sub-resource | GET/PUT /versions/{id}/status | |
| Same service + same schema | MCP tools call same service, same envelope | ✓ |
| MCP minimal subset | MCP returns compact id+status | |

**User's choice:** Same FastMCP process, include archive upload, RESTful + action verbs, same service + same schema.

---

## Claude's Discretion

- REST response envelope field enumeration, Starlette route mounting, queue_position/elapsed computation, metadata JSON schema, archive upload validation (size limits, traversal removal), MCP tool arg names, archive synthetic version identity.

## Deferred Ideas

- Auth/quotas/correlation/audit/metrics → Phase 8 (API-03/04).
- Webhooks — deferred in Phase 5.
- Multi-tenant worker isolation + key management — later hardening.