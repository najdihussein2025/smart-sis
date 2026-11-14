"""Smart SIS AI service. Only does AI; the .NET API owns all business data (docs/rules/ai-service.md).

Each feature has its own APIRouter; add one `include_router` line per feature.
"""

import logging

from fastapi import FastAPI

from app.chat.router import router as chat_router
from app.rag.router import router as ingest_router

logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(name)s %(message)s")

app = FastAPI(title="Smart SIS AI service", version="0.2.0")


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}


app.include_router(chat_router)
app.include_router(ingest_router)
