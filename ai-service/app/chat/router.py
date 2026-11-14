"""POST /chat (PB-15 assistant, PB-19 tutor)."""

import logging
import uuid
from collections.abc import Callable
from typing import Annotated

from fastapi import APIRouter, Depends, HTTPException, status

from app.chat import agent
from app.chat.agent import ApiFactory, Llm, LlmNotConfigured, LlmUnavailable
from app.config import Settings, get_settings
from app.deps import get_api_factory, get_embedder, get_llm_factory, get_store_factory, require_service_key, student_token
from app.rag.embeddings import Embedder
from app.rag.search import Passage, search
from app.rag.store import KnowledgeStore, StoreNotConfigured
from app.schemas import ChatRequest, ChatResponse

log = logging.getLogger(__name__)
router = APIRouter(dependencies=[Depends(require_service_key)])


@router.post("/chat", response_model=ChatResponse)
def chat(
    request: ChatRequest,
    token: Annotated[str, Depends(student_token)],
    settings: Annotated[Settings, Depends(get_settings)],
    api_factory: Annotated[ApiFactory, Depends(get_api_factory)],
    store_factory: Annotated[Callable[[], KnowledgeStore], Depends(get_store_factory)],
    embedder: Annotated[Embedder, Depends(get_embedder)],
    llm_factory: Annotated[Callable[[], Llm], Depends(get_llm_factory)],
) -> ChatResponse:
    request_id = uuid.uuid4().hex[:12]
    log.info("chat %s mode=%s history=%d", request_id, request.mode, len(request.history))

    def kb_search(query: str) -> list[Passage]:
        try:
            return search(query, store_factory(), embedder)
        except StoreNotConfigured:
            return []
        except Exception as ex:  # a broken knowledge base must not break the whole answer
            log.warning("chat %s knowledge search failed: %s", request_id, type(ex).__name__)
            return []

    try:
        llm = llm_factory()
        return agent.run(request, token, llm, api_factory, kb_search, settings.llm_max_tool_rounds)
    except LlmNotConfigured as ex:
        log.warning("chat %s: %s", request_id, ex)
        raise HTTPException(status.HTTP_503_SERVICE_UNAVAILABLE, "The assistant is not configured (LLM_API_KEY).") from ex
    except LlmUnavailable as ex:
        raise HTTPException(status.HTTP_502_BAD_GATEWAY, "The language model is not available right now.") from ex
