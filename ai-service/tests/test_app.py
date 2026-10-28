from fastapi.testclient import TestClient

from app.config import Settings, get_settings
from app.main import app
from app.rag.chunker import chunk_text
from app.rag.embeddings import pack, unpack

app.dependency_overrides[get_settings] = lambda: Settings(service_key="test-key")
client = TestClient(app)


def test_health() -> None:
    assert client.get("/health").json() == {"status": "ok"}


def test_chat_requires_service_key() -> None:
    res = client.post("/chat", json={"message": "hi"}, headers={"Authorization": "Bearer t"})
    assert res.status_code == 401


def test_chat_requires_student_token() -> None:
    res = client.post("/chat", json={"message": "hi"}, headers={"X-Service-Key": "test-key"})
    assert res.status_code == 401


def test_chunker_overlaps() -> None:
    chunks = chunk_text(" ".join(str(i) for i in range(10)), size=4, overlap=1)
    assert chunks[0] == "0 1 2 3" and chunks[1].startswith("3")


def test_embedding_roundtrip() -> None:
    v = [0.5, -1.25, 3.0]
    assert unpack(pack(v)) == v
