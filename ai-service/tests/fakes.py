"""Test doubles: a scripted LLM, a fake .NET API (httpx MockTransport) and an in-memory KnowledgeChunks store."""

import json
from typing import Any

import httpx

from app.chat.agent import LlmReply, ToolCall
from app.clients.sis_api import SisApiClient
from app.rag.store import StoredChunk


class ScriptedLlm:
    """Returns the scripted replies in order and records every request."""

    def __init__(self, *replies: LlmReply) -> None:
        self.replies = list(replies)
        self.requests: list[dict[str, Any]] = []

    def create(self, system: str, messages: list[dict[str, Any]], tools: list[dict[str, Any]]) -> LlmReply:
        # Copy: the agent keeps appending to the same list.
        self.requests.append({"system": system, "messages": json.loads(json.dumps(messages)), "tools": tools})
        return self.replies.pop(0)


def tool_use(*calls: tuple[str, str, dict[str, Any]]) -> LlmReply:
    blocks = [{"type": "tool_use", "id": i, "name": n, "input": inp} for i, n, inp in calls]
    return LlmReply("tool_use", "", [ToolCall(i, n, inp) for i, n, inp in calls], blocks)


def answer(text: str) -> LlmReply:
    return LlmReply("end_turn", text, [], [{"type": "text", "text": text}])


class FakeSisApi:
    """Answers like the .NET API, but only for the token it was given: data is keyed by token."""

    def __init__(self, data_by_token: dict[str, dict[str, Any]]) -> None:
        self.data_by_token = data_by_token
        self.calls: list[tuple[str, str | None]] = []

    def handler(self, request: httpx.Request) -> httpx.Response:
        auth = request.headers.get("Authorization")
        self.calls.append((request.url.path, auth))
        token = auth.removeprefix("Bearer ") if auth else None
        data = self.data_by_token.get(token or "")
        if data is None:
            return httpx.Response(401, json={"title": "Not signed in."})
        if request.url.path not in data:
            return httpx.Response(404, json={"title": "Not found."})
        return httpx.Response(200, json=data[request.url.path])

    def factory(self, token: str) -> SisApiClient:
        return SisApiClient(token, "http://sis.test", transport=httpx.MockTransport(self.handler))


class MemoryStore:
    def __init__(self) -> None:
        self.chunks: dict[int, list[tuple[str, bytes]]] = {}
        self.titles: dict[int, str] = {}
        self.ready: set[int] = set()

    def replace_chunks(self, document_id: int, chunks: list[tuple[str, bytes]]) -> None:
        self.chunks[document_id] = list(chunks)

    def load_ready_chunks(self) -> list[StoredChunk]:
        return [
            StoredChunk(doc, self.titles.get(doc, f"Doc {doc}"), content, emb)
            for doc, rows in self.chunks.items()
            if doc in self.ready
            for content, emb in rows
        ]
