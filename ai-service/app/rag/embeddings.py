"""Text embeddings + float32 <-> varbinary packing (KnowledgeChunks.Embedding).

Anthropic has no embeddings API, so the default embedder is self-contained and deterministic: a hashed bag of words
(unigrams + bigrams, sublinear term frequency, L2-normalised). No model download, no extra dependency, and the same text
always gives the same vector, which keeps tests exact. It sits behind the `Embedder` protocol so a neural embedder can
replace it later; after a swap, re-ingest the documents (search re-embeds chunks whose vector has the wrong size).
"""

import hashlib
import math
import re
import struct
from typing import Protocol

_TOKEN = re.compile(r"[^\W_]+", re.UNICODE)

# Common English words that carry no meaning for retrieval.
_STOPWORDS = frozenset(
    """a about above after again all am an and any are as at be because been before being below between both but by can
    could did do does doing down during each few for from further had has have having he her here hers him his how i if in
    into is it its itself just me more most my no nor not now of off on once only or other our ours out over own same she
    should so some such than that the their them then there these they this those through to too under until up very was
    we were what when where which while who whom why will with would you your yours""".split()
)


class Embedder(Protocol):
    dim: int

    def embed(self, text: str) -> list[float]: ...


def tokenize(text: str) -> list[str]:
    tokens = [t for t in _TOKEN.findall(text.lower()) if t not in _STOPWORDS]
    # Light stemming: "credits" ~ "credit", "dropped"/"drops" ~ "drop".
    return [_stem(t) for t in tokens]


def _stem(token: str) -> str:
    for suffix in ("ing", "ed", "es", "s"):
        if len(token) > len(suffix) + 2 and token.endswith(suffix):
            return token[: -len(suffix)]
    return token


class HashingEmbedder:
    """Feature hashing into `dim` buckets with a sign bit (blake2b, so it is stable across processes)."""

    def __init__(self, dim: int = 512) -> None:
        self.dim = dim

    def embed(self, text: str) -> list[float]:
        tokens = tokenize(text)
        features = tokens + [f"{a} {b}" for a, b in zip(tokens, tokens[1:])]
        counts: dict[str, int] = {}
        for f in features:
            counts[f] = counts.get(f, 0) + 1

        vector = [0.0] * self.dim
        for feature, count in counts.items():
            digest = hashlib.blake2b(feature.encode("utf-8"), digest_size=8).digest()
            value = int.from_bytes(digest, "little")
            index = value % self.dim
            sign = 1.0 if (value >> 63) & 1 else -1.0
            weight = (1.0 + math.log(count)) * (0.5 if " " in feature else 1.0)
            vector[index] += sign * weight

        norm = math.sqrt(sum(v * v for v in vector))
        return [v / norm for v in vector] if norm else vector


def pack(vector: list[float]) -> bytes:
    return struct.pack(f"<{len(vector)}f", *vector)


def unpack(blob: bytes) -> list[float]:
    return list(struct.unpack(f"<{len(blob) // 4}f", blob))
