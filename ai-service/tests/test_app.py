"""HTTP surface of the AI service (T60, T61, T64): service key, student token, 503 without an LLM key, ingest."""

from collections.abc import Iterator

import pytest
from fastapi.testclient import TestClient

from app.chat.agent import LlmNotConfigured
from app.config import Settings, get_settings
from app.deps import get_api_factory, get_llm_factory, get_store_factory
from app.main import app
from app.rag.chunker import chunk_text
from app.rag.embeddings import pack, unpack
from app.rag.store import StoreNotConfigured
from tests.fakes import FakeSisApi, MemoryStore, ScriptedLlm, answer, tool_use

KEY = {"X-Service-Key": "test-key"}


@pytest.fixture
def client() -> Iterator[TestClient]:
    app.dependency_overrides[get_settings] = lambda: Settings(service_key="test-key")
    yield TestClient(app)
    app.dependency_overrides.clear()


def test_health(client: TestClient) -> None:
    assert client.get("/health").json() == {"status": "ok"}


def test_chat_requires_service_key(client: TestClient) -> None:
    res = client.post("/chat", json={"message": "hi"}, headers={"Authorization": "Bearer t"})
    assert res.status_code == 401


def test_chat_wrong_service_key_401(client: TestClient) -> None:
    res = client.post("/chat", json={"message": "hi"}, headers={"Authorization": "Bearer t", "X-Service-Key": "nope"})
    assert res.status_code == 401


def test_chat_requires_student_token(client: TestClient) -> None:
    res = client.post("/chat", json={"message": "hi"}, headers=KEY)
    assert res.status_code == 401


def test_chat_without_llm_key_returns_503(client: TestClient) -> None:
    # Real factory with an empty LLM_API_KEY.
    res = client.post("/chat", json={"message": "hi"}, headers={**KEY, "Authorization": "Bearer t"})
    assert res.status_code == 503


def test_chat_end_to_end_forwards_the_callers_token(client: TestClient) -> None:
    api = FakeSisApi({"tok-a": {"/api/schedule/my": {"items": [{"courseCode": "CSC 210", "dayOfWeek": 1}]}}})
    llm = ScriptedLlm(tool_use(("t1", "get_my_schedule", {})), answer("You have CSC 210 on Monday."))
    app.dependency_overrides[get_llm_factory] = lambda: (lambda: llm)
    app.dependency_overrides[get_api_factory] = lambda: api.factory

    res = client.post("/chat", json={"message": "When is my class?"}, headers={**KEY, "Authorization": "Bearer tok-a"})

    assert res.status_code == 200
    assert res.json() == {"answer": "You have CSC 210 on Monday.", "tools_used": ["get_my_schedule"]}
    assert api.calls == [("/api/schedule/my", "Bearer tok-a")]


def test_chat_llm_not_configured_maps_to_503(client: TestClient) -> None:
    def factory():  # type: ignore[no-untyped-def]
        raise LlmNotConfigured("no key")

    app.dependency_overrides[get_llm_factory] = lambda: factory
    res = client.post("/chat", json={"message": "hi"}, headers={**KEY, "Authorization": "Bearer t"})
    assert res.status_code == 503


def test_chat_rejects_bad_mode(client: TestClient) -> None:
    res = client.post("/chat", json={"message": "hi", "mode": "admin"}, headers={**KEY, "Authorization": "Bearer t"})
    assert res.status_code == 422


def test_ingest_requires_service_key(client: TestClient) -> None:
    res = client.post("/ingest", json={"document_id": 1, "title": "T", "content": "x"})
    assert res.status_code == 401


def test_ingest_writes_chunks(client: TestClient) -> None:
    store = MemoryStore()
    app.dependency_overrides[get_store_factory] = lambda: (lambda: store)
    text = " ".join(f"word{i}" for i in range(400))

    res = client.post("/ingest", json={"document_id": 7, "title": "FAQ", "content": text}, headers=KEY)

    assert res.status_code == 200
    body = res.json()
    assert body["document_id"] == 7 and body["chunks"] == len(store.chunks[7]) >= 2
    content, blob = store.chunks[7][0]
    assert content.startswith("word0 ") and len(blob) == 512 * 4


def test_ingest_without_database_returns_503(client: TestClient) -> None:
    def factory():  # type: ignore[no-untyped-def]
        raise StoreNotConfigured("no db")

    app.dependency_overrides[get_store_factory] = lambda: factory
    res = client.post("/ingest", json={"document_id": 1, "title": "T", "content": "x"}, headers=KEY)
    assert res.status_code == 503


def test_chunker_overlaps() -> None:
    chunks = chunk_text(" ".join(str(i) for i in range(10)), size=4, overlap=1)
    assert chunks[0] == "0 1 2 3" and chunks[1].startswith("3")
    assert chunks[-1].endswith("9")


def test_chunker_short_text_is_one_chunk() -> None:
    assert chunk_text("one two three") == ["one two three"]
    assert chunk_text("   ") == []


def test_embedding_roundtrip() -> None:
    v = [0.5, -1.25, 3.0]
    assert unpack(pack(v)) == v
