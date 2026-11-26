"""
Tests for tenant_registry (AB#33978): claim parsing, the per-process cache, and the
first-use embed trigger. The database module is stubbed so this runs with only the stdlib.

    python test_tenant_registry.py
"""
import os
import sys
import types
import unittest
from unittest import mock

SRC = os.path.join(os.path.dirname(os.path.abspath(__file__)), "src")
if SRC not in sys.path:
    sys.path.insert(0, SRC)

sys.modules.setdefault("database", types.SimpleNamespace(db_manager=None))

import tenant_registry as tr  # noqa: E402


class FakeCursor:
    def __init__(self, db):
        self.db = db
        self._result = None

    def __enter__(self):
        return self

    def __exit__(self, *exc):
        return False

    def execute(self, sql, params=None):
        self.db.statements.append(sql)
        if "INSERT INTO tenant_registry" in sql:
            name, db_id, collection_id, schema_types, chat, chat_version, embedding, embedding_version = params
            new = (name, db_id, collection_id, list(schema_types), chat, chat_version, embedding, embedding_version)
            changed = self.db.rows.get(name) != new
            self.db.rows[name] = new
            self._result = (name,) if changed else None
        elif "SELECT embedded_with" in sql:
            value = self.db.embedded_with.get(params[0])
            self._result = (value,) if value else None
        elif "UPDATE tenant_registry SET embedded_with" in sql:
            self.db.embedded_with[params[1]] = params[0]
            self._result = None
        elif "WHERE tenant_name = %s" in sql:
            self._result = self.db.rows.get(params[0])
        else:
            self._result = None

    def fetchone(self):
        return self._result

    def fetchall(self):
        return sorted(self.db.rows.values())


class FakeConnection:
    def __init__(self, db):
        self.db = db

    def __enter__(self):
        return self

    def __exit__(self, *exc):
        return False

    def cursor(self):
        return FakeCursor(self.db)

    def commit(self):
        pass


class FakeDb:
    def __init__(self, embedded=(), embedded_with=None):
        self.rows = {}
        self.statements = []
        self.embedded = set(embedded)
        self.embedded_with = dict(embedded_with or {})

    def get_connection(self):
        return FakeConnection(self)

    def has_embeddings_for_db_ids(self, db_ids, collection_name):
        return set(db_ids) & self.embedded


def entry(name="Tenant A", db_id=7, collection_id=21, schema_types=("public", "custom"),
          chat="gpt-5-mini", embedding="text-embedding-3-large",
          chat_version="2024-10-21", embedding_version="2024-10-21"):
    return tr.TenantEntry(name, db_id, collection_id, list(schema_types),
                          chat, chat_version, embedding, embedding_version)


class ParseSchemaTypesTests(unittest.TestCase):
    def test_should_split_trim_and_dedupe_claim_value(self):
        self.assertEqual(tr.parse_schema_types(" Public, custom ,public"), ["public", "custom"])

    def test_should_default_when_claim_missing_or_empty(self):
        self.assertEqual(tr.parse_schema_types(None), ["public", "custom"])
        self.assertEqual(tr.parse_schema_types(" , "), ["public", "custom"])

    def test_should_accept_list(self):
        self.assertEqual(tr.parse_schema_types(["public"]), ["public"])


class RegistryTests(unittest.TestCase):
    def test_should_report_changed_only_when_values_change(self):
        registry = tr.TenantRegistry(FakeDb())

        self.assertTrue(registry.register(entry()))
        self.assertFalse(registry.register(entry()))
        self.assertTrue(registry.register(entry(collection_id=22)))
        self.assertEqual(registry.get("Tenant A").collection_id, 22)

    def test_should_not_hit_database_when_cached_entry_is_identical(self):
        db = FakeDb()
        registry = tr.TenantRegistry(db)
        registry.register(entry())
        count = len(db.statements)

        registry.register(entry())

        self.assertEqual(len(db.statements), count)

    def test_should_raise_for_unknown_tenant(self):
        registry = tr.TenantRegistry(FakeDb())

        with self.assertRaises(tr.UnknownTenantError):
            registry.get("Default Grants Program")

    def test_should_read_registered_tenant_from_database_in_new_process(self):
        db = FakeDb()
        tr.TenantRegistry(db).register(entry())

        fresh = tr.TenantRegistry(db)

        self.assertEqual(fresh.get_config("Tenant A"),
                         {"db_id": 7, "collection_id": 21, "schema_types": ["public", "custom"]})
        self.assertEqual(fresh.find_by_db_id(7).tenant_name, "Tenant A")
        self.assertEqual(fresh.get("Tenant A").models.embedding_deployment, "text-embedding-3-large")

    def test_should_report_changed_when_ai_setup_models_change(self):
        registry = tr.TenantRegistry(FakeDb())
        registry.register(entry())

        self.assertTrue(registry.register(entry(chat="gpt-5-nano")))

    def test_should_have_no_models_for_row_registered_before_ai_setup(self):
        legacy = tr.TenantEntry("Tenant A", 7, 21, ["public"])

        self.assertIsNone(legacy.models)


class FirstUseEmbedTests(unittest.TestCase):
    def test_should_return_ready_without_embedding_when_db_has_embeddings(self):
        registry = tr.TenantRegistry(FakeDb(embedded={7}, embedded_with={7: "text-embedding-3-large"}))
        with mock.patch.object(tr.threading, "Thread") as thread:
            self.assertTrue(registry.ensure_embedding_started(entry(), "embedded_schema"))
        thread.assert_not_called()

    def test_should_adopt_unrecorded_embeddings_from_before_ai_setup(self):
        db = FakeDb(embedded={7})
        registry = tr.TenantRegistry(db)
        with mock.patch.object(tr.threading, "Thread") as thread:
            self.assertTrue(registry.ensure_embedding_started(entry(), "embedded_schema"))
        thread.assert_not_called()

    def test_should_re_embed_unrecorded_embeddings_when_ai_setup_picks_another_model(self):
        # Unrecorded vectors were made with the legacy deployment, not with whatever AI Setup now says.
        registry = tr.TenantRegistry(FakeDb(embedded={7}))
        with mock.patch.object(tr.threading, "Thread") as thread:
            self.assertFalse(registry.ensure_embedding_started(
                entry(embedding="text-embedding-3-small"), "embedded_schema"))
        self.assertEqual(thread.call_count, 1)

    def test_should_not_trust_cached_readiness_for_a_model_replaced_since(self):
        db = FakeDb(embedded={7})
        registry = tr.TenantRegistry(db)
        registry.mark_embedded(7, "model-a")
        registry.mark_embedded(7, "model-b")

        self.assertFalse(registry.is_embedded(7, "embedded_schema", "model-a"))
        self.assertTrue(registry.is_embedded(7, "embedded_schema", "model-b"))

    def test_should_re_embed_when_ai_setup_embedding_model_changes(self):
        registry = tr.TenantRegistry(FakeDb(embedded={7}, embedded_with={7: "text-embedding-3-large"}))
        with mock.patch.object(tr.threading, "Thread") as thread:
            self.assertFalse(registry.ensure_embedding_started(
                entry(embedding="text-embedding-3-small"), "embedded_schema"))
        self.assertEqual(thread.call_count, 1)

    def test_should_refuse_to_embed_without_ai_setup_models(self):
        registry = tr.TenantRegistry(FakeDb())
        legacy = tr.TenantEntry("Tenant A", 7, 21, ["public"])
        with self.assertRaises(tr.ai_models.AIModelsNotSetError):
            registry.ensure_embedding_started(legacy, "embedded_schema")

    def test_should_start_one_background_embed_per_retry_window(self):
        registry = tr.TenantRegistry(FakeDb())
        with mock.patch.object(tr.threading, "Thread") as thread:
            self.assertFalse(registry.ensure_embedding_started(entry(), "embedded_schema"))
            self.assertFalse(registry.ensure_embedding_started(entry(), "embedded_schema"))
        self.assertEqual(thread.call_count, 1)


if __name__ == "__main__":
    unittest.main()
