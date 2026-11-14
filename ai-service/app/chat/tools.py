"""LLM tools (PB-15). Each tool is a thin call to the .NET API with the student's own JWT and returns compact JSON.

Endpoints that another feature hasn't shipped yet (404) or that fail come back as {"error": ...} so the model says it
can't see that information instead of guessing.
"""

import json
from collections.abc import Callable
from typing import Any

from app.clients.sis_api import SisApiClient, SisApiError
from app.rag.search import Passage

MAX_RESULT_CHARS = 8000

Searcher = Callable[[str], list[Passage]]


def _no_input(description: str, name: str) -> dict[str, Any]:
    return {
        "name": name,
        "description": description,
        "input_schema": {"type": "object", "properties": {}, "additionalProperties": False},
    }


TOOL_DEFINITIONS: list[dict[str, Any]] = [
    _no_input("The student's profile: name, university ID, major, campus and credit cap per term.", "get_my_profile"),
    _no_input(
        "The student's weekly class schedule for the current term: course, day (1=Mon..6=Sat), start/end time (local), "
        "room, instructor, lecture or lab.",
        "get_my_schedule",
    ),
    _no_input(
        "The student's enrolled sections in the current term, enrolled credits, credit cap and which registration "
        "window is open now (Enrollment, AddDrop or Closed).",
        "get_my_enrollments",
    ),
    _no_input(
        "The student's grades: per course the grade components with weight and score, final percentage and letter, "
        "and term / cumulative GPA.",
        "get_my_grades",
    ),
    _no_input("The student's absences per course with the absence limit and status.", "get_my_attendance"),
    _no_input("The student's charges, payments, outstanding balance and due dates.", "get_my_finance"),
    _no_input(
        "Upcoming deadlines: the current term's enrollment and add/drop window dates (UTC) and the window open now, "
        "plus unpaid charges with their due dates.",
        "get_deadlines",
    ),
    {
        "name": "search_knowledge_base",
        "description": "Search the university's policy and FAQ documents. Returns the most relevant passages with the "
        "document title. Use it for any question about rules, procedures or policies.",
        "input_schema": {
            "type": "object",
            "properties": {"query": {"type": "string", "description": "What to look for, in a few words."}},
            "required": ["query"],
            "additionalProperties": False,
        },
    },
]

TOOL_NAMES = frozenset(t["name"] for t in TOOL_DEFINITIONS)


def _call(api: SisApiClient, path: str, what: str) -> Any:
    try:
        return api.get(path)
    except SisApiError as ex:
        if ex.status == 404:
            return {"error": f"The student's {what} are not available in the system yet."}
        if ex.status in (401, 403):
            return {"error": f"Not allowed to read the {what}."}
        return {"error": f"The {what} could not be loaded right now."}


def _unpaid_charges(finance: Any) -> Any:
    """Charges with a due date that aren't fully paid, from whatever shape /api/finance/my returns."""
    if not isinstance(finance, (dict, list)) or (isinstance(finance, dict) and "error" in finance):
        return finance
    found: list[dict[str, Any]] = []

    def walk(node: Any) -> None:
        if isinstance(node, list):
            for item in node:
                walk(item)
        elif isinstance(node, dict):
            if any("due" in k.lower() for k in node) and str(node.get("status", "")).lower() != "paid":
                found.append(node)
            else:
                for value in node.values():
                    walk(value)

    walk(finance)
    return found


def run_tool(name: str, tool_input: dict[str, Any], api: SisApiClient, search: Searcher) -> str:
    """Runs one tool and returns its JSON result (trimmed) for the model."""
    result: Any
    if name == "get_my_profile":
        result = _call(api, "/api/profile", "profile")
    elif name == "get_my_schedule":
        result = _call(api, "/api/schedule/my", "schedule")
    elif name == "get_my_enrollments":
        result = _call(api, "/api/enrollment/my", "enrollments")
    elif name == "get_my_grades":
        result = _call(api, "/api/grades/my", "grades")
    elif name == "get_my_attendance":
        result = _call(api, "/api/attendance/my", "attendance records")
    elif name == "get_my_finance":
        result = _call(api, "/api/finance/my", "financial records")
    elif name == "get_deadlines":
        result = {
            "current_term": _call(api, "/api/terms/current", "term dates"),
            "unpaid_charges": _unpaid_charges(_call(api, "/api/finance/my", "financial records")),
        }
    elif name == "search_knowledge_base":
        query = str(tool_input.get("query", "")).strip()
        passages = search(query) if query else []
        result = (
            {"passages": [{"document": p.title, "text": p.content, "score": p.score} for p in passages]}
            if passages
            else {"passages": [], "note": "No matching policy passages were found."}
        )
    else:
        result = {"error": f"Unknown tool {name}."}

    text = json.dumps(result, ensure_ascii=False, separators=(",", ":"))
    return text if len(text) <= MAX_RESULT_CHARS else text[:MAX_RESULT_CHARS] + "...(truncated)"
