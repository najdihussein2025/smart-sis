"""Split documents into overlapping chunks for embedding."""

# Policy documents are short; ~180 words keeps one rule per passage, the overlap keeps sentences that straddle a cut.
DEFAULT_SIZE = 180
DEFAULT_OVERLAP = 30


def chunk_text(text: str, size: int = DEFAULT_SIZE, overlap: int = DEFAULT_OVERLAP) -> list[str]:
    """Word windows of `size` words, each starting `size - overlap` words after the previous one."""
    if size <= overlap:
        raise ValueError("size must be larger than overlap")
    words = text.split()
    chunks: list[str] = []
    start = 0
    while start < len(words):
        chunks.append(" ".join(words[start : start + size]))
        if start + size >= len(words):
            break
        start += size - overlap
    return chunks
