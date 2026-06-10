"""
Main application entry point.
Handles initialization and command-line interface.
"""
import sys
import logging
import os
from typing import Optional
from config import config
from database import db_manager, cache_repository
from embeddings import embedding_manager
from api import app

# Configure logging
logger = logging.getLogger(__name__)


def embed_schemas_command(db_id: Optional[int] = None):
    """Command to embed database schemas for a single db_id"""
    if db_id is None:
        db_id = config.metabase.default_db_id

    logger.info(f"Beginning schema embedding process for db_id: {db_id}...")

    # Get schema types and tenant_id for this database from config
    matched_tenant_id = None
    tenant_config = None
    for tid, cfg in config.tenant_mappings.items():
        if cfg["db_id"] == db_id:
            matched_tenant_id = tid
            tenant_config = cfg
            break

    if tenant_config:
        schema_types = tenant_config.get("schema_types", ["public"])
    else:
        schema_types = ["public"]

    embedding_manager.embed_schemas(db_id, schema_types, tenant_id=matched_tenant_id)
    logger.info("Finished embedding process.")


def embed_all_tenants():
    """Embed database schemas for ALL tenants defined in tenant_config.json"""
    # Collect unique db_ids with their schema_types
    db_configs = {}
    for tenant_id, cfg in config.tenant_mappings.items():
        db_id = cfg["db_id"]
        if db_id not in db_configs:
            db_configs[db_id] = {
                "schema_types": cfg.get("schema_types", ["public"]),
                "tenants": []
            }
        db_configs[db_id]["tenants"].append(tenant_id)

    logger.info(f"Embedding schemas for {len(db_configs)} unique database(s): {list(db_configs.keys())}")

    # Embed schemas for each unique db_id
    for db_id, db_cfg in db_configs.items():
        tenants = ", ".join(db_cfg["tenants"])
        # Use the first tenant's API key for embedding this database
        first_tenant_id = db_cfg["tenants"][0] if db_cfg["tenants"] else None
        logger.info(f"Embedding db_id={db_id} (tenants: {tenants})...")
        try:
            embedding_manager.embed_schemas(db_id, db_cfg["schema_types"], tenant_id=first_tenant_id)
            logger.info(f"Successfully embedded db_id={db_id}")
        except Exception as e:
            logger.exception(f"Failed to embed db_id={db_id}: {e}")

    logger.info("Finished embedding all tenant databases.")

    # Evict stale cache entries (older than 30 days) after re-embedding
    try:
        deleted = cache_repository.evict_old(days=30)
        if deleted:
            logger.info(f"Evicted {deleted} stale semantic cache entries")
    except Exception as e:
        logger.warning(f"Cache eviction failed (non-fatal): {e}")


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
            db_id = int(sys.argv[2]) if len(sys.argv) > 2 else None
            embed_schemas_command(db_id)

        elif command == "embed-all":
            # Embed schemas for ALL tenants
            embed_all_tenants()

        elif command == "help":
            print("""
Metabase Reporter - Natural Language to SQL Application

Commands:
    python app.py                  - Run the Flask server
    python app.py embed [db_id]    - Embed database schemas for a single db_id
    python app.py embed-all        - Embed database schemas for ALL tenants
    python app.py g [db_id]        - Embed database schemas (alias for embed)
    python app.py help             - Show this help message
            """)

        else:
            print(f"Unknown command: {command}")
            print("Use 'python app.py help' for available commands")

    else:
        # No command specified, run the server
        run_server()


# Initialize database when module is loaded (for gunicorn with --preload)
# Use environment variable to ensure this only runs ONCE in parent process
_initialized = os.environ.get('_APP_INITIALIZED')

if not _initialized and os.environ.get('WERKZEUG_RUN_MAIN') != 'true':
    # Mark as initialized before doing anything to prevent race conditions
    os.environ['_APP_INITIALIZED'] = '1'

    try:
        logger.info("Initializing database schema...")
        db_manager.init_tables()
        logger.info("Database schema initialized successfully")
    except Exception as e:
        logger.exception(f"Error initializing database schema: {e}")
        # Don't exit - let the app try to run anyway

    # Embed schemas for ALL tenants on startup (runs once with --preload before forking workers)
    try:
        logger.info("Embedding database schemas for all tenants...")
        embed_all_tenants()
        logger.info("Schema embedding completed successfully for all tenants")
    except Exception as e:
        logger.warning(f"Schema embedding failed: {e}", exc_info=True)
        # Don't exit - app can still run without embeddings


if __name__ == "__main__":
    main()
