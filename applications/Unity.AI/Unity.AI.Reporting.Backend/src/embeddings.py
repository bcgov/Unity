"""
Embeddings module for managing vector storage and retrieval.
Handles schema embedding and similarity search for NL to SQL.
"""
import logging
import time
from typing import List, Optional
from langchain_core.documents import Document
from langchain_openai import OpenAIEmbeddings, AzureOpenAIEmbeddings
from langchain_postgres import PGVector
from config import config
from database import db_manager
from metabase import metabase_client

# Configure logging
logger = logging.getLogger(__name__)


class SchemaExtractor:
    """Extract and format database schemas for embedding"""
    
    def __init__(self, metabase_client):
        self.metabase = metabase_client
        # Configure which columns/tables to exclude
        self.junk_columns = {
            "CreatorId", "LastModificationTime", "LastModifierId",
            "ExtraProperties", "ConcurrencyStamp", "CreationTime",
            "CorrelationProvider"
        }
        self.junk_tables = {"ApplicationFormSubmissions", "__EFMigrationsHistory"}
    
    def get_column_example(self, is_text: bool, schema: str, table: str, 
                          column: str, db_id: int) -> Optional[str]:
        """Get an example value for a column"""
        sql = f'SELECT "{column}" FROM "{schema}"."{table}" WHERE "{column}" IS NOT null'
        if is_text:
            sql += f' and "{column}" <> \'\''
        
        try:
            result = self.metabase.execute_sql(sql, db_id)
            if result["rows"]:
                return str(result["rows"][0][0])
        except:
            pass
        return None
    
    def extract_schemas(self, db_id: int, schema_type: str = "public") -> List[str]:
        """
        Extract table schemas from database.
        
        Args:
            db_id: Database ID in Metabase
            schema_type: Type of schema ('public' or 'custom')
            
        Returns:
            List of formatted schema descriptions
        """
        metadata = self.metabase.get_database_metadata(db_id)
        docs = []
        
        for table in metadata["tables"]:
            # Filter tables based on schema type and exclusion rules
            if table["name"] in self.junk_tables:
                continue
            
            if schema_type == "public" and table["schema"] != "public":
                continue
            elif schema_type == "custom" and "Worksheet" not in table["name"]:
                continue
            
            # Extract non-junk columns
            columns = [
                f"{field['name']} ({field['base_type']})"
                for field in table["fields"]
                if field["name"] not in self.junk_columns
            ]
            
            # Check if table has data
            schema_name = "Reporting" if schema_type == "custom" else "public"
            sql = f'SELECT * FROM "{schema_name}"."{table["name"]}" LIMIT 1'
            
            try:
                result = self.metabase.execute_sql(sql, db_id)
                if result["rows"]:
                    # Build schema description with examples
                    page = f'# "{schema_name}"."{table["name"]}"'
                    
                    for col in columns:
                        col_name = col.split(' ')[0]
                        is_text = 'Text' in col
                        example = self.get_column_example(
                            is_text, schema_name, table["name"], col_name, db_id
                        )
                        if example:
                            truncated = example[:50] + '...' if len(example) > 50 else example
                            page += f"\n - {col}: '{truncated}'"
                    
                    docs.append(page)
                    logger.debug(f"Extracted schema for {table['name']}")
            except Exception as e:
                logger.error(f"Error processing table {table['name']}: {e}", exc_info=True)
        
        return docs


class EmbeddingManager:
    """Manages vector embeddings for schema similarity search"""

    def __init__(self):
        # Initialize embeddings model based on configuration
        if config.ai.use_azure:
            self.embedding_model = AzureOpenAIEmbeddings(
                azure_endpoint=config.ai.azure_endpoint,
                api_key=config.ai.azure_api_key,
                azure_deployment=config.ai.azure_embedding_deployment,
                api_version=config.ai.azure_api_version
            )
        else:
            self.embedding_model = OpenAIEmbeddings(
                model=config.ai.embedding_model,
                api_key=config.ai.completion_key
            )

        self.vector_store = PGVector(
            embeddings=self.embedding_model,
            collection_name=config.app.collection_name,
            connection=config.database.url,
            use_jsonb=True
        )
        self.schema_extractor = SchemaExtractor(metabase_client)

    def _reconnect_vector_store(self):
        """Recreate vector store connection"""
        logger.warning("Reconnecting to vector store due to connection error")
        self.vector_store = PGVector(
            embeddings=self.embedding_model,
            collection_name=config.app.collection_name,
            connection=config.database.url,
            use_jsonb=True
        )

    def _retry_on_connection_error(self, func, *args, **kwargs):
        """Retry a function if it fails with a connection error"""
        max_retries = 3
        for attempt in range(max_retries):
            try:
                return func(*args, **kwargs)
            except Exception as e:
                error_msg = str(e).lower()
                # Check if it's a connection error
                if any(keyword in error_msg for keyword in ['connection', 'terminating', 'closed']):
                    logger.warning(f"Connection error on attempt {attempt + 1}/{max_retries}: {e}")
                    if attempt < max_retries - 1:
                        self._reconnect_vector_store()
                        time.sleep(0.5 * (attempt + 1))  # Exponential backoff
                        continue
                raise
    
    def embed_schemas(self, db_id: int, schema_types: Optional[List[str]] = None):
        """
        Embed database schemas for a specific database.
        
        Args:
            db_id: Database ID to embed schemas for
            schema_types: List of schema types to embed (e.g., ['public', 'custom'])
        """
        # Default schema types if not specified
        if schema_types is None:
            schema_types = ['public']
            if config.app.embed_worksheets:
                schema_types.append('custom')
        
        # Purge existing embeddings for this db_id
        db_manager.purge_embeddings(db_id, config.app.collection_name)

        logger.info(f"Embedding schemas for db_id: {db_id}, types: {schema_types}")

        # Extract and embed schemas for each type
        for schema_type in schema_types:
            schemas = self.schema_extractor.extract_schemas(db_id, schema_type)

            # Create documents with metadata
            documents = [
                Document(
                    page_content=schema.strip(),
                    metadata={
                        "db_id": db_id,
                        "schema_type": schema_type
                    }
                )
                for schema in schemas
            ]

            if documents:
                self.vector_store.add_documents(documents)
                logger.info(f"Added {len(documents)} {schema_type} schema embeddings")
    
    def search_similar_schemas(self, query: str, db_id: int,
                             k_public: int = 4, k_custom: int = 4) -> List[Document]:
        """
        Search for similar schemas based on query with automatic retry on connection errors.

        Args:
            query: Natural language query
            db_id: Database ID to filter by
            k_public: Number of public schemas to retrieve
            k_custom: Number of custom schemas to retrieve

        Returns:
            List of similar schema documents
        """
        retrieved = []

        # Get public schemas with retry
        if k_public > 0:
            public_results = self._retry_on_connection_error(
                self.vector_store.similarity_search,
                query,
                k=k_public,
                filter={"db_id": db_id, "schema_type": "public"}
            )
            retrieved.extend(public_results)

        # Get custom schemas if enabled with retry
        if k_custom > 0 and config.app.embed_worksheets:
            custom_results = self._retry_on_connection_error(
                self.vector_store.similarity_search,
                query,
                k=k_custom,
                filter={"db_id": db_id, "schema_type": "custom"}
            )
            retrieved.extend(custom_results)

        return retrieved
    
    def get_formatted_schemas(self, query: str, db_id: int) -> str:
        """Get formatted schema text for prompt"""
        schemas = self.search_similar_schemas(query, db_id)
        return "\n".join(doc.page_content for doc in schemas)


# Global embedding manager instance
embedding_manager = EmbeddingManager()