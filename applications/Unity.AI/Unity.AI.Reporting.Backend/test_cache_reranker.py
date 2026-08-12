"""
Unit tests for the semantic-cache discriminator guard and temporal validity
checks (AB#33664).

Covers:
  1. extract_discriminators — kind-tagged salient literals (years, quarters,
     months, counts), including the normalize_query abbreviation expansions.
  2. discriminators_conflict — the ticket's regression pair ("…in 2024" vs
     "…in 2026") must conflict, while pure paraphrases must NOT, so the guard
     rejects wrong hits without gutting the cache.
  3. relative_time_granularity / sql_has_hardcoded_date / same_period —
     the "submitted this year" staleness case, which arrives as an *exact*
     match and so is invisible to every similarity layer.
  4. cached_entry_is_temporally_valid — the composed rule, exercised across a
     year boundary via the injected clock.

Everything under test is a pure function, so nothing is stubbed here. In
particular llm_client is imported for real (via cache_reranker) rather than
faked: a sys.modules stub would shadow it for any other test module sharing the
process — test_model_generator needs build_async_client from it. The import is
still gated so a machine without rapidfuzz/openai skips rather than errors.

Run:  python -m unittest test_cache_reranker -v
"""
import os
import sys
import unittest
from datetime import datetime

SRC = os.path.join(os.path.dirname(__file__), "src")
if SRC not in sys.path:
    sys.path.insert(0, SRC)

try:
    import cache_reranker
    _OK = True
except ImportError:  # pragma: no cover - e.g. rapidfuzz or the openai SDK missing
    _OK = False


@unittest.skipUnless(_OK, "cache_reranker unavailable (rapidfuzz missing?)")
class TestExtractDiscriminators(unittest.TestCase):

    def test_year(self):
        self.assertEqual(
            cache_reranker.extract_discriminators("How many applications in 2024?"),
            frozenset({"year:2024"}),
        )

    def test_no_literals_is_empty(self):
        self.assertEqual(
            cache_reranker.extract_discriminators("How many applications are there?"),
            frozenset(),
        )

    def test_quarter_abbreviation_is_expanded_before_extraction(self):
        # normalize_query turns "Q3" into "quarter 3"; the quarter phrase is
        # consumed whole so the 3 must not resurface as num:3.
        self.assertEqual(
            cache_reranker.extract_discriminators("Applications in Q3 2024"),
            frozenset({"quarter:3", "year:2024"}),
        )

    def test_counts_are_tagged_separately_from_years(self):
        self.assertEqual(
            cache_reranker.extract_discriminators("Top 5 regions in 2024"),
            frozenset({"num:5", "year:2024"}),
        )

    def test_month_names(self):
        self.assertEqual(
            cache_reranker.extract_discriminators("Applications in January 2024"),
            frozenset({"month:january", "year:2024"}),
        )

    def test_fiscal_year_abbreviation_is_tagged_separately(self):
        # normalize_query turns "FY2024" into "fiscal year 2024"; the phrase is
        # consumed whole so the 2024 must not resurface as a calendar year:2024.
        self.assertEqual(
            cache_reranker.extract_discriminators("Applications in FY2024"),
            frozenset({"fiscal_year:2024"}),
        )

    def test_accepts_raw_or_normalised_input(self):
        raw = "  How Many Applications In 2024??  "
        normalised = cache_reranker.normalize_query(raw)
        self.assertEqual(
            cache_reranker.extract_discriminators(raw),
            cache_reranker.extract_discriminators(normalised),
        )


@unittest.skipUnless(_OK, "cache_reranker unavailable (rapidfuzz missing?)")
class TestDiscriminatorsConflict(unittest.TestCase):

    def test_ab33664_year_swap_conflicts(self):
        """The reported bug: these score 97.7 on rapidfuzz and must still be rejected."""
        self.assertTrue(cache_reranker.discriminators_conflict(
            "How many applications were submitted in 2024?",
            "How many applications were submitted in 2026?",
        ))

    def test_paraphrase_with_same_year_does_not_conflict(self):
        """Non-regression: the guard must not destroy legitimate cache hits."""
        self.assertFalse(cache_reranker.discriminators_conflict(
            "How many applications were submitted in 2024?",
            "Count of applications submitted during 2024",
        ))

    def test_two_literal_free_questions_do_not_conflict(self):
        self.assertFalse(cache_reranker.discriminators_conflict(
            "How many applications are there?",
            "What is the total number of applications?",
        ))

    def test_missing_literal_conflicts(self):
        """A year-filtered cache entry must not answer an all-time question."""
        self.assertTrue(cache_reranker.discriminators_conflict(
            "How many applications are there?",
            "How many applications in 2024?",
        ))

    def test_count_difference_conflicts(self):
        self.assertTrue(cache_reranker.discriminators_conflict(
            "Top 5 regions by funding",
            "Top 10 regions by funding",
        ))

    def test_quarter_difference_conflicts(self):
        self.assertTrue(cache_reranker.discriminators_conflict(
            "Applications in Q1 2024",
            "Applications in Q2 2024",
        ))

    def test_relative_direction_conflicts(self):
        """Review finding: these carry no literals, so an empty-set guard let them pass."""
        self.assertTrue(cache_reranker.discriminators_conflict(
            "How many applications were submitted this year?",
            "How many applications were submitted last year?",
        ))

    def test_relative_quantity_conflicts(self):
        self.assertTrue(cache_reranker.discriminators_conflict(
            "Applications in the last 30 days",
            "Applications in the last 60 days",
        ))

    def test_fiscal_and_calendar_year_conflict(self):
        """Relative "this year" and "this fiscal year" are different windows in BC."""
        self.assertTrue(cache_reranker.discriminators_conflict(
            "Applications this year", "Applications this fiscal year",
        ))

    def test_absolute_fiscal_and_calendar_year_conflict(self):
        """BC fiscal 2024 is Apr 2024 - Mar 2025, so it is not calendar 2024."""
        self.assertTrue(cache_reranker.discriminators_conflict(
            "Applications in FY2024", "Applications in 2024",
        ))

    def test_same_fiscal_year_does_not_conflict(self):
        """Non-regression: FY2024 and its expanded long form are the same window."""
        self.assertFalse(cache_reranker.discriminators_conflict(
            "Applications in FY2024", "Applications in fiscal year 2024",
        ))

    def test_relative_and_absolute_period_conflict(self):
        self.assertTrue(cache_reranker.discriminators_conflict(
            "Applications this year", "Applications in 2024",
        ))

    def test_same_relative_window_does_not_conflict(self):
        self.assertFalse(cache_reranker.discriminators_conflict(
            "How many applications were submitted this year?",
            "Count of applications submitted this year",
        ))

    def test_quantified_window_digits_are_not_double_counted(self):
        # "last 30 days" owns its 30; it must not also surface as num:30.
        self.assertEqual(
            cache_reranker.extract_discriminators("Applications in the last 30 days"),
            frozenset({"rel:last:30:day"}),
        )

    def test_word_level_difference_is_not_caught(self):
        """Documents the known blind spot the cache badge exists to cover.

        "north" vs "south" and "approved" vs "pending" are indistinguishable to
        a generic regex. Closing this needs the LLM judge, not more patterns.
        """
        self.assertFalse(cache_reranker.discriminators_conflict(
            "How many approved applications in 2024?",
            "How many pending applications in 2024?",
        ))


@unittest.skipUnless(_OK, "cache_reranker unavailable (rapidfuzz missing?)")
class TestRelativeTime(unittest.TestCase):

    def test_this_year(self):
        self.assertEqual(
            cache_reranker.relative_time_granularity(
                "How many applications were submitted this year?"),
            "year",
        )

    def test_last_quarter(self):
        self.assertEqual(
            cache_reranker.relative_time_granularity("Applications last quarter"),
            "quarter",
        )

    def test_ytd_abbreviation_is_expanded(self):
        self.assertEqual(
            cache_reranker.relative_time_granularity("Applications YTD"),
            "year",
        )

    def test_finest_granularity_wins(self):
        self.assertEqual(
            cache_reranker.relative_time_granularity(
                "Applications this month compared to last year"),
            "month",
        )

    def test_absolute_year_is_not_relative(self):
        self.assertIsNone(
            cache_reranker.relative_time_granularity(
                "How many applications were submitted in 2024?")
        )

    def test_today(self):
        self.assertEqual(
            cache_reranker.relative_time_granularity("Applications today"), "day"
        )

    def test_quantified_windows_expire_daily(self):
        """Review finding: these previously returned None and never expired.

        A rolling window's boundaries move every day, so day is the only safe
        granularity regardless of the unit named.
        """
        for question in ("Applications in the last 30 days",
                         "Applications previous 4 weeks",
                         "Applications rolling 12 months"):
            with self.subTest(question=question):
                self.assertEqual(
                    cache_reranker.relative_time_granularity(question), "day")

    def test_fiscal_flag_is_preserved(self):
        self.assertEqual(
            cache_reranker.relative_time_spec("Applications this fiscal year"),
            ("year", True),
        )
        self.assertEqual(
            cache_reranker.relative_time_spec("Applications this year"),
            ("year", False),
        )


@unittest.skipUnless(_OK, "cache_reranker unavailable (rapidfuzz missing?)")
class TestSqlHardcodedDate(unittest.TestCase):

    def test_literal_year_is_hardcoded(self):
        self.assertTrue(cache_reranker.sql_has_hardcoded_date(
            "SELECT COUNT(*) FROM a WHERE a.\"SubmissionDate\" >= '2025-01-01'"
        ))

    def test_current_date_expression_is_not_hardcoded(self):
        # The shape sql_generator's few-shot examples produce — self-updating.
        self.assertFalse(cache_reranker.sql_has_hardcoded_date(
            'SELECT COUNT(*) FROM a WHERE a."SubmissionDate" >= '
            "DATE_TRUNC('quarter', CURRENT_DATE) - INTERVAL '3 months'"
        ))

    def test_empty_sql(self):
        self.assertFalse(cache_reranker.sql_has_hardcoded_date(""))
        self.assertFalse(cache_reranker.sql_has_hardcoded_date(None))


@unittest.skipUnless(_OK, "cache_reranker unavailable (rapidfuzz missing?)")
class TestSamePeriod(unittest.TestCase):

    def test_year_boundary(self):
        self.assertFalse(cache_reranker.same_period(
            datetime(2025, 12, 31), "year", now=datetime(2026, 1, 1)))
        self.assertTrue(cache_reranker.same_period(
            datetime(2025, 1, 1), "year", now=datetime(2025, 12, 31)))

    def test_quarter_boundary(self):
        self.assertFalse(cache_reranker.same_period(
            datetime(2025, 3, 31), "quarter", now=datetime(2025, 4, 1)))
        self.assertTrue(cache_reranker.same_period(
            datetime(2025, 4, 1), "quarter", now=datetime(2025, 6, 30)))

    def test_month_and_day(self):
        self.assertFalse(cache_reranker.same_period(
            datetime(2025, 5, 31), "month", now=datetime(2025, 6, 1)))
        self.assertTrue(cache_reranker.same_period(
            datetime(2025, 6, 5, 9, 0), "day", now=datetime(2025, 6, 5, 17, 0)))

    def test_fiscal_year_boundary(self):
        """Review finding: March 31 -> April 1 is one calendar year, two fiscal years."""
        self.assertTrue(cache_reranker.same_period(
            datetime(2026, 3, 31), "year", now=datetime(2026, 4, 1)))
        self.assertFalse(cache_reranker.same_period(
            datetime(2026, 3, 31), "year", now=datetime(2026, 4, 1), fiscal=True))
        # Within one fiscal year, spanning the calendar boundary, stays valid.
        self.assertTrue(cache_reranker.same_period(
            datetime(2026, 4, 1), "year", now=datetime(2027, 3, 31), fiscal=True))

    def test_fiscal_year_id(self):
        self.assertEqual(cache_reranker.fiscal_year_id(datetime(2026, 3, 31)), 2025)
        self.assertEqual(cache_reranker.fiscal_year_id(datetime(2026, 4, 1)), 2026)

    def test_fiscal_quarter_boundary(self):
        # Fiscal Q1 is Apr-Jun; Apr 1 and Jun 30 share it, Jul 1 starts Q2.
        self.assertTrue(cache_reranker.same_period(
            datetime(2026, 4, 1), "quarter", now=datetime(2026, 6, 30), fiscal=True))
        self.assertFalse(cache_reranker.same_period(
            datetime(2026, 6, 30), "quarter", now=datetime(2026, 7, 1), fiscal=True))


@unittest.skipUnless(_OK, "cache_reranker unavailable (rapidfuzz missing?)")
class TestCachedEntryIsTemporallyValid(unittest.TestCase):

    RELATIVE = "How many applications were submitted this year?"
    PINNED_SQL = "SELECT COUNT(*) FROM a WHERE EXTRACT(YEAR FROM a.\"SubmissionDate\") = 2025"
    LIVE_SQL = (
        'SELECT COUNT(*) FROM a WHERE a."SubmissionDate" >= '
        "DATE_TRUNC('year', CURRENT_DATE)"
    )

    def test_stale_relative_question_across_year_boundary_is_rejected(self):
        """The exact-match staleness bug: cached in 2025, asked in 2026."""
        self.assertFalse(cache_reranker.cached_entry_is_temporally_valid(
            self.RELATIVE, self.PINNED_SQL,
            created_at=datetime(2025, 6, 1), now=datetime(2026, 1, 15),
        ))

    def test_same_year_is_still_valid(self):
        self.assertTrue(cache_reranker.cached_entry_is_temporally_valid(
            self.RELATIVE, self.PINNED_SQL,
            created_at=datetime(2025, 1, 5), now=datetime(2025, 12, 20),
        ))

    def test_self_updating_sql_never_goes_stale(self):
        self.assertTrue(cache_reranker.cached_entry_is_temporally_valid(
            self.RELATIVE, self.LIVE_SQL,
            created_at=datetime(2025, 6, 1), now=datetime(2026, 1, 15),
        ))

    def test_absolute_question_is_unaffected(self):
        self.assertTrue(cache_reranker.cached_entry_is_temporally_valid(
            "How many applications were submitted in 2024?", self.PINNED_SQL,
            created_at=datetime(2025, 6, 1), now=datetime(2026, 1, 15),
        ))

    def test_unknown_age_is_rejected(self):
        self.assertFalse(cache_reranker.cached_entry_is_temporally_valid(
            self.RELATIVE, self.PINNED_SQL, created_at=None, now=datetime(2026, 1, 15),
        ))

    def test_regenerated_entry_is_accepted_again(self):
        """Review finding: the stale entry must not be rejected forever.

        CacheRepository.save() refreshes created_at on conflict, so once the SQL
        is regenerated the entry is valid again — otherwise every subsequent
        request re-generates and the cache never recovers.
        """
        now = datetime(2026, 1, 15)
        self.assertFalse(cache_reranker.cached_entry_is_temporally_valid(
            self.RELATIVE, self.PINNED_SQL, created_at=datetime(2025, 6, 1), now=now))
        # …regenerated now, so created_at moves to now.
        self.assertTrue(cache_reranker.cached_entry_is_temporally_valid(
            self.RELATIVE, self.PINNED_SQL, created_at=now, now=now))

    def test_quantified_window_expires_next_day(self):
        self.assertFalse(cache_reranker.cached_entry_is_temporally_valid(
            "How many applications in the last 30 days?", self.PINNED_SQL,
            created_at=datetime(2026, 1, 14, 23, 0), now=datetime(2026, 1, 15, 1, 0),
        ))

    def test_fiscal_year_question_expires_on_april_1(self):
        self.assertFalse(cache_reranker.cached_entry_is_temporally_valid(
            "How many applications this fiscal year?", self.PINNED_SQL,
            created_at=datetime(2026, 3, 31), now=datetime(2026, 4, 1),
        ))


if __name__ == "__main__":  # pragma: no cover
    unittest.main(verbosity=2)
