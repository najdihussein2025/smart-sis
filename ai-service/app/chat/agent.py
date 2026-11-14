"""LLM loop with tool calling (PROJECT.md §6.2 chat flow).

The .NET API sends the message, recent history and the student's own JWT. In assistant mode the model may call the tools
in tools.py, each of which calls the .NET API with that JWT, and search the knowledge base. In tutor mode it gets a
separate prompt and no tools: it only explains concepts of the chosen course.
"""

import logging
import re
from collections.abc import Callable
from dataclasses import dataclass, field
from typing import Any, Protocol

from app.chat.prompts import GRADED_WORK_GUARD, SYSTEM_PROMPT, tutor_prompt
from app.chat.tools import TOOL_DEFINITIONS, Searcher, run_tool
from app.clients.sis_api import SisApiClient
from app.schemas import ChatRequest, ChatResponse

log = logging.getLogger(__name__)


class LlmNotConfigured(Exception):
    """No LLM API key: /chat answers 503."""


class LlmUnavailable(Exception):
    """The LLM provider failed (network, rate limit, outage): /chat answers 502."""


@dataclass
class ToolCall:
    id: str
    name: str
    input: dict[str, Any]


@dataclass
class LlmReply:
    stop_reason: str | None
    text: str
    tool_calls: list[ToolCall] = field(default_factory=list)
    # The assistant content exactly as returned, appended back unchanged on the next request.
    raw_content: Any = None


class Llm(Protocol):
    def create(self, system: str, messages: list[dict[str, Any]], tools: list[dict[str, Any]]) -> LlmReply: ...


ApiFactory = Callable[[str], SisApiClient]

# Phrases that usually mean "do my graded work for me". The tutor prompt already forbids it; this adds a reminder.
_GRADED_WORK = re.compile(
    r"\b(solve|do|answer|finish|complete|write)\s+(?:\w+\s+){0,2}?(my|this|the|our)\s+(?:\w+\s+){0,3}?"
    r"(assignment|homework|hw|quiz|exam|test|midterm|final|lab|project|essay|report)s?\b"
    r"|\b(answers?|solutions?)\s+(to|for)\s+(?:\w+\s+){0,3}?(assignment|homework|quiz|exam|test|midterm|lab)s?\b",
    re.IGNORECASE | re.DOTALL,
)


def looks_like_graded_work(message: str) -> bool:
    return bool(_GRADED_WORK.search(message))


FALLBACK_ANSWER = "Sorry, I couldn't finish answering that. Please try asking in a simpler way."
REFUSAL_ANSWER = "Sorry, I can't help with that request."


def run(
    request: ChatRequest,
    token: str,
    llm: Llm,
    api_factory: ApiFactory,
    search: Searcher,
    max_rounds: int = 6,
) -> ChatResponse:
    """One chat turn. `token` is the caller's own JWT; it is only handed to `api_factory`, never logged."""
    tutor = request.mode == "tutor"
    system = tutor_prompt(request.course) if tutor else SYSTEM_PROMPT
    if tutor and looks_like_graded_work(request.message):
        system = f"{system}\n\n{GRADED_WORK_GUARD}"
    tools = [] if tutor else TOOL_DEFINITIONS

    messages: list[dict[str, Any]] = [{"role": t.role, "content": t.content} for t in request.history]
    messages.append({"role": "user", "content": request.message})

    used: list[str] = []
    with api_factory(token) as api:
        for _ in range(max_rounds):
            reply = llm.create(system, messages, tools)
            if reply.stop_reason == "refusal":
                return ChatResponse(answer=REFUSAL_ANSWER, tools_used=used)
            if reply.stop_reason == "pause_turn":
                messages.append({"role": "assistant", "content": reply.raw_content})
                continue
            if reply.stop_reason != "tool_use" or not reply.tool_calls:
                return ChatResponse(answer=reply.text.strip() or FALLBACK_ANSWER, tools_used=used)

            messages.append({"role": "assistant", "content": reply.raw_content})
            results = []
            for call in reply.tool_calls:
                used.append(call.name)
                log.info("tool call %s", call.name)
                results.append(
                    {"type": "tool_result", "tool_use_id": call.id, "content": run_tool(call.name, call.input, api, search)}
                )
            messages.append({"role": "user", "content": results})

    return ChatResponse(answer=FALLBACK_ANSWER, tools_used=used)


class AnthropicLlm:
    """Claude through the Anthropic Python SDK (manual tool loop above, so tests can swap in a fake)."""

    def __init__(self, api_key: str, model: str, effort: str, max_tokens: int) -> None:
        if not api_key:
            raise LlmNotConfigured("LLM_API_KEY is not set.")
        import anthropic  # local import keeps the module importable in tests without touching the SDK

        self._anthropic = anthropic
        self._client = anthropic.Anthropic(api_key=api_key, timeout=90.0, max_retries=2)
        self._model = model
        self._effort = effort
        self._max_tokens = max_tokens

    def create(self, system: str, messages: list[dict[str, Any]], tools: list[dict[str, Any]]) -> LlmReply:
        a = self._anthropic
        kwargs: dict[str, Any] = {
            "model": self._model,
            "max_tokens": self._max_tokens,
            "system": system,
            "messages": messages,
            "output_config": {"effort": self._effort},
            # On a policy decline the API retries on its default fallback model inside the same call.
            "betas": ["server-side-fallback-2026-07-01"],
            "fallbacks": "default",
        }
        if tools:
            kwargs["tools"] = tools
        try:
            response = self._client.beta.messages.create(**kwargs)
        except a.AuthenticationError as ex:
            raise LlmNotConfigured("The LLM API key was rejected.") from ex
        except a.APIStatusError as ex:
            log.warning("LLM error %s (request id %s)", ex.status_code, getattr(ex, "request_id", None))
            raise LlmUnavailable(f"LLM error {ex.status_code}") from ex
        except a.APIConnectionError as ex:
            log.warning("LLM unreachable: %s", type(ex).__name__)
            raise LlmUnavailable("LLM unreachable") from ex

        text = "".join(b.text for b in response.content if b.type == "text")
        calls = [ToolCall(b.id, b.name, dict(b.input or {})) for b in response.content if b.type == "tool_use"]
        return LlmReply(response.stop_reason, text, calls, response.content)
