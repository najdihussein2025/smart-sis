"""Smart SIS AI service. Only does AI; the .NET API owns all business data (docs/rules/ai-service.md)."""

from typing import Annotated

from fastapi import Depends, FastAPI, Header, HTTPException, status

from app.config import Settings, get_settings
from app.schemas import ChatRequest, ChatResponse, IngestRequest, IngestResponse

app = FastAPI(title="Smart SIS AI service", version="0.1.0")


def require_service_key(
    settings: Annotated[Settings, Depends(get_settings)],
    x_service_key: Annotated[str | None, Header()] = None,
) -> None:
    """Only the .NET API may call this service."""
    if not settings.service_key or x_service_key != settings.service_key:
        raise HTTPException(status.HTTP_401_UNAUTHORIZED, "Invalid service key.")


def student_token(authorization: Annotated[str | None, Header()] = None) -> str:
    """The student's own JWT, forwarded on every tool call to the .NET API."""
    if not authorization or not authorization.startswith("Bearer "):
        raise HTTPException(status.HTTP_401_UNAUTHORIZED, "Missing student token.")
    return authorization.removeprefix("Bearer ")


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}


@app.post("/chat", response_model=ChatResponse, dependencies=[Depends(require_service_key)])
def chat(request: ChatRequest, token: Annotated[str, Depends(student_token)]) -> ChatResponse:
    # TODO(phase-7): app.chat.agent.run(request, token)
    raise HTTPException(status.HTTP_501_NOT_IMPLEMENTED, "Chat is built in phase 7.")


@app.post("/ingest", response_model=IngestResponse, dependencies=[Depends(require_service_key)])
def ingest(request: IngestRequest) -> IngestResponse:
    # TODO(phase-7): download → app.rag.chunker → app.rag.embeddings → write KnowledgeChunks
    raise HTTPException(status.HTTP_501_NOT_IMPLEMENTED, "Ingest is built in phase 7.")
