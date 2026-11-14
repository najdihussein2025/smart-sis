# Decisions and assumptions

Choices made while building the remaining sprints where the board or `PROJECT.md` left room for more than one reading.
Each one follows the project doc where it says anything; change it here and in the code together.

## Sprint 3

### Board vs code (Sprint 2 carry-over)
The board shows the Sprint 2 backend items as done, but the code only had entities for Cart and Enrollments.
The missing backend was built first, as part of the carried-over items:
cart API (T14), enroll + swap API with `EnrollmentPolicy` / `SwapPolicy` (T17, T18), my enrollments (T20) and the drop API (T24).
Work item numbers for these were inferred from the sequence (T13 = AB#66 … T16 = AB#69).

### Enrolling several sections at once
`POST /api/enrollment/enroll` takes up to 10 section ids and tries each **in its own transaction, in order**, returning
`200` with one result per section (`success`, `message`, `code`). It does not fail the whole request with a single `409`,
because the UI must show every failure (T19). A later section sees the credits and meetings of the earlier ones that succeeded.
Swap and drop are single actions and still answer `409` with the rule's message.

### Cart
- Adding to the cart is allowed outside the registration windows (students plan ahead). Rules run when they enroll.
- Two sections of the same course may sit in the cart, so one can be swapped for the other.
- A section must belong to the current term and the student must not already be enrolled in it.

### Seats
Seats taken = enrollments with status `Enrolled` **or** `Completed` (PROJECT.md §4 "Sections"), the same count the admin
sections page uses. `business-rules.md` E6 mentions only `Enrolled`; in the current term the two are the same until grades
are finalized, and counting `Completed` keeps the number stable after finalization.

### Auto-drop audit row
`EnrollmentActions.PerformedBy` for an `AutoDrop` is the instructor whose attendance submission caused it (there is no
system user account).

### Drop
Dropping sets `Status = Dropped`, `CountsInGpa = false`. Re-enrolling later creates a new enrollment row.

### Frontend placement
- The student catalog lives in `features/catalog` (the admin course pages stay in `features/courses`).
- Enroll results, swap and drop live on the Cart page (one registration screen), using the `enrollment` feature's hooks.

## PB-10/PB-11 Attendance

### Endpoints
- Instructor (`InstructorOnly`): `GET /api/attendance/teaching` (this term's sections with meetings, term dates and the
  server's date), `GET /api/attendance/sheet?meetingId=&date=` (the roster, or what was recorded once taken),
  `GET /api/attendance/sections/{id}/sessions` (history), `GET /api/attendance/sessions/{id}`, `POST /api/attendance/sessions` (201).
- Student (`StudentOnly`): `GET /api/attendance/my`. The AI assistant can call it with the student's token.

### Taking attendance
- "Session" in the UI is a weekly meeting (`SectionMeetings`); a date picks one occurrence of it.
- Allowed dates: the meeting's weekday, from the term's `StartDate` to today (or `EndDate` if earlier). "Today" is the
  server's **UTC** date (`TimeProvider`); the page gets it from `/teaching`, so the browser clock doesn't matter. A bad
  date is `409` with code `future_date`, `outside_term` or `wrong_day`.
- Every **Enrolled** student of the section must be marked exactly once: a missing student is `409 students_missing`, a
  student not on the roster `409 not_on_roster`, a duplicate `400`. A section with no students can't be submitted.
- Attendance can't be edited or deleted after submission (no endpoint). A second submission for the same meeting and date
  is `409 attendance_already_taken`; checked before the insert and again on a unique-index race (`DbUpdateException`).
- Ownership: `Sections.InstructorId == current user`, else `403`, for the sheet, the history and the submission.

### Absence alerts (A2, A3)
- Absences are counted from `AttendanceRecords` with `Status = Absent`, across all of the enrollment's sessions.
- After a submission, each absent student's count is checked against **its exact threshold** only: `limit − 1` → Warning,
  `limit` → Limit, `> limit` → auto-drop + AutoDropped. A level already in `AbsenceAlerts` is never sent again. Because one
  submission adds at most one absence per student, every threshold is crossed on its own.
- Warning and Limit use the existing `EmailTemplates.AbsenceWarning`; the drop uses the new `EmailTemplates.AbsenceAutoDropped`.
  Notifications use types `AbsenceWarning`, `AbsenceLimit`, `AutoDropped` with link `/student/attendance`.
- All of it (session, records, alerts, emails, notifications, the drop and its `AutoDrop` action) commits in the
  submission's transaction. `PerformedBy` of the auto-drop is the submitting instructor (see Sprint 3).
- Known limit: two submissions for **different** meetings of the same section at the same moment could both read the old
  count. Not handled (very unlikely: one instructor per section).

### Student view
- Lists the student's current-term enrollments with status `Enrolled` **and** `DroppedAbsence` (so a student sees why a
  course is gone), not `Dropped`. Colours follow A5 with the course's limit; a dropped course is red "Critical" with
  "Dropped (F)".
- `sessionsRecorded` = sessions the student was marked in.

### Test case mapping (assumed)
The board doesn't spell out TC_36–TC_43, so the traits use: TC_36 roster, TC_37 submit, TC_38 duplicate (BG_06),
TC_39 warning, TC_40 limit alert, TC_41 auto-drop, TC_42 student view, TC_43 another instructor's section (403).

### Frontend
- The instructor page keeps the section / session / date choice in component state (no React Hook Form): nothing is
  submitted from it, it only selects which sheet to load. The date field still validates the weekday and range before
  asking the API.
- Demo data: `AttendanceDevSeed` (Order 200) takes every meeting of CSC 101 A, CSC 210 A and MAT 201 A up to a week ago;
  Demo Student (3001) has 3 absences in MAT 201 (Warning) and none in CSC 210 (Perfect). The last week is left open.
