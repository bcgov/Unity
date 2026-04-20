"""
cache_reranker.py — Multi-layer cache reranking.
Phase 1: FuzzyMatcher — rapidfuzz layer 1.5 between exact and embedding search.
Phase 2: normalize_query — whitespace, punctuation, domain abbreviation expansion.
Phase 3: LLMJudge — binary equivalence judge for borderline cosine zone.
"""
import logging
import re
import aiohttp
from typing import Optional, List, Dict

from rapidfuzz import fuzz, process

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
    text = re.sub(r'\s+', ' ', text)            # collapse multiple spaces
    text = re.sub(r'[?!.]+$', '', text).strip() # strip trailing punctuation
    for pattern, replacement in _ABBREVIATIONS:
        text = pattern.sub(replacement, text)
    return text


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
        session: aiohttp.ClientSession,
        ai_config,
    ) -> tuple[int, int]:
        """Return (score, total_tokens). score in [0, 10]. Returns (0, 0) on any error."""
        try:
            headers = {
                "api-key": ai_config.azure_api_key,
                "Content-Type": "application/json",
            }
            endpoint = (
                f"{ai_config.azure_endpoint}/openai/deployments/"
                f"{ai_config.azure_deployment}/chat/completions"
                f"?api-version={ai_config.azure_api_version}"
            )
            payload = {
                "messages": [
                    {"role": "system", "content": _SCORER_SYSTEM},
                    {"role": "user", "content": _SCORER_PROMPT.format(q1=q1, q2=q2)},
                ],
                "max_completion_tokens": 1000,
            }
            if ai_config.supports_temperature:
                payload["temperature"] = 0
            async with session.post(endpoint, headers=headers, json=payload) as resp:
                if resp.status != 200:
                    error_body = await resp.text()
                    logger.warning(f"[llm_judge] API error status={resp.status} body={error_body}")
                    return 0, 0
                data = await resp.json()
                choice = data["choices"][0]
                finish_reason = choice.get("finish_reason", "unknown")
                text = choice["message"].get("content") or ""
                usage = data.get("usage", {})
                tokens = usage.get("total_tokens", 0)
                reasoning_tokens = usage.get("completion_tokens_details", {}).get("reasoning_tokens", 0)
                if finish_reason == "content_filter":
                    logger.warning(
                        f"[llm_judge] content_filter triggered — defaulting score=0"
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
