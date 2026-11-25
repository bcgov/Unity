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
            name, db_id, collection_id, schema_types = params
            new = (name, db_id, collection_id, list(schema_types))
            changed = self.db.rows.get(name) != new
            self.db.rows[name] = new
            self._result = (name,) if changed else None
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
    def __init__(self, embedded=()):
        self.rows = {}
        self.statements = []
        self.embedded = set(embedded)

    def get_connection(self):
        return FakeConnection(self)

    def has_embeddings_for_db_ids(self, db_ids, collection_name):
        return set(db_ids) & self.embedded


def entry(name="Tenant A", db_id=7, collection_id=21, schema_types=("public", "custom")):
    return tr.TenantEntry(name, db_id, collection_id, list(schema_types))


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


class FirstUseEmbedTests(unittest.TestCase):
    def test_should_return_ready_without_embedding_when_db_has_embeddings(self):
        registry = tr.TenantRegistry(FakeDb(embedded={7}))
        with mock.patch.object(tr.threading, "Thread") as thread:
            self.assertTrue(registry.ensure_embedding_started(entry(), "embedded_schema"))
        thread.assert_not_called()

    def test_should_start_one_background_embed_per_retry_window(self):
        registry = tr.TenantRegistry(FakeDb())
        with mock.patch.object(tr.threading, "Thread") as thread:
            self.assertFalse(registry.ensure_embedding_started(entry(), "embedded_schema"))
            self.assertFalse(registry.ensure_embedding_started(entry(), "embedded_schema"))
        self.assertEqual(thread.call_count, 1)


if __name__ == "__main__":
    unittest.main()
