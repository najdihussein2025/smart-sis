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
## PB-08/PB-09 Grading

### Grade components (admin)
- `/api/admin/grade-components` lists by course + term, and creates, edits and deletes. Course and term never change on edit.
- Rule G1 lives in `Rules/GradeComponentPolicy`: on insert and on update (the edited component left out) the weights may add up to
  at most 100 (`409 weight_over_100`, message says how much is left). Names are unique per course and term, ignoring case
  (`409 duplicate_component_name`).
- A component can be deleted only while it has no scores (`409 component_has_scores`); its max grade can't drop below a score
  already entered (`409 scores_above_max`). Deleting a component is a real delete (it is not a user record, D1 doesn't apply).
- Once any section of the course was finalized in that term, its components are locked (`409 grades_finalized`).

### Score entry (instructor)
- Only current-term sections the instructor teaches are listed; every action checks `Section.InstructorId == me` (403).
- Scores are saved per component as a batch; students left out keep their score, `score: null` clears one (the row is removed).
  Out-of-range scores and students who aren't Enrolled in the section are 400 field errors (`scores[i].score` / `.enrollmentId`).
- The roster is the section's Enrolled and Completed students, deactivated ones included (their grades are kept).

### Finalizing
- `POST /api/instructor/grading/sections/{id}/finalize`, one transaction. `Rules/FinalizePolicy` refuses (409) when the section is
  already finalized, has no Enrolled students, has no components, the weights don't add up to **exactly 100**, or **any score
  is missing**. Missing scores are never treated as 0: the instructor types 0 on purpose, so a forgotten score can't fail a student.
- Each Enrolled student gets `GradeCalculator.Final` (G3) and is completed through the new `IEnrollmentService.CompleteAsync`
  (the Enrollments feature changes its own entity); that also sets `CountsInGpa = false` on the student's other Completed /
  DroppedAbsence attempts of the course (G5). Then `ITermResultService.RebuildAsync` recomputes all of the student's
  TermResults rows (G6).
- "Finalized" is not stored: a section is finalized when it has Completed students and nobody is still Enrolled. No migration was
  needed.

### GPA
- GPA = Σ(points × credits) / Σ credits, rounded to 2 decimals (halves away from zero). Completed and DroppedAbsence (F, 0.0)
  attempts count; plain Dropped never does.
- **Cumulative GPA** uses the latest attempt of each course, which is exactly the set with `CountsInGpa = true`. In TermResults,
  the cumulative GPA of a term only looks at that term and the ones before it, so older rows don't change when a course is
  retaken later.
- **Term GPA** shows the grades earned in that term, including an attempt that a later retake replaced (the transcript marks it
  "Retaken").
- Credits attempted = credits of graded attempts; credits earned = those with D or better (points ≥ 1.0). Transcript totals use
  the latest attempt per course.

### Student views
- `GET /api/grades/my` (also for the AI assistant): current-term courses that aren't plain Dropped, each with its components
  (weight, max, score or null = "Not graded", %), graded weight, points earned out of 100, running % (earned ÷ graded weight) and
  the final %, letter and points once completed; term GPA (null until a course is final) and cumulative GPA computed live.
- `GET /api/grades/transcript`: terms oldest first. Term GPA and credits come from TermResults; a term without a row yet (e.g. an
  absence drop before the section is finalized) is computed with the same calculator, so the page is never wrong.

### Demo data and tests
- `GradingDevSeed` (order 100): components for every current-term course (weights add up to 100), scores for the first
  component of everyone and the second of every other student (finals not yet entered), and TermResults for the past term.
- EF InMemory never applies `HasData`, so the seed copies the GradeScales rows from the model's seed data when the table is empty
  (tests only; SQL Server gets them from the migration).
## PB-12 Announcements and notifications

### Audiences and feed (rule C1)
- Admin posts to All, Students, Instructors or one Major. Instructor posts to one Section, only a section they teach
  (403 otherwise, 400 if the section doesn't exist). "All" includes admins.
- `GET /api/announcements` is one feed for every role, newest `PublishAt` first, showing posts that are published and not
  expired: student = All + Students + own major + sections they are **Enrolled** in; instructor = All + Instructors +
  sections they teach; admin = everything. The author also sees their own **scheduled** posts there (status `Scheduled`).
- Manage lists show every status: `GET /api/admin/announcements` (all posts) and `GET /api/instructor/announcements`
  (own posts), with an optional `status` filter (`Scheduled` / `Live` / `Expired`, computed from the dates, never stored).
- An admin may edit or delete any post; an instructor only their own. When an admin edits an instructor's section post it
  keeps its section (the admin can't move a post onto a section).

### Publish and expiry times
- `PublishAt` empty or in the past means "now"; a future time schedules the post. Editing a post that is already live
  keeps its original `PublishAt` (fixing a typo doesn't push it to the top of the feed); editing a scheduled post with an
  empty time publishes it now.
- `ExpiresAt` is optional, must be in the future and after `PublishAt` (400 on `expiresAt` otherwise).
- Times are UTC in the API, like term windows.

### Delete is a hard delete
`Announcements` has no `DeletedAt` column and isn't a user record (rule D1 is about users), so deleting removes the row
without a migration. Notifications already sent stay in the bell; their link opens the feed, where the post is gone.

### Notifications for an announcement
- One `Notification` (type `Announcement`) per active audience member except the author. The title is the post title,
  the body the post text (cut to 1000 characters), the link the reader's own feed: `/<role>/announcements?announcement=<id>`.
- A post that is live when saved notifies in the same transaction. A scheduled post is notified by
  `AnnouncementPublishJob` (every minute) once its `PublishAt` passes, looking back 7 days.
- "Already notified" = a notification whose link ends in `?announcement=<id>` exists, so restarts and edits never notify
  twice. No migration was needed. A `NotifiedAt` column on `Announcements` would make this check cheaper if the table
  grows; it was left out because this sprint must not add migrations.
- The audience is resolved with one query of user ids and inserted in one `SaveChanges` (`INotificationService.NotifyMany`).

### Notifications bell
- `GET /api/notifications` (paged, newest first, `unreadOnly`), `GET /api/notifications/unread-count`,
  `POST /api/notifications/{id}/read` and `POST /api/notifications/read-all`, for any signed-in role. The user comes
  from the token; another user's notification id answers 404 (not 403), so ids can't be probed. Marking read twice is a no-op.
- The bell asks for the unread count every 60 s and loads the latest 10 only while the dropdown is open. Opening a
  notification marks it read and goes to its link.
- Notifications are never deleted by these endpoints (no "clear"); they stay as history.

### Email outbox retries (rule C2)
- `EmailOutboxJob` still runs every 30 s, but only picks emails that are due: a new email at once, then after a failed
  attempt it waits 1, 2, 4 and 8 minutes (`EmailRetryPolicy`). After `MaxAttempts` (5) failures the email is `Failed`.
- The last attempt time is the row's `UpdatedAt` (only the job writes queued rows), so no schema change was needed.
  The due check runs in SQL (one condition per attempt count).
## PB-15/PB-19 Chatbot and tutoring

### Chat flow and identity
`POST /api/chat/messages` (Student policy) sends the message, the last 20 user/assistant messages and **the caller's own
bearer token, copied from the request's `Authorization` header**, to the Python `/chat` with `X-Service-Key`. Python only
puts that token on its tool calls to the .NET API, so the bot can read exactly what the student can read. No tool takes a
student id. Both messages are saved only after an answer comes back, so a failed call saves nothing. Conversations of
other students behave as missing (404), never 403, so ids can't be probed.

### When the AI service fails
Python answers 503 when `LLM_API_KEY` is empty and 502 when the model provider fails. The .NET client turns any failure
(including the service being down or timing out after `AiService:TimeoutSeconds`, default 120) into a **409 with
`code: assistant_unavailable`** and a sentence for people ("isn't set up yet" for 503, "isn't available right now"
otherwise). We reuse `BusinessRuleException` instead of adding a 503 mapping to the shared `ExceptionMiddleware`.

### Model and tools
Anthropic Python SDK, manual tool-use loop (so tests swap in a scripted LLM). Default model `claude-opus-5-5`
(`LLM_MODEL` overrides), `output_config.effort` from `LLM_EFFORT` (default `medium`), at most 6 tool rounds, server-side
refusal fallback enabled. Tools: profile, schedule, enrollments, grades, attendance, finance, deadlines, knowledge search.
`get_deadlines` = new `GET /api/terms/current` (any signed-in user) + unpaid charges with a due date from
`/api/finance/my`. Grades, attendance and finance endpoints are built by other features; until they exist (404) the tool
returns `{"error": ...}` and the prompt makes the model say it can't see that information.

### Mode and course without a migration (T70)
`ChatConversations` has no mode/course column. A tutor conversation stores `{"kind":"conversation-context","mode":"Tutor",
"courseId":…,"courseCode":…,"courseName":…}` as its **first message with `Role = Tool`**. Tool messages are never returned
to the UI nor sent as history. Assistant conversations have no such message. If we later want to filter/sort by mode in SQL,
add `Mode` + `CourseId` columns to `ChatConversations` in a migration and backfill from these messages.

### Tutoring (T69)
Separate system prompt with the course code, name and description (the .NET API checks the student has an `Enrolled`
enrollment in that course, else 409 `not_enrolled`). Tutor mode gets **no data tools**. Academic integrity: the prompt says
to explain, show a similar worked example and give hints instead of finished graded work; a regex guard adds an extra
reminder to the system prompt when the message looks like "solve/do/write my assignment/homework/quiz/exam…".

### Knowledge base (T61)
- **How documents reach Python:** admins upload `.txt` / `.md` (UTF-8, ≤ 1 MB) as multipart. .NET keeps the text in
  `KnowledgeBase:StoragePath` (default `App_Data/knowledge`, git-ignored; `/home/app/knowledge` in Docker, which lasts as long
  as the container) under a random file name stored in `KnowledgeDocuments.FileUrl`, then calls Python `/ingest` with
  `{document_id, title, content}` **inline** (no file URLs to fetch). Ingest is synchronous; failure marks the document
  `Failed` and the admin can re-ingest. PDF/Word are not accepted (would need parsers).
- **Python writes only `KnowledgeChunks`:** it deletes the document's old chunks and inserts the new ones in one
  transaction (pymssql, `SQL_CONNECTION_STRING`). For search it reads chunks joined to `KnowledgeDocuments` (title, and
  `Status = 'Ready'` only). Deleting a document is done by .NET (chunks + document + file, a real delete: these are not
  user records, D1 is about users).
- **Embeddings:** Anthropic has no embeddings API and SQL Server 2022 has no `VECTOR`. We use a self-contained,
  deterministic hashed bag-of-words embedder (unigrams + bigrams, light stemming, stop words, sublinear tf, 512 dims,
  L2-normalised) behind an `Embedder` protocol, stored as float32 little-endian `varbinary(max)`, cosine similarity in
  Python over all Ready chunks (fine for a few hundred policy passages). Chunks whose vector is empty or of another size
  are embedded on the fly, which lets the demo seed store chunks without vectors and lets us swap the embedder later.
- **Demo seed:** `ChatDevSeed` (order 700) adds one Ready "Academic policies (demo)" document with simulated passages and
  no stored file (`FileUrl = ""`), so it can be deleted but not re-ingested (409 `file_missing`).
## PB-16/PB-17/PB-20 Career

### One assessment per term
§9 says "retake per term". We read it as **one attempt per student per term**: the attempt belongs to the current term,
and a retake opens when the next term becomes current. There is a unique index on `CareerAssessments(StudentId, TermId)`;
a second submit (or loading the quiz again) in the same term answers `409 already_taken`. No current term → `409 no_current_term`.
Recommendations always use the latest attempt.

### Which questions make up the quiz
The quiz is the **first 30 active questions by id** (all of them when there are 20–30). It is deterministic, so the
submit is checked against the same set the student saw. Fewer than 20 active questions → `409 not_enough_questions`.
Submit checks, in order: a question not in the quiz → `409 quiz_changed` (the bank changed while answering); a question
answered twice or an option of another question → `400` with the field (`answers[i].optionId`); a question left out →
`409 quiz_incomplete`. Trait points are never sent to the student.

### Trait scores
Per trait: points earned / the most points the quiz could give that trait (best option of every question), as a whole
percent (half away from zero). Stored on `CareerAssessmentTraitScores` at submit, so later bank edits don't change history.

### Question bank history
A question that was answered can't be deleted (`409 question_answered`); deactivate it instead (`isActive: false`).
Options are kept by `id` on save (like section meetings); removing an option a student chose → `409 option_answered`.
Editing the text or points of an answered question is allowed: stored attempts keep their scores.
Traits can be created and renamed, not deleted (careers, tags and stored scores point at them).

### Matching formula (T65, `Rules/CareerScoring`)
- Grade score per trait = average over the student's completed, GPA-counting attempts (latest retake) of courses tagged
  with the trait, using `GradePoints / 4 × 100` (or `FinalPct` when there are no grade points). Ungraded courses are skipped.
- Trait score = 70% quiz + 30% grades. A trait with **no graded course uses the quiz alone** (not 70% of it), so a new
  student isn't penalised.
- Career match = trait scores averaged with the career's weights (1–10), rounded to a whole percent. Top 3, ties by name.
  Careers without weights and inactive careers are never recommended. `quizPct` and `gradePct` are returned too.

### AI explanation (T66)
.NET calls `POST {AiService:BaseUrl}/career/explain` with `X-Service-Key` through a typed `HttpClient` (20 s timeout).
The payload has no name, id or email: only career names and percentages, trait names and scores, and the passed courses
(code and name). When the key is empty, the service is down, slow, returns non-2xx, or the Python side couldn't use the
LLM (`source != "llm"`), .NET uses the deterministic `CareerExplanation.Fallback` text and the response says
`explanationSource: "Fallback"`. Recommendations never fail because of the LLM. The model is `LLM_MODEL`
(default `claude-opus-5-5`) through the Anthropic Python SDK with `LLM_API_KEY`. The LLM only writes the explanation; it
does not change the ranking. The roadmap is personalised by rules (below), not by the LLM.

### Roadmap (T72)
Steps are ordered per career and may link a course. Status per step: **Done** (course passed, D or better),
**InProgress** (enrolled this term), **Next** (first course step neither done nor in progress), **Later**, or
**Milestone** (no course: advice, not tracked). Each course step carries this term's sections with seats and
Available / InCart / Enrolled, so the UI can "Add to cart" through the existing cart API (cart rules apply unchanged).
Saving a career replaces its weights (matched by trait) and steps (matched by position).

### Suggested courses
Courses from the top 3 careers' roadmaps that the student hasn't passed and isn't taking, best career first then step
order, at most 6, each with this term's sections (empty = not offered this term).

### Admin UI placement
Traits, question bank, careers with roadmaps and course-trait tags are tabs of one page, `/admin/career`.

## PB-22 Usability

### Dashboards (real numbers)
- One read-only feature, `Features/Dashboard`, with `GET /api/dashboard/student | instructor | admin` (policy per
  action, user from the token). It owns no tables.
- Student and instructor numbers are **composed from the owning features' read services** (`IStudentGradesService`,
  `IEnrollmentService`, `IScheduleService`, `IMyFinanceService`, `INotificationInboxService`, `IAttendanceService`,
  `IGradeEntryService`), so GPA, credit cap, balance and attendance standing use exactly the rules of their own pages.
  The admin counts are plain read-only queries in `DashboardRepository`.
- "Today" is the **UTC date**, the same day attendance uses (`AttendanceService.Today`). Day-of-week 1 = Monday … 6 = Saturday,
  so Sunday has no classes.
- Student "attendance warnings" = enrolled courses this term whose standing (rule A5) is Warning or Critical; the card
  also says how many are Critical.
- Instructor "missing scores" = the grading page's `NotGraded` summed over the instructor's current-term sections that
  aren't finalized. "Attendance to take" (`Rules/AttendanceDue`) = meetings from **Monday of this week up to today**,
  inside the term, with no session yet for that meeting and date.
- Admin: active = `IsActive` users (deactivated and soft-deleted excluded); "open support requests" = Open + InProgress
  ("new" = Open); "unpaid charges" = Pending + Partial, total = Σ amount − Σ payments of those charges.
- Every card links to the page behind the number; the unread-notifications card has no link (the bell is the place).

### Consistent states, errors and accessibility (T77)
- **Friendly errors** live in one place, `errorMessage()` (`shared/api/problem.ts`): no response → connection (or timeout)
  sentence; any 5xx → "Something went wrong on our side…" (the 500 `detail` is never shown); 4xx → the API's
  ProblemDetails title (written for people), except framework titles ("One or more validation errors occurred.") and
  empty bodies, which get a sentence for the status. Tests that mocked 500s with custom titles now expect the 5xx sentence.
- **Contrast** (WCAG 2.1 AA): `success` and `warning` were darkened so badge text passes on their soft backgrounds;
  `accent` is kept for decoration and text on the dark brand surfaces, and a new `accent-ink` is used for gold text on light
  surfaces (eyebrows, empty-state icons, calendar "today"). New `line-strong` for form-control edges (3:1) and `focus` for
  the focus outline (3:1 on light, 4.7:1 on brand). Placeholders use `text-muted` (no opacity). Ratios in docs/usability.md.
- **Keyboard**: `useFocusTrap` (`shared/lib`) gives `Dialog` and the mobile menu focus-in, Tab trapping, Escape and focus
  return; tabs also take Home / End; the shell has a "Skip to main content" link and `<main id="main">`.
- **Titles**: `PageHeader` sets `document.title` ("Grades · Smart SIS"); dashboards use "Dashboard"; auth pages and 404
  set their own.
- **Live regions**: `TableSkeleton`, the dashboard skeleton and the `Pagination` count are `role="status"`; alerts were
  already `alert` / `status`.
- Colour is never the only signal: the attendance Present / Absent choice now shows a tick on the chosen option, and
  dashboard warning tones always come with words in the hint.
