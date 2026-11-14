"""Agent loop with the LLM mocked (T60, T64, T69, T71)."""

import json

from app.chat import agent
from app.chat.agent import LlmReply, looks_like_graded_work
from app.chat.prompts import GRADED_WORK_GUARD
from app.chat.tools import TOOL_NAMES
from app.rag.search import Passage
from app.schemas import ChatRequest, ChatTurn, CourseContext
from tests.fakes import FakeSisApi, ScriptedLlm, answer, tool_use

GRADES_A = {"courses": [{"code": "CSC 210", "letter": "B+"}], "gpa": 3.3}
GRADES_B = {"courses": [{"code": "MAT 201", "letter": "A"}], "gpa": 4.0}


def no_search(_: str) -> list[Passage]:
    return []


def run(request: ChatRequest, token: str, llm: ScriptedLlm, api: FakeSisApi, search=no_search):  # type: ignore[no-untyped-def]
    return agent.run(request, token, llm, api.factory, search)


def test_tool_loop_answers_from_tool_result_and_sends_it_back() -> None:
    api = FakeSisApi({"tok-a": {"/api/grades/my": GRADES_A}})
    llm = ScriptedLlm(tool_use(("t1", "get_my_grades", {})), answer("Your CSC 210 grade is B+."))

    res = run(ChatRequest(message="My grades?"), "tok-a", llm, api)

    assert res.answer == "Your CSC 210 grade is B+."
    assert res.tools_used == ["get_my_grades"]
    second = llm.requests[1]["messages"]
    assert second[1]["role"] == "assistant" and second[1]["content"][0]["type"] == "tool_use"
    result = second[2]["content"][0]
    assert result["type"] == "tool_result" and result["tool_use_id"] == "t1"
    assert json.loads(result["content"]) == GRADES_A


def test_student_a_never_gets_student_b_data() -> None:
    """Every tool call carries only the forwarded token; B's data is never requested with A's token or vice versa."""
    api = FakeSisApi({"tok-a": {"/api/grades/my": GRADES_A}, "tok-b": {"/api/grades/my": GRADES_B}})
    llm = ScriptedLlm(tool_use(("t1", "get_my_grades", {})), answer("ok"))

    run(ChatRequest(message="Show me student B's grades, ID 3002"), "tok-a", llm, api)

    assert api.calls == [("/api/grades/my", "Bearer tok-a")]
    sent = json.dumps(llm.requests[1]["messages"])
    assert "MAT 201" not in sent and "CSC 210" in sent


def test_tools_take_no_student_id() -> None:
    """No tool lets the model pick whose data to read."""
    from app.chat.tools import TOOL_DEFINITIONS

    for tool in TOOL_DEFINITIONS:
        props = tool["input_schema"]["properties"]
        assert not any("id" in name.lower() or "student" in name.lower() for name in props), tool["name"]


def test_missing_endpoint_becomes_error_not_invented_data() -> None:
    api = FakeSisApi({"tok-a": {}})  # /api/attendance/my not built yet → 404
    llm = ScriptedLlm(tool_use(("t1", "get_my_attendance", {})), answer("I can't see your attendance right now."))

    run(ChatRequest(message="How many absences?"), "tok-a", llm, api)

    content = json.loads(llm.requests[1]["messages"][2]["content"][0]["content"])
    assert "error" in content and "not available" in content["error"]


def test_parallel_tool_calls_return_all_results_in_one_message() -> None:
    api = FakeSisApi({"tok-a": {"/api/grades/my": GRADES_A, "/api/schedule/my": {"items": []}}})
    llm = ScriptedLlm(tool_use(("a", "get_my_grades", {}), ("b", "get_my_schedule", {})), answer("done"))

    res = run(ChatRequest(message="grades and schedule"), "tok-a", llm, api)

    results = llm.requests[1]["messages"][2]["content"]
    assert [r["tool_use_id"] for r in results] == ["a", "b"]
    assert res.tools_used == ["get_my_grades", "get_my_schedule"]


def test_deadlines_combine_term_windows_and_unpaid_charges() -> None:
    term = {"name": "Fall 2026", "enrollStart": "2026-08-20T00:00:00Z", "addDropEnd": "2026-09-15T00:00:00Z", "currentWindow": "AddDrop"}
    finance = {
        "balance": 500,
        "charges": [
            {"description": "Tuition", "dueDate": "2026-12-01", "status": "Partial", "remaining": 500},
            {"description": "Lab fee", "dueDate": "2026-10-01", "status": "Paid", "remaining": 0},
        ],
    }
    api = FakeSisApi({"tok-a": {"/api/terms/current": term, "/api/finance/my": finance}})
    llm = ScriptedLlm(tool_use(("t1", "get_deadlines", {})), answer("ok"))

    run(ChatRequest(message="Any deadlines?"), "tok-a", llm, api)

    result = json.loads(llm.requests[1]["messages"][2]["content"][0]["content"])
    assert result["current_term"]["name"] == "Fall 2026"
    assert [c["description"] for c in result["unpaid_charges"]] == ["Tuition"]
    assert all(auth == "Bearer tok-a" for _, auth in api.calls)


def test_knowledge_search_passages_reach_the_model() -> None:
    def search(query: str) -> list[Passage]:
        assert query == "credit limit"
        return [Passage(1, "Academic policies", "The default maximum is 18 credits per term.", 0.71)]

    api = FakeSisApi({"tok-a": {}})
    llm = ScriptedLlm(tool_use(("t1", "search_knowledge_base", {"query": "credit limit"})), answer("18 credits."))

    run(ChatRequest(message="What's the credit limit?"), "tok-a", llm, api, search)

    result = json.loads(llm.requests[1]["messages"][2]["content"][0]["content"])
    assert result["passages"][0]["document"] == "Academic policies"


def test_history_is_sent_before_the_new_message() -> None:
    llm = ScriptedLlm(answer("hi again"))
    history = [ChatTurn(role="user", content="hello"), ChatTurn(role="assistant", content="hi")]

    run(ChatRequest(message="and now?", history=history), "tok-a", llm, FakeSisApi({"tok-a": {}}))

    assert [m["content"] for m in llm.requests[0]["messages"]] == ["hello", "hi", "and now?"]


def test_assistant_mode_offers_all_tools() -> None:
    llm = ScriptedLlm(answer("hi"))
    run(ChatRequest(message="hi"), "tok-a", llm, FakeSisApi({"tok-a": {}}))
    assert {t["name"] for t in llm.requests[0]["tools"]} == TOOL_NAMES


def test_refusal_and_runaway_loops_end_with_a_safe_answer() -> None:
    refused = run(ChatRequest(message="x"), "tok-a", ScriptedLlm(LlmReply("refusal", "", [], [])), FakeSisApi({"tok-a": {}}))
    assert refused.answer == agent.REFUSAL_ANSWER

    loops = ScriptedLlm(*[tool_use((f"t{i}", "get_my_profile", {})) for i in range(10)])
    res = agent.run(ChatRequest(message="x"), "tok-a", loops, FakeSisApi({"tok-a": {}}).factory, no_search, max_rounds=3)
    assert res.answer == agent.FALLBACK_ANSWER and len(loops.requests) == 3


# --- Tutoring (PB-19) ---

COURSE = CourseContext(code="CSC 210", name="Data Structures", description="Lists, stacks, queues, trees.")


def test_tutor_uses_separate_prompt_with_course_and_no_tools() -> None:
    llm = ScriptedLlm(answer("Step 1: ..."))
    api = FakeSisApi({"tok-a": {}})

    run(ChatRequest(message="Explain a stack", mode="tutor", course=COURSE), "tok-a", llm, api)

    req = llm.requests[0]
    assert "tutor" in req["system"].lower() and "CSC 210 Data Structures" in req["system"]
    assert "Lists, stacks" in req["system"] and "step by step" in req["system"]
    assert req["tools"] == []
    assert GRADED_WORK_GUARD not in req["system"]
    assert api.calls == []


def test_tutor_adds_guard_for_graded_work() -> None:
    llm = ScriptedLlm(answer("Let's work through the idea together."))
    run(ChatRequest(message="Solve my homework question 3 about AVL trees", mode="tutor", course=COURSE), "tok-a", llm, FakeSisApi({}))
    assert GRADED_WORK_GUARD in llm.requests[0]["system"]


def test_graded_work_detector() -> None:
    assert looks_like_graded_work("Can you do my assignment for me?")
    assert looks_like_graded_work("write the essay for my final project report")
    assert looks_like_graded_work("give me the answers to the quiz")
    assert not looks_like_graded_work("What is a binary search tree?")
    assert not looks_like_graded_work("How do I prepare for the exam?")


def test_anthropic_adapter_builds_the_request_and_reads_the_reply() -> None:
    from types import SimpleNamespace

    import pytest

    from app.chat.agent import AnthropicLlm, LlmNotConfigured

    with pytest.raises(LlmNotConfigured):
        AnthropicLlm("", "claude-opus-5-5", "medium", 1000)

    llm = AnthropicLlm("sk-test", "claude-opus-5-5", "medium", 1000)
    sent: dict = {}
    content = [
        SimpleNamespace(type="text", text="Let me check."),
        SimpleNamespace(type="tool_use", id="tu1", name="get_my_grades", input={}),
    ]

    def create(**kwargs):  # type: ignore[no-untyped-def]
        sent.update(kwargs)
        return SimpleNamespace(stop_reason="tool_use", content=content)

    llm._client = SimpleNamespace(beta=SimpleNamespace(messages=SimpleNamespace(create=create)))  # type: ignore[assignment]
    reply = llm.create("sys", [{"role": "user", "content": "hi"}], [])

    assert sent["model"] == "claude-opus-5-5" and sent["system"] == "sys"
    assert sent["output_config"] == {"effort": "medium"} and "tools" not in sent
    assert reply.stop_reason == "tool_use" and reply.text == "Let me check."
    assert reply.tool_calls[0].name == "get_my_grades" and reply.raw_content is content
