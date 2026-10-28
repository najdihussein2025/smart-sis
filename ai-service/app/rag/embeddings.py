"""Embedding model + float32 <-> varbinary packing (KnowledgeChunks.Embedding). Built in phase 7."""

import struct


def pack(vector: list[float]) -> bytes:
    return struct.pack(f"<{len(vector)}f", *vector)


def unpack(blob: bytes) -> list[float]:
    return list(struct.unpack(f"<{len(blob) // 4}f", blob))
