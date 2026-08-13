"""
conversation.py — Shared reading of conversation history (AB#34050).

One question, asked in one place: *which previous turn does this request carry?*

Two callers depend on the answer and must never disagree about it:

  - sql_generator.build_prompt — injects the previous turn's question and SQL
    into the prompt, so the generated SQL is only correct under that turn.
  - cache_reranker.build_context_key — derives the cache identity, so an entry
    generated under a previous turn is only served back under that same turn.

When those two drifted apart, a follow-up like "break it down by region" was
cached under its bare text and then served to anyone typing the same words as
the *first* question of a fresh chat — a report for a different question
entirely. Keeping the decision in one function makes that class of bug
structural rather than a convention two modules have to keep agreeing on.

Deliberately dependency-free (stdlib only): cache_reranker is imported by tests
that stub nothing, so pulling config/database/metabase in through here would
break them.
"""
from typing import Dict, List, Optional


def previous_turn(past_questions: Optional[List[Dict]]) -> Optional[Dict]:
    """The prior turn whose question + SQL belong in the prompt and the cache key.

    Returns None when the request carries no usable prior turn — the first
    question of a chat, or a predecessor too incomplete to condition on.

    The prior turn sits at [-2], not [-1]: the frontend pushes the in-flight
    turn into `conversation` before calling /api/ask (app.ts askQuestion), so
    the last element is the question being asked right now.

    Both `question` and `SQL` must be present and non-blank. A turn that errored
    carries SQL "", and conditioning on 'the generated SQL was: ""' is noise in
    the prompt. Rejecting it *here* is also what keeps the empty cache key safe:
    unusable context must drop out of the prompt and the key together, or an
    entry generated with context would be stored as though it had none — the
    original bug, reintroduced.
    """
    if not past_questions or len(past_questions) < 2:
        return None

    candidate = past_questions[-2]
    if not isinstance(candidate, dict):
        return None

    question = candidate.get("question")
    sql = candidate.get("SQL")
    if not isinstance(question, str) or not question.strip():
        return None
    if not isinstance(sql, str) or not sql.strip():
        return None

    return candidate
