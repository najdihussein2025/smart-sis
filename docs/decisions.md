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
