"""
Shared Azure OpenAI client factory for chat completions.

Embeddings go through langchain_openai (see embeddings.py); this module is the
single place that builds the native `openai` SDK client for chat/completions so
the four chat call sites stop hand-rolling HTTP.

Why a factory and not a module-level singleton: every request runs through its
own ``asyncio.run()`` (see api.py), which opens and closes a fresh event loop.
A persistent ``AsyncAzureOpenAI`` would bind its underlying httpx pool to the
first request's loop and raise "Event loop is closed" on the next one. Build the
client inside ``async with build_async_client() as client:`` at request scope so
the pool lives and dies with the current loop — mirroring the old
``aiohttp.ClientSession()`` lifetime.
"""
import logging
from typing import Optional

from openai import AsyncAzureOpenAI

from config import config

logger = logging.getLogger(__name__)


def build_async_client(max_retries: int = 2) -> AsyncAzureOpenAI:
    """Build a per-request AsyncAzureOpenAI client from the AI config.

    Use as ``async with build_async_client() as client:`` so the underlying
    httpx connection pool is bound to — and closed with — the current event loop.

    Args:
        max_retries: SDK-level retries on 429/5xx/connection errors (with
            backoff). Pass 0 where an outer retry loop already exists.
    """
    ai = config.ai
    return AsyncAzureOpenAI(
        azure_endpoint=ai.azure_endpoint,
        api_key=ai.azure_api_key,
        api_version=ai.azure_api_version,
        max_retries=max_retries,
    )


def usage_to_dict(usage) -> dict:
    """Map the SDK's CompletionUsage object to the dict shape the rest of the
    codebase expects. Returns zeros when usage is missing."""
    if usage is None:
        return {"prompt_tokens": 0, "completion_tokens": 0, "total_tokens": 0}
    return {
        "prompt_tokens": usage.prompt_tokens or 0,
        "completion_tokens": usage.completion_tokens or 0,
        "total_tokens": usage.total_tokens or 0,
    }


async def chat_completion(
    client: AsyncAzureOpenAI,
    *,
    system_message: str,
    user_message: str,
    temperature: Optional[float] = None,
    max_completion_tokens: Optional[int] = None,
):
    """Send a single chat completion and return the raw SDK response.

    Error handling is the caller's: the SDK raises typed exceptions
    (RateLimitError, APITimeoutError, APIConnectionError, APIStatusError) that
    callers either propagate (sql_generator) or swallow into a fallback
    (explain_sql, model_generator, cache_reranker).

    ``temperature`` is only sent when the deployed model supports a non-default
    value (``config.ai.supports_temperature``) — gpt-5* deployments reject it.

    Per-request timeouts are the caller's concern too: pass a timeout-bound
    client via ``client.with_options(timeout=...)`` rather than threading a
    timeout through here (keeps this coroutine free of timeout state).
    """
    kwargs = {
        "model": config.ai.azure_deployment,
        "messages": [
            {"role": "system", "content": system_message},
            {"role": "user", "content": user_message},
        ],
    }
    if temperature is not None and config.ai.supports_temperature:
        kwargs["temperature"] = temperature
    if max_completion_tokens is not None:
        kwargs["max_completion_tokens"] = max_completion_tokens
    return await client.chat.completions.create(**kwargs)
