# Smart SIS: Project Reference

**AI-Assisted Student Information System for Antonine University**
Document ID: HMH-SIS · Team: Hussein Najdi, Hussein Haydar, Mahdi Marmar · Last updated: 28 Oct 2026

This file is the single reference for what the project is, what was decided, and how it is built. Read it before touching code or the database. If a decision changes, update this file.

---

## 1. What the project is

A web-based Student Information System with three roles (Student, Instructor, Admin) plus two AI features: an academic chatbot and a career recommendation module.

The project was first built as a **mobile app** (React Native, Node.js, MySQL, Python AI) together with another team (RJK). We are now **rebuilding it as a web app**. Everything the mobile app did carries over, and we add the BRD items the mobile app never had.

**Source documents** (in `docs/`):
- `BRD-HMH-SIS.docx`: business and software requirements (BR-1 to BR-8, SR-1 to SR-27)
- `Smart-SIS-Test-Cases.xlsx`: 114 test cases from the mobile version (TC_01 to TC_114)
- `UI-UX_Designs.pdf`: mobile screens for all three roles
- `Activity1.drawio`: activity diagrams (login, dashboards, admin flows)
- `Smart-SIS-Database-Schema.pdf`: ERD for all modules

---

## 2. Tech stack

| Part | Technology |
|---|---|
| Backend | ASP.NET Core Web API, **.NET 10 (LTS)**, controllers |
| ORM | Entity Framework Core 10 (code-first migrations) |
| Database | **SQL Server**, managed with SSMS |
| Frontend | React + Vite + TypeScript, TanStack Query, React Hook Form + Zod, Tailwind |
| AI service | Python, FastAPI |
| Auth | JWT bearer tokens, role-based policies, BCrypt password hashing |
| Validation | FluentValidation |
| Tests | xUnit + WebApplicationFactory |
| Containers | **None** (no Docker) |

---

## 3. Roles and features

### Shared by all roles
- Login with University ID + password. Wrong credentials show an error; the error clears when the user types again.
- Forgot password: enter ID → reset link by email → set new password → back to login.
- After login the user lands on their role's dashboard.
- Profile page and logout.
- Notifications bell (in-app) and announcements feed.

### Student
| Feature | Details |
|---|---|
| Dashboard | Name, ID, major, photo, current GPA, credits, quick links |
| Schedule | Day tabs Mon–Sat; each class shows time, course, room, instructor. Empty-day state |
| Course catalog | Major / Elective tabs → course details (code, name, credits, description, prerequisites) → sections with day, time, room, instructor, capacity |
| Cart | Add a section; button shows "In Cart" or "Enrolled". Enroll several at once; remove items |
| Swap | Swap an enrolled course for one in the cart (different course or section change) |
| Drop | Drop with confirmation; seat returns to the section |
| Grades / transcript | Per course: components with weight, earned/max, %, "Not graded"; progress bar; term and cumulative GPA |
| Attendance | Absences per course with colour status; drill down to dates |
| Financial account | Pending / Completed tabs, outstanding balance, due dates, partial payments |
| Support requests | Submit an issue (registration, grade dispute, finance…), follow its status, reply in a thread |
| AI chatbot | Personal questions (my grades, schedule, deadlines) and concept explanations; saved conversations |
| Career module | **Parked**, see §9 |

### Instructor
| Feature | Details |
|---|---|
| Dashboard | Name, ID, department, active courses count |
| Teaching schedule | Day tabs; lecture / lab labels |
| Attendance | 3 steps: course/section → date + session → students with Present/Absent → submit. Duplicate submit blocked |
| Grading | 3 steps: course/section → component → score per student. Existing scores pre-filled; "X not graded" hint |
| Announcements | Post to **their own sections only** |

### Admin
| Feature | Details |
|---|---|
| Users | Add, edit, deactivate students and instructors (soft delete) |
| Lookups | Campuses, departments, majors (with credit cap), rooms, payment types, support categories |
| Terms | Create terms with their enrollment and add/drop windows |
| Courses | Create courses, prerequisites, major/elective mapping |
| Sections | Create sections, assign instructor, capacity, meetings (day, time, room, lecture/lab) |
| Grade components | Per course per term: name, max grade, weight; total weight ≤ 100% |
| Payments | Charge a whole major at once; record full or partial payments |
| Announcements | Post to everyone, students, instructors, or a major |
| Support inbox | Handle all student support requests |
| Knowledge base | Upload university policies / FAQs used by the chatbot |

---

## 4. Business rules (agreed decisions)

### Terms and enrollment windows
Each term has two windows:
- **Enrollment period:** students enroll from the cart and can **swap**. No dropping.
- **Add/Drop period:** students can **add, drop and swap**.
- Outside both windows, enrollment is locked.

### Enrollment checks (enroll and swap)
1. The relevant window is open.
2. **Credit cap:** default **18 credits per term**, set on the major when the admin creates it. The admin can override it per student.
3. **Prerequisites:** passed with **D or better** (grade points ≥ 1.0) in a completed attempt.
4. Not already enrolled in another section of the same course.
5. No time conflict with the student's other enrolled sections.
6. The section has a free seat (checked inside a transaction with a row lock).

### Grading
- The **admin** defines grade components per course per term; the **instructor** fills in the scores.
- Final percentage = weighted sum of components, **rounded to the nearest whole number**, then looked up in the grade scale.
- **Retakes:** only the **latest** completed attempt counts in the GPA.
- GPA is on a **4.0 scale**:

| Letter | Status | GPA | % |
|---|---|---|---|
| A+ | Outstanding | 4.0 | 97–100 |
| A | Excellent | 4.0 | 93–96 |
| A- | Very Good | 3.7 | 89–92 |
| B+ | Good | 3.3 | 85–88 |
| B | Good | 3.0 | 80–84 |
| B- | Good | 2.7 | 77–79 |
| C+ | Satisfactory | 2.3 | 73–76 |
| C | Satisfactory | 2.0 | 70–72 |
| C- | Satisfactory | 1.7 | 66–69 |
| D+ | Passing | 1.3 | 63–65 |
| D | Passing | 1.0 | 60–62 |
| F | Failure | 0.0 | 0–59 |

### Attendance
- Absence limit per course = **credits × 3** (a 3-credit course allows 9 absences).
- Warning **email + notification** at limit − 1 and again at the limit.
- At **limit + 1** the student is **automatically dropped** from the course.
- An absence drop records an **F** (0.0) that **counts in the GPA**.
- Student view colours: 0 absences green "Perfect", some absences orange "Warning", near/at limit red "Critical".

### Finance
- Admin charges all students of a major in one action (a charge batch).
- **Partial payments** are allowed; a charge is Pending → Partial → Paid based on the sum of its payments.

### Data safety
- **Users are never hard-deleted** (soft delete), so grades and payment history are kept.
- Foreign keys use RESTRICT, never CASCADE.
- Development and testing use **simulated data only** (SR-23).

### Announcements
- Admin: all users, students only, instructors only, or a specific major.
- Instructor: only sections they teach.

### Support
- All support requests are handled by the **admin**.

---

## 5. Database

SQL Server, created from EF Core migrations. **38 tables in 9 modules.** Tables are PascalCase plural (`Enrollments`), columns PascalCase (`StudentId`). Every table also has `Id` (int identity), `CreatedAt`, `UpdatedAt` (`datetime2`). Tables with a natural key (`Students`, `Instructors`, `MajorCourses`, `CoursePrerequisites`, `TermResults`) use that key instead of `Id`. Enums are stored as `nvarchar` so data stays readable in SSMS. Money and GPA use `decimal(10,2)`.

### 5.1 Users and auth
| Table | Key columns |
|---|---|
| Users | UniversityId (unique, numeric), Email (unique), PasswordHash, Role (Student / Instructor / Admin), FullName, Phone, DateOfBirth, PhotoUrl, IsActive, DeletedAt |
| Students | UserId (PK/FK), MajorId, CampusId, EnrollmentDate, MaxCreditsOverride (null = major default) |
| Instructors | UserId (PK/FK), DepartmentId |
| PasswordResetTokens | UserId, TokenHash, ExpiresAt, UsedAt |

### 5.2 Lookups (admin-managed)
| Table | Key columns |
|---|---|
| Campuses | Name |
| Departments | Code (unique), Name |
| Majors | DepartmentId, Name, DefaultMaxCredits (default 18) |
| Rooms | CampusId, Name, Capacity |
| PaymentTypes | Name (Tuition, Registration, Lab fee…) |
| GradeScales | Letter, StatusLabel, GpaPoints, MinPct, MaxPct (seeded from the table in §4) |

### 5.3 Terms and courses
| Table | Key columns |
|---|---|
| Terms | Name, StartDate, EndDate, EnrollStart, EnrollEnd, AddDropStart, AddDropEnd, IsCurrent |
| Courses | Code (unique), Name, Credits, Description, DepartmentId |
| MajorCourses | MajorId + CourseId (PK), Type (Major / Elective) |
| CoursePrerequisites | CourseId + PrerequisiteId (PK) |
| Sections | CourseId, TermId, SectionCode, InstructorId, Capacity |
| SectionMeetings | SectionId, DayOfWeek (1 Mon – 6 Sat), StartTime, EndTime, RoomId, Type (Lecture / Lab) |

Major vs elective lives on `MajorCourses` because one course can be "major" for one major and "elective" for another.

### 5.4 Enrollment
| Table | Key columns |
|---|---|
| CartItems | StudentId, SectionId (unique pair) |
| Enrollments | StudentId, SectionId, Status (Enrolled / Dropped / DroppedAbsence / Completed), FinalPct, LetterGrade, GradePoints, CountsInGpa, EnrolledAt, DroppedAt |
| EnrollmentActions | StudentId, Action (Enroll / Swap / Drop / AutoDrop), FromSectionId, ToSectionId, PerformedBy, PerformedAt |

Seats taken are **counted** from Enrollments, never stored on the section, so capacity can't drift. `CountsInGpa` is set to false on older attempts when a course is retaken.

### 5.5 Grading
| Table | Key columns |
|---|---|
| GradeComponents | CourseId, TermId, Name, MaxGrade, Weight (sum ≤ 100) |
| StudentGrades | EnrollmentId, ComponentId (unique pair), Score, GradedBy, GradedAt |
| TermResults | StudentId + TermId (PK), TermGpa, CumulativeGpa, CreditsAttempted, CreditsEarned |

`TermResults` is a cache rebuilt when grades are finalized, so dashboards stay fast.

### 5.6 Attendance
| Table | Key columns |
|---|---|
| AttendanceSessions | SectionId, MeetingId, SessionDate (unique MeetingId + SessionDate), SubmittedBy, SubmittedAt |
| AttendanceRecords | SessionId, EnrollmentId, Status (Present / Absent) |
| AbsenceAlerts | EnrollmentId, Level (Warning / Limit / AutoDropped), AbsenceCount, SentAt |

The unique key on sessions blocks duplicate attendance (fixes mobile bug BG_06). `AbsenceAlerts` makes sure each warning email goes out only once.

### 5.7 Finance
| Table | Key columns |
|---|---|
| ChargeBatches | MajorId, TermId, PaymentTypeId, Title, Amount, DueDate, CreatedBy |
| Charges | StudentId, BatchId (null for one-off), TermId, PaymentTypeId, Title, Amount, DueDate, Status (Pending / Partial / Paid) |
| Payments | ChargeId, Amount, PaidAt, RecordedBy, Note |

### 5.8 Communication and support
| Table | Key columns |
|---|---|
| Announcements | AuthorId, Title, Body, Scope (All / Students / Instructors / Major / Section), MajorId, SectionId, PublishAt, ExpiresAt |
| Notifications | UserId, Type, Title, Body, Link, ReadAt |
| EmailOutbox | ToEmail, Subject, Body, Status (Queued / Sent / Failed), Attempts, SentAt |
| SupportCategories | Name |
| SupportRequests | StudentId, CategoryId, Subject, Description, Status (Open / InProgress / Resolved / Closed), AssignedAdminId, ResolvedAt |
| SupportMessages | RequestId, AuthorId, Body |

Emails are queued in `EmailOutbox` and sent by a background job, so a mail failure never blocks submitting attendance.

### 5.9 AI chatbot
| Table | Key columns |
|---|---|
| ChatConversations | StudentId, Title, DeletedAt |
| ChatMessages | ConversationId, Role (User / Assistant / Tool), Content |
| KnowledgeDocuments | Title, FileUrl, UploadedBy, Status (Processing / Ready / Failed) |
| KnowledgeChunks | DocumentId, Content, Embedding |

**Embeddings on SQL Server:** if the server is SQL Server 2025, use the native `VECTOR` type. On older versions, store the embedding as `varbinary` and let the Python service do the similarity search. *(Open item: confirm the SQL Server version. The code currently uses `varbinary(max)`.)*

### 5.10 Where each rule is enforced
| Rule | Enforced by |
|---|---|
| Enrollment / add-drop windows | Backend, using the dates on `Terms` |
| Credit cap | Backend: `Students.MaxCreditsOverride` ?? `Majors.DefaultMaxCredits` |
| Prerequisite ≥ D | Backend: `GradePoints >= 1.0` on a completed enrollment |
| One section per course, no clash, free seat | Backend, one transaction with `UPDLOCK, ROWLOCK` on the section |
| Same section not twice in cart | Unique index (StudentId, SectionId) on `CartItems` |
| Absence limit and auto-drop | Backend after each attendance submit; logged in `AbsenceAlerts` |
| Absence drop = F in GPA | `Status = DroppedAbsence`, `LetterGrade = F`, `GradePoints = 0` |
| Latest retake counts | Backend sets older attempts' `CountsInGpa = false` |
| Component weights ≤ 100 | Backend validation on insert / update |
| No duplicate attendance | Unique index (MeetingId, SessionDate) |
| One grade per student per component | Unique index (EnrollmentId, ComponentId) |
| Charge status follows payments | Backend after each payment |
| No hard deletes | `DeletedAt` + `IsActive`, global query filter, FK RESTRICT |
| Instructor announcements own sections only | Backend role and ownership check |

---

## 6. Architecture

### 6.1 Summary (how to describe it)
> A **feature-based modular monolith** (vertical slices) built with ASP.NET Core, using the **Service + Repository pattern**, with **tactical DDD** (rich entities and pure rule classes) in the rule-heavy features: Enrollment, Grading, Attendance and Finance. The AI features run as a separate **Python service**. The frontend is a **React SPA** talking to the API over REST with JWT and role-based authorization.

### 6.2 System overview
```
React SPA ──HTTPS / JSON + JWT──► ASP.NET Core API ──EF Core──► SQL Server
                                     │   ▲
                                     │   └── tool calls with the student's own JWT
                                     ▼   │
                              Python AI service (FastAPI) ──► LLM provider

Background jobs inside the API: email outbox, absence alerts, GPA rebuild
```
- The **.NET API owns all business data and rules**. Nothing else writes to the core tables.
- The **Python service only does AI**. It reads student data by calling the .NET API with the student's JWT, so it can only ever see that student's data. It writes only to `KnowledgeChunks`.
- **Chat flow:** React → .NET `/api/chat` → .NET forwards message + history + JWT to Python → the LLM calls tools (my grades, my schedule) through the .NET API and searches the knowledge base → answer returns to .NET → .NET saves both messages.

### 6.3 Why this architecture (for the review)
- **Why not microservices?** One team, one database, one deployment. Microservices would add network calls, distributed transactions and DevOps overhead without solving a problem we have. The AI part is split out because it uses a different language.
- **Why not full DDD?** About 70% of the system is CRUD (users, lookups, courses, announcements). Full DDD there is pure overhead. We apply it where the business rules live, so those rules are isolated and unit-tested.
- **Where are the rules?** In each feature's `Rules/` folder as pure classes with no database access: `EnrollmentPolicy`, `SwapPolicy`, `GradeCalculator`, `GpaCalculator`, `AbsencePolicy`. They map directly to the test cases.
- **Security:** JWT, role policies on every endpoint, ownership checks in services (an instructor can only grade sections they teach), and the AI service uses the student's own token.
- **Data integrity:** soft delete, restrictive foreign keys, unique indexes for duplicates, and transactions with row locks for enrollment.

### 6.4 Request flow example: enroll from cart
```
POST /api/enrollment/enroll
 → JWT gives StudentId
 → FluentValidation checks the request
 → EnrollmentService.EnrollAsync
     begin transaction
     EnrollmentPolicy: window → credit cap → prerequisites (≥ D)
                       → same course other section → time conflict → capacity (row lock)
     insert Enrollments + EnrollmentActions, delete CartItems
     commit
 → 200 with the enrolled list
   or 409 ProblemDetails naming the broken rule ("Schedule conflict", "Section is full")
```

---

## 7. Folder structure

### 7.1 Repository root
```
smart-sis/
├── README.md
├── CLAUDE.md             rules entry point for Claude (imports docs/rules/*)
├── docs/                 BRD, test cases, ERD PDF, this file, conventions.md, rules/
├── database/             SQL scripts for SSMS (create DB, schema script, seeds, views)
├── backend/              SmartSis.slnx
│   ├── src/SmartSis.Api/
│   └── tests/SmartSis.Tests/
├── frontend/             React + Vite + TypeScript
└── ai-service/           Python FastAPI
```
EF migrations are the source of truth for the schema. `database/02_schema.sql` is generated from them for SSMS.

### 7.2 Backend: one API project, one folder per feature
Each feature folder contains its **whole flow**, so you can read a feature top to bottom without jumping around:

```
Features/<Feature>/
├── <Feature>Module.cs       DI registration: services.Add<Feature>Feature()
├── Controllers/             HTTP endpoints only
├── Interfaces/              I<Feature>Service, I<Feature>Repository
├── Services/                flow + transactions
├── Repositories/            EF queries only
├── Models/
│   ├── Entities/            tables owned by this feature
│   └── Dtos/                request / response objects
├── Enums/
├── Rules/                   pure business rules (rule-heavy features only)
├── Validators/              FluentValidation
├── Events/                  domain events (where needed)
├── Handlers/ / Jobs/        event handlers, background jobs (where needed)
└── Data/                    IEntityTypeConfiguration per entity (keys, indexes, enum → nvarchar)
```

Example, Enrollment in full:
```
Features/Enrollment/
├── EnrollmentModule.cs
├── Controllers/EnrollmentController.cs          enroll, swap, drop, my courses
├── Interfaces/IEnrollmentService.cs, IEnrollmentRepository.cs
├── Services/EnrollmentService.cs
├── Repositories/EnrollmentRepository.cs
├── Models/Entities/Enrollment.cs, EnrollmentAction.cs
├── Models/Dtos/EnrollRequest.cs, SwapRequest.cs, DropRequest.cs, EnrollmentResponse.cs
├── Enums/EnrollmentStatus.cs, EnrollmentActionType.cs
├── Rules/EnrollmentPolicy.cs, SwapPolicy.cs
├── Validators/EnrollRequestValidator.cs, SwapRequestValidator.cs
├── Events/StudentAutoDropped.cs
└── Data/EnrollmentConfiguration.cs, EnrollmentActionConfiguration.cs
```

Features and the entities each one owns (38 total):

| Feature | Owns |
|---|---|
| Auth | PasswordResetToken |
| Users | User, Student, Instructor |
| Lookups | Campus, Department, Major, Room, PaymentType, GradeScale |
| Terms | Term |
| Courses | Course, MajorCourse, CoursePrerequisite |
| Sections | Section, SectionMeeting |
| Schedule | (read-only, none) |
| Cart | CartItem |
| Enrollment | Enrollment, EnrollmentAction |
| Grading | GradeComponent, StudentGrade, TermResult |
| Attendance | AttendanceSession, AttendanceRecord, AbsenceAlert |
| Finance | ChargeBatch, Charge, Payment |
| Announcements | Announcement |
| Notifications | Notification, EmailOutbox |
| Support | SupportCategory, SupportRequest, SupportMessage |
| Dashboard | (read-only, none) |
| Chat | ChatConversation, ChatMessage, KnowledgeDocument, KnowledgeChunk |

Shared plumbing (no business logic):
```
Shared/
├── Data/           SisDbContext, IUnitOfWork / UnitOfWork, Interceptors (audit, soft delete), Migrations, Seed
├── Models/         BaseEntity, ISoftDeletable, IDomainEvent, PagedResult
├── Repositories/   IRepository<T>, Repository<T>  (generic CRUD; feature repos add their own queries)
├── Auth/           Roles, Policies, JwtSettings, ICurrentUser, JwtTokenService, PasswordHasher
├── Exceptions/     BusinessRuleException (409), NotFoundException (404), ForbiddenException (403), ExceptionMiddleware
├── Email/          IEmailSender, templates
└── FeatureRegistration.cs   AddFeatures() calls every <Feature>Module
```

### 7.3 Tests
`tests/SmartSis.Tests/` mirrors `Features/`:
- Unit tests for every `Rules/` class (e.g. `EnrollmentPolicyTests` ↔ TC_54–56, `SwapPolicyTests` ↔ TC_63–67, `AbsencePolicyTests`, `GradeCalculatorTests`, `GpaCalculatorTests`).
- Integration tests per feature with `WebApplicationFactory` (e.g. auth TC_01–06, attendance TC_36–43).
- `Security/RoleAccessTests` for SR-22 (unauthorized role access is denied).

### 7.4 Frontend
```
frontend/src/
├── app/            App.tsx, router.tsx (/login, /student/*, /instructor/*, /admin/*), providers, RequireRole
├── layouts/        AuthLayout, StudentLayout, InstructorLayout, AdminLayout
├── shared/         api client (axios + JWT), UI components (Table, Modal, Stepper, DayTabs,
│                   EmptyState, ConfirmDialog, StatCard), hooks, format utils
└── features/<feature>/   pages/  components/  api.ts  hooks.ts  types.ts
    auth, profile, dashboard, schedule, courses, sections, terms, cart, enrollment, grading,
    attendance, finance, users, lookups, announcements, notifications, support, chat
```
API types are generated from the backend's OpenAPI document so frontend and backend never drift.

### 7.5 AI service
```
ai-service/app/
├── main.py         POST /chat, POST /ingest, GET /health
├── chat/           agent.py (LLM + tools), tools.py (get_my_grades, get_my_schedule, get_deadlines), prompts.py
├── rag/            chunker.py, embeddings.py, search.py
├── clients/        sis_api.py (calls the .NET API with the student's JWT)
└── career/         parked
```

---

## 8. Team conventions

1. **A feature owns its entities.** Other features may read them, but only the owning feature's service changes them. Example: Attendance auto-drop calls `IEnrollmentService.DropForAbsence()`; it never edits the enrollment directly.
2. **Controllers** validate → call the service → return a DTO. No DbContext in controllers.
3. **Services** hold the flow and transactions (`IUnitOfWork`). **Repositories** hold queries only.
4. **Rules** are pure (no DbContext) and each one has a unit test.
5. **Entities never leave the API.** Always return DTOs.
6. **Errors** go through `ExceptionMiddleware` as ProblemDetails: 400 validation, 401 bad login, 403 forbidden, 404 not found, 409 business rule.
7. Each feature registers itself in `<Feature>Module.cs`; `Program.cs` stays short.
8. Names follow the BRD vocabulary: Term, Section, Enrollment, Component, Charge, Payment.

The detailed, enforceable version of these lives in `docs/rules/`.

---

## 9. Parked: career recommendation module

Agreed to design later. Proposed direction for when we pick it up:
- **Admin sets up:** traits/skills, career paths with a weight per trait, roadmap steps, linked courses, course-to-trait tags, and a question bank where each answer adds points to traits.
- **Student flow:** take a 20–30 question quiz (retake per term) → rule-based scoring combines quiz traits (~70%) and grades in trait-tagged courses (~30%) → top 3 careers with % match → the LLM writes the explanation and personalises the roadmap → suggested courses with "Add to cart".
- It will add its own tables and link only to `Students` and `Courses`.

---

## 10. Build order

1. **Foundation:** solution, Shared/, DbContext, all entities and configurations, first migration, seeds, JWT login, role layouts in React. ← *scaffolded 28 Oct 2026; first migration still to be generated locally*
2. **Admin setup:** lookups, terms, courses, sections, users, grade components (everything else needs this data).
3. **Student read-only:** dashboard, schedule, catalog, profile, finance.
4. **Enrollment:** cart, enroll, swap, drop with all rules and tests.
5. **Instructor:** attendance (with absence alerts, emails, auto-drop) and grading (with GPA and TermResults).
6. **Payments, announcements, notifications, support.**
7. **AI chatbot,** then the career module.

The mobile test cases (TC_01–TC_114) map to phases 1–6 and are reused as acceptance tests.

---

## 11. Open items

- [ ] Confirm the SQL Server version (decides how embeddings are stored).
- [ ] Career module design (§9).
- [ ] Refresh tokens: login currently issues one access token (8 h). Decide whether we need refresh tokens (would add a `RefreshTokens` table, 39th).
- [ ] Fix the BRD: SR-1 is listed twice, SR-8 and SR-12 are missing, the §5.3 table references shifted SR numbers, and the out-of-scope row cites SR-22 (which is the security requirement) for "no mobile app".
- [ ] BRD open issues still pending with stakeholders: OI-2 (official authentication service), OI-4 (response-time limits), OI-6 (supported browsers). OI-1, OI-3, OI-5 and OI-7 are answered by the decisions in §4 and §6 and can be closed in the next BRD version.
