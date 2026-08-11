"""
cache_reranker.py — Multi-layer cache reranking.
Phase 1: FuzzyMatcher — rapidfuzz layer 1.5 between exact and embedding search.
Phase 2: normalize_query — whitespace, punctuation, domain abbreviation expansion.
Phase 3: LLMJudge — binary equivalence judge for borderline cosine zone.
Phase 4: discriminator guard + temporal validity — deterministic rejection of
         cache candidates whose salient literals or time period differ (AB#33664).
"""
import logging
import re
from datetime import datetime
from typing import Optional, List, Dict

from rapidfuzz import fuzz, process

from llm_client import chat_completion

logger = logging.getLogger(__name__)

# Domain-specific BC Government fiscal reporting abbreviations.
# Extend this list as new patterns are identified from query logs.
_ABBREVIATIONS = [
    (re.compile(r'\bfy\s*(\d{4})\b', re.I),  r'fiscal year \1'),  # FY2024 → fiscal year 2024
    (re.compile(r'\bq([1-4])\b', re.I),        r'quarter \1'),      # Q3 → quarter 3
    (re.compile(r'\bytd\b', re.I),             'year to date'),
    (re.compile(r'\bmtd\b', re.I),             'month to date'),
    (re.compile(r'\bqtd\b', re.I),             'quarter to date'),
    (re.compile(r'\bapprox\.?\b', re.I),       'approximately'),
]


def normalize_query(text: str) -> str:
    """Normalise a natural-language query before any cache lookup.

    Safe operations only: whitespace collapsing, trailing punctuation removal,
    and domain-specific abbreviation expansion.

    Stopword removal and stemming are intentionally excluded — they hurt
    transformer embedding quality for short analytical NL queries where
    every word carries semantic weight (e.g. 'not', 'by', 'excluding').
    """
    text = text.strip().lower()
    text = re.sub(r'\s+', ' ', text)              # collapse multiple spaces
    text = re.sub(r'[?!.]{1,10}$', '', text).strip() # strip trailing punctuation
    for pattern, replacement in _ABBREVIATIONS:
        text = pattern.sub(replacement, text)
    return text


# ── Phase 4a: discriminator guard ─────────────────────────────────────────────
# Text similarity is the wrong signal for filter literals: "…in 2024" vs "…in 2026"
# scores 97.7 on rapidfuzz and ~0.98 on cosine, yet needs completely different SQL.
# Worse, the score *rises* with question length, so no threshold fixes the class.
# Extract the literals that force different SQL and require them to match exactly.

_MONTHS = (
    "january", "february", "march", "april", "may", "june",
    "july", "august", "september", "october", "november", "december",
)

_YEAR_RE = re.compile(r'\b(?:19|20)\d{2}\b')
# normalize_query has already expanded "Q3" -> "quarter 3" by the time we run.
_QUARTER_RE = re.compile(r'\bquarter\s*([1-4])\b')
_MONTH_RE = re.compile(r'\b(' + '|'.join(_MONTHS) + r')\b')
_NUMBER_RE = re.compile(r'\b\d+(?:\.\d+)?\b')


def extract_discriminators(text: str) -> frozenset:
    """Return the kind-tagged salient literals in a question.

    Kind tags keep a year from colliding with a count, so "top 2024" and
    "in 2024" are not treated as the same constraint:
        "applications in 2024"        -> {"year:2024"}
        "top 5 regions in Q3 2024"    -> {"num:5", "quarter:3", "year:2024"}
        "how many applications"       -> frozenset()

    Normalisation is applied internally, so callers may pass either a raw
    query_text or an already-normalised query — normalize_query is idempotent.
    """
    remaining = normalize_query(text)
    found = set()

    # Years first, then strip them so they are not re-counted as bare numbers.
    found.update(f"year:{m.group(0)}" for m in _YEAR_RE.finditer(remaining))
    remaining = _YEAR_RE.sub(' ', remaining)

    # "quarter 3" is consumed whole, so the 3 does not resurface as num:3.
    found.update(f"quarter:{m.group(1)}" for m in _QUARTER_RE.finditer(remaining))
    remaining = _QUARTER_RE.sub(' ', remaining)

    found.update(f"month:{m.group(1)}" for m in _MONTH_RE.finditer(remaining))
    found.update(f"num:{m.group(0)}" for m in _NUMBER_RE.finditer(remaining))

    return frozenset(found)


def discriminators_conflict(q1: str, q2: str) -> bool:
    """True when two questions carry different salient literals.

    Set equality, not subset: any difference means different SQL. Two questions
    with no literals at all compare equal and are left to the similarity layers.

    This is a conflict detector, not an equivalence prover — it cannot see
    word-level differences ("approved" vs "pending" both yield {"year:2024"}).
    Those remain the job of the LLM judge and the user-facing cache badge.
    """
    return extract_discriminators(q1) != extract_discriminators(q2)


# ── Phase 4b: temporal validity ───────────────────────────────────────────────
# sql_generator injects "The current date is <today>" into the prompt, so the
# model may freeze a relative period as a literal. "…submitted this year" cached
# in 2025 pins 2025, and re-asking the identical words in 2026 is an *exact*
# match — no similarity score involved, so no reranker can catch it.

_GRANULARITY_ORDER = ("day", "week", "month", "quarter", "year")

_RELATIVE_PERIOD_RE = re.compile(
    r'\b(?:this|last|next|past|previous|current|recent)\s+'
    r'(?:fiscal\s+)?(year|quarter|month|week|day)s?\b'
)
# normalize_query expands ytd/mtd/qtd into these long forms.
_TO_DATE_RE = re.compile(r'\b(year|quarter|month|week) to date\b')
_DAY_WORD_RE = re.compile(r'\b(?:today|yesterday|tomorrow)\b')

# Any 4-digit year literal means the SQL pins a date instead of deriving it.
_SQL_HARDCODED_DATE_RE = re.compile(r'\b(?:19|20)\d{2}\b')


def relative_time_granularity(text: str) -> Optional[str]:
    """Return the finest relative-time granularity in a question, or None.

    Finest wins because it is the most restrictive: a question mentioning both
    "this month" and "last year" must be re-checked monthly, not yearly.
    """
    normalized = normalize_query(text)
    found = [m.group(1) for m in _RELATIVE_PERIOD_RE.finditer(normalized)]
    found += [m.group(1) for m in _TO_DATE_RE.finditer(normalized)]
    if _DAY_WORD_RE.search(normalized):
        found.append("day")
    if not found:
        return None
    return min(found, key=_GRANULARITY_ORDER.index)


def sql_has_hardcoded_date(sql: str) -> bool:
    """True when the SQL pins a literal year rather than deriving it from CURRENT_DATE.

    SQL built from CURRENT_DATE / DATE_TRUNC / INTERVAL recomputes its own window
    on every run and stays correct indefinitely, so it never goes stale.
    """
    return bool(_SQL_HARDCODED_DATE_RE.search(sql or ""))


def same_period(created_at: datetime, granularity: str,
                now: Optional[datetime] = None) -> bool:
    """True when created_at falls in the same calendar period as now."""
    now = now or datetime.now()
    if granularity == "year":
        return created_at.year == now.year
    if granularity == "quarter":
        return (created_at.year, (created_at.month - 1) // 3) == \
               (now.year, (now.month - 1) // 3)
    if granularity == "month":
        return (created_at.year, created_at.month) == (now.year, now.month)
    if granularity == "week":
        return created_at.isocalendar()[:2] == now.isocalendar()[:2]
    if granularity == "day":
        return created_at.date() == now.date()
    return True


def cached_entry_is_temporally_valid(
    normalized_query: str,
    cached_sql: str,
    created_at: Optional[datetime],
    now: Optional[datetime] = None,
) -> bool:
    """False when a relative-time question's cached SQL pins a now-expired period.

    `now` is injectable so the year-boundary case is testable without waiting
    for one.
    """
    granularity = relative_time_granularity(normalized_query)
    if granularity is None:
        return True
    if not sql_has_hardcoded_date(cached_sql):
        return True
    if created_at is None:
        # Relative question + pinned date + unknown age — not worth the risk.
        return False
    return same_period(created_at, granularity, now)


class FuzzyMatcher:
    """Layer 1.5: rapidfuzz token_sort_ratio + ratio with length guard.

    Uses two algorithms and takes the higher score:
    - token_sort_ratio: handles word-order variations ("show grants this year" vs
      "this year show grants")
    - ratio: character-level Levenshtein distance, best for typos

    A length ratio guard (±30%) rejects pairs that share words but differ
    in intent due to significant length differences.
    """

    def find_best(
        self,
        query: str,
        candidates: List[Dict],
        threshold: float = 92.0,
    ) -> Optional[Dict]:
        """Return the best-matching candidate above threshold or None.

        Args:
            query: The normalized incoming query.
            candidates: List of {"normalized_query": str, "cache_id": str}.
            threshold: Minimum score on 0–100 scale to accept a match.

        Returns:
            The matching candidate dict with an added "score" key, or None.
        """
        if not candidates:
            return None

        candidate_strings = [c["normalized_query"] for c in candidates]

        # token_sort_ratio is order-independent — best for rephrased queries
        result = process.extractOne(
            query, candidate_strings, scorer=fuzz.token_sort_ratio
        )
        if result is None:
            return None

        matched_str, token_score, idx = result

        # Also score with character-level ratio for typo detection
        char_score = fuzz.ratio(query, matched_str)
        final_score = max(token_score, char_score)

        if final_score < threshold:
            return None

        if not self._length_ok(query, matched_str):
            return None

        return {**candidates[idx], "score": final_score}

    def _length_ok(self, q1: str, q2: str, max_ratio: float = 0.30) -> bool:
        """Return True if the two strings are within max_ratio length of each other.

        Prevents accepting fuzzy matches where one query has significantly more
        content than the other (different intent despite word overlap).
        """
        longer = max(len(q1), len(q2))
        if longer == 0:
            return False
        return abs(len(q1) - len(q2)) / longer <= max_ratio


fuzzy_matcher = FuzzyMatcher()


_SCORER_SYSTEM = (
    "You are a strict query equivalence scorer for an analytical SQL cache system. "
    "Your job is to detect any difference that would cause a different SQL query to be required. "
    "When in doubt, score lower — a false cache hit returns wrong data. "
    "Reply with a single integer from 0 to 10 and nothing else."
)

_SCORER_PROMPT = """\
Rate the semantic equivalence of these two analytical questions on a scale of 0 to 10.
Two questions are equivalent only if the SAME SQL query would correctly answer both.

Q1: {q1}
Q2: {q2}

Scoring guide:
  10 — Identical intent; the same SQL correctly answers both.
   8-9 — Same intent; only trivial phrasing differences (synonyms, word order, punctuation).
          No difference in time range, filters, aggregation, grouping, or entities.
   5-7 — Same topic; differ in grouping dimension only (e.g. by region vs by sector),
          while time range, filters, and aggregation are identical.
   2-4 — Differ in time range, filter value, aggregation function, metric, or named entity —
          these require different SQL and produce different results.
   0-1 — Unrelated or contradictory.

Hard ceiling rules — score MUST NOT exceed:
- Time range differs (e.g. "last year" vs "this year", Q1 vs Q2, 2023 vs 2024, YTD vs full year): 4
- Filter value differs (e.g. region A vs B, approved vs pending, one program vs another): 4
- Aggregation function differs (e.g. COUNT vs SUM, average vs total): 4
- Measured column differs (e.g. approved amount vs requested amount): 4
- Grouping dimension differs (e.g. by region vs by sector): 5

Reply with exactly one integer, no punctuation, no explanation.\
"""


class LLMJudge:
    """Phase 3 — Scored equivalence ranker for borderline cosine zone [low, threshold).

    Calls the configured Azure OpenAI deployment with a 0-10 scoring prompt.
    Scores ALL borderline candidates and returns the best one above the threshold,
    rather than stopping at the first acceptable match.
    Fail-safe: returns score=0 on any API error — a miss is always safer than
    a false cache hit that returns wrong SQL.
    """

    async def score_candidate(
        self,
        q1: str,
        q2: str,
        client,
    ) -> tuple[int, int]:
        """Return (score, total_tokens). score in [0, 10]. Returns (0, 0) on any error."""
        try:
            response = await chat_completion(
                client,
                system_message=_SCORER_SYSTEM,
                user_message=_SCORER_PROMPT.format(q1=q1, q2=q2),
                temperature=0,
                max_completion_tokens=1000,
            )
            choice = response.choices[0]
            finish_reason = choice.finish_reason or "unknown"
            text = choice.message.content or ""
            usage = response.usage
            tokens = getattr(usage, "total_tokens", 0) if usage else 0
            reasoning_tokens = 0
            details = getattr(usage, "completion_tokens_details", None) if usage else None
            if details:
                reasoning_tokens = getattr(details, "reasoning_tokens", 0) or 0
            if finish_reason == "content_filter":
                logger.warning(
                    "[llm_judge] content_filter triggered — defaulting score=0"
                )
                return 0, tokens
            score = self._parse_score(text)
            logger.debug(
                f"[llm_judge] finish_reason={finish_reason} "
                f"reasoning_tokens={reasoning_tokens} "
                f"raw_response={text!r} parsed_score={score}"
            )
            return score, tokens
        except Exception as exc:
            logger.warning(f"[llm_judge] Exception, defaulting score=0: {exc}")
            return 0, 0

    def _parse_score(self, text: str) -> int:
        """Parse integer 0-10 from LLM output. Returns 0 on any parse failure."""
        cleaned = re.sub(r'[^0-9.]', ' ', text.strip()).strip()
        first_token = cleaned.split()[0] if cleaned.split() else ""
        try:
            return max(0, min(10, int(float(first_token))))
        except (ValueError, IndexError):
            return 0


llm_judge = LLMJudge()
