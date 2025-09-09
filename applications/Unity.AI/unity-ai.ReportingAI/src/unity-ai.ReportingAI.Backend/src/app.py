"""
Main application entry point.
Handles initialization and command-line interface.
"""
import sys
from config import config
from database import db_manager
from embeddings import embedding_manager
from api import app


def embed_schemas_command(db_id: int = None):
    """Command to embed database schemas"""
    if db_id is None:
        db_id = config.metabase.default_db_id
    
    print(f"Beginning schema embedding process for db_id: {db_id}...")
    
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
    print("Finished embedding process.")


def run_server():
    """Run the Flask development server"""
    print(f"Starting Flask app in {config.app.flask_env} mode with debug={config.app.debug}")
    db_manager.init_tables()
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

Environment Variables:
    FLASK_ENV          - Environment (development/production)
    DB_HOST            - PostgreSQL host
    DB_PORT            - PostgreSQL port
    DB_NAME            - Database name
    DB_USER            - Database user
    DB_PASSWORD        - Database password
    MB_EMBED_URL       - Metabase URL
    METABASE_KEY       - Metabase API key
    MB_EMBED_SECRET    - Metabase embed secret
    COMPLETION_ENDPOINT - LLM API endpoint
    COMPLETION_KEY     - LLM API key
            """)
        
        else:
            print(f"Unknown command: {command}")
            print("Use 'python app.py help' for available commands")
    
    else:
        # No command specified, run the server
        run_server()


if __name__ == "__main__":
    main()