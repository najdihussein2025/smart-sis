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
