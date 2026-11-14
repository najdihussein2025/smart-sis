"""Career explanation (PB-17 / T66): the LLM writes why the top careers fit; a built-in text is used when it can't.

The ranking is decided by the .NET rules (CareerScoring). The model only explains it, from the scores it is given.
"""

import json
import logging
from collections.abc import Callable
from typing import Any

import anthropic

from app.career.schemas import CareerExplainRequest, CareerExplainResponse
from app.config import Settings

logger = logging.getLogger(__name__)

DEFAULT_MODEL = "claude-opus-5-5"

# Models that accept server-side refusal fallbacks and effort (see the Anthropic docs). Others get a plain request.
_FALLBACK_MODELS = {"claude-opus-5-5", "claude-opus-5", "claude-sonnet-5-5", "claude-fable-5-2", "claude-fable-5-1"}
_FALLBACK_BETA = "server-side-fallback-2026-07-01"

SYSTEM_PROMPT = (
    "You are a friendly career advisor at Antonine University. A rule-based system has already matched a student to "
    "their top careers. Explain in 3 to 5 short sentences, in plain English and second person, why these careers fit, "
    "using only the scores, traits and courses you are given. Mention the best match first. Never invent grades, "
    "courses or facts, never change the ranking or the percentages, and don't give a heading or a list. "
    "Do not include any XML tags in your answer."
)

LlmCall = Callable[[Settings, str, str], str | None]


def build_prompt(request: CareerExplainRequest) -> str:
    """Compact JSON for the model: no personal data is ever in the request."""
    data: dict[str, Any] = {
        "topCareers": [
            {
                "name": c.name,
                "matchPct": c.match_pct,
                "fromQuizPct": c.quiz_pct,
                "fromGradesPct": c.grade_pct,
                "strongestTraits": c.top_traits,
            }
            for c in request.careers
        ],
        "traitScores": {t.name: t.score for t in request.traits},
        "passedCourses": request.completed_courses,
        "gradesUsed": request.uses_grades,
    }
    return (
        "Scores combine the career assessment (about 70%) with grades in related courses (about 30%) when grades exist.\n"
        f"Student data:\n{json.dumps(data, ensure_ascii=False)}\n\nWrite the explanation."
    )


def fallback_text(request: CareerExplainRequest) -> str:
    best = request.careers[0]
    text = f"Your strongest match is {best.name} ({best.match_pct}%)"
    text += f", driven mostly by {_join(best.top_traits)}." if best.top_traits else "."
    others = [f"{c.name} ({c.match_pct}%)" for c in request.careers[1:]]
    if others:
        text += f" You also fit {_join(others)}."
    return text


def _join(items: list[str]) -> str:
    if len(items) <= 1:
        return "".join(items)
    return f"{', '.join(items[:-1])} and {items[-1]}"


def call_anthropic(settings: Settings, system: str, prompt: str) -> str | None:
    """One Messages API call. Returns the text, or None when the model refused or wrote nothing."""
    client = anthropic.Anthropic(api_key=settings.llm_api_key, timeout=15.0, max_retries=0)
    model = settings.llm_model or DEFAULT_MODEL
    messages: list[anthropic.types.MessageParam] = [{"role": "user", "content": prompt}]

    if model in _FALLBACK_MODELS:
        # A short explanation needs little thinking; refusals fall back server-side to another model.
        response: Any = client.beta.messages.create(
            model=model,
            max_tokens=2000,
            system=system,
            messages=messages,  # type: ignore[arg-type]
            output_config={"effort": "low"},
            betas=[_FALLBACK_BETA],
            fallbacks="default",
        )
    else:
        response = client.messages.create(model=model, max_tokens=2000, system=system, messages=messages)

    if response.stop_reason == "refusal":
        logger.warning("Career explanation refused by the model (request %s).", getattr(response, "_request_id", None))
        return None
    text = "".join(block.text for block in response.content if block.type == "text").strip()
    return text or None


def explain(request: CareerExplainRequest, settings: Settings, llm: LlmCall | None = None) -> CareerExplainResponse:
    """Never raises because of the LLM: no key, an API error or an empty answer all give the built-in text."""
    if settings.llm_api_key:
        try:
            text = (llm or call_anthropic)(settings, SYSTEM_PROMPT, build_prompt(request))
            if text and text.strip():
                return CareerExplainResponse(explanation=text.strip(), source="llm")
        except anthropic.APIError as e:
            logger.warning("Career explanation LLM call failed: %s", type(e).__name__)
    return CareerExplainResponse(explanation=fallback_text(request), source="fallback")
