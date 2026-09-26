# Graph Report - codebadger  (2026-09-17)

## Corpus Check
- 239 files · ~213,454 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 3529 nodes · 6471 edges · 212 communities (189 shown, 23 thin omitted)
- Extraction: 92% EXTRACTED · 8% INFERRED · 0% AMBIGUOUS · INFERRED: 524 edges (avg confidence: 0.9)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `6ad481c9`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- validators.py
- TestCodeBadgerIntegration
- taint_analysis_tools.py
- models.py
- CodebaseInfo
- .load
- PostgresJobStore
- CodeBrowsingService
- FastMCP
- register_tools
- QueryResult
- ValidationError
- RedisCoordinator
- QueryExecutor
- test_security_parity.py
- test_toctou.py
- ProjectVersionService
- logging.py
- device.c
- AuthService
- GitManager
- register_core_tools
- core_tools.py
- AI-SPEC — Phase 5: Secure Ingestion & Version Catalog
- PostgresDBManager
- CPGGenerator
- prepare_container_compile_db
- Deployment
- SessionStatus
- setup_logging
- exceptions.py
- scope_exclude_regex
- test_memory_admission.py
- memory.c
- get_cpg_cache_key
- github-actions-vps-flow-explained.md
- main.py
- recommend.py
- CPGQLValidator
- test_uninitialized_read.py
- Codebadger Conventions
- Codebadger Conventions
- test_auth_api.py
- test_heap_overflow.py
- test_credential_store.py
- datetime
- rest_routes.py
- JoernServerManager
- RedisPoolStore
- test_null_pointer_deref.py
- test_stack_overflow.py
- TestExtraGitHosts
- callbacks.c
- config.c
- 3. Requirements Analysis
- query_executor.py
- Durable CPG Lifecycle & Backend Contract — Giải thích Kỹ Thuật
- JoernServerClient
- TestMCPTools
- utils.c
- network.c
- Moderate Pitfalls
- ArchiveUploadService
- .spawn_server
- Implementation Decisions
- test_quotas_and_sanitization.py
- PortManager
- validate_git_branch
- test_joern_client_load.py
- test_worker_pool.py
- TestLoadConfig
- test_common_helpers.py
- _hash_tree_in_process
- CodeBadger MCP Server — Luồng hoạt động chi tiết
- 2. Provider-by-provider
- Phase 5: Secure Ingestion & Version Catalog - Context
- 2. Pattern Mapping & Concrete Code Excerpts
- Project
- B. Local development (MCP on the host)
- defaults.py
- timedelta
- DurableCPGQueue
- CacheCleanupScheduler
- validate_cpgql_query
- README.md
- app_lifespan
- Architecture Patterns
- CPGGenerationQueue
- _FakeStore
- test_version_lifecycle_recovery.py
- 2. Luồng Kỹ Thuật Chi Tiết (Detailed Technical Flows)
- cmdline.c
- ._make_room
- _autodetect_c_includes
- _generate_cpg_async
- _FakePoolStore
- health_check
- 3. Chi tiết từng bước
- Technology Stack
- program_slice.scala
- test_postgres_db_manager.py
- test_taint_tools_usability.py
- TestSnippetLanguageInferAndValidate
- analyzeChecks
- CodeBadger
- _ssh_clone_env
- test_startup_tuning.py
- test_pool_memory_guard.py
- test_build_opts.py
- hasOverflowGuard
- use_after_free.scala
- main.c
- Available Tools
- security.md
- slice_scenarios.c
- Milestone Summary: v0.7 Codebase Context Backend
- TestParseSnippetBlocks
- areInMutuallyExclusiveBranches
- Tests
- Requirements: CodeBadger v0.7 Codebase Context Backend
- Requirements: CodeBadger v0.7 Codebase Context Backend
- null_pointer_deref.scala
- trace
- snippet_filename
- TestDictToConfig
- test_pool_store_redis.py
- TestValidateGithubUrl
- AtomBase
- .get_or_create_client
- areInMutuallyExclusiveBranches
- areInMutuallyExclusiveBranches
- test_main.py
- asyncio
- TestValidateTimeout
- Architecture
- 🦡 codebadger Security
- Milestone Audit Report: v0.7 Codebase Context Backend
- Phase 6: Durable CPG Lifecycle & Backend Contract - Discussion Log
- 2. Architecture & Decisions
- Completed Tasks & Components
- Feature Landscape
- bfsEdges
- validate_search_pattern
- CodeBadger agent guide
- /feature-development
- Custom Tools
- Phase 5: Secure Ingestion & Version Catalog - Discussion Log
- Recommended Architecture
- Research Summary: Codebase Context Backend
- deploy.sh
- escape_scala_string
- validate_snippet_label
- test_compose_security.py
- TestExposureCheck
- 2026
- ECC for Codex CLI
- sameTarget
- isSyntheticLocal
- ConcurrencyLimitMiddleware
- Phase 05 Plan 01: Secure Ingestion Version Catalog Summary
- Phase 05 Plan 02: Git Sync & Snapshot Promotion Summary
- Phase 8: Authorization, Quotas & Production Verification - Discussion Log
- Phase 8 Validation Report: Authorization, Quotas & Production Verification
- Roadmap: CodeBadger v0.7 Codebase Context Backend
- 🦡 codebadger
- .from_dict
- taint_flows.scala
- TestEffectiveConfigSelfCheck
- slice_inline.h
- Usage
- Roadmap: CodeBadger
- Project State — CodeBadger
- getMethodName
- Contributing
- sample_client.py
- 05-01-PLAN.md
- 05-02-PLAN.md
- Phase 8 Context: Authorization, Quotas & Production Verification
- ._start_shared_exec
- TestShutdown
- .test_root_endpoint
- TestMiddleware
- cleanup.sh
- Configuration
- 🦡 codebadger Documentation
- Roadmap
- _validate_repo_url_path
- Phase 8: Authorization, Quotas & Production Verification — Execution Plan
- smoke-test.sh
- pathBoundaryRegex
- pathBoundaryRegex
- 06-01-PLAN.md
- 06-02-PLAN.md
- 06-03-PLAN.md
- 07-PLAN.md
- EXEC_PROMPT.md
- build.sh
- build-joern.sh
- build-mcp.sh
- deploy-prod.sh
- push.sh
- rollback.sh
- sync-env.sh
- codebadger

## God Nodes (most connected - your core abstractions)
1. `register_tools()` - 155 edges
2. `QueryResult` - 99 edges
3. `ValidationError` - 77 edges
4. `JoernServerManager` - 62 edges
5. `PostgresDBManager` - 61 edges
6. `CodebaseInfo` - 58 edges
7. `ProjectVersionService` - 53 edges
8. `TestMCPTools` - 45 edges
9. `Config` - 42 edges
10. `register_core_tools()` - 42 edges

## Surprising Connections (you probably didn't know these)
- `test_error_sanitizer_middleware()` --uses--> `ErrorSanitizerMiddleware`  [INFERRED]
  tests/unit/api/test_quotas_and_sanitization.py → src/api/error_sanitizer.py
- `test_reap_evicted_removes_containers_and_marks_sleeping()` --uses--> `SessionStatus`  [INFERRED]
  tests/test_memory_admission.py → src/models.py
- `test_get_or_create_client_reuses_client_on_matching_port()` --uses--> `JoernServerClient`  [INFERRED]
  tests/test_worker_pool.py → src/services/joern_client.py
- `test_reload_with_retry_does_not_retry_empty_build()` --uses--> `JoernServerClient`  [INFERRED]
  tests/test_worker_pool.py → src/services/joern_client.py
- `app_lifespan()` --uses--> `ArchiveUploadService`  [INFERRED]
  main.py → src/services/archive_upload_service.py

## Import Cycles
- None detected.

## Communities (212 total, 23 thin omitted)

### Community 0 - "validators.py"
Cohesion: 0.04
Nodes (54): CodeBadger Server - A Model Context Protocol server for static code analysis…, _extra_repo_host_entries(), hash_query(), infer_snippet_language(), is_extra_repo_host(), _language_signal_score(), _normalize_extra_host(), parse_snippet_blocks() (+46 more)

### Community 1 - "TestCodeBadgerIntegration"
Cohesion: 0.07
Nodes (39): asyncio, fixture, Integration Tests for CodeBadger Server These tests verify the complete…, Test stack overflow detection filtered to main.c. main.c contains: char…, Test integer-overflow detection on guest-controlled allocation sizes. memory.c…, Test TOCTOU (CWE-367) detection in the config module. config.c…, Test null-pointer-dereference detection runs across the codebase. Many…, Test uninitialized-read detection runs across the codebase. (+31 more)

### Community 2 - "taint_analysis_tools.py"
Cohesion: 0.05
Nodes (50): anyio, _deduplicate_type_definitions(), Code Browsing MCP Tools for CodeBadger Server Tools for exploring and…, Normalize Joern duplicate suffixes and prefer canonical definitions., Register code browsing MCP tools with the FastMCP server, register_code_browsing_tools(), Shared helpers for MCP tool implementations. The taint, browsing, and custom…, Validate the hash and return its CodebaseInfo, or raise ValidationError. Raises… (+42 more)

### Community 3 - "models.py"
Cohesion: 0.06
Nodes (40): _dict_to_config(), load_config(), Configuration management for the CodeBadger Server., Convert dictionary to Config object with proper type conversions Uses…, Load configuration from file or environment variables Priority order: 1.…, JoernConfig, ProjectCredential, QueryConfig (+32 more)

### Community 4 - "CodebaseInfo"
Cohesion: 0.05
Nodes (57): CodebaseInfo, Config, CPGConfig, CPG generation configuration, iof_services(), asyncio, fixture, Tests for the Integer Overflow/Underflow detection tool. (+49 more)

### Community 5 - ".load"
Cohesion: 0.06
Nodes (40): Clear the query cache., Render a bare-integer placeholder, raising on a non-integer value., Render a bare-boolean placeholder as lowercase Scala true/false., Load a query template and substitute variables. Args: query_name: Name of the…, clamp_int(), Coerce value to int and clamp to [minimum, maximum]. Used to bound caller-…, _dexec(), fixture (+32 more)

### Community 6 - "PostgresJobStore"
Cohesion: 0.06
Nodes (27): _now(), PostgresJobStore, Any, Close the connection pool, if any. Safe to call repeatedly., Enqueue a job. Returns (job_id|None,…, Atomically claim the oldest queued job via FOR UPDATE SKIP LOCKED., True if a queued or running job exists for this codebase. Used by…, 1-based position of this codebase's QUEUED job among all queued jobs. Returns… (+19 more)

### Community 7 - "CodeBrowsingService"
Cohesion: 0.07
Nodes (37): CodeBrowsingService, _decode_count(), _decode_parameter(), _decode_parameter_method(), Any, Service for code browsing operations with caching support, Helper to check cache, execute query if needed, and cache result, Decode a scalar count returned by Joern across output formats. (+29 more)

### Community 8 - "FastMCP"
Cohesion: 0.09
Nodes (45): FastMCP, _make_services(), asyncio, Tests for the get_program_slice function with simplified input/output.…, Test backward slicing mode., Test forward slicing mode., Test that data dependencies are correctly returned., Test that control dependencies are correctly returned. (+37 more)

### Community 9 - "register_tools"
Cohesion: 0.09
Nodes (43): Register all MCP tools with the FastMCP server, register_tools(), asyncio, Test find_taint_sources with filename parameter, Test find_taint_sinks with filename parameter, Test that node_id based queries work, Test that missing source returns validation error, test_find_taint_flows_success() (+35 more)

### Community 10 - "QueryResult"
Cohesion: 0.06
Nodes (35): QueryResult, Query execution result, double_free_services(), asyncio, fixture, Tests for the Double-Free detection tool., Test double-free detection with filename filter., Test double-free detection respects limit parameter. (+27 more)

### Community 11 - "ValidationError"
Cohesion: 0.07
Nodes (30): Input validation failed, ValidationError, _copy_local_source_tree_via_daemon(), Copy a host source tree into the playground via a short-lived helper container.…, _allowed_source_roots(), _is_within(), Canonical allowlisted local-source roots from ALLOWED_SOURCE_ROOTS. Read at…, Validate and resolve a host path. All string-level security checks (control… (+22 more)

### Community 12 - "RedisCoordinator"
Cohesion: 0.07
Nodes (27): skipif, make_coordinator(), Exception, QueryLockTimeout, Cross-process coordination primitives. The query path serializes work per CPG…, Build the Redis-backed coordinator. Raises if ``redis_url`` is empty or Redis…, Raised when a per-CPG query lock can't be acquired in time., Redis-backed coordinator: per-CPG query lock holds across processes/hosts.… (+19 more)

### Community 13 - "QueryExecutor"
Cohesion: 0.07
Nodes (34): Any, QueryExecutor, Normalize a query to a JSON-producing form. The result is serialized with…, Execute query using Joern server client, Parse Joern query output, Service for executing CPGQL queries against CPGs using Joern HTTP server, True if the codebase is mid-load/build, so its JVM is legitimately busy (not…, Execute a CPGQL query using the Joern server for the specific codebase (+26 more)

### Community 14 - "test_security_parity.py"
Cohesion: 0.08
Nodes (27): CorrelationMiddleware, get_current_correlation_id(), BaseHTTPMiddleware, Request, Response, Correlation ID Middleware (Phase 8 - API-04) Extracts or generates…, Return the correlation ID for the current async task context., ASGI Middleware to trace requests across services via X-Correlation-ID. (+19 more)

### Community 15 - "test_toctou.py"
Cohesion: 0.08
Nodes (38): _copy_local_source_tree(), Copy a local source tree into the playground snapshot dir, atomically. Symlink-…, Delete the source snapshot once the CPG exists (ephemeral source). The CPG…, _reclaim_source_snapshot(), Unit tests for the off-loop local-source copy helper (path-race fix A)., TestCopyLocalSourceTree, _cfg(), asyncio (+30 more)

### Community 16 - "ProjectVersionService"
Cohesion: 0.10
Nodes (19): ProjectVersion, Immutable project version bound to a specific commit SHA and build config., ContextRetrievalService, Any, compute_version_id(), ProjectVersionService, Any, Create a new version or return an existing immutable version. Returns… (+11 more)

### Community 17 - "logging.py"
Cohesion: 0.08
Nodes (23): Lock, _get_project_lock(), GitSyncService, _mask_text(), Git synchronization service using safe subprocess execution with ephemeral…, Fetch remote branch, resolve SHA, update snapshot, return (version_dict,…, Mask credentials in logs, URLs, and subprocess output., Service to safely sync Git repositories using safe CLI subprocess calls. (+15 more)

### Community 18 - "device.c"
Cohesion: 0.13
Nodes (34): DeviceCallbacks, DeviceManager, DeviceState, ConfigContext, Device, NetworkContext, device_add(), device_configure() (+26 more)

### Community 19 - "AuthService"
Cohesion: 0.09
Nodes (23): main(), AuthService, hash_password(), Any, Authentication & Tenancy Service (Phase 8 - API-03) Handles PBKDF2 password…, Fetch user record by username., Verify username and password. Returns user dict on success, None on failure., Create a permanent (non-expiring) signed JWT token specifically for MCP clients. (+15 more)

### Community 20 - "GitManager"
Cohesion: 0.08
Nodes (20): GitOperationError, GitManager, _mask_token_in_text(), Git repository manager for cloning and managing remote git repositories.…, Clone a repo (https for github/gitlab; ssh for custom hosts), Blocking clone operation, Rewrite the 'origin' remote URL to the credential-free form. git clone stores…, Validate that repository exists and is accessible (+12 more)

### Community 21 - "register_core_tools"
Cohesion: 0.09
Nodes (18): Register core MCP tools with the FastMCP server, register_core_tools(), asyncio, Test getting CPG status when CPG doesn't exist (valid-format but unknown hash), A malformed codebase_hash is rejected by validation, not treated as not_found., remove_cpg validates the hash before any DB/filesystem action., Test successful CPG generation from GitHub, generate_cpg large-project guard: configurable + toggleable (fix #2). (+10 more)

### Community 22 - "core_tools.py"
Cohesion: 0.08
Nodes (33): _build_job_alive(), _calculate_repo_size_mb(), _codebase_label(), _count_lines_of_code(), _cpgs_disk_usage(), _estimate_processing_time(), gc_cold_cpgs(), get_cpg_cache_path() (+25 more)

### Community 23 - "AI-SPEC — Phase 5: Secure Ingestion & Version Catalog"
Cohesion: 0.06
Nodes (31): 1. System Classification, 1b. Domain Context, 2. Framework Decision, 3. Framework Quick Reference, 4. Implementation Guidance, 4b. AI Systems Best Practices, 5. Evaluation Strategy, 6. Guardrails (+23 more)

### Community 24 - "PostgresDBManager"
Cohesion: 0.11
Nodes (10): _now(), PostgresDBManager, Any, Update version status and merge metadata updates atomically., Atomically merge metadata and set scalar columns for one codebase. SELECT ...…, All codebase rows in ONE query (read-only). Avoids one Postgres connection per…, Catalog/cache/findings + durable job queue, backed by Postgres., Close the inherited connection pool (no-op when pooling is disabled). (+2 more)

### Community 25 - "CPGGenerator"
Cohesion: 0.08
Nodes (17): CPGGenerator, Calculate total repository size in MB Args: source_path: Path to the repository…, Escape special regex characters while preserving regex patterns Args: pattern:…, Generates CPG from source code using Docker containers, Convert host path to container path The container mounts ./playground as…, Apply and persist Joern's default overlays (incl. OSS dataflow) into the…, Remove a leftover overlay workspace so a failed run can't strand disk., Execute command synchronously INSIDE Docker container with timeout (+9 more)

### Community 26 - "prepare_container_compile_db"
Cohesion: 0.11
Nodes (14): find_compile_db(), prepare_container_compile_db(), compile_commands.json handling for c2cpg `--compilation-database`. A…, Load a compile_commands.json, rebase its paths, write a container-usable copy…, Locate a compile_commands.json in a copied source tree. Checks conventional…, Map an absolute path under host_root onto container_root. Relative paths are…, Rebase `directory`/`file` fields of compile-db entries. Returns…, rebase_entries() (+6 more)

### Community 27 - "Deployment"
Cohesion: 0.06
Nodes (31): Analyzing code from a chat interface, Backing services & Docker, Build, push, and deploy, Configuration: .env.defaults vs .env, Configuration reference, Continuous deployment with GitHub Actions, CPG generation & queue, CPG size tiers (+23 more)

### Community 28 - "SessionStatus"
Cohesion: 0.10
Nodes (28): Enum, Source type enumeration, Status enumeration for CPG operations., SessionStatus, SourceType, _get_active_restart_task(), _get_restart_task_registry(), Schedule a background Joern-server restart. Returns True if a restart task was… (+20 more)

### Community 29 - "setup_logging"
Cohesion: 0.10
Nodes (19): Logger, get_logger(), get_run_log_path(), Point <log_dir>/codebadger-latest.log at the current run file (best-effort)., Configure root logging with a stdout stream and an optional per-run file. Each…, Absolute path of the current run's log file, or None if file logging is off., setup_logging(), _update_latest_symlink() (+11 more)

### Community 30 - "exceptions.py"
Cohesion: 0.10
Nodes (20): CPGGenerationError, JoernMCPException, Exception, QueryExecutionError, Custom exceptions for CodeBadger Server, CPG generation failed, Query execution failed, Resource limit exceeded (+12 more)

### Community 31 - "scope_exclude_regex"
Cohesion: 0.12
Nodes (13): combine_exclude_regexes(), glob_to_path_regex(), include_globs scoping → c2cpg/frontend `--exclude-regex` construction. Scoping…, Translate a path glob to a full-match regex against a relative path. Supported:…, Build an `--exclude-regex` that drops out-of-scope SOURCE files. A path is…, OR several full-match exclude-regex alternatives into one. A file is excluded…, scope_exclude_regex(), _excluded() (+5 more)

### Community 32 - "test_memory_admission.py"
Cohesion: 0.08
Nodes (18): _FakeRedisPool, manager(), fixture, parametrize, Unit tests for the memory-aware Joern admission ledger and tiered heaps. These…, In-memory stand-in for RedisPoolStore exercising the make-room ledger., The Redis make-room loop must release the ledger (so the loop sees reclaimed…, Shared mode returns the believed-live registry without a TCP probe per server… (+10 more)

### Community 33 - "memory.c"
Cohesion: 0.14
Nodes (29): MemoryRegion, MemoryController, device_dma_read(), device_dma_write(), init_subsystems(), interactive_mode(), buffer_release(), MemoryController (+21 more)

### Community 34 - "get_cpg_cache_key"
Cohesion: 0.10
Nodes (29): get_cpg_cache_key(), Generate a deterministic CPG cache key based on source type, path, language,…, Tests for get_cpg_cache_key — branch, build-options (extra), and content…, Trailing .git must not change the Azure cache key., The same repo name on different hosts must not collide., Two branches of the same repo must produce distinct CPG hashes., No branch given -> stable key (back-compat with existing default-branch CPGs)., Branch keying also applies to gitlab URLs (same source_type='github'). (+21 more)

### Community 35 - "github-actions-vps-flow-explained.md"
Cohesion: 0.07
Nodes (28): 10. Failure points và trace, 11. Source map, 1. Vấn đề và kiến trúc, 2. Trigger và concurrency, 3. Job build-and-push, 4. Job deploy, 5. Runtime Compose, 6. Health và smoke test (+20 more)

### Community 36 - "main.py"
Cohesion: 0.13
Nodes (27): _build_health(), _get_active_servers(), _get_cache_size(), _get_codebase_list(), _get_cpg_cache_mb(), _get_port_utilization(), _periodic_status_log(), Return the active Joern server map and count. (+19 more)

### Community 37 - "recommend.py"
Cohesion: 0.14
Nodes (26): main(), apply_startup_tuning(), Startup memory tuning for the CodeBadger MCP server. Extracted from main.py:…, Log the memory-aware recommendation and auto-derive unset memory limits.…, _clamp(), compute(), current_from_config(), detect_host() (+18 more)

### Community 38 - "CPGQLValidator"
Cohesion: 0.09
Nodes (17): CPGQLValidator, Any, QueryTransformer, CPGQL Query Validator and Helper Utilities Provides syntax validation, error…, Check for common regex syntax errors, Check for common filter/where syntax errors, Validator for CPGQL queries with syntax checking and suggestions, Check for invalid method chains (+9 more)

### Community 39 - "test_uninitialized_read.py"
Cohesion: 0.10
Nodes (28): asyncio, fixture, Tests for the uninitialized read detection tool (CWE-457)., Test that CWE-457 is present in findings., Test that detected issues are marked HIGH confidence., Test that each finding includes a code context snippet., Test that declaration line and read line are both reported., Test that the filename filter is embedded in the generated query. (+20 more)

### Community 40 - "Codebadger Conventions"
Cohesion: 0.07
Nodes (27): Architecture, Best Practices, Code Style, Codebadger Conventions, Commit Conventions, Commit Style: Mixed Style, Common Workflows, Configuration Files (+19 more)

### Community 41 - "Codebadger Conventions"
Cohesion: 0.07
Nodes (27): Architecture, Best Practices, Code Style, Codebadger Conventions, Commit Conventions, Commit Style: Mixed Style, Common Workflows, Configuration Files (+19 more)

### Community 42 - "test_auth_api.py"
Cohesion: 0.08
Nodes (14): JSONResponse, Root endpoint providing basic server information, root(), AuthMiddleware, BaseHTTPMiddleware, Request, Response, Authentication Middleware (Phase 8 - API-03) ASGI Starlette middleware… (+6 more)

### Community 43 - "test_heap_overflow.py"
Cohesion: 0.10
Nodes (27): ho_services(), asyncio, fixture, Tests for the Heap Overflow vulnerability detection tool (CWE-122)., Test that unbounded writes (strcpy, gets, sprintf) are detected as HIGH…, Test that size-mismatched bounded writes are detected., Test that allocation site and buffer name are shown., Test heap overflow detection with filename filter. (+19 more)

### Community 44 - "test_credential_store.py"
Cohesion: 0.10
Nodes (16): ABC, CredentialEncryptionAdapter, FernetCredentialEncryptionAdapter, InMemoryCredentialEncryptionAdapter, Credential Encryption Adapter Interface and Implementations., Abstract interface for encrypting and decrypting sensitive credentials., Encrypt plaintext secret and return ciphertext token/envelope., Decrypt ciphertext token/envelope and return plaintext secret. (+8 more)

### Community 45 - "datetime"
Cohesion: 0.10
Nodes (26): datetime, fs_services(), asyncio, fixture, Tests for the Format String vulnerability detection tool (CWE-134)., Test that HIGH confidence findings are reported for taint source assignments., Test that MEDIUM confidence findings are reported for non-literal format args., Test format string detection with filename filter. (+18 more)

### Community 46 - "rest_routes.py"
Cohesion: 0.11
Nodes (22): HTMLResponse, build_openapi_schema(), check_tenant_build_quota(), docs_swagger_endpoint(), format_version_response(), get_actor_info(), get_tenant_context(), openapi_schema_endpoint() (+14 more)

### Community 47 - "JoernServerManager"
Cohesion: 0.11
Nodes (10): JoernServerManager, Believed-live server map (hash -> host port). A status/health summary. Returns…, Start the background idle-worker reaper (no-op if disabled)., Hashes that haven't served a query within the idle TTL., Wire in the shared restart-dedup registry from core_tools. The callback…, Manages individual Joern server instances running in Docker container using…, Tear down ledger-evicted victims' containers, OFF the admit lock. Called after…, Remove worker containers left over from a previous run (pool mode). (+2 more)

### Community 48 - "RedisPoolStore"
Cohesion: 0.08
Nodes (8): Redis-backed shared pool state for multi-process pool mode. In pool mode every…, First free port in range not already in the registry, sweeping the whole range…, Drop all shared state for a CPG (evicted/terminated). One MULTI/EXEC…, Hashes whose last-touch is older than ``ttl_seconds`` (idle workers). The LRU…, Global lock for the atomic make-room + allocate-port + reserve step., Per-CPG lock so only one process spawns a given CPG at a time., Atomically reserve memory + register the port + touch LRU. One MULTI/EXEC…, RedisPoolStore

### Community 49 - "test_null_pointer_deref.py"
Cohesion: 0.11
Nodes (26): asyncio, Tests for the Null Pointer Dereference detection tool., Test basic null pointer deref detection returns expected output format., Test null pointer deref detection with filename filter., Test null pointer deref detection respects limit parameter., Test that null pointer deref detection identifies different dereference types., Test error handling for invalid codebase hash., Test output when no null pointer deref issues are detected. (+18 more)

### Community 50 - "test_stack_overflow.py"
Cohesion: 0.11
Nodes (26): asyncio, Tests for the Stack Buffer Overflow vulnerability detection tool (CWE-121)., Test that unbounded writes (strcpy, gets, sprintf) are reported as HIGH…, Test that non-literal write sizes not bounded by the array dimension are…, Test that buffer name, type, location, and array size are shown., Test that the filename filter is embedded in the generated query., Test that the limit parameter is embedded in the generated query., Test error handling for an invalid or missing codebase hash. (+18 more)

### Community 51 - "TestExtraGitHosts"
Cohesion: 0.07
Nodes (14): fixture, GIT_CLONE_EXTRA_HOSTS: operator-allowlisted custom git servers.…, Custom hosts are ssh-only; http(s) never reaches a clone., A `host:port` entry only allows that port on that host., A bare `host` entry is a git server, not a licence to reach the box. Without…, urlparse only range-checks the upper bound; port 0 must not pass., ssh:// stays rejected for github.com/gitlab.com., The SSRF posture applies to allowlisted hosts verbatim. (+6 more)

### Community 52 - "callbacks.c"
Cohesion: 0.14
Nodes (22): CallbackEntry, Device, callback_device_irq(), callback_device_read(), callback_device_reset(), callback_device_write(), callback_dispatch_chain(), callback_find() (+14 more)

### Community 53 - "config.c"
Cohesion: 0.20
Nodes (25): ConfigEntry, ConfigContext, config_apply_entry(), config_create(), config_destroy(), config_emit_banner(), config_finalize_loading(), config_get_bool() (+17 more)

### Community 54 - "3. Requirements Analysis"
Cohesion: 0.08
Nodes (25): 1. Executive Summary, 2.1 Database & Schema Extensions (`src/models.py`, `src/utils/postgres_db_manager.py`), 2.2 Lifecycle State Flow (CPG-02), 2.3 Existing Codebase References & Integration Points, 2. Architecture & Design Patterns, 3. Requirements Analysis, 4.1 Race Conditions in Concurrent Sync / Retry, 4.2 Safe Archive Extraction (API-01 Upload) (+17 more)

### Community 55 - "query_executor.py"
Cohesion: 0.11
Nodes (15): CodebaseTracker, Any, Codebase tracker for managing CPG codebase information by hash, Tracks codebase information by hash, Save or update codebase information, Update codebase fields. Delegates to the DB layer, which merges metadata and…, Delete codebase record and associated data., List all tracked codebase hashes (+7 more)

### Community 56 - "Durable CPG Lifecycle & Backend Contract — Giải thích Kỹ Thuật"
Cohesion: 0.08
Nodes (23): 1. ArchiveUploadService — Nạp Source từ File Nén An Toàn, 1. Explicit Build Cancellation (`cancel_version_build`), 1. Vấn đề — Tại sao cần module này?, 2. Idempotent Build Retry (`retry_version_build`), 2. Nội dung chính — Từng bước một, 2. REST API Routes & Standard Response Formatter, 3. Capped Retry Recovery Khi Server Restart (`requeue_running_jobs`), 3. Flowchart — Sơ đồ xử lý logic (Mermaid) (+15 more)

### Community 57 - "JoernServerClient"
Cohesion: 0.11
Nodes (14): Session, JoernServerClient, Any, Close the session and cleanup connections, Context manager entry, Context manager exit - cleanup session, Quick health check to verify the Joern server is responsive. Args: timeout:…, Execute a query synchronously using the /query-sync endpoint Args: query: The… (+6 more)

### Community 58 - "TestMCPTools"
Cohesion: 0.11
Nodes (13): Test listing methods successfully, The security blocklist is enforced on the raw-query path, before execution., Drive _generate_cpg_async far enough to capture the frontend cmd, then bail…, include_globs scoping works for non-C languages via --exclude-regex., compile_commands is rebased and passed as --compilation-database for C., A compile_commands.json shipped in the source is used automatically., Auto-detect can be turned off via config., Non-C frontends don't get --compilation-database (graceful). (+5 more)

### Community 59 - "utils.c"
Cohesion: 0.11
Nodes (8): MemoryType, monitor_parse_args(), memory_region_create(), network_get_config_path(), descriptor_table_store(), descriptor_table_store_checked(), slot_store(), xstrdup()

### Community 60 - "network.c"
Cohesion: 0.19
Nodes (22): NetworkPacket, server_mode(), NetworkContext, net_copy_into(), net_recv_into_window(), network_accept(), network_close_connection(), network_configure_from_env() (+14 more)

### Community 61 - "Moderate Pitfalls"
Cohesion: 0.09
Nodes (23): 10. Retrieval returns plausible but incorrect context, 11. Cross-version and cross-principal cache contamination, 12. Language/frontend and repository edge cases are treated as generic failures, 13. Filename and path normalization differs between manifest, DB, and citations, 14. Retention/GC races with active queries, 15. Observability records sensitive source data, 16. API retries create accidental duplicates, 1. Archive extraction becomes a host filesystem primitive (+15 more)

### Community 62 - "ArchiveUploadService"
Cohesion: 0.11
Nodes (7): ArchiveUploadService, Any, Service handling safe ingestion of uploaded source archives with ZipSlip and…, Safely extract archive bytes, compute content digest, register version and…, DummyDBManager, fixture, service_env()

### Community 63 - ".spawn_server"
Cohesion: 0.13
Nodes (9): Wait (briefly) for a host port to be free before republishing it. Pool mode…, Heap (GB) from the configured JAVA_OPTS -Xmx, defaulting to 4., Size of the codebase's CPG .bin on disk in GB, or None if unknown., Decide (heap_gb, reservation_mb) for a server. In memory mode, size the heap to…, Render JAVA_OPTS with -Xmx/-Xms set to this server's tiered heap., Return (host, port) for connecting to a Joern server. Pool mode with…, Pool-mode spawn coordinated across processes via Redis. Holds a per-CPG spawn…, Pool mode: launch a dedicated cgroup-capped container for this CPG. Joern binds… (+1 more)

### Community 64 - "Implementation Decisions"
Cohesion: 0.09
Nodes (21): Auth & quotas posture, Build trigger & job binding, Cancel, retry & recovery (CPG-03), Canonical References, Claude's Discretion, Deferred Ideas, Established Patterns, Existing Code Insights (+13 more)

### Community 65 - "test_quotas_and_sanitization.py"
Cohesion: 0.13
Nodes (13): BaseHTTPMiddleware, Request, Response, RateLimitMiddleware, Rate Limiting Middleware (Phase 8 - API-04) In-memory Token Bucket rate limiter…, Attempt to consume tokens. Returns (success, retry_after_seconds)., ASGI Token Bucket Rate Limiter per tenant / client IP., TokenBucket (+5 more)

### Community 66 - "PortManager"
Cohesion: 0.10
Nodes (12): PortManager, Manages port allocation for Joern server instances, Smallest available port >= cursor, wrapping to the lowest otherwise., Allocate a port for a session, Get the port assigned to a session, Release the port assigned to a session, Get the session ID for a given port, Get all current port allocations (+4 more)

### Community 67 - "validate_git_branch"
Cohesion: 0.13
Nodes (13): Validate a pasted code snippet (source_type='snippet')., Validate a git branch/ref name (no-op when not provided)., Validate a GitHub token's shape (no-op when not provided). The token is…, validate_code_snippet(), validate_git_branch(), validate_github_token(), parametrize, Test GitHub token shape validation (+5 more)

### Community 68 - "test_joern_client_load.py"
Cohesion: 0.16
Nodes (17): HTTP client for communicating with Joern server API, Derive a collision-free Joern project name from a hash/path. Every CPG file is…, _safe_project_name(), _client(), _no_sleep(), _ok(), fixture, Tests for JoernServerClient.load_cpg robustness (#5: 'No projects loaded').… (+9 more)

### Community 69 - "test_worker_pool.py"
Cohesion: 0.10
Nodes (10): Unit tests for Phase-2 pool mode: each CPG runs in its own Docker container.…, Evicting a CPG must return its host port to the pool (no port leak)., Defensive: a port allocated but out of sync with _exec_ids is still freed., Local mode: only workers untouched beyond the TTL are reap candidates., test_evict_releases_port_back_to_pool(), test_evict_releases_port_even_when_terminate_noops(), test_get_or_create_client_reuses_client_on_matching_port(), test_idle_candidates_local_mode_picks_stale_only() (+2 more)

### Community 70 - "TestLoadConfig"
Cohesion: 0.12
Nodes (12): Any, Recursively substitute environment variables in config, _substitute_env_vars(), Test environment variable substitution with defaults, Test that non-template strings are unchanged, Test configuration loading, Test loading config from YAML file, Test loading config from environment variables (+4 more)

### Community 71 - "test_common_helpers.py"
Cohesion: 0.18
Nodes (17): is_error_output(), True if a string tool result is an error sentinel (so: don't cache it)., parametrize, Unit tests for the shared tool helpers (src/tools/_common.py)., _Result, _services(), test_is_error_output_detects_each_prefix(), test_is_error_output_false_for_normal_output() (+9 more)

### Community 72 - "_hash_tree_in_process"
Cohesion: 0.19
Nodes (17): _fingerprint_local_source(), _fingerprint_local_source_via_daemon(), _hash_tree_in_process(), _joern_helper_image(), Resolve an image id to use for short-lived playground helper containers. Reuses…, Deterministic content fingerprint of a directory tree (this process reads it).…, Content fingerprint of a host tree the MCP can't read, via a helper container.…, Content fingerprint for a local source, used as the CPG cache key. Reads the… (+9 more)

### Community 73 - "CodeBadger MCP Server — Luồng hoạt động chi tiết"
Cohesion: 0.12
Nodes (16): 10. Deployment options, 1. Mở đầu — Vấn đề, 2. Kiến trúc tổng quan, 3. Luồng xử lý từ request → response, 4. Từng bước một, 5. Call Graph — Mối quan hệ các service, 6. Analogy — Hình dung dễ hơn, 7. Bảng mapping source code (+8 more)

### Community 74 - "2. Provider-by-provider"
Cohesion: 0.12
Nodes (16): 1. Head-to-head (hosted since embeddings are input-only, price = input price), 2. Provider-by-provider, 3. Self-hosted / open-source options, 4. Recommendation, (a) Hosted API — lowest cost + stability, Amazon Bedrock, (b) Hosted API — best quality, Bottom line (+8 more)

### Community 75 - "Phase 5: Secure Ingestion & Version Catalog - Context"
Cohesion: 0.12
Nodes (16): Canonical References, Deferred Ideas, Established Patterns, Existing Code Insights, Existing remote source safeguards, Implementation Decisions, Integration Points, Milestone scope (+8 more)

### Community 76 - "2. Pattern Mapping & Concrete Code Excerpts"
Cohesion: 0.12
Nodes (16): 1. Summary of Files to Modify and Create, 2. Pattern Mapping & Concrete Code Excerpts, 3. Implementation Verification Points, Code Excerpt (Error Masking Helper):, Code Excerpt (`main.py` / `src/api/rest_routes.py`):, Code Excerpt (REST / MCP Envelope Format):, Code Excerpt (`src/models.py`):, Code Excerpt (`src/services/git_sync_service.py`): (+8 more)

### Community 77 - "Project"
Cohesion: 0.14
Nodes (9): Finding, Project, Any, Convert codebase info to dictionary, Convert result to dictionary, Project entity representing a registered repository source., Security finding/vulnerability, Convert finding to dictionary (+1 more)

### Community 78 - "B. Local development (MCP on the host)"
Cohesion: 0.12
Nodes (16): 1. Prerequisites, 1. Prerequisites, 2. Get the code, 2. Install Python dependencies, 3. Configure for your host, 3. Start the backing services only, 4. Check your config, 4. Deploy (+8 more)

### Community 79 - "defaults.py"
Cohesion: 0.14
Nodes (14): _log_effective_config(), Log the RESOLVED runtime config and flag env-vs-effective mismatches. Env vars…, frontend_capabilities(), frontend_supports(), Centralized default configuration values. This module contains all default…, # NOTE: every token below is anchored with a leading `(?:^|.*/)` boundary, Postgres URL: DATABASE_URL if set, else built from POSTGRES_* env/defaults., Capabilities of the Joern frontend for `language` (safe fallback for unknown… (+6 more)

### Community 80 - "timedelta"
Cohesion: 0.12
Nodes (8): Create a signed short-lived JWT access token., Create a signed long-lived JWT refresh token., get_cpg_status surfaces phase/elapsed/deadline and queue_position while…, A 'generating' build past its deadline with no live worker is reconciled to…, A 'generating' build past its deadline but with a live worker is NOT condemned., Default GC frees allocations of cold running servers (delete_files=False) and…, With delete_cold=True, cold sleeping (not-running) CPG binaries are deleted., timedelta

### Community 81 - "DurableCPGQueue"
Cohesion: 0.13
Nodes (4): DurableCPGQueue, Postgres-backed CPG generation queue. Same interface as CPGGenerationQueue, but…, True if a build for this codebase is queued or running in the DB., 1-based position among queued jobs, or None if not queued.

### Community 82 - "CacheCleanupScheduler"
Cohesion: 0.12
Nodes (9): CacheCleanupScheduler, Periodic cache cleanup scheduler for CodeBadger, Runs periodic cache cleanup in a background thread. The scheduler uses a daemon…, Initialize the cache cleanup scheduler. Args: db_manager: PostgresDBManager…, Start the cleanup scheduler in a background thread., Stop the cleanup scheduler. Args: timeout: Maximum time to wait for thread to…, Check if the scheduler is currently running., Main cleanup loop - runs in background thread. (+1 more)

### Community 83 - "validate_cpgql_query"
Cohesion: 0.17
Nodes (9): Validate a raw CPGQL query before it reaches Joern's Ammonite Scala REPL.…, validate_cpgql_query(), Test CPGQL query validation, Test valid CPGQL queries, Test query that exceeds length limit, Test queries with dangerous operations, Patterns added after the audit: file reads, $ivy, sys.exit, reflection bypass,…, Common analysis queries must still pass the expanded blocklist. (+1 more)

### Community 85 - "app_lifespan"
Cohesion: 0.13
Nodes (15): lifespan, app_lifespan(), _check_exposure(), _check_joern_container_status(), _graceful_shutdown(), Gracefully shutdown all services, Inspect the Joern Docker container without raising on Docker issues., Flag a fail-open network-exposure posture as startup issue(s). The riskiest… (+7 more)

### Community 86 - "Architecture Patterns"
Cohesion: 0.13
Nodes (15): Anti-Pattern 1: Treating an uploaded archive as a local source path, Anti-Pattern 2: Rebuilding queue and Joern orchestration for REST, Anti-Pattern 3: Exposing raw paths, hashes, or CPGQL as the public context contract, Anti-Pattern 4: Extract-then-validate archives, Anti-Patterns to Avoid, Architecture Patterns, Architecture Research Gaps, Build Order (+7 more)

### Community 87 - "CPGGenerationQueue"
Cohesion: 0.13
Nodes (6): CPGGenerationQueue, Bounded async queue for CPG generation jobs (B1 dedup + B3 concurrency limit)., Submit a CPG generation job. Returns SUBMITTED, DUPLICATE (already in-flight),…, Jobs currently queued or being generated (dedup set size)., True if a build for this codebase is queued or running., Best-effort queue position. The in-memory asyncio.Queue doesn't expose per-item…

### Community 88 - "_FakeStore"
Cohesion: 0.20
Nodes (9): _FakeStore, _patch_sleep(), asyncio, Infra-free unit tests for DurableCPGQueue's worker loop (poll backoff). These…, Minimal job-store stand-in: claim_next_job returns queued results in order., Record asyncio.sleep delays; abort the worker loop after ``stop_after``., test_backoff_resets_after_a_job_is_claimed(), test_idle_poll_backs_off_exponentially_capped() (+1 more)

### Community 89 - "test_version_lifecycle_recovery.py"
Cohesion: 0.14
Nodes (8): fixture, Tests for version lifecycle recovery: cancellation, retry, and startup…, setup_project(), temp_db(), test_retry_cancelled(), test_retry_failed(), test_retry_idempotent(), test_retry_ready_guard()

### Community 90 - "2. Luồng Kỹ Thuật Chi Tiết (Detailed Technical Flows)"
Cohesion: 0.14
Nodes (13): 1. Tổng quan Kiến trúc Middleware & Services, 2. Luồng Kỹ Thuật Chi Tiết (Detailed Technical Flows), 3. Danh mục Tệp Mã Nguồn Phase 8, A. Token Bucket Rate Limiting (`RateLimitMiddleware`), B. Giới hạn dung lượng tải lên (`MAX_PAYLOAD_SIZE_BYTES`), C. Giới hạn hàng đợi Build đồng thời (`MAX_CONCURRENT_BUILDS_PER_TENANT`), Luồng 1.1: Cấu hình Token vào MCP Client (Claude Desktop, Claude Code, Cursor), Luồng 1: Xác thực người dùng và cấp phát Token (Authentication Flow) (+5 more)

### Community 91 - "cmdline.c"
Cohesion: 0.21
Nodes (11): FILE, format_status_line(), monitor_capture(), monitor_exec(), monitor_exec_filtered(), monitor_format_status(), monitor_prompt_exec(), monitor_read_from_file() (+3 more)

### Community 92 - "._make_room"
Cohesion: 0.16
Nodes (6): Snapshot the memory-admission ledger for /health and status logs., Return the Docker container's current RSS in MB (0.0 on any error)., Tear down a server and mark its codebase sleeping. The local-state mutations…, Free a victim's ledger + local state WITHOUT the blocking Docker teardown;…, Evict the LRU server if container RSS is over the configured threshold. Shared…, Evict servers so a new ``needed_mb`` reservation can be admitted. Memory mode…

### Community 93 - "_autodetect_c_includes"
Cohesion: 0.31
Nodes (13): _autodetect_c_includes(), Lightweight C/C++ include-dir discovery for the c2cpg `--include` path. Returns…, Tests for _autodetect_c_includes — the C/C++ include-dir discovery that lets…, test_depth_bounded(), test_finds_dir_with_config_h(), test_finds_dir_with_generated_version_header(), test_finds_include_dir(), test_git_dir_excluded() (+5 more)

### Community 94 - "_generate_cpg_async"
Cohesion: 0.18
Nodes (8): _classify_cpg_build_failure(), _generate_cpg_async(), Async task to generate CPG and start Joern server, Record the current build phase so get_cpg_status can report progress. Best-…, Map a failed c2cpg/frontend run to (error_code, human message). Distinguishes…, _set_build_phase(), Fix #4: failed CPG builds carry a labeled cause (OOM/TIMEOUT/BUILD_ERROR)., TestCpgBuildFailureLabeling

### Community 95 - "_FakePoolStore"
Cohesion: 0.14
Nodes (6): _FakePoolStore, In-memory stand-in for RedisPoolStore (resv/registry/worker/lru)., A stale Redis ledger entry (reserved + LRU, no registry port) must be purged by…, Redis (pool) mode reads idle candidates from the shared LRU ledger., test_idle_candidates_redis_mode_delegates(), test_make_room_purges_stale_pool_entry_without_spinning()

### Community 96 - "health_check"
Cohesion: 0.22
Nodes (9): custom_route, health_check(), Dependency-aware health check. status ∈ {up, partial, down}. HTTP 200 for…, Test custom HTTP endpoints, Populate main.services with mocks so /health reports status=up., /health returns up + mcp + a dependencies map when everything is healthy., A failing Postgres ping makes the overall status down (HTTP 503)., The public /health response must not include the codebase list/sources. (+1 more)

### Community 97 - "3. Chi tiết từng bước"
Cohesion: 0.15
Nodes (13): 1. Vấn đề là gì?, 2. Tổng quan luồng, 3. Chi tiết từng bước, 4. CallGraph — Quan hệ các script, 5. Ví dụ hình dung (Analogy) — Chuyển nhà bằng thùng carton, 6. Bảng mapping source code, Bước 1: Build images — `build.sh` → `build-mcp.sh` + `build-joern.sh`, Bước 2: Push lên GHCR — `push.sh` (+5 more)

### Community 98 - "Technology Stack"
Cohesion: 0.15
Nodes (12): Alternatives Considered, Core Framework, Database, Exact Additions to `requirements.txt`, Implementation Notes and Version Risks, Infrastructure, Recommended Integration Shape, Recommended Stack (+4 more)

### Community 99 - "program_slice.scala"
Cohesion: 0.29
Nodes (12): backwardTrace(), clip(), findAnchor(), forwardTrace(), isAssignmentOp(), normalizeFilename(), Boolean, Int (+4 more)

### Community 100 - "test_postgres_db_manager.py"
Cohesion: 0.22
Nodes (12): db(), fixture, pg, Tests for the Phase-3c Postgres catalog/cache/findings store. Needs a real…, FOR UPDATE serializes concurrent metadata merges so no writer's key is lost., test_codebase_crud_and_created_at_preserved(), test_construction_does_not_connect(), test_findings_save_filter_and_stats() (+4 more)

### Community 101 - "test_taint_tools_usability.py"
Cohesion: 0.17
Nodes (12): mock_services(), fixture, Test that missing sink triggers the new helpful error message, Test that missing source triggers the new helpful error message, Test that legacy arguments like source_pattern trigger specific error, Register tools and return a dict of {name: function}, Test detection of multiple legacy arguments, taint_tools() (+4 more)

### Community 103 - "analyzeChecks"
Cohesion: 0.24
Nodes (11): Call, analyzeChecks(), classifySizedOp(), extractVariable(), pathBoundaryRegex(), Int, Method, Option (+3 more)

### Community 104 - "CodeBadger"
Cohesion: 0.17
Nodes (12): Active, CodeBadger, Constraints, Context, Core Value, Current Milestone: v0.7 Codebase Context Backend (Completed 2026-09-17), Key Decisions, Out of Scope (+4 more)

### Community 105 - "_ssh_clone_env"
Cohesion: 0.24
Nodes (6): Environment overrides for an ssh:// clone of a custom git server. git invokes…, _ssh_clone_env(), GIT_SSH_COMMAND construction for ssh:// clones of custom hosts., git runs GIT_SSH_COMMAND through a shell — a spaced path must survive., GitPython layers env over os.environ, so don't copy the whole thing. This…, TestSshCloneEnv

### Community 106 - "test_startup_tuning.py"
Cohesion: 0.29
Nodes (11): guard_build_concurrency(), Clamp build_workers so concurrent build JVMs fit the build container's cap.…, _cfg(), parametrize, Unit tests for src/startup_tuning.py (extracted from main.py)., test_guard_build_concurrency_clamps_overcommit(), test_guard_build_concurrency_floors_at_one(), test_guard_build_concurrency_leaves_safe_config_alone() (+3 more)

### Community 107 - "test_pool_memory_guard.py"
Cohesion: 0.29
Nodes (11): guard_pool_memory(), parse_mem_to_mb(), Parse a Docker-style memory string ('100g', '512m', '2048') to MB, or None., Prevent pool-mode host over-commit by clamping the worker memory budget. The…, _pool_cfg(), Tests for pool-mode memory coordination: recommender split + over-commit guard.…, test_guard_clamps_when_build_cap_too_large(), test_guard_floors_when_build_cap_exceeds_budget() (+3 more)

### Community 108 - "test_build_opts.py"
Cohesion: 0.29
Nodes (11): Validate + normalize a list of c2cpg build options (include paths / defines).…, _sanitize_build_opt_list(), Tests for _sanitize_build_opt_list — validation of c2cpg include paths /…, test_absolute_include_allowed(), test_blanks_are_dropped_and_trimmed(), test_control_characters_rejected(), test_defines_pass_through(), test_dotdot_only_checked_for_include_paths_not_defines() (+3 more)

### Community 109 - "hasOverflowGuard"
Cohesion: 0.29
Nodes (11): findEntryPoint(), hasOverflowGuard(), isConstantExpr(), isSmallConstant(), pathBoundaryRegex(), Boolean, Int, List (+3 more)

### Community 110 - "use_after_free.scala"
Cohesion: 0.29
Nodes (11): areInMutuallyExclusiveBranches(), fieldName(), findEntryPoint(), mayExecuteAfter(), pathBoundaryRegex(), Boolean, Int, Method (+3 more)

### Community 111 - "main.c"
Cohesion: 0.29
Nodes (9): DeviceType, device_create(), apply_vcpu_affinity(), cleanup_subsystems(), load_configuration(), log_startup_message(), main(), process_env_command() (+1 more)

### Community 112 - "Available Tools"
Cohesion: 0.18
Nodes (11): Arithmetic & format, Available Tools, Code browsing, Concurrency & injection, CPG lifecycle, Extensibility, Memory safety, Semantic analysis (+3 more)

### Community 113 - "security.md"
Cohesion: 0.24
Nodes (5): Checkpoint [2026-08-18T15:39:03Z] mode=--normal, Checkpoint [2026-08-18T15:41:51Z] mode=--verify, Checkpoint [2026-08-18T15:45:21Z] mode=--milestone, Checkpoint [2026-08-18T15:46:28Z] mode=--verify, Checkpoint [2026-08-18T15:46:48Z] mode=--verify

### Community 114 - "slice_scenarios.c"
Cohesion: 0.20
Nodes (5): OJPEGFull, iter_loop(), ojpeg_setup_decode(), ojpeg_setup_decode_subsampled(), ProgIter

### Community 115 - "Milestone Summary: v0.7 Codebase Context Backend"
Cohesion: 0.18
Nodes (10): 1. Tổng quan Dự án (Executive Overview), 2. Các Tính Năng Mới Đã Xây Dựng (Key Features Built), 3. Kiến trúc Tổng thể Hệ thống v0.7, 4. Bảng Tra cứu Yêu cầu (Requirements Traceability - 16/16), 5. Hướng dẫn Dành cho Người mới Bắt đầu (Getting Started), A. Phase 5: Secure Ingestion & Version Catalog (Nạp mã nguồn an toàn & Danh mục phiên bản), B. Phase 6: Durable CPG Lifecycle & Backend Contract (Hàng đợi CPG bền vững & Hợp đồng API), C. Phase 7: Cited Hybrid Context Retrieval (Truy xuất ngữ cảnh lai kèm trích dẫn) (+2 more)

### Community 117 - "areInMutuallyExclusiveBranches"
Cohesion: 0.22
Nodes (9): Long, areInMutuallyExclusiveBranches(), parseIntLit(), pathBoundaryRegex(), Boolean, Int, Method, Option (+1 more)

### Community 118 - "Tests"
Cohesion: 0.20
Nodes (9): 1. User Authentication & JWT Issuance, 2. Protected Endpoints Reject Unauthenticated Requests, 3. Cross-Tenant Isolation, 4. Rate Limiting & Concurrent Build Quotas, 5. Correlation ID Propagation & Audit Trail, Current Test, Gaps, Summary (+1 more)

### Community 119 - "Requirements: CodeBadger v0.7 Codebase Context Backend"
Cohesion: 0.20
Nodes (9): Agent Context, Backend API, CPG Lifecycle, Ingestion & Catalog, Out of Scope, Requirements: CodeBadger v0.7 Codebase Context Backend, Traceability, v1 Requirements (+1 more)

### Community 120 - "Requirements: CodeBadger v0.7 Codebase Context Backend"
Cohesion: 0.20
Nodes (9): Agent Context, Backend API, CPG Lifecycle, Ingestion & Catalog, Out of Scope, Requirements: CodeBadger v0.7 Codebase Context Backend, Traceability, v1 Requirements (+1 more)

### Community 121 - "null_pointer_deref.scala"
Cohesion: 0.29
Nodes (9): areInMutuallyExclusiveBranches(), findEntryPoint(), isNullConstCode(), pathBoundaryRegex(), Boolean, Int, Method, Option (+1 more)

### Community 122 - "trace"
Cohesion: 0.36
Nodes (9): isAssignmentOp(), pathBoundaryRegex(), relevant(), Boolean, Int, List, String, Unit (+1 more)

### Community 123 - "snippet_filename"
Cohesion: 0.31
Nodes (4): Resolve a safe, single-segment filename for a pasted snippet. Honors a caller-…, snippet_filename(), Test snippet filename resolution, TestSnippetFilename

### Community 124 - "TestDictToConfig"
Cohesion: 0.20
Nodes (6): Test dictionary to config conversion, Test converting full config dictionary, Test converting partial config dictionary, Test converting empty config dictionary, Test type conversions in config, TestDictToConfig

### Community 125 - "test_pool_store_redis.py"
Cohesion: 0.33
Nodes (9): _make_manager(), redis_only, Phase-3c multi-process pool: two JoernServerManager instances sharing Redis.…, A stale entry (reserved + LRU, no registry port) — e.g. a crash between reserve…, claim() registers reservation+port+LRU together; release() clears all., test_claim_and_release_are_atomic(), test_cross_process_discovery_admission_and_eviction(), test_make_room_purges_stale_ledger_entry() (+1 more)

### Community 126 - "TestValidateGithubUrl"
Cohesion: 0.20
Nodes (6): Test GitHub URL validation, Valid github.com / gitlab.com / dev.azure.com https URLs are accepted., Malformed or off-allowlist URLs are rejected., SSRF / undefined-behavior vectors must all be rejected., The string must literally begin with an allowed https://host/ prefix., TestValidateGithubUrl

### Community 127 - "AtomBase"
Cohesion: 0.31
Nodes (5): AtomBase, GetDts, sample_table_get_dts(), SttsAtom, data

### Community 128 - ".get_or_create_client"
Cohesion: 0.25
Nodes (3): Spawn + load an existing cpg.bin, retrying transient load failures. Each failed…, Spawn a fresh Joern process and load the existing CPG binary (no regeneration)., Load CPG into Joern server. importCpg triggers expensive overlay computation…

### Community 129 - "areInMutuallyExclusiveBranches"
Cohesion: 0.28
Nodes (8): areInMutuallyExclusiveBranches(), findEntryPoint(), pathBoundaryRegex(), Boolean, Int, Method, Option, String

### Community 130 - "areInMutuallyExclusiveBranches"
Cohesion: 0.28
Nodes (8): areInMutuallyExclusiveBranches(), parseArraySize(), pathBoundaryRegex(), Boolean, Int, Method, Option, String

### Community 131 - "test_main.py"
Cohesion: 0.22
Nodes (7): fixture, Tests for main module, Isolate tests from global state held in main.services., Test health helper behavior., Internal status paths can still request full source locations., reset_main_services(), TestHealthHelpers

### Community 132 - "asyncio"
Cohesion: 0.28
Nodes (6): asyncio, Test lifespan with initialization failure, Startup should succeed even when Docker/Joern is unavailable., Test FastMCP lifespan management, Test successful lifespan startup and shutdown, TestLifespan

### Community 133 - "TestValidateTimeout"
Cohesion: 0.22
Nodes (5): Test timeout validation, Test valid timeout values, Test negative timeout, Test timeout exceeding maximum, TestValidateTimeout

### Community 134 - "Architecture"
Cohesion: 0.25
Nodes (8): Architecture, CPG / server lifecycle, Design decisions, Health & observability, Memory-aware admission, Query flow (with auto-wake), Repository layout, System overview

### Community 135 - "🦡 codebadger Security"
Cohesion: 0.25
Nodes (8): 🦡 codebadger Security, Controls we provide, How strong is each layer?, Production hardening checklist, Reporting a vulnerability, Trust boundaries (threat model), What we do NOT protect against (non-goals & residual risk), Who/what we trust

### Community 136 - "Milestone Audit Report: v0.7 Codebase Context Backend"
Cohesion: 0.25
Nodes (7): 1. Executive Summary, 2. Requirements Verification Matrix (16/16 Satisfied), 3. Cross-Phase Integration & E2E Flow Verification, 4. Test Suite Audit, 5. Technical Debt & Deferred Scope (v2), 6. Milestone Conclusion, Milestone Audit Report: v0.7 Codebase Context Backend

### Community 137 - "Phase 6: Durable CPG Lifecycle & Backend Contract - Discussion Log"
Cohesion: 0.25
Nodes (7): Build trigger & job binding, Cancel, retry & recovery, Claude's Discretion, Deferred Ideas, Lifecycle state model, Phase 6: Durable CPG Lifecycle & Backend Contract - Discussion Log, REST surface & archive upload

### Community 138 - "2. Architecture & Decisions"
Cohesion: 0.25
Nodes (7): 1. Authentication & Tenancy Model (API-03), 1. System Classification, 2. Architecture & Decisions, 2. Audit Logging & Correlation Tracking (API-04), 3. Rate Limiting & Queue Backpressure (API-04), 4. Error Sanitization & Diagnostics (API-04), AI-SPEC — Phase 8: Authorization, Quotas & Production Verification

### Community 139 - "Completed Tasks & Components"
Cohesion: 0.25
Nodes (7): 1. 08-01: JWT Authentication, User Seeding & Tenancy Service (API-03), 2. 08-02: Auth Middleware & Route/MCP Protection (API-03), 3. 08-03: Correlation Tracking & Structured Audit Logging (API-04), 4. 08-04: Rate Limiting, Backpressure Quotas & Error Sanitization (API-04), 5. 08-05: End-to-End & Security Parity Verification, Completed Tasks & Components, Phase 8: Authorization, Quotas & Production Verification — Execution Summary

### Community 140 - "Feature Landscape"
Cohesion: 0.25
Nodes (8): Anti-Features, Differentiators, Feature Dependencies, Feature Landscape, MVP Recommendation, Product Boundary, Sources, Table Stakes

### Community 141 - "bfsEdges"
Cohesion: 0.36
Nodes (7): bfsEdges(), printEdges(), Boolean, Int, List, String, Unit

### Community 142 - "validate_search_pattern"
Cohesion: 0.36
Nodes (4): Validate a caller-supplied regex / name filter (no-op when empty). Bounds…, validate_search_pattern(), Test regex/name-filter ReDoS + length guard, TestValidateSearchPattern

### Community 143 - "CodeBadger agent guide"
Cohesion: 0.29
Nodes (6): CodeBadger agent guide, CPG and worker rules, Development workflow, Handoff expectations, MCP/tool contracts, What this repository is

### Community 144 - "/feature-development"
Cohesion: 0.29
Nodes (6): Common Files, /feature-development, Goal, Notes, Suggested Sequence, Typical Commit Signals

### Community 145 - "Custom Tools"
Cohesion: 0.29
Nodes (7): 1. Query template, 2. Python tool, Custom Tools, Design decisions, Helpers, Tags, Tips

### Community 146 - "Phase 5: Secure Ingestion & Version Catalog - Discussion Log"
Cohesion: 0.29
Nodes (6): Credentials and trigger, Deferred Ideas, Phase 5: Secure Ingestion & Version Catalog - Discussion Log, Provider support and version behavior, Source delivery, the agent's Discretion

### Community 147 - "Recommended Architecture"
Cohesion: 0.29
Nodes (7): Component Boundaries, Data Flow, Data Model and Lifecycle, Integration Seams in the Current Repository, Recommended Architecture, Semantic context retrieval, Upload, snapshot, and build

### Community 148 - "Research Summary: Codebase Context Backend"
Cohesion: 0.29
Nodes (6): Build order, Non-negotiable safeguards, Recommendation, Research gaps to resolve during planning, Research Summary: Codebase Context Backend, Stack decisions

### Community 149 - "deploy.sh"
Cohesion: 0.33
Nodes (5): DOCKER_SOCK, PLAYGROUND_HOST_PATH, POSTGRES_DATA_PATH, deploy.sh script, wait_for_health()

### Community 150 - "escape_scala_string"
Cohesion: 0.29
Nodes (5): Sanitize a value to prevent template injection. Replaces {{ sequences in user-…, escape_scala_string(), Any, Helpers for rendering user input into CPGQL/Scala query strings safely., Escape a value for safe embedding inside a Scala string literal. This preserves…

### Community 151 - "validate_snippet_label"
Cohesion: 0.38
Nodes (4): Validate/sanitize the human label stored for a snippet (source_path). Cosmetic…, validate_snippet_label(), Test snippet label sanitization, TestValidateSnippetLabel

### Community 152 - "test_compose_security.py"
Cohesion: 0.29
Nodes (6): compose(), fixture, parametrize, Guards on docker-compose security posture. These parse the committed docker-…, Joern REPL, Postgres, and Redis must never publish on all interfaces. Joern…, test_internal_services_bind_loopback()

### Community 154 - "2026"
Cohesion: 0.29
Nodes (7): 2026, CVE-2025-51602 — VLC Media Player · Out-of-Bounds Read · February 11, 2026, CVE-2025-6021 — libxml2 · Integer Overflow → Stack Buffer Overflow · February 2, 2026, CVE-2025-6170 — libxml2 · Stack Buffer Overflow in xmllint · February 2, 2026, CVE-2025-6491 — php-src · Integer Overflow in SoapVar · February 2, 2026, CVE-2026-1801 — libsoup · HTTP Request Smuggling · February 11, 2026, 🏆 Trophies

### Community 155 - "ECC for Codex CLI"
Cohesion: 0.33
Nodes (5): ECC for Codex CLI, MCP Baseline, Multi-Agent Support, Repo Skill, Workflow Files

### Community 156 - "sameTarget"
Cohesion: 0.40
Nodes (5): Expression, pathBoundaryRegex(), sameTarget(), Boolean, String

### Community 157 - "isSyntheticLocal"
Cohesion: 0.33
Nodes (5): Local, isSyntheticLocal(), pathBoundaryRegex(), Boolean, String

### Community 158 - "ConcurrencyLimitMiddleware"
Cohesion: 0.33
Nodes (4): ConcurrencyLimitMiddleware, BaseHTTPMiddleware, Request, Return 503 when too many MCP connections are active (B2).

### Community 159 - "Phase 05 Plan 01: Secure Ingestion Version Catalog Summary"
Cohesion: 0.33
Nodes (5): Executive Summary, Key Files Created / Modified, Phase 05 Plan 01: Secure Ingestion Version Catalog Summary, Self-Check: PASSED, Tasks Completed

### Community 160 - "Phase 05 Plan 02: Git Sync & Snapshot Promotion Summary"
Cohesion: 0.33
Nodes (5): Executive Summary, Key Files Created / Modified, Phase 05 Plan 02: Git Sync & Snapshot Promotion Summary, Self-Check: PASSED, Tasks Completed

### Community 161 - "Phase 8: Authorization, Quotas & Production Verification - Discussion Log"
Cohesion: 0.33
Nodes (5): 1. Authentication & Tenancy Model (API-03), 2. Audit Logging & Correlation Tracking (API-04), 3. Rate Limiting & Queue Backpressure (API-04), 4. REST & MCP Security Parity, Phase 8: Authorization, Quotas & Production Verification - Discussion Log

### Community 162 - "Phase 8 Validation Report: Authorization, Quotas & Production Verification"
Cohesion: 0.33
Nodes (5): 1. Requirements Compliance Matrix, 2. Test Suite Status, 3. Security & Operational Posture Review, 4. Success Criteria Audit, Phase 8 Validation Report: Authorization, Quotas & Production Verification

### Community 163 - "Roadmap: CodeBadger v0.7 Codebase Context Backend"
Cohesion: 0.33
Nodes (5): Phase 5: Secure Ingestion & Version Catalog, Phase 6: Durable CPG Lifecycle & Backend Contract, Phase 7: Cited Hybrid Context Retrieval, Phase 8: Authorization, Quotas & Production Verification, Roadmap: CodeBadger v0.7 Codebase Context Backend

### Community 164 - "🦡 codebadger"
Cohesion: 0.33
Nodes (6): Citation, 🦡 codebadger, Documentation, Found a vulnerability using codebadger?, News, Quick deploy

### Community 165 - ".from_dict"
Cohesion: 0.33
Nodes (3): Create codebase info from dictionary, Get codebase information by hash, All codebases as CodebaseInfo in a single bulk query (read-only). For /health…

### Community 166 - "taint_flows.scala"
Cohesion: 0.53
Nodes (5): getMethodName(), getName(), pathBoundaryRegex(), StoredNode, String

### Community 167 - "TestEffectiveConfigSelfCheck"
Cohesion: 0.47
Nodes (3): The startup self-check logs resolved config and flags env-vs-effective drift., CPG_QUEUE_BACKEND=durable but effective memory → a warning., TestEffectiveConfigSelfCheck

### Community 168 - "slice_inline.h"
Cohesion: 0.40
Nodes (4): Ap4Atom, ap4_atom_set_type(), is_pixel_gray(), Quantum

### Community 169 - "Usage"
Cohesion: 0.40
Nodes (5): Connect an MCP client, Example session, Researcher workflow, Tool catalog, Usage

### Community 170 - "Roadmap: CodeBadger"
Cohesion: 0.40
Nodes (4): Active Milestone, Milestone History, Roadmap: CodeBadger, v0.7 - Codebase Context Backend

### Community 171 - "Project State — CodeBadger"
Cohesion: 0.40
Nodes (4): Completed Milestones, Current Status, Next Steps, Project State — CodeBadger

### Community 172 - "getMethodName"
Cohesion: 0.60
Nodes (4): getMethodName(), getName(), StoredNode, String

### Community 173 - "Contributing"
Cohesion: 0.50
Nodes (4): Contributing, Development setup, Guidelines, Running tests

### Community 174 - "sample_client.py"
Cohesion: 0.67
Nodes (3): extract_tool_result(), main(), Extract dictionary data from CallToolResult

### Community 175 - "05-01-PLAN.md"
Cohesion: 0.50
Nodes (3): Mitigations, Residual risk, Threats

### Community 176 - "05-02-PLAN.md"
Cohesion: 0.50
Nodes (3): Mitigations, Residual risk, Threats

### Community 177 - "Phase 8 Context: Authorization, Quotas & Production Verification"
Cohesion: 0.50
Nodes (3): Current Goal, Phase 8 Context: Authorization, Quotas & Production Verification, Prior State

### Community 179 - "TestShutdown"
Cohesion: 0.50
Nodes (3): Test graceful shutdown behavior., Graceful shutdown should cancel tracked restart tasks before clearing services., TestShutdown

### Community 180 - ".test_root_endpoint"
Cohesion: 0.50
Nodes (3): Test root endpoint behavior., Test the / root endpoint returns correct response, TestRootEndpoint

### Community 181 - "TestMiddleware"
Cohesion: 0.50
Nodes (3): Test middleware behavior., The concurrency middleware should return a valid 503 response when full., TestMiddleware

### Community 183 - "Configuration"
Cohesion: 0.67
Nodes (3): Configuration, Key settings, Telemetry (OpenTelemetry)

### Community 184 - "🦡 codebadger Documentation"
Cohesion: 0.67
Nodes (3): 🦡 codebadger Documentation, Contents, Quick links

### Community 185 - "Roadmap"
Cohesion: 0.67
Nodes (3): In progress / next, Roadmap, Shipped

### Community 186 - "_validate_repo_url_path"
Cohesion: 0.67
Nodes (3): ParseResult, Path check shared by every accepted scheme: at least /owner/repo., _validate_repo_url_path()

## Knowledge Gaps
- **488 isolated node(s):** `GetDts`, `data`, `codebadger`, `build-joern.sh script`, `build-mcp.sh script` (+483 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **23 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `ValidationError` connect `ValidationError` to `validators.py`, `taint_analysis_tools.py`, `TestValidateTimeout`, `validate_search_pattern`, `logging.py`, `GitManager`, `register_core_tools`, `core_tools.py`, `validate_snippet_label`, `exceptions.py`, `rest_routes.py`, `TestExtraGitHosts`, `_validate_repo_url_path`, `validate_git_branch`, `test_common_helpers.py`, `_hash_tree_in_process`, `validate_cpgql_query`, `TestSnippetLanguageInferAndValidate`, `test_build_opts.py`, `TestParseSnippetBlocks`, `TestValidateGithubUrl`?**
  _High betweenness centrality (0.063) - this node is a cross-community bridge._
- **Why does `register_tools()` connect `register_tools` to `taint_analysis_tools.py`, `main.py`, `CodebaseInfo`, `test_uninitialized_read.py`, `FastMCP`, `QueryResult`, `test_heap_overflow.py`, `datetime`, `test_toctou.py`, `test_null_pointer_deref.py`, `test_stack_overflow.py`, `register_core_tools`, `app_lifespan`?**
  _High betweenness centrality (0.038) - this node is a cross-community bridge._
- **Why does `CodebaseInfo` connect `CodebaseInfo` to `taint_analysis_tools.py`, `models.py`, `test_main.py`, `CodeBrowsingService`, `FastMCP`, `register_tools`, `QueryResult`, `test_toctou.py`, `register_core_tools`, `core_tools.py`, `SessionStatus`, `.from_dict`, `test_uninitialized_read.py`, `test_heap_overflow.py`, `datetime`, `test_null_pointer_deref.py`, `test_stack_overflow.py`, `query_executor.py`, `TestMCPTools`, `Project`, `timedelta`, `_generate_cpg_async`?**
  _High betweenness centrality (0.030) - this node is a cross-community bridge._
- **Are the 169 inferred relationships involving `FastMCP` (e.g. with `full_security_env()` and `app_env()`) actually correct?**
  _`FastMCP` has 169 INFERRED edges - model-reasoned connections that need verification._
- **Are the 56 inferred relationships involving `QueryResult` (e.g. with `double_free_services()` and `test_find_double_free_detects_free_variants()`) actually correct?**
  _`QueryResult` has 56 INFERRED edges - model-reasoned connections that need verification._
- **Are the 27 inferred relationships involving `ValidationError` (e.g. with `test_control_characters_rejected()` and `test_relative_include_with_dotdot_rejected()`) actually correct?**
  _`ValidationError` has 27 INFERRED edges - model-reasoned connections that need verification._
- **Are the 4 inferred relationships involving `JoernServerManager` (e.g. with `JoernServerClient` and `RedisPoolStore`) actually correct?**
  _`JoernServerManager` has 4 INFERRED edges - model-reasoned connections that need verification._