"""POST /ingest: chunk + embed a knowledge-base document and write its KnowledgeChunks."""

import logging
from collections.abc import Callable
from typing import Annotated

from fastapi import APIRouter, Depends, HTTPException, status

from app.deps import get_embedder, get_store_factory, require_service_key
from app.rag.embeddings import Embedder
from app.rag.search import ingest as ingest_document
from app.rag.store import KnowledgeStore, StoreNotConfigured
from app.schemas import IngestRequest, IngestResponse

log = logging.getLogger(__name__)
router = APIRouter(dependencies=[Depends(require_service_key)])


def _store(factory: Annotated[Callable[[], KnowledgeStore], Depends(get_store_factory)]) -> KnowledgeStore:
    try:
        return factory()
    except StoreNotConfigured as ex:
        raise HTTPException(status.HTTP_503_SERVICE_UNAVAILABLE, "The knowledge base is not configured.") from ex


@router.post("/ingest", response_model=IngestResponse)
def ingest(
    request: IngestRequest,
    store: Annotated[KnowledgeStore, Depends(_store)],
    embedder: Annotated[Embedder, Depends(get_embedder)],
) -> IngestResponse:
    try:
        count = ingest_document(request.document_id, request.content, store, embedder)
    except Exception as ex:
        log.warning("ingest of document %d failed: %s", request.document_id, type(ex).__name__)
        raise HTTPException(status.HTTP_502_BAD_GATEWAY, "Could not store the document's passages.") from ex
    log.info("ingested document %d into %d chunks", request.document_id, count)
    return IngestResponse(document_id=request.document_id, chunks=count)
