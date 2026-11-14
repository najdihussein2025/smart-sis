"""Smoke test: call every GET (and the main POST/PUT flows) as each seeded role and report non-2xx answers.

Run against a dev stack with the simulated seed (SR-23). Mutates data (cart, enrollments, support, announcements...),
so use a throw-away database (`docker compose down -v` afterwards).

    pip install httpx            # or: python -m venv .venv && .venv\\Scripts\\activate && pip install -r requirements.txt
    python tools/smoke/smoke.py --base http://127.0.0.1:5080

Exit code 1 when any call answers 5xx (or an unexpected status).
"""
from __future__ import annotations

import argparse
import datetime as dt
import sys
import time
import traceback

import httpx

PASSWORD = "Pass@1234"
results: list[tuple[str, str, str, int, str]] = []


def call(c: httpx.Client, who: str, method: str, url: str, ok=(200, 201, 204), **kw) -> httpx.Response:
    t0 = time.perf_counter()
    r = c.request(method, url, **kw)
    ms = (time.perf_counter() - t0) * 1000
    print(f"   {r.status_code} {ms:6.0f} ms {who:10} {method:6} {url}", flush=True)
    note = ""
    if r.status_code not in ok:
        note = r.text[:300].replace("\n", " ")
    results.append((who, method, url, r.status_code, note))
    return r


def login(base: str, uid: int) -> httpx.Client:
    c = httpx.Client(base_url=base, timeout=30)
    r = c.post("/api/auth/login", json={"universityId": uid, "password": PASSWORD})
    r.raise_for_status()
    c.headers["Authorization"] = f"Bearer {r.json()['accessToken']}"
    return c


def items(r: httpx.Response):
    if r.status_code != 200:
        return []
    j = r.json()
    if isinstance(j, dict):
        for k in ("items", "courses", "sections", "questions"):
            if k in j:
                return j[k]
        return []
    return j


def sid_of(x: dict) -> int:
    return x.get("sectionId", x.get("id"))


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="http://127.0.0.1:5080")
    a = ap.parse_args()
    base = a.base

    anon = httpx.Client(base_url=base, timeout=30)
    call(anon, "anon", "GET", "/health")
    call(anon, "anon", "POST", "/api/auth/login", ok=(401,), json={"universityId": 3001, "password": "wrong"})
    call(anon, "anon", "POST", "/api/auth/forgot-password", ok=(204,), json={"universityId": 3001})

    admin, instr = login(base, 1001), login(base, 2001)
    students = {n: login(base, 3000 + n) for n in range(1, 7)}
    st = students[1]

    # ---- shared ----
    for who, c in (("admin", admin), ("instructor", instr), ("student", st)):
        for u in ("/api/auth/me", "/api/profile", "/api/terms/current", "/api/announcements",
                  "/api/notifications", "/api/notifications?unreadOnly=true", "/api/notifications/unread-count",
                  "/api/schedule/my" if who == "student" else "/api/schedule/teaching"):
            call(c, who, "GET", u)

    # ---- student ----
    courses = items(call(st, "student", "GET", "/api/catalog/courses"))
    call(st, "student", "GET", "/api/catalog/majors")
    call(st, "student", "GET", "/api/catalog/courses?type=Major&majorId=1&search=csc")
    call(st, "student", "GET", "/api/catalog/courses?type=Elective&majorId=1")
    section_ids: list[int] = []
    for co in courses:
        d = call(st, "student", "GET", f"/api/catalog/courses/{co['id']}")
        if d.status_code == 200:
            section_ids += [s["id"] for s in d.json()["sections"]]
    for u in ("/api/cart", "/api/enrollment/my", "/api/grades/my", "/api/grades/transcript", "/api/attendance/my",
              "/api/finance/my", "/api/support/categories", "/api/support/requests", "/api/chat/conversations",
              "/api/career/assessments", "/api/career/careers"):
        call(st, "student", "GET", u)
    call(st, "student", "GET", "/api/career/recommendations", ok=(200, 409))  # 409 assessment_required before the quiz
    careers = items(call(st, "student", "GET", "/api/career/careers"))
    if careers:
        call(st, "student", "GET", f"/api/career/careers/{careers[0]['id']}")

    # cart + enroll + swap + drop with student 6 (fewest seeded enrollments)
    s6 = students[6]
    held = {e["sectionId"] for e in items(call(s6, "student6", "GET", "/api/enrollment/my"))}
    free = [s for s in section_ids if s not in held]
    for sid in free[:3]:
        call(s6, "student6", "POST", "/api/cart", ok=(200, 201, 204, 409), json={"sectionId": sid})
    call(s6, "student6", "GET", "/api/cart")
    cart = [i["sectionId"] for i in items(call(s6, "student6", "GET", "/api/cart"))]
    if cart:
        call(s6, "student6", "POST", "/api/enrollment/enroll", ok=(200, 409), json={"sectionIds": cart[:1]})
    mine = items(call(s6, "student6", "GET", "/api/enrollment/my"))
    cart = [i["sectionId"] for i in items(call(s6, "student6", "GET", "/api/cart"))]
    if mine and cart:
        call(s6, "student6", "POST", "/api/enrollment/swap", ok=(200, 409),
             json={"fromSectionId": mine[0]["sectionId"], "toSectionId": cart[0]})
    mine = items(call(s6, "student6", "GET", "/api/enrollment/my"))
    if mine:
        call(s6, "student6", "POST", "/api/enrollment/drop", ok=(200, 204, 409), json={"sectionId": mine[-1]["sectionId"]})
    for sid in cart:
        call(s6, "student6", "DELETE", f"/api/cart/{sid}", ok=(200, 204, 404))

    # support
    cats = items(call(st, "student", "GET", "/api/support/categories"))
    if cats:
        r = call(st, "student", "POST", "/api/support/requests", ok=(200, 201),
                 json={"categoryId": cats[0]["id"], "subject": "Smoke test", "description": "Simulated request from smoke.py"})
        if r.status_code in (200, 201):
            rid = r.json()["id"]
            call(st, "student", "GET", f"/api/support/requests/{rid}")
            call(st, "student", "POST", f"/api/support/requests/{rid}/messages", ok=(200, 201), json={"body": "More detail"})
            call(admin, "admin", "GET", f"/api/admin/support/{rid}")
            call(admin, "admin", "PUT", f"/api/admin/support/{rid}/status", ok=(200, 204), json={"status": "InProgress"})
            call(admin, "admin", "PUT", f"/api/admin/support/{rid}/assignee", ok=(200, 204), json={"adminId": None})
            call(admin, "admin", "POST", f"/api/admin/support/{rid}/messages", ok=(200, 201), json={"body": "On it"})

    # chat (the AI service may be off: 502/503 are acceptable, 500 is not)
    call(st, "student", "POST", "/api/chat/messages", ok=(200, 201, 409, 502, 503), json={"message": "What is the credit cap?"})
    convs = items(call(st, "student", "GET", "/api/chat/conversations"))
    if convs:
        call(st, "student", "GET", f"/api/chat/conversations/{convs[0]['id']}")

    # career quiz submit (once per term: 409 already_taken on a re-run)
    quiz = call(st, "student", "GET", "/api/career/quiz", ok=(200, 409))  # 409 already_taken on a re-run
    if quiz.status_code == 200:
        qs = quiz.json().get("questions", [])
        answers = [{"questionId": q["id"], "optionId": q["options"][0]["id"]} for q in qs]
        call(st, "student", "POST", "/api/career/assessments", ok=(200, 201, 409), json={"answers": answers})
    call(st, "student", "GET", "/api/career/recommendations")

    # notifications
    notes = items(call(st, "student", "GET", "/api/notifications"))
    if notes:
        call(st, "student", "POST", f"/api/notifications/{notes[0]['id']}/read", ok=(200, 204))
    call(st, "student", "POST", "/api/notifications/read-all", ok=(200, 204))

    # ---- instructor ----
    for u in ("/api/attendance/teaching", "/api/instructor/grading/sections", "/api/instructor/announcements",
              "/api/instructor/announcements/sections"):
        call(instr, "instructor", "GET", u)
    teaching = items(call(instr, "instructor", "GET", "/api/attendance/teaching"))
    for sec in teaching:
        call(instr, "instructor", "GET", f"/api/attendance/sections/{sid_of(sec)}/sessions")
    sessions = items(call(instr, "instructor", "GET", f"/api/attendance/sections/{sid_of(teaching[0])}/sessions")) if teaching else []
    if sessions:
        call(instr, "instructor", "GET", f"/api/attendance/sessions/{sessions[0].get('sessionId', sessions[0].get('id'))}")
    if teaching and teaching[0]["meetings"]:
        sec = teaching[0]
        m = sec["meetings"][0]
        start = dt.date.fromisoformat(sec["termStart"]) if "termStart" in sec else dt.date.today() - dt.timedelta(days=14)
        day = dt.date.today()
        while day.isoweekday() != m["dayOfWeek"]:
            day -= dt.timedelta(days=1)
        sheet = call(instr, "instructor", "GET", f"/api/attendance/sheet?meetingId={m['id']}&date={day.isoformat()}", ok=(200, 409))
        if sheet.status_code == 200 and not sheet.json().get("sessionId"):
            recs = [{"enrollmentId": s["enrollmentId"], "status": "Present"} for s in sheet.json().get("students", [])]
            if recs:
                call(instr, "instructor", "POST", "/api/attendance/sessions", ok=(200, 201, 409),
                     json={"meetingId": m["id"], "sessionDate": day.isoformat(), "records": recs})
        _ = start
    gsecs = items(call(instr, "instructor", "GET", "/api/instructor/grading/sections"))
    for gs in gsecs:
        d = call(instr, "instructor", "GET", f"/api/instructor/grading/sections/{sid_of(gs)}")
        comps = d.json().get("components", []) if d.status_code == 200 else []
        for comp in comps[:1]:
            sheet = call(instr, "instructor", "GET", f"/api/instructor/grading/sections/{sid_of(gs)}/components/{comp['id']}")
            rows = sheet.json().get("students", sheet.json().get("rows", [])) if sheet.status_code == 200 else []
            scores = [{"enrollmentId": r["enrollmentId"], "score": r.get("score")} for r in rows]
            if scores:
                call(instr, "instructor", "PUT", f"/api/instructor/grading/sections/{sid_of(gs)}/components/{comp['id']}",
                     ok=(200, 204, 409), json={"scores": scores})
    isecs = items(call(instr, "instructor", "GET", "/api/instructor/announcements/sections"))
    if isecs:
        r = call(instr, "instructor", "POST", "/api/instructor/announcements", ok=(200, 201),
                 json={"title": "Smoke", "body": "Simulated section announcement", "sectionId": isecs[0]["id"]})
        if r.status_code in (200, 201):
            aid = r.json()["id"]
            call(instr, "instructor", "PUT", f"/api/instructor/announcements/{aid}", ok=(200,),
                 json={"title": "Smoke 2", "body": "Edited", "sectionId": isecs[0]["id"]})
            call(instr, "instructor", "DELETE", f"/api/instructor/announcements/{aid}", ok=(200, 204))

    # ---- admin ----
    for u in ("/api/admin/users", "/api/admin/users?search=demo&role=Student", "/api/admin/users?status=Inactive",
              "/api/admin/courses", "/api/admin/courses?search=csc", "/api/admin/sections", "/api/admin/sections?search=csc",
              "/api/admin/terms", "/api/admin/lookups", "/api/admin/announcements", "/api/admin/announcements?status=Live",
              "/api/admin/finance/options", "/api/admin/finance/batches", "/api/admin/finance/charges",
              "/api/admin/finance/charges?status=Pending", "/api/admin/finance/charges?search=demo",
              "/api/admin/finance/charges?search=3001", "/api/admin/support", "/api/admin/support?status=Open&search=demo",
              "/api/admin/support?unassignedOnly=true", "/api/admin/support/admins", "/api/admin/support/categories",
              "/api/admin/knowledge", "/api/admin/career/traits", "/api/admin/career/questions",
              "/api/admin/career/careers", "/api/admin/career/course-tags"):
        call(admin, "admin", "GET", u)
    users = items(call(admin, "admin", "GET", "/api/admin/users"))
    for u in users[:3]:
        call(admin, "admin", "GET", f"/api/admin/users/{u['id']}")
    for path in ("courses", "sections", "terms"):
        lst = items(call(admin, "admin", "GET", f"/api/admin/{path}"))
        if lst:
            call(admin, "admin", "GET", f"/api/admin/{path}/{lst[0]['id']}")
    qs = items(call(admin, "admin", "GET", "/api/admin/career/questions"))
    if qs:
        call(admin, "admin", "GET", f"/api/admin/career/questions/{qs[0]['id']}")
    cs = items(call(admin, "admin", "GET", "/api/admin/career/careers"))
    if cs:
        call(admin, "admin", "GET", f"/api/admin/career/careers/{cs[0]['id']}")
    term = call(admin, "admin", "GET", "/api/terms/current").json()
    clist = items(call(admin, "admin", "GET", "/api/admin/courses"))
    if clist:
        call(admin, "admin", "GET", f"/api/admin/grade-components?courseId={clist[0]['id']}&termId={term['id']}")
    batches = items(call(admin, "admin", "GET", "/api/admin/finance/batches"))
    if batches:
        call(admin, "admin", "GET", f"/api/admin/finance/batches/{batches[0]['id']}")
    opts = call(admin, "admin", "GET", "/api/admin/finance/options").json()
    if opts.get("majors") and opts.get("paymentTypes"):
        due = (dt.date.today() + dt.timedelta(days=30)).isoformat()
        call(admin, "admin", "POST", "/api/admin/finance/batches", ok=(200, 201),
             json={"majorId": opts["majors"][0]["id"], "termId": term["id"], "paymentTypeId": opts["paymentTypes"][0]["id"],
                   "title": "Smoke lab fee", "amount": 50.00, "dueDate": due})
        r = call(admin, "admin", "POST", "/api/admin/finance/charges", ok=(200, 201),
                 json={"universityId": 3002, "termId": term["id"], "paymentTypeId": opts["paymentTypes"][0]["id"],
                       "title": "Smoke one-off", "amount": 100.50, "dueDate": due})
        if r.status_code in (200, 201):
            cid = r.json()["id"]
            call(admin, "admin", "GET", f"/api/admin/finance/charges/{cid}")
            call(admin, "admin", "POST", f"/api/admin/finance/charges/{cid}/payments", ok=(200, 201), json={"amount": 40.25, "note": "part"})
            call(admin, "admin", "POST", f"/api/admin/finance/charges/{cid}/payments", ok=(409, 400), json={"amount": 1000, "note": "too much"})
    call(admin, "admin", "POST", "/api/admin/announcements", ok=(200, 201),
         json={"title": "Smoke all", "body": "Simulated announcement", "scope": "Students"})
    call(admin, "admin", "POST", "/api/admin/announcements", ok=(200, 201),
         json={"title": "Smoke later", "body": "Scheduled", "scope": "All",
               "publishAt": (dt.datetime.now(dt.UTC) + dt.timedelta(days=2)).isoformat().replace("+00:00", "Z")})
    lk = call(admin, "admin", "GET", "/api/admin/lookups").json()
    r = call(admin, "admin", "POST", "/api/admin/users", ok=(200, 201, 409),
             json={"universityId": 9990001, "email": "smoke.student@smartsis.local", "fullName": "Smoke Student", "role": "Student",
                   "initialPassword": PASSWORD,
                   "student": {"majorId": lk["majors"][0]["id"], "campusId": lk["campuses"][0]["id"],
                               "enrollmentDate": dt.date.today().isoformat(), "maxCreditsOverride": None}})
    call(admin, "admin", "POST", "/api/admin/support/categories", ok=(200, 201, 409), json={"name": "Smoke category"})

    # admin edits (saved with the values they already have, so the seed stays usable)
    c1 = call(admin, "admin", "GET", "/api/admin/courses/1").json()
    call(admin, "admin", "PUT", "/api/admin/courses/1", ok=(200,), json={
        "code": c1["code"], "name": c1["name"], "credits": c1["credits"], "description": c1["description"],
        "departmentId": c1["departmentId"], "prerequisiteIds": [p["id"] for p in c1["prerequisites"]],
        "majors": [{"majorId": m["majorId"], "type": m["type"]} for m in c1["majors"]]})
    s1 = call(admin, "admin", "GET", "/api/admin/sections/1").json()
    call(admin, "admin", "PUT", "/api/admin/sections/1", ok=(200,), json={
        "courseId": s1["courseId"], "termId": s1["termId"], "sectionCode": s1["sectionCode"], "instructorId": s1["instructorId"],
        "capacity": s1["capacity"], "meetings": [{k: m[k] for k in ("id", "dayOfWeek", "startTime", "endTime", "roomId", "type")}
                                                for m in s1["meetings"]]})
    t1 = call(admin, "admin", "GET", f"/api/admin/terms/{term['id']}").json()
    call(admin, "admin", "PUT", f"/api/admin/terms/{term['id']}", ok=(200,), json={
        k: t1[k] for k in ("name", "startDate", "endDate", "enrollStart", "enrollEnd", "addDropStart", "addDropEnd", "isCurrent")})
    u = call(admin, "admin", "GET", "/api/admin/users?search=9990001").json()["items"]
    if u:
        d = call(admin, "admin", "GET", f"/api/admin/users/{u[0]['id']}").json()
        call(admin, "admin", "PUT", f"/api/admin/users/{u[0]['id']}", ok=(200,), json={
            "email": d["email"], "fullName": d["fullName"] + " X", "phone": None, "dateOfBirth": None,
            "student": {"majorId": d["student"]["majorId"], "campusId": d["student"]["campusId"],
                        "enrollmentDate": d["student"]["enrollmentDate"], "maxCreditsOverride": 15},
            "instructor": None})
        call(admin, "admin", "POST", f"/api/admin/users/{u[0]['id']}/deactivate", ok=(200, 204))
        call(admin, "admin", "GET", "/api/admin/users?status=Inactive")
    traits = call(admin, "admin", "GET", "/api/admin/career/traits").json()
    call(admin, "admin", "PUT", f"/api/admin/career/traits/{traits[0]['id']}", ok=(200,),
         json={"name": traits[0]["name"], "description": traits[0]["description"]})
    tags = call(admin, "admin", "GET", "/api/admin/career/course-tags").json()
    call(admin, "admin", "PUT", f"/api/admin/career/course-tags/{tags[0]['courseId']}", ok=(200,), json={"traitIds": tags[0]["traitIds"]})
    gc = call(admin, "admin", "POST", "/api/admin/grade-components", ok=(200, 201, 409),
              json={"courseId": 4, "termId": term["id"], "name": "Smoke quiz", "maxGrade": 10, "weight": 1})
    if gc.status_code in (200, 201):
        gid = gc.json()["id"]
        call(admin, "admin", "GET", f"/api/admin/grade-components/{gid}")
        call(admin, "admin", "PUT", f"/api/admin/grade-components/{gid}", ok=(200,), json={"name": "Smoke quiz 2", "maxGrade": 10, "weight": 1})
        call(admin, "admin", "DELETE", f"/api/admin/grade-components/{gid}", ok=(200, 204))
    # finalize: 409 when scores are missing (expected on the seed); exercises the read path either way
    for gs in gsecs:
        call(instr, "instructor", "POST", f"/api/instructor/grading/sections/{sid_of(gs)}/finalize", ok=(200, 409))

    # ---- report ----
    bad = [r for r in results if r[4]]
    for who, m, u, s, note in results:
        flag = "!!" if note else "  "
        print(f"{flag} {s} {who:10} {m:6} {u}")
    print(f"\n{len(results)} calls, {len(bad)} unexpected")
    for who, m, u, s, note in bad:
        print(f"  {s} {who} {m} {u}: {note}")
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main())
