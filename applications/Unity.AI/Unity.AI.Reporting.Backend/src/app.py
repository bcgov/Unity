"""
Main application entry point.
Handles initialization and command-line interface.
"""
import sys
import logging
import os
import threading
import ai_models
from config import config
from database import db_manager, cache_repository
from embeddings import embedding_manager
from tenant_registry import tenant_registry
from api import app

# Configure logging
logger = logging.getLogger(__name__)


def embed_schemas_command(db_id: int):
    """Command to embed database schemas for a single db_id"""
    logger.info(f"Beginning schema embedding process for db_id: {db_id}...")

    # Schema types, tenant and AI Setup's models for this database from the tenant registry
    entry = tenant_registry.find_by_db_id(db_id)
    if entry is None or entry.models is None:
        logger.error(
            f"db_id={db_id} has no registered tenant with AI Setup models; "
            "open AI Reporting for that tenant once so UGM sends them")
        return

    _embed_db(db_id, list(entry.schema_types), entry.models, entry.tenant_name)
    logger.info("Finished embedding process.")


def _embed_db(db_id: int, schema_types: list, models: ai_models.AIModels, tenant_id: str) -> None:
    """Embed one database with AI Setup's models. When its vectors were made with another
    embedding model, every vector is redone and its cached answers are dropped."""
    force = tenant_registry.get_embedded_with(db_id) != models.embedding_deployment
    with ai_models.use(models):
        if force:
            cache_repository.purge_db(db_id)
        if embedding_manager.embed_schemas(db_id, schema_types, tenant_id=tenant_id, force=force):
            tenant_registry.mark_embedded(db_id, models.embedding_deployment)


def embed_all_tenants() -> tuple[int, int]:
    """Embed database schemas for ALL tenants in the tenant registry.

    Returns (success_count, failure_count). Per-db failures are caught and
    logged so one bad tenant doesn't block the rest; the CronJob/CLI wrapper
    in main() uses the counts to set the exit code so a total failure is
    visible (Job pod shows failed) while a partial success stays exit 0.
    """
    # Collect unique db_ids with their schema_types and AI Setup models. A tenant registered
    # before AI Setup's models were sent has none: it is skipped until it opens AI Reporting.
    db_configs = {}
    skipped = []
    for entry in tenant_registry.list_all():
        if entry.models is None:
            skipped.append(entry.tenant_name)
            continue
        if entry.db_id not in db_configs:
            db_configs[entry.db_id] = {
                "schema_types": list(entry.schema_types),
                "models": entry.models,
                "tenants": []
            }
        db_configs[entry.db_id]["tenants"].append(entry.tenant_name)
    if skipped:
        logger.warning(f"Skipping tenants without AI Setup models (open AI Reporting once): {skipped}")

    logger.info(f"Embedding schemas for {len(db_configs)} unique database(s): {list(db_configs.keys())}")

    success_count = 0
    failure_count = 0

    # Embed schemas for each unique db_id
    for db_id, db_cfg in db_configs.items():
        tenants = ", ".join(db_cfg["tenants"])
        # The first tenant names this database in logs and Metabase calls
        first_tenant_id = db_cfg["tenants"][0] if db_cfg["tenants"] else None
        logger.info(f"Embedding db_id={db_id} (tenants: {tenants})...")
        try:
            _embed_db(db_id, db_cfg["schema_types"], db_cfg["models"], first_tenant_id)
            logger.info(f"Successfully embedded db_id={db_id}")
            success_count += 1
        except Exception as e:
            logger.exception(f"Failed to embed db_id={db_id}: {e}")
            failure_count += 1

    logger.info(
        f"Finished embedding all tenant databases "
        f"(success={success_count}, failure={failure_count})."
    )

    # Evict stale cache entries (older than 30 days) after re-embedding
    try:
        deleted = cache_repository.evict_old(days=30)
        if deleted:
            logger.info(f"Evicted {deleted} stale semantic cache entries")
    except Exception as e:
        logger.warning(f"Cache eviction failed (non-fatal): {e}")

    return success_count, failure_count


def run_server():
    """Run the Flask development server"""
    logger.info(f"Starting Flask app in {config.app.flask_env} mode with debug={config.app.debug}")

    app.run(
        host="0.0.0.0",
        port=5000,
        debug=config.app.debug,
        use_reloader=config.app.debug
    )


def main():
    """Main entry point for the application"""
    if len(sys.argv) > 1:
        command = sys.argv[1]

        if command == "embed" or command == "g":
            # Embed schemas for a single db_id
            if len(sys.argv) < 3:
                print("Usage: python app.py embed <db_id>")
                sys.exit(2)
            embed_schemas_command(int(sys.argv[2]))

        elif command == "embed-all":
            # Embed schemas for ALL tenants. Exit non-zero only when *every*
            # db failed so the CronJob surfaces a total failure as a failed
            # Job pod (partial success stays exit 0).
            success_count, failure_count = embed_all_tenants()
            if failure_count > 0 and success_count == 0:
                sys.exit(1)

        elif command == "help":
            print("""
Metabase Reporter - Natural Language to SQL Application

Commands:
    python app.py                  - Run the Flask server
    python app.py embed <db_id>    - Embed database schemas for a single db_id
    python app.py embed-all        - Embed database schemas for ALL tenants
    python app.py g <db_id>        - Embed database schemas (alias for embed)
    python app.py help             - Show this help message
            """)

        else:
            print(f"Unknown command: {command}")
            print("Use 'python app.py help' for available commands")

    else:
        # No command specified, run the server
        run_server()


# True when launched as `python app.py <cli-command>` rather than as the server.
# Under gunicorn sys.argv[1] is a gunicorn flag, so this stays False — the
# server still runs its startup seed-embed. CLI commands (the OpenShift
# re-embed CronJob included) run their own embed in main(), so the
# import-time embed is skipped to avoid double work.
_is_cli_command = (
    len(sys.argv) > 1
    and sys.argv[1] in {"embed", "g", "embed-all", "help"}
)

# True only for Werkzeug's reloader *watcher* process: `python app.py` with
# no args, debug/reload enabled, about to spawn the child that actually
# serves. Werkzeug sets WERKZEUG_RUN_MAIN=true in that child's environment —
# the watcher itself never has it set, and neither does a gunicorn worker
# (gunicorn imports this module directly as `app:app`, so __name__ is never
# "__main__" and no reloader is ever involved) or a plain `flask run`
# without reload. WERKZEUG_RUN_MAIN being unset can't by itself distinguish
# "gunicorn / no reload" from "the watcher" — both look identical — so this
# also requires __name__ == "__main__" and debug=True, which only the
# watcher path hits. The watcher must skip entirely: it never serves any
# requests, and because the child inherits the watcher's os.environ, having
# the watcher run this previously left the child (the one actually serving)
# thinking initialization was already done and skipping it.
_is_reloader_watcher = (
    not _is_cli_command
    and __name__ == "__main__"
    and config.app.debug
    and os.environ.get('WERKZEUG_RUN_MAIN') != 'true'
)

if not _is_reloader_watcher:
    try:
        logger.info("Initializing database schema...")
        db_manager.init_tables()
        logger.info("Database schema initialized successfully")
    except Exception as e:
        logger.exception(f"Error initializing database schema: {e}")
        # Don't exit - let the app try to run anyway

    # Startup seed-embed for the server only — first-deploy safety net before
    # the nightly CronJob's first run. CLI commands embed in main() themselves.
    #
    # Runs in a background thread instead of blocking here: this executes
    # before gunicorn workers start accepting connections, so blocking here
    # keeps /health and /ready from responding until the embed finishes —
    # and duration scales with the number of tenant databases. With more
    # than one tenant configured, that pushed startup past OpenShift's
    # liveness-probe grace period and put the pod in a permanent
    # CrashLoopBackOff. Backgrounding it lets the worker start serving
    # immediately.
    if not _is_cli_command:
        def _run_startup_seed_embed():
            try:
                # Only one worker should run the startup pass — each of
                # gunicorn's workers independently reaches this code (no
                # --preload, see entrypoint.sh), so without coordination all
                # of them race through embed_all_tenants() together. The
                # per-db_id lock inside embed_schemas only stops two workers
                # touching the *same* db_id at the *same* instant; it can't
                # stop a slower worker from redoing a db a faster worker
                # already finished and released the lock for. A single
                # pod-startup-scoped lock (reusing embed_lock's generic
                # advisory-lock mechanism with a sentinel key unrelated to
                # any real db_id/collection) makes exactly one worker the
                # leader for this pass; the rest skip it entirely.
                with db_manager.embed_lock(0, "__startup_seed_embed_leader__") as is_leader:
                    if not is_leader:
                        logger.info(
                            "Another worker is already running the startup "
                            "seed-embed; skipping"
                        )
                        return
                    logger.info("Embedding database schemas for all tenants...")
                    _, failure_count = embed_all_tenants()
                    if failure_count:
                        logger.warning("Startup schema embedding finished with %d failed database(s)", failure_count)
                    else:
                        logger.info("Schema embedding completed successfully for all tenants")
            except Exception as e:
                logger.warning(f"Schema embedding failed: {e}", exc_info=True)
                # Don't exit - app can still run without embeddings

        threading.Thread(
            target=_run_startup_seed_embed, daemon=True, name="startup-seed-embed"
        ).start()


if __name__ == "__main__":
    main()
