"""POST /career/explain (PB-17 / T66, T68). The LLM is always mocked: no network calls."""

from types import SimpleNamespace
from typing import Any

import anthropic
import httpx
import pytest
from fastapi.testclient import TestClient

from app.career import explainer
from app.career.schemas import CareerExplainRequest
from app.config import Settings, get_settings
from app.main import app

BODY: dict[str, Any] = {
    "careers": [
        {"name": "Data Scientist", "matchPct": 81, "quizPct": 78, "gradePct": 88, "topTraits": ["Analytical thinking", "Attention to detail"]},
        {"name": "Software Engineer", "matchPct": 77, "quizPct": 75, "gradePct": None, "topTraits": ["Technical aptitude"]},
    ],
    "traits": [{"name": "Analytical thinking", "score": 90}],
    "completedCourses": ["CSC 101 Introduction to Programming"],
    "usesGrades": True,
}
HEADERS = {"X-Service-Key": "test-key"}


@pytest.fixture
def client_with(monkeypatch: pytest.MonkeyPatch):
    """A TestClient with the given settings; restores the shared overrides afterwards."""
    previous = dict(app.dependency_overrides)

    def make(settings: Settings) -> TestClient:
        app.dependency_overrides[get_settings] = lambda: settings
        return TestClient(app)

    yield make
    app.dependency_overrides.clear()
    app.dependency_overrides.update(previous)


def test_explain_requires_service_key(client_with) -> None:
    client = client_with(Settings(service_key="test-key"))
    assert client.post("/career/explain", json=BODY).status_code == 401
    assert client.post("/career/explain", json=BODY, headers={"X-Service-Key": "wrong"}).status_code == 401


def test_explain_validates_body(client_with) -> None:
    client = client_with(Settings(service_key="test-key"))
    bad = {**BODY, "careers": [{**BODY["careers"][0], "matchPct": 150}]}
    assert client.post("/career/explain", json=bad, headers=HEADERS).status_code == 422
    assert client.post("/career/explain", json={**BODY, "careers": []}, headers=HEADERS).status_code == 422


def test_explain_without_llm_key_returns_fallback(client_with) -> None:
    client = client_with(Settings(service_key="test-key", llm_api_key=""))
    res = client.post("/career/explain", json=BODY, headers=HEADERS)
    assert res.status_code == 200
    assert res.json() == {
        "explanation": "Your strongest match is Data Scientist (81%), driven mostly by Analytical thinking and Attention to "
        "detail. You also fit Software Engineer (77%).",
        "source": "fallback",
    }


def test_explain_uses_llm_text(client_with, monkeypatch: pytest.MonkeyPatch) -> None:
    seen: dict[str, str] = {}

    def fake_llm(settings: Settings, system: str, prompt: str) -> str:
        seen["system"], seen["prompt"] = system, prompt
        return "  You love finding patterns in data.  "

    monkeypatch.setattr(explainer, "call_anthropic", fake_llm)
    client = client_with(Settings(service_key="test-key", llm_api_key="sk-test"))

    res = client.post("/career/explain", json=BODY, headers=HEADERS)

    assert res.json() == {"explanation": "You love finding patterns in data.", "source": "llm"}
    assert '"matchPct": 81' in seen["prompt"]
    assert "CSC 101" in seen["prompt"]
    assert "never change the ranking" in seen["system"]


def test_explain_llm_error_falls_back() -> None:
    def failing(settings: Settings, system: str, prompt: str) -> str:
        request = httpx.Request("POST", "https://api.anthropic.com/v1/messages")
        raise anthropic.APIConnectionError(request=request)

    result = explainer.explain(CareerExplainRequest.model_validate(BODY), Settings(llm_api_key="sk-test"), llm=failing)
    assert result.source == "fallback"


def test_explain_empty_llm_answer_falls_back() -> None:
    result = explainer.explain(CareerExplainRequest.model_validate(BODY), Settings(llm_api_key="sk-test"), llm=lambda *_: None)
    assert result.source == "fallback"


class _FakeMessages:
    def __init__(self, response: Any) -> None:
        self.response = response
        self.calls: list[dict[str, Any]] = []

    def create(self, **kwargs: Any) -> Any:
        self.calls.append(kwargs)
        return self.response


class _FakeAnthropic:
    instances: list["_FakeAnthropic"] = []
    response: Any = None

    def __init__(self, **kwargs: Any) -> None:
        self.kwargs = kwargs
        self.messages = _FakeMessages(self.response)
        self.beta = SimpleNamespace(messages=_FakeMessages(self.response))
        _FakeAnthropic.instances.append(self)


def _response(stop_reason: str, *texts: str) -> SimpleNamespace:
    return SimpleNamespace(stop_reason=stop_reason, content=[SimpleNamespace(type="text", text=t) for t in texts])


def test_call_anthropic_default_model_uses_low_effort_and_fallbacks(monkeypatch: pytest.MonkeyPatch) -> None:
    _FakeAnthropic.instances, _FakeAnthropic.response = [], _response("end_turn", "Hello ", "there.")
    monkeypatch.setattr(explainer.anthropic, "Anthropic", _FakeAnthropic)

    text = explainer.call_anthropic(Settings(llm_api_key="sk-test"), "system", "prompt")

    assert text == "Hello there."
    fake = _FakeAnthropic.instances[0]
    assert fake.kwargs["api_key"] == "sk-test"
    call = fake.beta.messages.calls[0]
    assert call["model"] == "claude-opus-5-5"
    assert call["output_config"] == {"effort": "low"}
    assert call["fallbacks"] == "default"
    assert call["betas"] == ["server-side-fallback-2026-07-01"]
    assert fake.messages.calls == []


def test_call_anthropic_other_model_plain_request(monkeypatch: pytest.MonkeyPatch) -> None:
    _FakeAnthropic.instances, _FakeAnthropic.response = [], _response("end_turn", "Fine.")
    monkeypatch.setattr(explainer.anthropic, "Anthropic", _FakeAnthropic)

    assert explainer.call_anthropic(Settings(llm_api_key="k", llm_model="claude-haiku-5-5"), "s", "p") == "Fine."
    call = _FakeAnthropic.instances[0].messages.calls[0]
    assert call["model"] == "claude-haiku-5-5"
    assert "fallbacks" not in call


def test_call_anthropic_refusal_returns_none(monkeypatch: pytest.MonkeyPatch) -> None:
    _FakeAnthropic.instances, _FakeAnthropic.response = [], _response("refusal")
    monkeypatch.setattr(explainer.anthropic, "Anthropic", _FakeAnthropic)
    assert explainer.call_anthropic(Settings(llm_api_key="k"), "s", "p") is None
