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
