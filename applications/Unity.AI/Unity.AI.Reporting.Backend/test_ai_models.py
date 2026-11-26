"""
Tests for ai_models (AB#35030): AI Setup's AI Reporting models come from the token, are
validated, and follow the request or job into executor threads. Stdlib only.

    python test_ai_models.py
"""
import concurrent.futures
import os
import sys
import unittest

SRC = os.path.join(os.path.dirname(os.path.abspath(__file__)), "src")
if SRC not in sys.path:
    sys.path.insert(0, SRC)

import ai_models  # noqa: E402

MODELS = ai_models.AIModels("gpt-5-mini", "2024-10-21", "text-embedding-3-large", "2024-06-01")


class ParseTests(unittest.TestCase):
    def test_should_accept_ai_setup_values_each_with_its_own_api_version(self):
        self.assertEqual(
            ai_models.parse(" gpt-5-mini ", "2024-10-21", "text-embedding-3-large", "2024-06-01"), MODELS)

    def test_should_accept_preview_api_version(self):
        parsed = ai_models.parse("a", "2025-01-01-preview", "b", "2024-10-21")
        self.assertEqual(parsed.chat_api_version, "2025-01-01-preview")

    def test_should_reject_missing_or_malformed_values(self):
        for args in [
            (None, "2024-10-21", "text-embedding-3-large", "2024-10-21"),
            ("gpt-5-mini", None, "text-embedding-3-large", "2024-10-21"),
            ("gpt-5-mini", "2024-10-21", "", "2024-10-21"),
            ("gpt-5-mini", "2024-10-21", "text-embedding-3-large", None),
            ("gpt-5-mini/../x", "2024-10-21", "text-embedding-3-large", "2024-10-21"),
            ("gpt-5-mini", "latest", "text-embedding-3-large", "2024-10-21"),
        ]:
            with self.subTest(args=args), self.assertRaises(ValueError):
                ai_models.parse(*args)


class ContextTests(unittest.TestCase):
    def test_should_have_no_models_outside_a_request_or_job(self):
        with self.assertRaises(ai_models.AIModelsNotSetError):
            ai_models.current()

    def test_should_use_models_inside_block_and_clear_after(self):
        with ai_models.use(MODELS):
            self.assertEqual(ai_models.current(), MODELS)
        with self.assertRaises(ai_models.AIModelsNotSetError):
            ai_models.current()

    def test_should_carry_models_into_executor_threads_with_bind(self):
        with ai_models.use(MODELS), concurrent.futures.ThreadPoolExecutor(1) as pool:
            unbound = pool.submit(lambda: ai_models._current.get()).result()
            bound = pool.submit(ai_models.bind(ai_models.current)).result()

        self.assertIsNone(unbound)
        self.assertEqual(bound, MODELS)


if __name__ == "__main__":
    unittest.main()
