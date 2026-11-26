"""
Tests for the semantic cache's vector-size check (AB#35030): query_cache stores halfvec(3072),
so an embedding model with another size skips the cache instead of failing the request.

    python test_cache_dimensions.py
"""
import os
import sys
import unittest

SRC = os.path.join(os.path.dirname(os.path.abspath(__file__)), "src")
if SRC not in sys.path:
    sys.path.insert(0, SRC)

import database  # noqa: E402


class NoDb:
    def get_connection(self):
        raise AssertionError("the cache must not touch the database for a vector it can't store")


class CacheDimensionTests(unittest.TestCase):
    def setUp(self):
        self.cache = database.CacheRepository(NoDb())

    def test_should_skip_lookup_for_other_vector_sizes(self):
        with self.assertLogs(database.logger, "ERROR"):
            hits = self.cache.find_similar_topk("Tenant A", 7, ["public"], "embedded_schema",
                                                [0.0] * 1536, threshold=0.8, context_key="")
        self.assertEqual(hits, [])

    def test_should_skip_save_for_other_vector_sizes(self):
        with self.assertLogs(database.logger, "ERROR"):
            self.cache.save("Tenant A", 7, ["public"], "embedded_schema", "q", "q",
                            [0.0] * 1536, {}, context_key="")

    def test_should_accept_the_cache_size(self):
        self.assertTrue(database._cache_fits([0.0] * database.CACHE_EMBEDDING_DIMENSIONS))


if __name__ == "__main__":
    unittest.main()
