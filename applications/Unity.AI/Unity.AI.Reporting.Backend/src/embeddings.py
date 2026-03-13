"""
Embeddings module for managing vector storage and retrieval.
Handles schema embedding and similarity search for NL to SQL.
"""
import logging
import time
from typing import List, Optional
from langchain_core.documents import Document
from langchain_openai import OpenAIEmbeddings, AzureOpenAIEmbeddings
from pydantic import SecretStr
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
    
    def get_view_metadata(self, view_name: str, db_id: int,
                         tenant_id: Optional[str] = None) -> dict:
        """Returns {column_name: {label, forms_type}} from ReportColumnsMaps for a view."""
        sql = f"""
        SELECT
            row_data->>'ColumnName' AS column_name,
            row_data->>'Label'      AS label,
            row_data->>'Type'       AS forms_type
        FROM "Reporting"."ReportColumnsMaps" rcm,
             jsonb_array_elements(rcm."Mapping"->'Rows') AS row_data
        WHERE rcm."ViewName" = '{view_name}'
        """
        try:
            result = self.metabase.execute_sql(sql, db_id, tenant_id=tenant_id)
            return {
                row[0]: {"label": row[1], "forms_type": row[2]}
                for row in result["rows"]
                if row[0]
            }
        except Exception as e:
            logger.warning(f"Could not fetch metadata for view {view_name}: {e}")
            return {}

    def get_custom_field_labels(self, db_id: int,
                               tenant_id: Optional[str] = None) -> dict:
        """Returns {key: label} from Flex.CustomFields as fallback for old views."""
        sql = 'SELECT "Key", "Label" FROM "Flex"."CustomFields" WHERE "Key" IS NOT NULL'
        try:
            result = self.metabase.execute_sql(sql, db_id, tenant_id=tenant_id)
            return {row[0]: row[1] for row in result["rows"] if row[0]}
        except Exception as e:
            logger.warning(f"Could not fetch custom field labels: {e}")
            return {}

    def get_column_example(self, is_text: bool, schema: str, table: str,
                          column: str, db_id: int,
                          tenant_id: Optional[str] = None) -> Optional[str]:
        """Get an example value for a column"""
        sql = f'SELECT "{column}" FROM "{schema}"."{table}" WHERE "{column}" IS NOT null'
        if is_text:
            sql += f' and "{column}" <> \'\''

        try:
            result = self.metabase.execute_sql(sql, db_id, tenant_id=tenant_id)
            if result["rows"]:
                return str(result["rows"][0][0])
        except (KeyError, IndexError, TypeError):
            pass  # No example value available for this column
        return None
    
    def _should_skip_table(self, table: dict, schema_type: str) -> bool:
        """Check if a table should be excluded based on schema type and exclusion rules."""
        if table["name"] in self.junk_tables:
            return True
        if schema_type == "public" and table["schema"] != "public":
            return True
        if schema_type == "public" and (
            "scoresheet" in table["name"].lower() or "worksheet" in table["name"].lower()
        ):
            return True
        if schema_type == "custom" and (
            ("worksheet" not in table["name"].lower() and "scoresheet" not in table["name"].lower())
            or table["schema"] != "Reporting"
        ):
            return True
        return False

    def _has_data(self, schema_name: str, table_name: str, db_id: int,
                  tenant_id: Optional[str] = None) -> bool:
        """Check if a table has at least one row of data."""
        sql = f'SELECT * FROM "{schema_name}"."{table_name}" LIMIT 1'
        result = self.metabase.execute_sql(sql, db_id, tenant_id=tenant_id)
        return bool(result["rows"])

    def _format_schema_with_examples(self, schema_name: str, table_name: str,
                                     columns: List[str], db_id: int,
                                     tenant_id: Optional[str] = None,
                                     view_metadata: Optional[dict] = None,
                                     custom_labels: Optional[dict] = None) -> str:
        """Build a schema description string with example values for each column."""
        page = f'# "{schema_name}"."{table_name}"'
        meta = view_metadata or {}
        fallback = custom_labels or {}
        for col in columns:
            col_name = col.split(' ')[0]
            is_text = 'Text' in col
            example = self.get_column_example(is_text, schema_name, table_name, col_name, db_id,
                                              tenant_id=tenant_id)
            col_meta = meta.get(col_name, {})
            label = col_meta.get("label") or fallback.get(col_name, "")
            forms_type = col_meta.get("forms_type", "")

            line = f"\n - {col}"
            if label:
                line += f" | {label}"
            if forms_type and forms_type not in ("textfield", "textarea"):
                line += f" ({forms_type})"
            if example:
                truncated = example[:50] + '...' if len(example) > 50 else example
                line += f": '{truncated}'"
            page += line
        return page

    def extract_schemas(self, db_id: int, schema_type: str = "public",
                        tenant_id: Optional[str] = None) -> List[str]:
        """
        Extract table schemas from database.

        Args:
            db_id: Database ID in Metabase
            schema_type: Type of schema ('public' or 'custom')
            tenant_id: Optional tenant ID for tenant-specific Metabase API key

        Returns:
            List of formatted schema descriptions
        """
        metadata = self.metabase.get_database_metadata(db_id, tenant_id=tenant_id)
        docs = []
        schema_name = "Reporting" if schema_type == "custom" else "public"

        # Fetch custom field labels once as fallback for old views without ReportColumnsMaps records
        custom_labels = {}
        if schema_type == "custom":
            custom_labels = self.get_custom_field_labels(db_id, tenant_id=tenant_id)

        for table in metadata["tables"]:
            # Filter tables based on schema type and exclusion rules
            if self._should_skip_table(table, schema_type):
                continue

            # Extract non-junk columns
            columns = [
                f"{field['name']} ({field['base_type']})"
                for field in table["fields"]
                if field["name"] not in self.junk_columns
            ]

            try:
                # Check if table has data
                if not self._has_data(schema_name, table["name"], db_id, tenant_id=tenant_id):
                    continue
                # Fetch column labels and forms types for worksheet views
                view_metadata = {}
                if schema_type == "custom":
                    view_metadata = self.get_view_metadata(
                        table["name"], db_id, tenant_id=tenant_id
                    )
                # Build schema description with examples
                page = self._format_schema_with_examples(schema_name, table["name"], columns, db_id,
                                                         tenant_id=tenant_id,
                                                         view_metadata=view_metadata,
                                                         custom_labels=custom_labels)
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
                api_key=SecretStr(config.ai.azure_api_key),
                azure_deployment=config.ai.azure_embedding_deployment,
                api_version=config.ai.azure_api_version
            )
        else:
            self.embedding_model = OpenAIEmbeddings(
                model=config.ai.embedding_model,
                api_key=SecretStr(config.ai.completion_key)
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
        return None

    def embed_schemas(self, db_id: int, schema_types: Optional[List[str]] = None,
                      tenant_id: Optional[str] = None):
        """
        Embed database schemas for a specific database.

        Args:
            db_id: Database ID to embed schemas for
            schema_types: List of schema types to embed (e.g., ['public', 'custom'])
            tenant_id: Optional tenant ID for tenant-specific Metabase API key
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
            schemas = self.schema_extractor.extract_schemas(db_id, schema_type, tenant_id=tenant_id)

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
    
    def _get_all_custom_schemas(self, query: str, db_id: int) -> List[Document]:
        """Retrieve ALL embedded custom/worksheet schemas for a db_id.

        Uses a high k cap instead of top-k similarity — worksheet counts per
        tenant are small (< 20) and we must never miss the relevant one.
        Empty worksheets are already excluded at embed time via _has_data.
        """
        return self._retry_on_connection_error(
            self.vector_store.similarity_search,
            query,
            k=200,
            filter={"db_id": db_id, "schema_type": "custom"}
        ) or []

    def search_similar_schemas(self, query: str, db_id: int,
                             k_public: int = 4) -> List[Document]:
        """
        Search for similar schemas based on query with automatic retry on connection errors.

        Args:
            query: Natural language query
            db_id: Database ID to filter by
            k_public: Number of public schemas to retrieve

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
            if public_results:
                retrieved.extend(public_results)

        # Get ALL custom/worksheet schemas — don't rely on top-k similarity
        if config.app.embed_worksheets:
            custom_results = self._get_all_custom_schemas(query, db_id)
            if custom_results:
                retrieved.extend(custom_results)

        return retrieved
    
    def get_formatted_schemas(self, query: str, db_id: int) -> str:
        """Get formatted schema text for prompt, grouped by section with headers."""
        schemas = self.search_similar_schemas(query, db_id)

        section_headers = {
            "public": "=== PUBLIC TABLES ===",
            "worksheet": "=== WORKSHEET VIEWS ===",
            "scoresheet": "=== SCORESHEET VIEWS ===",
        }

        sections: dict[str, list[str]] = {}
        for doc in schemas:
            schema_type = doc.metadata.get("schema_type", "public")
            if schema_type == "custom":
                first_line = doc.page_content.split('\n')[0].lower()
                key = "scoresheet" if "scoresheet" in first_line else "worksheet"
            else:
                key = schema_type
            sections.setdefault(key, []).append(doc.page_content)

        parts = []
        for stype in ("public", "worksheet", "scoresheet"):
            if stype in sections:
                parts.append(section_headers[stype])
                parts.extend(sections[stype])

        return "\n".join(parts)


# Global embedding manager instance
embedding_manager = EmbeddingManager()