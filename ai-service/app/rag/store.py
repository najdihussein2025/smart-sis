"""KnowledgeChunks access. The only table the AI service writes (docs/rules/ai-service.md).

Reads join KnowledgeDocuments for the title and to keep only documents whose Status is 'Ready'.
"""

from dataclasses import dataclass
from typing import Any, Protocol


@dataclass(frozen=True)
class StoredChunk:
    document_id: int
    title: str
    content: str
    embedding: bytes


class KnowledgeStore(Protocol):
    def replace_chunks(self, document_id: int, chunks: list[tuple[str, bytes]]) -> None: ...

    def load_ready_chunks(self) -> list[StoredChunk]: ...


class StoreNotConfigured(Exception):
    pass


def parse_connection_string(value: str) -> dict[str, Any]:
    """ADO.NET style ("Server=db,1433;Database=SmartSis;User Id=sa;Password=...") to pymssql arguments."""
    parts: dict[str, str] = {}
    for item in value.split(";"):
        if "=" in item:
            key, _, val = item.partition("=")
            parts[key.strip().lower()] = val.strip()

    server = parts.get("server") or parts.get("data source") or parts.get("address") or "localhost"
    server = server.removeprefix("tcp:")
    host, _, port = server.partition(",")
    return {
        "server": host,
        "port": int(port) if port else 1433,
        "user": parts.get("user id") or parts.get("uid") or parts.get("user") or "",
        "password": parts.get("password") or parts.get("pwd") or "",
        "database": parts.get("database") or parts.get("initial catalog") or "",
    }


class SqlKnowledgeStore:
    """pymssql (FreeTDS bundled in the wheel, so no ODBC driver in the image)."""

    def __init__(self, connection_string: str) -> None:
        if not connection_string:
            raise StoreNotConfigured("SQL_CONNECTION_STRING is not set.")
        self._args = parse_connection_string(connection_string)

    def _connect(self) -> Any:
        import pymssql  # imported lazily so tests and /chat without a database never need it

        return pymssql.connect(**self._args, login_timeout=10, timeout=30)

    def replace_chunks(self, document_id: int, chunks: list[tuple[str, bytes]]) -> None:
        """Old chunks of the document are removed and the new ones inserted in one transaction."""
        with self._connect() as conn:
            with conn.cursor() as cur:
                cur.execute("DELETE FROM KnowledgeChunks WHERE DocumentId = %s", (document_id,))
                if chunks:
                    cur.executemany(
                        "INSERT INTO KnowledgeChunks (DocumentId, Content, Embedding, CreatedAt, UpdatedAt) "
                        "VALUES (%s, %s, %s, SYSUTCDATETIME(), SYSUTCDATETIME())",
                        [(document_id, content, embedding) for content, embedding in chunks],
                    )
            conn.commit()

    def load_ready_chunks(self) -> list[StoredChunk]:
        with self._connect() as conn:
            with conn.cursor() as cur:
                cur.execute(
                    "SELECT c.DocumentId, d.Title, c.Content, c.Embedding FROM KnowledgeChunks c "
                    "JOIN KnowledgeDocuments d ON d.Id = c.DocumentId WHERE d.Status = 'Ready'"
                )
                return [StoredChunk(int(r[0]), str(r[1]), str(r[2]), bytes(r[3] or b"")) for r in cur.fetchall()]
