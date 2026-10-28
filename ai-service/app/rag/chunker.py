"""Split documents into overlapping chunks for embedding. Built in phase 7."""


def chunk_text(text: str, size: int = 800, overlap: int = 120) -> list[str]:
    if size <= overlap:
        raise ValueError("size must be larger than overlap")
    words = text.split()
    chunks: list[str] = []
    start = 0
    while start < len(words):
        chunks.append(" ".join(words[start : start + size]))
        start += size - overlap
    return chunks
