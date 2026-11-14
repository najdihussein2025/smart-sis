"""RAG (T61): deterministic embeddings, ingest, cosine search, connection-string parsing, SIS client."""

import httpx
import pytest

from app.clients.sis_api import SisApiClient, SisApiError
from app.rag.embeddings import HashingEmbedder, pack, tokenize
from app.rag.search import cosine, ingest, search
from app.rag.store import parse_connection_string
from tests.fakes import MemoryStore

E = HashingEmbedder()

POLICIES = {
    1: "Attendance: the absence limit is three absences per credit. Over the limit the student is dropped with an F.",
    2: "Payments: tuition charges have a due date. Partial payments are accepted until the charge is paid.",
    3: "Credit load: the default maximum is 18 credits per term. An overload needs registrar approval.",
}


def make_store() -> MemoryStore:
    store = MemoryStore()
    for doc, text in POLICIES.items():
        ingest(doc, text, store, E)
        store.titles[doc] = f"Policy {doc}"
        store.ready.add(doc)
    return store


def test_embedding_is_deterministic_and_normalised() -> None:
    a, b = E.embed("Absence limit per credit"), E.embed("Absence limit per credit")
    assert a == b and len(a) == 512
    assert abs(sum(x * x for x in a) - 1.0) < 1e-9


def test_tokenize_drops_stopwords_and_stems() -> None:
    assert tokenize("The credits of the students") == ["credit", "student"]


def test_similar_text_scores_higher_than_unrelated() -> None:
    q = E.embed("how many absences am I allowed")
    assert cosine(q, E.embed(POLICIES[1])) > cosine(q, E.embed(POLICIES[2]))


@pytest.mark.parametrize(
    ("query", "doc"),
    [("absence limit", 1), ("when is tuition due, can I pay partially", 2), ("maximum credits per term overload", 3)],
)
def test_search_finds_the_right_policy(query: str, doc: int) -> None:
    results = search(query, make_store(), E)
    assert results and results[0].document_id == doc
    assert results[0].title == f"Policy {doc}"


def test_search_ignores_documents_that_are_not_ready() -> None:
    store = make_store()
    store.ready.discard(1)
    assert all(p.document_id != 1 for p in search("absence limit", store, E))


def test_search_embeds_chunks_without_a_vector_on_the_fly() -> None:
    store = MemoryStore()
    store.chunks[9] = [(POLICIES[3], b"")]  # seeded by the .NET DevSeeder with an empty embedding
    store.ready.add(9)
    assert search("credits per term", store, E)[0].document_id == 9


def test_search_unrelated_query_returns_nothing() -> None:
    assert search("zebra quantum guitar", make_store(), E) == []


def test_reingest_replaces_old_chunks() -> None:
    store = MemoryStore()
    ingest(5, "old text " * 300, store, E)
    ingest(5, "new text", store, E)
    assert store.chunks[5] == [("new text", pack(E.embed("new text")))]


def test_parse_connection_string() -> None:
    args = parse_connection_string("Server=db,1433;Database=SmartSis;User Id=sa;Password=P@ss=w;rd;TrustServerCertificate=True")
    assert args["server"] == "db" and args["port"] == 1433 and args["database"] == "SmartSis" and args["user"] == "sa"
    assert parse_connection_string("Data Source=tcp:host;Initial Catalog=X;UID=u;PWD=p") == {
        "server": "host", "port": 1433, "user": "u", "password": "p", "database": "X",
    }


def test_sis_client_sends_only_the_given_token_and_maps_errors() -> None:
    seen: list[str | None] = []

    def handler(request: httpx.Request) -> httpx.Response:
        seen.append(request.headers.get("Authorization"))
        if request.url.path == "/api/grades/my":
            return httpx.Response(404, json={"title": "Not here yet."})
        return httpx.Response(200, json={"ok": True})

    with SisApiClient("tok-x", "http://sis.test", transport=httpx.MockTransport(handler)) as api:
        assert api.get("/api/profile") == {"ok": True}
        with pytest.raises(SisApiError) as err:
            api.get("/api/grades/my")
    assert err.value.status == 404 and str(err.value) == "Not here yet."
    assert seen == ["Bearer tok-x", "Bearer tok-x"]
