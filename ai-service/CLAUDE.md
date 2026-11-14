# ai-service/ — read before editing

@../docs/rules/ai-service.md

- `/chat` (assistant + tutor agent, `app/chat/`) and `/ingest` (RAG, `app/rag/`) are built. Each feature has an
  `APIRouter`; `main.py` only includes them.
- Tests never call the LLM, the .NET API or SQL Server: override `get_llm_factory`, `get_api_factory`,
  `get_store_factory` (see `tests/fakes.py`).
- `pytest` must pass.
