from typing import Literal

from pydantic import BaseModel, Field


class ChatTurn(BaseModel):
    role: Literal["user", "assistant"]
    content: str


class ChatRequest(BaseModel):
    """Sent by the .NET API. The student's JWT comes in the Authorization header, not here."""

    message: str = Field(min_length=1, max_length=4000)
    history: list[ChatTurn] = Field(default_factory=list, max_length=50)


class ChatResponse(BaseModel):
    answer: str
    tools_used: list[str] = Field(default_factory=list)


class IngestRequest(BaseModel):
    document_id: int
    file_url: str


class IngestResponse(BaseModel):
    document_id: int
    chunks: int
