"""
Focused unit tests for the data-model generator performance changes:

  1. `_get_metadata` and `_get_report_columns_maps_info` — short-TTL caches that
     de-dupe the repeated Metabase fetches done during a single preview (the latter
     also feeds source-type detection, which used to query once per view).
  2. `preview_model` — now makes ONE LLM call per preview (naming-only when the view
     is fully labeled, a combined enhance+name call otherwise) and ONE bounded SQL
     execution that both validates the query and returns the preview rows.

The backend's real dependencies (aiohttp, config env, Metabase) are not needed here —
they are stubbed in sys.modules before import so this runs with only the stdlib.

Run:  python -m unittest test_model_generator -v
"""
import os
import sys
import types
import unittest
from types import SimpleNamespace
from unittest import mock

# --- Make `src` importable and stub heavy/optional imports -------------------------

SRC = os.path.join(os.path.dirname(__file__), "src")
if SRC not in sys.path:
    sys.path.insert(0, SRC)

# model_generator does `import aiohttp`, `from config import config`,
# `from metabase import metabase_client` at module load. None are exercised directly
# by these tests, so lightweight stubs are enough to import the module.
sys.modules.setdefault("aiohttp", types.ModuleType("aiohttp"))

_config_mod = types.ModuleType("config")
_config_mod.config = SimpleNamespace(
    ai=SimpleNamespace(),
    app=SimpleNamespace(data_model_preview_row_limit=20),
)
sys.modules.setdefault("config", _config_mod)

_metabase_mod = types.ModuleType("metabase")
_metabase_mod.metabase_client = SimpleNamespace()
sys.modules.setdefault("metabase", _metabase_mod)

import model_generator  # noqa: E402
from model_generator import DataModelGenerator  # noqa: E402


class GetMetadataCacheTests(unittest.TestCase):
    def setUp(self):
        self.gen = DataModelGenerator()
        self.fetch = mock.Mock(side_effect=lambda db_id, tenant_id=None: {"db": db_id, "tenant": tenant_id})
        # _get_metadata calls the module-global metabase_client.get_database_metadata
        self.patcher = mock.patch.object(
            model_generator, "metabase_client",
            SimpleNamespace(get_database_metadata=self.fetch),
        )
        self.patcher.start()
        self.addCleanup(self.patcher.stop)

    def test_cache_hit_within_ttl_fetches_once(self):
        first = self.gen._get_metadata(7, "tenant-a")
        second = self.gen._get_metadata(7, "tenant-a")
        self.assertIs(first, second)
        self.assertEqual(self.fetch.call_count, 1)

    def test_distinct_tenants_do_not_share_cache(self):
        self.gen._get_metadata(7, "tenant-a")
        self.gen._get_metadata(7, "tenant-b")
        self.assertEqual(self.fetch.call_count, 2)

    def test_key_is_normalized(self):
        # int-vs-str db_id and padded tenant_id should resolve to one cache entry
        self.gen._get_metadata(7, "tenant-a")
        self.gen._get_metadata("7", "  tenant-a  ")
        self.assertEqual(self.fetch.call_count, 1)

    def test_expired_entry_refetches(self):
        self.gen._get_metadata(7, "tenant-a")
        # Force the cached entry to look stale
        self.gen._META_CACHE_TTL = 0
        self.gen._get_metadata(7, "tenant-a")
        self.assertEqual(self.fetch.call_count, 2)


class ReportColumnsMapsCacheTests(unittest.TestCase):
    """The ReportColumnsMaps fetch backs both has_labels and source-type detection;
    caching it collapses the old per-view CorrelationProvider queries to one."""

    def setUp(self):
        self.gen = DataModelGenerator()
        self.exec = mock.Mock(return_value={"rows": [["V1", "worksheet"], ["V2", "formversion"]]})
        self.patcher = mock.patch.object(
            model_generator, "metabase_client",
            SimpleNamespace(execute_sql=self.exec),
        )
        self.patcher.start()
        self.addCleanup(self.patcher.stop)

    def test_cache_hit_within_ttl_fetches_once(self):
        first = self.gen._get_report_columns_maps_info(1, "t")
        second = self.gen._get_report_columns_maps_info(1, "t")
        self.assertIs(first, second)
        self.assertEqual(self.exec.call_count, 1)
        labels, providers = first
        self.assertEqual(labels, {"V1", "V2"})
        self.assertEqual(providers, {"V1": "worksheet", "V2": "formversion"})

    def test_detect_source_type_reads_cached_map(self):
        # No metadata round-trip needed for a name carrying a known provider.
        self.gen._get_metadata = mock.Mock(return_value={"tables": []})
        st = self.gen._detect_source_type("V1", 1, "t")
        self.assertEqual(st, "worksheet_view")
        self.assertEqual(self.exec.call_count, 1)  # single ReportColumnsMaps query

    def test_expired_entry_refetches(self):
        self.gen._get_report_columns_maps_info(1, "t")
        self.gen._META_CACHE_TTL = 0
        self.gen._get_report_columns_maps_info(1, "t")
        self.assertEqual(self.exec.call_count, 2)


class PreviewModelTests(unittest.IsolatedAsyncioTestCase):
    def setUp(self):
        self.gen = DataModelGenerator()
        # Form view that builds deterministic SQL. Default: fully labeled
        # (needs_enhancement=False) → naming-only path.
        self.gen._detect_source_type = mock.Mock(return_value="form_view")
        self.gen._build_form_view_sql = mock.Mock(
            return_value=("SELECT 1", ["ColA"], {"ColA": ["x"]}, False)
        )
        self.gen._validate_enhancement = mock.Mock(return_value=True)
        # One bounded execution returns (is_valid, error, preview_rows).
        self.run_query = mock.Mock(return_value=(True, None, {"cols": [], "rows": []}))
        self.patcher = mock.patch.object(
            model_generator, "metabase_client",
            SimpleNamespace(run_query_checked=self.run_query),
        )
        self.patcher.start()
        self.addCleanup(self.patcher.stop)

    def _build_returns(self, needs_enhancement):
        self.gen._build_form_view_sql = mock.Mock(
            return_value=("SELECT 1", ["ColA"], {"ColA": ["x"]}, needs_enhancement)
        )

    async def test_labeled_view_uses_naming_only(self):
        self.gen._generate_name_description = mock.AsyncMock(return_value=("AI Name", "AI Desc"))
        self.gen._enhance_and_name = mock.AsyncMock()  # must NOT be called

        result = await self.gen.preview_model("Form-Demo-V1", db_id=1, tenant_id="t")

        self.gen._enhance_and_name.assert_not_called()
        self.assertEqual(result["sql"], "SELECT 1")  # deterministic SQL untouched
        self.assertEqual(result["name"], "AI Name")
        self.assertEqual(result["description"], "AI Desc")
        self.assertTrue(result["valid"])
        self.assertEqual(self.run_query.call_count, 1)   # single execution
        self.assertIn("_preview_raw", result)            # rows reused by the API layer

    async def test_unlabeled_view_uses_combined_call(self):
        self._build_returns(needs_enhancement=True)
        self.gen._enhance_and_name = mock.AsyncMock(
            return_value=("SELECT enhanced", "AI Name", "AI Desc")
        )
        self.gen._generate_name_description = mock.AsyncMock()  # must NOT be called

        result = await self.gen.preview_model("Form-Demo-V1", db_id=1, tenant_id="t")

        self.gen._generate_name_description.assert_not_called()
        self.assertEqual(result["sql"], "SELECT enhanced")
        self.assertEqual(result["name"], "AI Name")

    async def test_rejected_enhancement_keeps_deterministic_sql(self):
        self._build_returns(needs_enhancement=True)
        self.gen._enhance_and_name = mock.AsyncMock(
            return_value=("DROP TABLE x", "AI Name", "AI Desc")
        )
        self.gen._validate_enhancement = mock.Mock(return_value=False)

        result = await self.gen.preview_model("Form-Demo-V1", db_id=1, tenant_id="t")

        self.assertEqual(result["sql"], "SELECT 1")   # rejected → deterministic kept
        self.assertEqual(result["name"], "AI Name")   # name from the same call still used

    async def test_enhance_and_name_failure_falls_back(self):
        self._build_returns(needs_enhancement=True)
        self.gen._enhance_and_name = mock.AsyncMock(return_value=(None, None, None))

        result = await self.gen.preview_model("Form-Demo-V1", db_id=1, tenant_id="t")

        fb_name, fb_desc = self.gen._fallback_name_description("Form-Demo-V1")
        self.assertEqual(result["sql"], "SELECT 1")    # deterministic SQL preserved
        self.assertEqual(result["name"], fb_name)
        self.assertEqual(result["description"], fb_desc)

    async def test_naming_failure_uses_fallback(self):
        self.gen._generate_name_description = mock.AsyncMock(side_effect=RuntimeError("LLM down"))

        result = await self.gen.preview_model("Form-Demo-V1", db_id=1, tenant_id="t")

        fb_name, fb_desc = self.gen._fallback_name_description("Form-Demo-V1")
        self.assertEqual(result["name"], fb_name)
        self.assertEqual(result["description"], fb_desc)

    async def test_self_heal_runs_and_succeeds(self):
        self.gen._generate_name_description = mock.AsyncMock(return_value=("N", "D"))
        # First execution fails validation; the healed query passes.
        self.run_query.side_effect = [
            (False, "syntax error", None),
            (True, None, {"cols": [], "rows": []}),
        ]
        self.gen._self_heal = mock.AsyncMock(return_value="SELECT healed")

        result = await self.gen.preview_model("Form-Demo-V1", db_id=1, tenant_id="t")

        self.gen._self_heal.assert_awaited_once()
        self.assertTrue(result["valid"])
        self.assertEqual(result["sql"], "SELECT healed")
        self.assertEqual(self.run_query.call_count, 2)


if __name__ == "__main__":
    unittest.main()
