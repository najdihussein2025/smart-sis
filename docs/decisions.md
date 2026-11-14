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
