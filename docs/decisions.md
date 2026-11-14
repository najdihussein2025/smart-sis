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

## PB-18 Finance

### Endpoints
- Admin: `/api/admin/finance` — `options` (terms, majors with their active-student count, payment types), `batches` (list, get,
  create), `charges` (list, get, one-off create) and `charges/{id}/payments`. Student: `GET /api/finance/my`.
- The dropdown data comes from the finance feature's own `options` endpoint, because `/api/admin/lookups` has no payment types
  or terms; this keeps the Lookups feature untouched.

### Charge batches (F1)
- "Active student" = `Users.IsActive` and not soft-deleted. Students are read inside the batch's transaction; the batch, its
  charges, the notifications and the queued emails are saved together.
- A major with no active student → `409 no_active_students`, nothing is created (an empty batch would be noise).
- Later students joining the major are **not** charged automatically; the admin adds a one-off charge.
- Any due date is accepted (also a past one, to record an old fee). Amounts: > 0, ≤ 1,000,000, at most 2 decimals.

### One-off charges
- Addressed by University ID (what the admin knows), not by internal id. Unknown or deactivated student → 400 on `universityId`.

### Payments (F2, F3)
- `PaidAt` is the time the admin records it (no back-dating yet). The charge row is locked (`UPDLOCK, ROWLOCK`) inside the
  transaction so two admins can't overpay together.
- 409 codes: `overpayment` (more than the remaining balance), `already_paid`, `invalid_amount`. Payments can't be edited or
  deleted (no reversal flow yet).
- Charges and payments of a deactivated student stay visible to the admin (payment history is kept); no notification is sent
  to a deactivated student.

### Notifications
- Charged and payment recorded → in-app notification (`Payment`, link `/student/finance`) + queued email. The email texts live
  in `Features/Finance/Services/FinanceMessages.cs` rather than the shared `EmailTemplates`, to avoid editing shared code.

### Student account
- Overdue = something still owed and the due date is **before** today (UTC date); due today is not overdue.
- Outstanding balance = Σ(amount − paid) over unpaid charges; `nextDueDate` = earliest due date today or later of an unpaid charge.
- "Completed" tab = `Paid`; "Pending" tab = `Pending` + `Partial`.

### Admin payments screen
- `/admin/payments` has two tabs: **Charges** (search by University ID or name, status, term, major; unpaid first) and
  **Batches** (paid / charged count, "Show charges" filters the Charges tab by that batch).
- The payment dialog shows amount, paid and remaining balance, offers "Full balance", and refuses more than the balance
  before sending (F3 in the browser; the API checks again). Paid charges have no "Record payment" button.
## PB-13/PB-14 Support

### Student side (T50, T51)
- Routes: `GET /api/support/categories`, `GET|POST /api/support/requests`, `GET /api/support/requests/{id}`,
  `POST /api/support/requests/{id}/messages`. The student id always comes from the token.
- Another student's request answers **404**, not 403, so request ids can't be probed.
- The description is stored on the request; the thread (`SupportMessages`) holds the replies that follow it.
- A **Closed** request takes no replies from anyone (409 `request_closed`): the student submits a new request.
  A student reply on a **Resolved** request reopens it (Open, `ResolvedAt` cleared), since it means the problem isn't solved.
- A student reply notifies the assigned admin in-app (no email); unassigned requests are seen in the inbox.
- Lists are ordered by newest activity (latest reply, else submission time).
- Demo data: `SupportDevSeed` (Order 400) adds four simulated requests, one per status, three for student 3001.

### Admin inbox (T53, T54)
- Routes under `/api/admin/support` (Admin policy): list with filters `status`, `categoryId`, `assigned` (`Any` / `Me` /
  `Unassigned`) and `search` (subject, student name or university id); `GET /{id}`; `PUT /{id}/status`;
  `PUT /{id}/assignee` (`adminId` of an active admin, or null to unassign); `POST /{id}/messages`; `GET /admins`
  (active admins for the assignee list).
- Any admin can act on any request (C3); assignment only shows who is handling it.
- Status changes are free in every direction (an admin can reopen a closed request). `ResolvedAt` is stamped on the first
  move to Resolved or Closed and cleared when the request goes back to Open / In progress. Setting the same status again
  changes nothing and sends nothing.
- The student gets a `Support` notification **and** a queued email (C2) when an admin replies and when the status changes.
  Assigning a request to another admin notifies that admin in-app. Assignment itself doesn't notify the student.
- An admin reply on an **Open** request moves it to **In progress**. Admins can't reply on a Closed request either
  (409 `request_closed`): reopen it first.
- Support emails are built inside the Support feature (`SupportNotifier`), not in `Shared/Email/EmailTemplates.cs`, to keep
  the feature self-contained.

### Support categories
- Managed on the support inbox page (Categories tab), not on `/admin/lookups`: `GET|POST /api/admin/support/categories`,
  `PUT|DELETE /api/admin/support/categories/{id}`.
- Names are unique ignoring case (409 `duplicate_category`), 1–100 characters.
- A category used by any request can't be deleted (409 `category_in_use`); rename it instead. An **unused** category is
  deleted for good (hard delete): nothing refers to it, and soft delete (D1) is only required for users.
- No schema change was needed, so there is no migration.
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
