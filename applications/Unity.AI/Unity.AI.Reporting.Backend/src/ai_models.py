"""
The Azure OpenAI models AI Reporting uses, set in UGM's Unity Admin -> AI Setup (AB#35030).

UAI has no model names or API versions of its own. UGM sends them in the AI Reporting token:
each model with its own API version (ai_chat_deployment + ai_chat_api_version,
ai_embedding_deployment + ai_embedding_api_version), so every call goes to
.../openai/deployments/{deployment}/...?api-version={that model's version}. The tenant registry
keeps them for work that runs without a token (startup seed-embed, the embed-all CronJob,
first-use embeds).

The models for the current request or job live in a context variable. Work handed to another
thread must carry the context with it: use ``bind`` for ``run_in_executor`` and ``use`` inside a
new thread's target.
"""
import contextvars
import functools
import re
from contextlib import contextmanager
from dataclasses import dataclass
from typing import Iterator, Optional

# Azure deployment names: letters, digits, '.', '_', '-'. API versions: 2024-10-21[-preview].
_DEPLOYMENT_NAME = re.compile(r"^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$")
_API_VERSION = re.compile(r"^\d{4}-\d{2}-\d{2}(-preview)?$")


class AIModelsNotSetError(RuntimeError):
    """No AI Reporting models in this context: AI Setup hasn't set them, or a thread lost them."""


@dataclass(frozen=True)
class AIModels:
    chat_deployment: str
    chat_api_version: str
    embedding_deployment: str
    embedding_api_version: str


def parse(chat_deployment, chat_api_version, embedding_deployment, embedding_api_version) -> AIModels:
    """Validate values from a token or the registry. Raises ValueError when any is missing or malformed."""
    chat = _deployment(chat_deployment, "chat")
    embedding = _deployment(embedding_deployment, "embedding")
    return AIModels(chat, _api_version(chat_api_version, "chat"),
                    embedding, _api_version(embedding_api_version, "embedding"))


def _deployment(value, kind: str) -> str:
    text = str(value or "").strip()
    if not _DEPLOYMENT_NAME.match(text):
        raise ValueError(f"Invalid AI Reporting {kind} deployment: {text!r}")
    return text


def _api_version(value, kind: str) -> str:
    text = str(value or "").strip()
    if not _API_VERSION.match(text):
        raise ValueError(f"Invalid Azure OpenAI API version for the {kind} model: {text!r}")
    return text


_current: contextvars.ContextVar[Optional[AIModels]] = contextvars.ContextVar("ai_models", default=None)


def current() -> AIModels:
    models = _current.get()
    if models is None:
        raise AIModelsNotSetError(
            "AI Reporting's models are not set for this request. They come from UGM's AI Setup.")
    return models


def set_current(models: Optional[AIModels]) -> None:
    """Set the models for the rest of this request (cleared at request teardown)."""
    _current.set(models)


@contextmanager
def use(models: AIModels) -> Iterator[AIModels]:
    """Run a block (a job or a background thread's work) with the given models."""
    token = _current.set(models)
    try:
        yield models
    finally:
        _current.reset(token)


def bind(func):
    """Carry the current context into an executor thread: run_in_executor(None, bind(f), *args)."""
    return functools.partial(contextvars.copy_context().run, func)
