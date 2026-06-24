"""
Embeddings module for managing vector storage and retrieval.
Handles schema embedding and similarity search for NL to SQL.
"""
import hashlib
import json
import logging
import time
from typing import List, Optional, Tuple
from langchain_core.documents import Document
from langchain_openai import AzureOpenAIEmbeddings
from pydantic import SecretStr
from langchain_postgres import PGVector
from config import config, DEFAULT_TENANT
from database import db_manager, cache_repository
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
            "CorrelationProvider", "AIScoresheetAnswers", "AIAnalysis"
        }
        self.junk_tables = {"ApplicationFormSubmissions", "__EFMigrationsHistory"}
        # Set by helpers when a silent exception is swallowed during extraction.
        # extract_schemas resets and checks this so we never advance the schema
        # fingerprint based on a glitchy Metabase fetch (which would cause a
        # spurious cache purge).
        self._extraction_had_error = False
    
    def get_all_custom_views(self, db_id: int,
                             tenant_id: Optional[str] = None) -> List[dict]:
        """
        Returns all custom views from ReportColumnsMaps as the embedding entry point.
        Each entry: {view_name, correlation_type, description, columns: {col: {label, forms_type}}}
        """
        correlation_types = (
            "'worksheet','worksheet_consolidated','scoresheet'"
        )
        sql = f"""
        SELECT "ViewName", "CorrelationProvider", "Mapping"
        FROM "Reporting"."ReportColumnsMaps"
        WHERE "CorrelationProvider" IN ({correlation_types})
        """
        try:
            result = self.metabase.execute_sql(sql, db_id, tenant_id=tenant_id)
        except Exception as e:
            logger.warning(f"Could not fetch custom views from ReportColumnsMaps: {e}")
            self._extraction_had_error = True
            return []

        views = []
        for row in result["rows"]:
            view_name, correlation_type, mapping = row[0], row[1], row[2]
            if not view_name:
                continue
            if isinstance(mapping, str):
                try:
                    mapping = json.loads(mapping)
                except (ValueError, TypeError):
                    logger.warning(f"Could not parse Mapping JSON for view {view_name}")
                    continue
            if not isinstance(mapping, dict):
                continue
            metadata = mapping.get("Metadata") or {}
            description = (metadata.get("Description") or "").strip()
            columns = {
                r["ColumnName"]: {"label": r.get("Label", ""), "forms_type": r.get("Type", "")}
                for r in (mapping.get("Rows") or [])
                if r.get("ColumnName")
            }
            views.append({
                "view_name": view_name,
                "correlation_type": correlation_type,
                "description": description,
                "columns": columns,
            })
        return views

    def get_custom_field_labels(self, db_id: int,
                               tenant_id: Optional[str] = None) -> dict:
        """Returns {key: label} from Flex.CustomFields as fallback for old views."""
        sql = 'SELECT "Key", "Label" FROM "Flex"."CustomFields" WHERE "Key" IS NOT NULL'
        try:
            result = self.metabase.execute_sql(sql, db_id, tenant_id=tenant_id)
            return {row[0]: row[1] for row in result["rows"] if row[0]}
        except Exception as e:
            logger.warning(f"Could not fetch custom field labels: {e}")
            self._extraction_had_error = True
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
        except Exception:
            pass  # No example value available for this column
        return None
    
    def _is_junk_column(self, field_name: str) -> bool:
        """True if a column should be excluded from embedding.

        Matches both top-level junk columns and Metabase's auto-unfolded nested
        JSON fields, which arrive named like
        'AIScoresheetAnswers → <uuid> → citation'. Exact-name matching alone
        misses these nested expansions (so the embedded schema fills up with AI
        result blobs), so we also check the root segment before the first ' → '.
        """
        if field_name in self.junk_columns:
            return True
        root = field_name.split("→", 1)[0].strip()
        return root in self.junk_columns

    def _should_skip_table(self, table: dict) -> bool:
        """Check if a public table should be excluded based on exclusion rules."""
        if table["name"] in self.junk_tables:
            return True
        if table["schema"] != "public":
            return True
        if "scoresheet" in table["name"].lower() or "worksheet" in table["name"].lower():
            return True
        return False

    def _has_data(self, schema_name: str, table_name: str, db_id: int,
                  tenant_id: Optional[str] = None) -> bool:
        """Check if a table has at least one row of data."""
        sql = f'SELECT * FROM "{schema_name}"."{table_name}" LIMIT 1'
        try:
            result = self.metabase.execute_sql(sql, db_id, tenant_id=tenant_id)
            return bool(result["rows"])
        except Exception:
            # A real "no rows" returns [] above; reaching here means the query
            # itself failed — mark so the schema fingerprint isn't advanced
            # based on a transiently dropped table.
            self._extraction_had_error = True
            return False

    def _format_column_line(self, col: str, schema_name: str, table_name: str,
                            db_id: int, meta: dict, fallback: dict,
                            tenant_id: Optional[str] = None) -> str:
        col_name = col.split(' ')[0]
        col_meta = meta.get(col_name, {})
        label = col_meta.get("label") or fallback.get(col_name, "")
        forms_type = col_meta.get("forms_type", "")
        is_text = 'Text' in col or forms_type in ("textfield", "textarea")
        example = self.get_column_example(is_text, schema_name, table_name, col_name, db_id,
                                          tenant_id=tenant_id)

        line = f"\n - {col}"
        if label:
            line += f" | {label}"
        if forms_type and forms_type not in ("textfield", "textarea"):
            line += f" ({forms_type})"
        if example:
            truncated = example[:50] + '...' if len(example) > 50 else example
            line += f": '{truncated}'"
        return line

    def _format_schema_with_examples(self, schema_name: str, table_name: str,
                                     columns: List[str], db_id: int,
                                     tenant_id: Optional[str] = None,
                                     view_metadata: Optional[dict] = None,
                                     custom_labels: Optional[dict] = None,
                                     description: str = "") -> str:
        """Build a schema description string with example values for each column."""
        page = f'# "{schema_name}"."{table_name}"'
        if description:
            page += f"\nDescription: {description}"
        meta = view_metadata or {}
        fallback = custom_labels or {}
        for col in columns:
            page += self._format_column_line(
                col, schema_name, table_name, db_id, meta, fallback,
                tenant_id=tenant_id,
            )
        return page

    def _signature_line(self, schema_name: str, table_name: str,
                        columns: List[str],
                        view_metadata: Optional[dict] = None) -> str:
        """Structural signature for one table/view, used for the schema
        fingerprint that drives cache invalidation. Sample values are
        deliberately excluded so row-data churn never shifts it; sorting keeps
        the line stable regardless of Metabase's response order."""
        sig_cols = ",".join(sorted(columns))
        meta = view_metadata or {}
        sig_meta = ";".join(
            f"{k}={(v.get('label') or '')}|{(v.get('forms_type') or '')}"
            for k, v in sorted(meta.items())
        )
        return f"{schema_name}.{table_name}|{sig_cols}|{sig_meta}"

    def extract_schemas(self, db_id: int, schema_type: str = "public",
                        tenant_id: Optional[str] = None) -> Tuple[List[dict], List[str], bool]:
        """
        Extract table schemas from database.

        Args:
            db_id: Database ID in Metabase
            schema_type: Type of schema ('public' or 'custom')
            tenant_id: Optional tenant ID for tenant-specific Metabase API key

        Returns:
            Tuple of (docs, sig_parts, extraction_ok):
              - docs: list of dicts {page_content, correlation_type} for embedding
              - sig_parts: structural signature lines (no sample values) for the
                schema fingerprint — one per embedded table/view
              - extraction_ok: False if any silent error was swallowed during the
                run; callers must NOT advance the schema fingerprint when False
        """
        # Reset the per-run error flag — helpers set it on swallowed exceptions.
        self._extraction_had_error = False

        # Custom (worksheet/scoresheet) schemas use a separate ReportColumnsMaps
        # path; derive sig_parts from the per-doc signatures it produces.
        if schema_type == "custom":
            docs = self._extract_custom_schemas(db_id, tenant_id=tenant_id)
            return docs, [d["signature"] for d in docs], not self._extraction_had_error

        # Public schema: use Metabase metadata as before
        metadata = self.metabase.get_database_metadata(db_id, tenant_id=tenant_id)
        docs = []
        for table in metadata["tables"]:
            if self._should_skip_table(table):
                continue
            columns = [
                f"{field['name']} ({field['base_type']})"
                for field in table["fields"]
                if not self._is_junk_column(field["name"])
            ]
            try:
                if not self._has_data("public", table["name"], db_id, tenant_id=tenant_id):
                    continue
                page = self._format_schema_with_examples(
                    "public", table["name"], columns, db_id, tenant_id=tenant_id
                )
                docs.append({
                    "page_content": page,
                    "correlation_type": "public",
                    "signature": self._signature_line("public", table["name"], columns),
                })
                logger.debug(f"Extracted schema for {table['name']}")
            except Exception as e:
                logger.exception(f"Error processing table {table['name']}: {e}")
                # Per-table failure leaves the signature incomplete — skip the
                # fingerprint update for this run.
                self._extraction_had_error = True
        return docs, [d["signature"] for d in docs], not self._extraction_had_error

    def extract_signatures(self, db_id: int, schema_type: str = "public",
                           tenant_id: Optional[str] = None) -> Tuple[List[str], bool]:
        """Cheap structural-signature pass for the fingerprint-first fast-path.

        Mirrors extract_schemas but skips per-column sample-value queries and
        doc construction — only does what's needed to compute the schema
        fingerprint. On a quiet night this lets embed_schemas skip the entire
        Azure-embedding + DB-write workload when nothing changed.

        Returns (sig_parts, extraction_ok). extraction_ok=False if any silent
        error was swallowed — callers must NOT treat the resulting fingerprint
        as authoritative.
        """
        self._extraction_had_error = False

        if schema_type == "custom":
            return self._extract_custom_signatures(db_id, tenant_id=tenant_id)

        sig_parts: List[str] = []
        metadata = self.metabase.get_database_metadata(db_id, tenant_id=tenant_id)
        for table in metadata["tables"]:
            if self._should_skip_table(table):
                continue
            columns = [
                f"{field['name']} ({field['base_type']})"
                for field in table["fields"]
                if not self._is_junk_column(field["name"])
            ]
            try:
                if not self._has_data("public", table["name"], db_id, tenant_id=tenant_id):
                    continue
                sig_parts.append(self._signature_line("public", table["name"], columns))
            except Exception as e:
                logger.exception(f"Error reading signature for table {table['name']}: {e}")
                self._extraction_had_error = True
        return sig_parts, not self._extraction_had_error

    def _get_legacy_custom_tables(self, db_id: int,
                                  tenant_id: Optional[str] = None) -> List[dict]:
        """Return Metabase metadata tables in the Reporting schema whose name
        contains 'worksheet' or 'scoresheet' (legacy auto-generated views)."""
        metadata = self.metabase.get_database_metadata(db_id, tenant_id=tenant_id)
        tables = []
        for table in metadata["tables"]:
            if table["schema"] != "Reporting":
                continue
            name = table["name"].lower()
            if "worksheet" in name or "scoresheet" in name:
                tables.append(table)
        return tables

    def _build_custom_doc(self, db_id: int, view_name: str, correlation_type: str,
                          columns: List[str], view_metadata: Optional[dict],
                          custom_labels: dict, description: str,
                          tenant_id: Optional[str] = None) -> Optional[dict]:
        """Build one embedding doc for a Reporting view, skipping empty views."""
        try:
            if not self._has_data("Reporting", view_name, db_id, tenant_id=tenant_id):
                return None
            page = self._format_schema_with_examples(
                "Reporting", view_name, columns, db_id,
                tenant_id=tenant_id,
                view_metadata=view_metadata,
                custom_labels=custom_labels,
                description=description,
            )
            logger.debug(f"Extracted schema for {view_name} ({correlation_type})")
            return {
                "page_content": page,
                "correlation_type": correlation_type,
                "signature": self._signature_line(
                    "Reporting", view_name, columns, view_metadata
                ),
            }
        except Exception as e:
            logger.exception(f"Error processing view {view_name}: {e}")
            self._extraction_had_error = True
            return None

    def _extract_custom_schemas(self, db_id: int,
                                tenant_id: Optional[str] = None) -> List[dict]:
        """Extract worksheet/scoresheet schemas.

        Primary source is ReportColumnsMaps (richest metadata). The legacy
        name-prefix sweep over Metabase metadata is a supplement that covers
        legacy-generated views not yet registered in ReportColumnsMaps. Views
        are de-duplicated by name, preferring the ReportColumnsMaps entry.
        """
        custom_labels = self.get_custom_field_labels(db_id, tenant_id=tenant_id)
        views = self.get_all_custom_views(db_id, tenant_id=tenant_id)
        covered = {view["view_name"] for view in views}
        docs = []

        # Primary: ReportColumnsMaps views
        for view in views:
            view_metadata = view["columns"]
            columns = [
                col_name
                for col_name in view_metadata.keys()
                if not self._is_junk_column(col_name)
            ]
            doc = self._build_custom_doc(
                db_id, view["view_name"], view["correlation_type"], columns,
                view_metadata=view_metadata, custom_labels=custom_labels,
                description=view["description"], tenant_id=tenant_id,
            )
            if doc:
                docs.append(doc)

        # Supplement: legacy name-prefix sweep for views not in ReportColumnsMaps
        for table in self._get_legacy_custom_tables(db_id, tenant_id=tenant_id):
            view_name = table["name"]
            if view_name in covered:
                continue
            correlation_type = (
                "scoresheet" if "scoresheet" in view_name.lower() else "worksheet"
            )
            columns = [
                f"{field['name']} ({field['base_type']})"
                for field in table["fields"]
                if not self._is_junk_column(field["name"])
            ]
            doc = self._build_custom_doc(
                db_id, view_name, correlation_type, columns,
                view_metadata=None, custom_labels=custom_labels,
                description="", tenant_id=tenant_id,
            )
            if doc:
                docs.append(doc)

        return docs

    def _extract_custom_signatures(self, db_id: int,
                                   tenant_id: Optional[str] = None) -> Tuple[List[str], bool]:
        """Cheap signature pass for custom (worksheet/scoresheet) schemas.

        Mirrors _extract_custom_schemas but skips _format_schema_with_examples
        (the per-column sample-value queries) — only enough to build the
        structural signature line per view. Note: unlike the full pass, this
        does not fetch Flex.CustomFields labels — they feed embedded document
        text, not the structural signature, so they're irrelevant here.
        """
        views = self.get_all_custom_views(db_id, tenant_id=tenant_id)
        covered = {view["view_name"] for view in views}
        sig_parts: List[str] = []

        # Primary: ReportColumnsMaps views
        for view in views:
            view_metadata = view["columns"]
            columns = [
                col_name
                for col_name in view_metadata.keys()
                if not self._is_junk_column(col_name)
            ]
            try:
                if not self._has_data("Reporting", view["view_name"], db_id, tenant_id=tenant_id):
                    continue
                sig_parts.append(
                    self._signature_line("Reporting", view["view_name"], columns, view_metadata)
                )
            except Exception as e:
                logger.exception(f"Error reading signature for view {view['view_name']}: {e}")
                self._extraction_had_error = True

        # Supplement: legacy name-prefix sweep
        for table in self._get_legacy_custom_tables(db_id, tenant_id=tenant_id):
            view_name = table["name"]
            if view_name in covered:
                continue
            columns = [
                f"{field['name']} ({field['base_type']})"
                for field in table["fields"]
                if not self._is_junk_column(field["name"])
            ]
            try:
                if not self._has_data("Reporting", view_name, db_id, tenant_id=tenant_id):
                    continue
                sig_parts.append(self._signature_line("Reporting", view_name, columns))
            except Exception as e:
                logger.exception(f"Error reading signature for legacy view {view_name}: {e}")
                self._extraction_had_error = True

        return sig_parts, not self._extraction_had_error


class EmbeddingManager:
    """Manages vector embeddings for schema similarity search"""

    def __init__(self):
        self.embedding_model = AzureOpenAIEmbeddings(
            azure_endpoint=config.ai.azure_endpoint,
            api_key=SecretStr(config.ai.azure_api_key),
            azure_deployment=config.ai.azure_embedding_deployment,
            api_version=config.ai.azure_api_version
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

        Flow:
          1. Cheap signature pass — compute the structural fingerprint without
             per-column sample queries or Azure embedding calls. If it matches
             the stored fingerprint, return early (no DB writes, no API spend).
          2. Otherwise, full extract → add new embeddings → delete the old ones
             captured up-front. This add-then-delete swap means a live query
             during the refresh always sees a complete embedding set (worst
             case: old + new superset), never the empty window the old
             delete-then-add path created.
          3. Update the schema fingerprint, which conditionally invalidates the
             semantic query_cache when the structural fingerprint changed.

        Args:
            db_id: Database ID to embed schemas for
            schema_types: List of schema types to embed (e.g., ['public', 'custom'])
            tenant_id: Optional tenant ID for tenant-specific Metabase API key
        """
        if schema_types is None:
            schema_types = ['public']

        collection_name = config.app.collection_name

        # --- Phase 1: cheap fingerprint check ---
        if self._schema_unchanged(db_id, schema_types, collection_name,
                                  tenant_id=tenant_id):
            return

        # --- Phase 2: full extract → add-then-delete swap ---
        logger.info(f"Embedding schemas for db_id: {db_id}, types: {schema_types}")

        # Serialize the swap per (db_id, collection). The capture-then-delete
        # below is not safe under concurrent runs (overlapping CronJob + manual
        # embed, etc.): each would capture the same old_ids and leave the
        # other's fresh rows behind as duplicates. Skip if another run owns it.
        with db_manager.embed_lock(db_id, collection_name) as acquired:
            if not acquired:
                logger.info(
                    f"Another embed for db_id={db_id} is already in flight; "
                    f"skipping this concurrent run"
                )
                return

            # Capture the existing row set BEFORE adding fresh embeddings so we
            # can delete them after the new rows are committed (atomic-ish swap).
            old_ids = db_manager.get_embedding_ids(db_id, collection_name)

            all_documents, all_sig_parts, all_extractions_ok = self._extract_documents(
                db_id, schema_types, tenant_id=tenant_id
            )

            # Bail out without touching the DB on partial extraction or no docs —
            # leaves the existing embeddings + cache intact for the next run.
            if not all_extractions_ok:
                logger.warning(
                    f"Skipping embed for db_id={db_id} due to silent extraction "
                    f"error(s); existing embeddings + cache retained, will retry next run"
                )
                return
            if not all_documents:
                logger.warning(
                    f"No documents extracted for db_id={db_id}; "
                    f"existing embeddings left untouched"
                )
                return

            # Add new first, then delete old. Between these the running app sees a
            # superset (old + new) — safe. If add_documents fails, old rows remain
            # and the next run retries (no destructive failure mode).
            self.vector_store.add_documents(all_documents)
            logger.info(f"Added {len(all_documents)} embeddings for db_id={db_id}")

            if old_ids:
                db_manager.purge_embeddings_by_ids(old_ids, collection_name)

            # Conditional cache invalidation — purges query_cache only on real change.
            fingerprint = hashlib.sha256(
                "\n".join(sorted(all_sig_parts)).encode("utf-8")
            ).hexdigest()[:16]
            cache_repository.update_schema_fingerprint(db_id, collection_name, fingerprint)

    def _schema_unchanged(self, db_id: int, schema_types: List[str],
                          collection_name: str,
                          tenant_id: Optional[str] = None) -> bool:
        """Cheap structural-fingerprint pass for the embed fast-path.

        Computes the schema fingerprint without per-column sample queries or
        Azure embedding calls. Returns True when it matches the stored
        fingerprint, letting the caller skip the full embed (no DB writes, no
        API spend).

        Only trusts the fast-path fingerprint when the cheap pass had no
        swallowed errors AND produced something. An empty signature would let a
        transiently broken Metabase look like "schema unchanged" if a prior
        empty run had stored the same empty hash.
        """
        cheap_sig_parts: List[str] = []
        cheap_ok = True
        for schema_type in schema_types:
            sig_parts, extraction_ok = self.schema_extractor.extract_signatures(
                db_id, schema_type, tenant_id=tenant_id
            )
            cheap_sig_parts.extend(sig_parts)
            if not extraction_ok:
                cheap_ok = False

        if not (cheap_ok and cheap_sig_parts):
            return False

        fingerprint = hashlib.sha256(
            "\n".join(sorted(cheap_sig_parts)).encode("utf-8")
        ).hexdigest()[:16]
        stored = cache_repository.get_schema_fingerprint(db_id, collection_name)
        if stored == fingerprint:
            logger.info(
                f"Schema unchanged for db_id={db_id} "
                f"(fingerprint={fingerprint}); skipping embed"
            )
            return True
        return False

    def _extract_documents(self, db_id: int, schema_types: List[str],
                           tenant_id: Optional[str] = None
                           ) -> Tuple[List[Document], List[str], bool]:
        """Full schema extract → embedding Documents + signature parts.

        Returns (documents, signature_parts, all_extractions_ok). A False
        all_extractions_ok flags a silently-swallowed extraction error so the
        caller can skip the destructive swap and retry on the next run.
        """
        all_documents: List[Document] = []
        all_sig_parts: List[str] = []
        all_extractions_ok = True

        for schema_type in schema_types:
            schemas, sig_parts, extraction_ok = self.schema_extractor.extract_schemas(
                db_id, schema_type, tenant_id=tenant_id
            )
            all_sig_parts.extend(sig_parts)
            if not extraction_ok:
                all_extractions_ok = False
            for schema in schemas:
                all_documents.append(
                    Document(
                        page_content=schema["page_content"].strip(),
                        metadata={
                            "db_id": db_id,
                            "schema_type": "custom" if schema["correlation_type"] != "public" else "public",
                            "correlation_type": schema["correlation_type"],
                        }
                    )
                )

        return all_documents, all_sig_parts, all_extractions_ok
    
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
                             k_public: int = 4,
                             tenant_id: Optional[str] = None) -> List[Document]:
        """
        Search for similar schemas based on query with automatic retry on connection errors.

        Args:
            query: Natural language query
            db_id: Database ID to filter by
            k_public: Number of public schemas to retrieve
            tenant_id: Optional tenant ID to determine which schema types to include

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
        tenant_schema_types = config.get_tenant_config(tenant_id or DEFAULT_TENANT).get("schema_types", ["public"])
        if "custom" in tenant_schema_types:
            custom_results = self._get_all_custom_schemas(query, db_id)
            if custom_results:
                retrieved.extend(custom_results)

        return retrieved

    def embed_query(self, query: str) -> list:
        """Return raw embedding vector for a single query string."""
        return self.embedding_model.embed_query(query)

    def get_formatted_schemas(self, query: str, db_id: int,
                              tenant_id: Optional[str] = None) -> str:
        """Get formatted schema text for prompt, grouped by section with headers."""
        schemas = self.search_similar_schemas(query, db_id, tenant_id=tenant_id)

        section_headers = {
            "public":                     "=== PUBLIC TABLES ===",
            "worksheet":                  "=== WORKSHEET VIEWS ===",
            "worksheet_consolidated":     "=== WORKSHEET VIEWS (CONSOLIDATED) ===",
            "formversion":                "=== FORM VERSION VIEWS ===",
            "formversion_consolidated":   "=== FORM VERSION VIEWS (CONSOLIDATED) ===",
            "scoresheet":                 "=== SCORESHEET VIEWS ===",
        }

        sections: dict[str, list[str]] = {}
        for doc in schemas:
            key = doc.metadata.get("correlation_type")
            if not key:
                # Backward-compatible fallback for embeddings persisted before
                # correlation_type existed: infer worksheet vs scoresheet from
                # the page content for "custom" docs.
                schema_type = doc.metadata.get("schema_type", "public")
                if schema_type == "custom":
                    first_line = doc.page_content.split('\n')[0].lower()
                    key = "scoresheet" if "scoresheet" in first_line else "worksheet"
                else:
                    key = schema_type
            key = key.lower()
            sections.setdefault(key, []).append(doc.page_content)

        parts = []
        for stype in ("public", "worksheet", "worksheet_consolidated",
                      "formversion", "formversion_consolidated", "scoresheet"):
            if stype in sections:
                parts.append(section_headers[stype])
                parts.extend(sections[stype])

        return "\n".join(parts)


# Global embedding manager instance
embedding_manager = EmbeddingManager()