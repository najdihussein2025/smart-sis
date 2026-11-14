"""Shared FastAPI dependencies. Tests override the factories with fakes (no LLM, no database, no .NET API)."""

from collections.abc import Callable
from typing import Annotated

from fastapi import Depends, Header, HTTPException, status

from app.chat.agent import AnthropicLlm, ApiFactory, Llm
from app.clients.sis_api import SisApiClient
from app.config import Settings, get_settings
from app.rag.embeddings import Embedder, HashingEmbedder
from app.rag.store import KnowledgeStore, SqlKnowledgeStore

_embedder = HashingEmbedder()


def require_service_key(
    settings: Annotated[Settings, Depends(get_settings)],
    x_service_key: Annotated[str | None, Header()] = None,
) -> None:
    """Only the .NET API may call this service."""
    if not settings.service_key or x_service_key != settings.service_key:
        raise HTTPException(status.HTTP_401_UNAUTHORIZED, "Invalid service key.")


def student_token(authorization: Annotated[str | None, Header()] = None) -> str:
    """The student's own JWT, forwarded on every tool call to the .NET API. Never logged."""
    if not authorization or not authorization.startswith("Bearer ") or not authorization[7:].strip():
        raise HTTPException(status.HTTP_401_UNAUTHORIZED, "Missing student token.")
    return authorization[7:].strip()


def get_llm_factory(settings: Annotated[Settings, Depends(get_settings)]) -> Callable[[], Llm]:
    """Creating the client raises LlmNotConfigured (→ 503) when there is no API key."""
    return lambda: AnthropicLlm(settings.llm_api_key, settings.model, settings.llm_effort, settings.llm_max_tokens)


def get_api_factory(settings: Annotated[Settings, Depends(get_settings)]) -> ApiFactory:
    return lambda token: SisApiClient(token, settings.sis_api_base_url)


def get_store_factory(settings: Annotated[Settings, Depends(get_settings)]) -> Callable[[], KnowledgeStore]:
    """Creating the store raises StoreNotConfigured when there is no connection string."""
    return lambda: SqlKnowledgeStore(settings.sql_connection_string)


def get_embedder() -> Embedder:
    return _embedder
