"""Cosine similarity over KnowledgeChunks (SQL Server 2022 has no VECTOR type, so the search runs here)."""

from dataclasses import dataclass

from app.rag.chunker import chunk_text
from app.rag.embeddings import Embedder, pack, unpack
from app.rag.store import KnowledgeStore


@dataclass(frozen=True)
class Passage:
    document_id: int
    title: str
    content: str
    score: float


def cosine(a: list[float], b: list[float]) -> float:
    dot = sum(x * y for x, y in zip(a, b))
    na = sum(x * x for x in a) ** 0.5
    nb = sum(y * y for y in b) ** 0.5
    return dot / (na * nb) if na and nb else 0.0


def search(query: str, store: KnowledgeStore, embedder: Embedder, top_k: int = 4, min_score: float = 0.08) -> list[Passage]:
    """Best matching passages of Ready documents. Chunks stored without a vector of the current size
    (seeded demo chunks, or after the embedder changed) are embedded on the fly."""
    q = embedder.embed(query)
    scored: list[Passage] = []
    for chunk in store.load_ready_chunks():
        vector = unpack(chunk.embedding) if len(chunk.embedding) == embedder.dim * 4 else embedder.embed(chunk.content)
        score = cosine(q, vector)
        if score >= min_score:
            scored.append(Passage(chunk.document_id, chunk.title, chunk.content, round(score, 3)))
    scored.sort(key=lambda p: p.score, reverse=True)
    return scored[:top_k]


def ingest(document_id: int, content: str, store: KnowledgeStore, embedder: Embedder) -> int:
    """Chunk, embed and store (replacing the document's old chunks). Returns the number of chunks."""
    chunks = chunk_text(content)
    store.replace_chunks(document_id, [(c, pack(embedder.embed(c))) for c in chunks])
    return len(chunks)
