"""Destructive flows the plain smoke test leaves alone, run once against a throw-away dev stack (SQL Server):

1. Grade every component of the instructor's first section, finalize it (Enrollments -> Completed, retake rule,
   TermResults rebuild) and read the students' grades and transcript.
2. Mark one student absent at every past meeting of a section (absence counts, alerts, e-mails queued).

    python tools/smoke/deep_flows.py --base http://127.0.0.1:5080
"""
from __future__ import annotations

import argparse
import datetime as dt
import sys

import httpx

PASSWORD = "Pass@1234"


def login(base: str, uid: int) -> httpx.Client:
    c = httpx.Client(base_url=base, timeout=60)
    r = c.post("/api/auth/login", json={"universityId": uid, "password": PASSWORD})
    r.raise_for_status()
    c.headers["Authorization"] = f"Bearer {r.json()['accessToken']}"
    return c


def check(r: httpx.Response, *ok: int) -> httpx.Response:
    print(f"   {r.status_code} {r.request.method} {r.request.url.path}", flush=True)
    if r.status_code not in ok:
        raise SystemExit(f"unexpected {r.status_code}: {r.text[:500]}")
    return r


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="http://127.0.0.1:5080")
    base = ap.parse_args().base
    instr = login(base, 2001)

    # 1. finalize
    sid = check(instr.get("/api/instructor/grading/sections"), 200).json()[0]["sectionId"]
    detail = check(instr.get(f"/api/instructor/grading/sections/{sid}"), 200).json()
    for comp in detail["components"]:
        sheet = check(instr.get(f"/api/instructor/grading/sections/{sid}/components/{comp['id']}"), 200).json()
        scores = [{"enrollmentId": s["enrollmentId"], "score": round(comp["maxGrade"] * 0.8, 2)} for s in sheet["students"]]
        check(instr.put(f"/api/instructor/grading/sections/{sid}/components/{comp['id']}", json={"scores": scores}), 200)
    res = check(instr.post(f"/api/instructor/grading/sections/{sid}/finalize"), 200).json()
    print("   finalized:", [(r["universityId"], r["letterGrade"]) for r in res["students"]])
    for uid in {r["universityId"] for r in res["students"]}:
        st = login(base, uid)
        check(st.get("/api/grades/my"), 200)
        t = check(st.get("/api/grades/transcript"), 200).json()
        print(f"   {uid} transcript cumulative GPA:", t.get("cumulativeGpa"))

    # 2. absences
    teaching = check(instr.get("/api/attendance/teaching"), 200).json()["sections"]
    sec = next(s for s in teaching if s["sectionId"] != sid and s["enrolledCount"] > 0)
    start = dt.date.fromisoformat(sec["termStart"])
    today = dt.date.today()
    target = None
    for m in sec["meetings"]:
        day = start
        while day <= today:
            if day.isoweekday() == m["dayOfWeek"]:
                sheet = check(instr.get(f"/api/attendance/sheet?meetingId={m['id']}&date={day.isoformat()}"), 200, 409)
                if sheet.status_code == 200 and not sheet.json().get("sessionId"):
                    students = sheet.json()["students"]
                    target = target or students[0]["enrollmentId"]
                    recs = [{"enrollmentId": s["enrollmentId"], "status": "Absent" if s["enrollmentId"] == target else "Present"}
                            for s in students]
                    check(instr.post("/api/attendance/sessions", json={"meetingId": m["id"], "sessionDate": day.isoformat(),
                                                                         "records": recs}), 200, 201)
            day += dt.timedelta(days=1)
    sessions = check(instr.get(f"/api/attendance/sections/{sec['sectionId']}/sessions"), 200).json()
    print(f"   section {sec['sectionId']}: {len(sessions)} sessions, absent enrollment {target}")
    print("OK")
    return 0


if __name__ == "__main__":
    sys.exit(main())
