"""POST /career/explain: the LLM explains the student's top careers (PB-17 / T66)."""

from typing import Annotated

from fastapi import APIRouter, Depends, Header, HTTPException, status

from app.career.explainer import explain
from app.career.schemas import CareerExplainRequest, CareerExplainResponse
from app.config import Settings, get_settings

router = APIRouter(prefix="/career", tags=["career"])


def require_service_key(
    settings: Annotated[Settings, Depends(get_settings)],
    x_service_key: Annotated[str | None, Header()] = None,
) -> None:
    """Only the .NET API may call this service."""
    if not settings.service_key or x_service_key != settings.service_key:
        raise HTTPException(status.HTTP_401_UNAUTHORIZED, "Invalid service key.")


@router.post("/explain", response_model=CareerExplainResponse, dependencies=[Depends(require_service_key)])
def explain_careers(
    request: CareerExplainRequest, settings: Annotated[Settings, Depends(get_settings)]
) -> CareerExplainResponse:
    """Never fails because of the LLM: without a key, or on any LLM error, returns the built-in text (source "fallback")."""
    return explain(request, settings)
