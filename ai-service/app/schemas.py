from typing import Literal

from pydantic import BaseModel, Field


class ChatTurn(BaseModel):
    role: Literal["user", "assistant"]
    content: str


class CourseContext(BaseModel):
    """The course a tutoring conversation is about (PB-19). The .NET API already checked the enrollment."""

    code: str = Field(max_length=32)
    name: str = Field(max_length=200)
    description: str | None = Field(default=None, max_length=4000)


class ChatRequest(BaseModel):
    """Sent by the .NET API. The student's JWT comes in the Authorization header, not here."""

    message: str = Field(min_length=1, max_length=4000)
    history: list[ChatTurn] = Field(default_factory=list, max_length=50)
    mode: Literal["assistant", "tutor"] = "assistant"
    course: CourseContext | None = None


class ChatResponse(BaseModel):
    answer: str
    tools_used: list[str] = Field(default_factory=list)


class IngestRequest(BaseModel):
    """The .NET API stores the uploaded file and sends its text inline."""

    document_id: int = Field(gt=0)
    title: str = Field(min_length=1, max_length=200)
    content: str = Field(min_length=1, max_length=2_000_000)


class IngestResponse(BaseModel):
    document_id: int
    chunks: int
