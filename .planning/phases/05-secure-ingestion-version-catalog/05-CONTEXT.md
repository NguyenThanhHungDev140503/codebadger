# Phase 5: Secure Ingestion & Version Catalog - Context

**Gathered:** 2026-08-09
**Status:** Ready for planning

<domain>
## Phase Boundary

Create a Git-remote-backed project/version catalog that synchronizes a selected
GitHub, GitLab, or Azure DevOps branch on explicit API request and turns each
new commit into an immutable source snapshot. The phase establishes the safe
source-of-truth and version identity consumed by later CPG lifecycle work; it
does not build public context retrieval.

</domain>

<decisions>
## Implementation Decisions

### Remote source and branch behavior
- **D-01:** Git remote synchronization is the primary v0.7 ingestion path; do not
  make ZIP upload the required workflow for continuously changing codebases.
- **D-02:** Support the same allowlisted providers as the existing MCP: GitHub,
  GitLab, and Azure DevOps, using Git CLI for clone/fetch/branch resolution.
- **D-03:** A client configures a remote and selected branch for a project; an
  explicit `POST /versions:update` synchronizes that branch. Webhooks are out of scope.
- **D-04:** Branch switching is supported by changing the selected branch and
  synchronizing it. Worker checkout must use an isolated repository/worktree and
  a detached resolved commit, never a user's mutable working copy.

### Version identity and deduplication
- **D-05:** A version is immutable and identified by the resolved commit SHA plus
  source/build configuration; the branch is only the moving reference used to find it.
- **D-06:** If an update resolves to the currently known equivalent version, return
  that existing version with `unchanged`; do not create a duplicate version or CPG job.

### Private repository credentials
- **D-07:** Store credentials encrypted per project so later explicit updates can
  synchronize private repositories without resupplying a token on every request.
- **D-08:** Credentials must not appear in URLs, Git config, logs, API responses,
  manifests, or error messages. Decryption/use is confined to the Git execution seam.

### the agent's Discretion
- Exact REST resource shapes, token envelope/key-management adapter, retention policy,
  branch metadata fields, and whether a secondary archive adapter is introduced later.
- Reasonable Git CLI command construction and safe temporary workspace lifecycle,
  subject to the existing URL/ref validation and no-shell execution rules.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Milestone scope
- `.planning/ROADMAP.md` — Phase 5 goal, requirements, dependencies, and success criteria.
- `.planning/REQUIREMENTS.md` — v0.7 requirement IDs; remote-sync wording must be reconciled before implementation.
- `.planning/PROJECT.md` — milestone-level product boundary and deferred scope.

### Existing remote source safeguards
- `src/utils/validators.py` — allowlisted GitHub/GitLab/Azure DevOps remote validation and conservative branch/ref validation.
- `src/services/git_manager.py` — existing clone, credential stripping, error masking, and cleanup patterns to evolve toward Git CLI sync.
- `src/tools/core_tools.py` — current remote generation flow, commit-aware CPG cache key, and durable CPG handoff seam.
- `docs/security.md` — trust boundary, token handling, local path restrictions, and raw CPGQL residual risk.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `validate_repo_url` / `validate_git_branch`: exact HTTPS provider allowlist and ref injection defenses.
- `GitManager`: token masking, post-clone credential stripping, isolated clone directory cleanup.
- `get_cpg_cache_key`: supports commit hash and branch as cache inputs.

### Established Patterns
- CPG generation is asynchronous and deduplicated through Postgres-backed jobs.
- Source code is staged beneath the configured workspace/playground rather than queried from arbitrary host paths.
- Inputs are validated at MCP boundaries and errors are sanitized before return.

### Integration Points
- Add project/version source metadata ahead of `generate_cpg`'s clone/stage handoff.
- Replace one-shot clone behavior with isolated Git CLI fetch + detached revision snapshot for API-triggered sync.

</code_context>

<specifics>
## Specific Ideas

- Code changes frequently, so clients should update from a selected branch instead of repeatedly uploading archives.
- An unchanged remote branch should return the existing immutable version.

</specifics>

<deferred>
## Deferred Ideas

- Git provider webhooks — deferred; v0.7 uses explicit update calls.
- Archive upload as an alternative source adapter — not required for this phase.
- Full multi-tenant worker isolation and key-management infrastructure — later hardening scope.

</deferred>

---

*Phase: 05-secure-ingestion-version-catalog*
*Context gathered: 2026-08-09*
