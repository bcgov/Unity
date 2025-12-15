"""
Main application entry point.
Handles initialization and command-line interface.
"""
import sys
import logging
import os
from config import config
from database import db_manager
from embeddings import embedding_manager
from api import app

# Configure logging
logger = logging.getLogger(__name__)

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
        logger.error(f"Error initializing database schema: {e}", exc_info=True)
        # Don't exit - let the app try to run anyway

    # Embed schemas on startup (runs once with --preload before forking workers)
    try:
        logger.info("Embedding database schemas...")
        db_id = config.metabase.default_db_id
        schema_types = ["public"]

        embedding_manager.embed_schemas(db_id, schema_types)
        logger.info("Schema embedding completed successfully")
    except Exception as e:
        logger.warning(f"Schema embedding failed: {e}", exc_info=True)
        # Don't exit - app can still run without embeddings


def embed_schemas_command(db_id: int = None):
    """Command to embed database schemas"""
    if db_id is None:
        db_id = config.metabase.default_db_id

    logger.info(f"Beginning schema embedding process for db_id: {db_id}...")

    # Get schema types for this database from config
    tenant_config = None
    for tenant_id, cfg in config.tenant_mappings.items():
        if cfg["db_id"] == db_id:
            tenant_config = cfg
            break

    if tenant_config:
        schema_types = tenant_config.get("schema_types", ["public"])
    else:
        schema_types = ["public", "custom"] if config.app.embed_worksheets else ["public"]

    embedding_manager.embed_schemas(db_id, schema_types)
    logger.info("Finished embedding process.")


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
            # Embed schemas command
            db_id = int(sys.argv[2]) if len(sys.argv) > 2 else None
            embed_schemas_command(db_id)
        
        elif command == "help":
            print("""
Metabase Reporter - Natural Language to SQL Application

Commands:
    python app.py                  - Run the Flask server
    python app.py embed [db_id]    - Embed database schemas
    python app.py g [db_id]        - Embed database schemas (alias)
    python app.py help             - Show this help message
            """)
        
        else:
            print(f"Unknown command: {command}")
            print("Use 'python app.py help' for available commands")
    
    else:
        # No command specified, run the server
        run_server()


if __name__ == "__main__":
    main()