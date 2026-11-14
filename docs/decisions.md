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
