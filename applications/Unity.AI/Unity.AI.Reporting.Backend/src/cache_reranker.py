"""
cache_reranker.py — Multi-layer cache reranking.
Phase 1: FuzzyMatcher, normalize_query.
"""
import logging
import re
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
