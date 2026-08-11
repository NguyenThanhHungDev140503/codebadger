"""
Postgres-backed durable job store.

Exposes the job-queue method surface used by DurableCPGQueue (enqueue_job /
claim_next_job / complete_job / fail_job / get_job / count_jobs /
requeue_running_jobs). claim_next_job uses `FOR UPDATE SKIP LOCKED`, so many
generation workers across multiple processes / hosts can pull from one shared
queue concurrently without blocking each other or double-claiming.

Connections are served from a psycopg_pool ConnectionPool when the optional
`psycopg[pool]` extra is installed, so the many low-latency queue/catalog ops
reuse warm connections instead of paying a fresh TCP+auth(+TLS) handshake each
call — under a large batch the per-op connect was steady churn (idle workers
poll on an interval) and pressured Postgres `max_connections`. When the extra is
absent we fall back to per-operation connects, so the store still works with a
plain `psycopg` install.
"""

import json
import logging
from datetime import datetime, timezone
from typing import Any, Dict, Optional

import psycopg
from psycopg.rows import dict_row

try:  # connection pooling is the optional psycopg[pool] extra
    from psycopg_pool import ConnectionPool
    _HAVE_POOL = True
except ImportError:
    ConnectionPool = None
    _HAVE_POOL = False

logger = logging.getLogger(__name__)


def _now() -> str:
    return datetime.now(timezone.utc).isoformat()


class PostgresJobStore:
    """Durable job queue on Postgres with SKIP LOCKED claims."""

    def __init__(self, dsn: str, max_pool_size: int = 16, use_pool: bool = True):
        # No live connection here — construction must not require a live DB (so it
        # can be imported/instantiated cheaply). The pool is created openable
        # (open=False) and actually opened in init_schema(), once the DB is known
        # reachable. Call init_schema() at startup.
        self.dsn = dsn
        self._pool = None
        if use_pool and _HAVE_POOL:
            self._pool = ConnectionPool(
                dsn,
                min_size=1,
                max_size=max(2, max_pool_size),
                open=False,
                name="codebadger-pg",
                kwargs={"row_factory": dict_row, "autocommit": False},
            )
        elif use_pool and not _HAVE_POOL:
            logger.warning(
                "psycopg_pool not installed; using per-operation Postgres connections. "
                "Install the psycopg[pool] extra for connection reuse under load."
            )

    def _open_pool(self) -> None:
        """Open the pool if pooling is enabled (idempotent; fail-fast on a bad DB)."""
        if self._pool is not None:
            # wait=True so an unreachable DB raises here at startup rather than on
            # first query, preserving the fail-fast boot contract. open() no-ops
            # if the pool is already open.
            self._pool.open(wait=True, timeout=30)

    def _connect(self):
        """Yield a connection: from the pool (returned on exit) or a fresh one."""
        if self.dsn.startswith("sqlite://"):
            import sqlite3

            class SqliteConnectionWrapper:
                def __init__(self, conn):
                    self.conn = conn

                def execute(self, sql, params=()):
                    sql_converted = sql.replace("%s", "?")
                    if "RETURNING" in sql_converted and "INSERT INTO jobs" in sql_converted:
                        cur = self.conn.execute(sql_converted.split("RETURNING")[0], params)
                        jid = cur.lastrowid
                        class ReturningIdWrapper:
                            def __init__(self, jid): self.jid = jid
                            def fetchone(self): return {"id": self.jid}
                            def __getitem__(self, k): return self.jid if k == "id" else None
                        return ReturningIdWrapper(jid)
                    if "RETURNING" in sql_converted and "UPDATE project_versions" in sql_converted:
                        split_sql = sql_converted.split("RETURNING")[0]
                        cur = self.conn.execute(split_sql, params)
                        vid = params[2] if len(params) >= 3 else (params[1] if len(params) > 1 else params[0])
                        return self.conn.execute("SELECT * FROM project_versions WHERE id = ?", (vid,))
                    return self.conn.execute(sql_converted, params)

                def commit(self):
                    return self.conn.commit()

                def rollback(self):
                    return self.conn.rollback()

                def __enter__(self):
                    return self

                def __exit__(self, exc_type, exc_val, exc_tb):
                    self.conn.close()

            db_path = self.dsn[len("sqlite://"):]
            conn = sqlite3.connect(db_path, check_same_thread=False)
            conn.row_factory = sqlite3.Row
            return SqliteConnectionWrapper(conn)
        if self._pool is not None:
            return self._pool.connection()
        return psycopg.connect(self.dsn, row_factory=dict_row, autocommit=False)

    def close(self) -> None:
        """Close the connection pool, if any. Safe to call repeatedly."""
        if self._pool is not None:
            try:
                self._pool.close()
            except Exception as e:
                logger.warning(f"Error closing Postgres pool: {e}")
            self._pool = None

    def init_schema(self) -> None:
        if not self.dsn.startswith("sqlite://"):
            self._open_pool()
        with self._connect() as conn:
            if self.dsn.startswith("sqlite://"):
                conn.execute("""
                    CREATE TABLE IF NOT EXISTS jobs (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        codebase_hash TEXT NOT NULL,
                        job_type TEXT NOT NULL DEFAULT 'generate_cpg',
                        status TEXT NOT NULL DEFAULT 'queued',
                        payload TEXT,
                        result TEXT,
                        error TEXT,
                        attempts INTEGER NOT NULL DEFAULT 0,
                        created_at TEXT NOT NULL,
                        updated_at TEXT NOT NULL
                    )
                """)
            else:
                conn.execute("""
                    CREATE TABLE IF NOT EXISTS jobs (
                        id BIGSERIAL PRIMARY KEY,
                        codebase_hash TEXT NOT NULL,
                        job_type TEXT NOT NULL DEFAULT 'generate_cpg',
                        status TEXT NOT NULL DEFAULT 'queued',
                        payload TEXT,
                        result TEXT,
                        error TEXT,
                        attempts INTEGER NOT NULL DEFAULT 0,
                        created_at TEXT NOT NULL,
                        updated_at TEXT NOT NULL
                    )
                """)
            conn.execute("CREATE INDEX IF NOT EXISTS idx_jobs_status ON jobs(status, created_at)")
            conn.execute("""
                CREATE UNIQUE INDEX IF NOT EXISTS idx_jobs_active_unique
                ON jobs(codebase_hash, job_type) WHERE status IN ('queued', 'running')
            """)
            conn.commit()
        logger.info("Postgres job store schema ready")

    def enqueue_job(self, codebase_hash: str, job_type: str, payload: Dict[str, Any],
                    max_queued: int = 0) -> tuple:
        """Enqueue a job. Returns (job_id|None, 'submitted'|'duplicate'|'queue_full'|'error')."""
        now = _now()
        try:
            with self._connect() as conn:
                # Dedup precedes backpressure: a re-submit of an active job is a
                # 'duplicate', never 'queue_full'.
                row = conn.execute(
                    "SELECT id FROM jobs WHERE codebase_hash = %s AND job_type = %s "
                    "AND status IN ('queued', 'running') LIMIT 1",
                    (codebase_hash, job_type),
                ).fetchone()
                if row:
                    return row["id"], "duplicate"
                if max_queued and max_queued > 0:
                    queued = conn.execute(
                        "SELECT COUNT(*) AS c FROM jobs WHERE status = 'queued'"
                    ).fetchone()["c"]
                    if queued >= max_queued:
                        return None, "queue_full"
                try:
                    jid = conn.execute(
                        "INSERT INTO jobs (codebase_hash, job_type, status, payload, "
                        "attempts, created_at, updated_at) VALUES (%s, %s, 'queued', %s, 0, %s, %s) "
                        "RETURNING id",
                        (codebase_hash, job_type, json.dumps(payload), now, now),
                    ).fetchone()["id"]
                    conn.commit()
                    return jid, "submitted"
                except psycopg.errors.UniqueViolation:
                    conn.rollback()  # clear the aborted txn before re-querying
                    row = conn.execute(
                        "SELECT id FROM jobs WHERE codebase_hash = %s AND job_type = %s "
                        "AND status IN ('queued', 'running') LIMIT 1",
                        (codebase_hash, job_type),
                    ).fetchone()
                    return (row["id"] if row else None), "duplicate"
        except Exception as e:
            logger.error(f"Postgres enqueue_job failed for {codebase_hash}: {e}")
            return None, "error"

    def claim_next_job(self, job_type: str) -> Optional[Dict[str, Any]]:
        """Atomically claim the oldest queued job via FOR UPDATE SKIP LOCKED."""
        now = _now()
        try:
            with self._connect() as conn:
                if self.dsn.startswith("sqlite://"):
                    res = conn.execute(
                        "SELECT id FROM jobs WHERE status = 'queued' AND job_type = ? ORDER BY created_at LIMIT 1",
                        (job_type,),
                    ).fetchone()
                    if not res:
                        return None
                    jid = res["id"]
                    conn.execute("UPDATE jobs SET status = 'running', attempts = attempts + 1, updated_at = ? WHERE id = ?", (now, jid))
                    conn.commit()
                    row = conn.execute("SELECT id, codebase_hash, job_type, payload, attempts FROM jobs WHERE id = ?", (jid,)).fetchone()
                    if not row:
                        return None
                    job = dict(row)
                    job["payload"] = json.loads(job["payload"]) if job["payload"] else {}
                    return job

                row = conn.execute(
                    "UPDATE jobs SET status = 'running', attempts = attempts + 1, updated_at = %s "
                    "WHERE id = (SELECT id FROM jobs WHERE status = 'queued' AND job_type = %s "
                    "ORDER BY created_at FOR UPDATE SKIP LOCKED LIMIT 1) "
                    "RETURNING id, codebase_hash, job_type, payload, attempts",
                    (now, job_type),
                ).fetchone()
                conn.commit()
                if not row:
                    return None
                job = dict(row)
                job["payload"] = json.loads(job["payload"]) if job["payload"] else {}
                return job
        except Exception as e:
            logger.error(f"Postgres claim_next_job failed: {e}", exc_info=True)
            return None

    def complete_job(self, job_id: int, result: Optional[Any] = None) -> None:
        self._finish_job(job_id, "done", result=result)

    def fail_job(self, job_id: int, error: str) -> None:
        self._finish_job(job_id, "failed", error=error)

    def _finish_job(self, job_id: int, status: str, result: Any = None, error: str = None) -> None:
        try:
            with self._connect() as conn:
                conn.execute(
                    "UPDATE jobs SET status = %s, result = %s, error = %s, updated_at = %s WHERE id = %s",
                    (status, json.dumps(result) if result is not None else None, error, _now(), job_id),
                )
                conn.commit()
        except Exception as e:
            logger.error(f"Postgres finish job {job_id} failed: {e}")

    def get_job(self, job_id: int) -> Optional[Dict[str, Any]]:
        try:
            with self._connect() as conn:
                row = conn.execute("SELECT * FROM jobs WHERE id = %s", (job_id,)).fetchone()
                if not row:
                    logger.error(f"get_job: row for {job_id} is None")
                    return None
                job = dict(row)
                if job.get("payload"):
                    job["payload"] = json.loads(job["payload"])
                return job
        except Exception as e:
            logger.error(f"Postgres get_job {job_id} failed: {e}", exc_info=True)
            return None

    def has_active_job(self, codebase_hash: str, job_type: str = "generate_cpg") -> bool:
        """True if a queued or running job exists for this codebase.

        Used by get_cpg_status to tell a still-building/queued CPG apart from one
        whose worker died — only the latter (no active job + past deadline) is
        reconciled to FAILED. On error, return True (fail safe: don't condemn a
        build just because the DB hiccuped).
        """
        try:
            with self._connect() as conn:
                row = conn.execute(
                    "SELECT 1 FROM jobs WHERE codebase_hash = %s AND job_type = %s "
                    "AND status IN ('queued', 'running') LIMIT 1",
                    (codebase_hash, job_type),
                ).fetchone()
                return row is not None
        except Exception as e:
            logger.error(f"Postgres has_active_job failed for {codebase_hash}: {e}")
            return True

    def queue_position(self, codebase_hash: str, job_type: str = "generate_cpg") -> Optional[int]:
        """1-based position of this codebase's QUEUED job among all queued jobs.

        Returns None if the job isn't queued (running, done, or absent). Position
        is by created_at order, matching claim_next_job's ORDER BY.
        """
        try:
            with self._connect() as conn:
                row = conn.execute(
                    "SELECT created_at FROM jobs WHERE codebase_hash = %s AND job_type = %s "
                    "AND status = 'queued' LIMIT 1",
                    (codebase_hash, job_type),
                ).fetchone()
                if not row:
                    return None
                ahead = conn.execute(
                    "SELECT COUNT(*) AS c FROM jobs WHERE job_type = %s AND status = 'queued' "
                    "AND created_at < %s",
                    (job_type, row["created_at"]),
                ).fetchone()["c"]
                return ahead + 1
        except Exception as e:
            logger.error(f"Postgres queue_position failed for {codebase_hash}: {e}")
            return None

    def count_jobs(self, status: Optional[str] = None) -> int:
        try:
            with self._connect() as conn:
                if status:
                    row = conn.execute(
                        "SELECT COUNT(*) AS c FROM jobs WHERE status = %s", (status,)
                    ).fetchone()
                else:
                    row = conn.execute("SELECT COUNT(*) AS c FROM jobs").fetchone()
                return row["c"]
        except Exception as e:
            logger.error(f"Postgres count_jobs failed: {e}")
            return 0

    def requeue_running_jobs(self, max_retries: int = 3) -> int:
        try:
            with self._connect() as conn:
                running_jobs = conn.execute(
                    "SELECT id, codebase_hash, payload, attempts FROM jobs WHERE status = 'running'"
                ).fetchall()
                if not running_jobs:
                    return 0

                now = _now()
                requeued_count = 0

                for r in running_jobs:
                    job = dict(r)
                    jid = job["id"]
                    attempts = job.get("attempts", 0)
                    payload_raw = job.get("payload")
                    version_id = None
                    if payload_raw:
                        try:
                            payload_dict = json.loads(payload_raw) if isinstance(payload_raw, str) else payload_raw
                            if isinstance(payload_dict, dict):
                                version_id = payload_dict.get("version_id")
                        except Exception:
                            pass

                    if attempts >= max_retries:
                        err_msg = "EXCEEDED_MAX_RETRIES: Job exceeded maximum startup retry attempts"
                        conn.execute(
                            "UPDATE jobs SET status = 'failed', error = %s, updated_at = %s WHERE id = %s",
                            (err_msg, now, jid),
                        )
                        if version_id:
                            err_meta = json.dumps({"error": {"error_code": "EXCEEDED_MAX_RETRIES", "message": "Job exceeded maximum startup retry attempts"}})
                            conn.execute(
                                """
                                UPDATE project_versions
                                SET build_status = 'failed',
                                    build_metadata = %s,
                                    updated_at = %s
                                WHERE id = %s
                                """,
                                (err_meta, now, version_id),
                            )
                    else:
                        conn.execute(
                            "UPDATE jobs SET status = 'queued', attempts = attempts + 1, updated_at = %s WHERE id = %s",
                            (now, jid),
                        )
                        if version_id:
                            conn.execute(
                                "UPDATE project_versions SET build_status = 'queued', updated_at = %s WHERE id = %s",
                                (now, version_id),
                            )
                    requeued_count += 1

                conn.commit()
                return requeued_count
        except Exception as e:
            logger.error(f"Postgres requeue_running_jobs failed: {e}", exc_info=True)
            return 0
