# AI service rules (Python, FastAPI)

## Boundaries
- The AI service **only does AI**. The .NET API owns every business table.
- It may write only to `KnowledgeChunks` (during `/ingest`). Everything else is read through the .NET API.
- It never receives an admin token. `/chat` gets the **student's own JWT** from the .NET API and forwards it
  on every tool call (`clients/sis_api.py`), so a student can only ever see their own data.
- The .NET API saves chat messages. The AI service is stateless between requests.
- Calls from .NET → Python are authenticated with a shared service key header (`X-Service-Key`).

## Structure
```
app/
├── main.py       FastAPI app: POST /chat, POST /ingest, GET /health
├── config.py     settings from environment (.env), never hardcoded keys
├── chat/         agent.py (LLM loop + tools), tools.py, prompts.py
├── rag/          chunker.py, embeddings.py, search.py
├── clients/      sis_api.py
└── career/       parked (PROJECT.md §9)
```

## Rules
- Tools are thin: each tool = one .NET endpoint call, returns compact JSON for the model.
- The system prompt tells the model to answer only from tool results / knowledge base for personal or policy
  questions, and to say when it doesn't know. No invented grades, deadlines or policies.
- Concept explanations (e.g. "what is recursion") may use the model's own knowledge.
- Never log full JWTs or full student records. Log request IDs.
- Pydantic models for every request/response. Type hints everywhere.
- Config via environment: `LLM_API_KEY`, `LLM_MODEL`, `SIS_API_BASE_URL`, `SERVICE_KEY`, `SQL_CONNECTION_STRING`
  (KnowledgeChunks only).
- `pip install -r requirements.txt` must work on a clean venv; pin versions.
