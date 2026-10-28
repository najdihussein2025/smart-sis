# Database rules (SQL Server + EF Core 10)

## Naming
- Tables: PascalCase **plural** (`Enrollments`, `SectionMeetings`). Set explicitly with `ToTable(...)` in the configuration.
- Columns: PascalCase (`StudentId`). FK column = `<Navigation>Id`.
- Indexes: let EF name them (`IX_<Table>_<Cols>`).

## Every table
- `Id int identity` PK — except natural/composite keys: `Students(UserId)`, `Instructors(UserId)`,
  `MajorCourses(MajorId, CourseId)`, `CoursePrerequisites(CourseId, PrerequisiteId)`, `TermResults(StudentId, TermId)`.
- `CreatedAt`, `UpdatedAt` `datetime2`, set by `AuditInterceptor`. Don't set them by hand.

## Types (applied by convention in `SisDbContext.ConfigureConventions`)
| C# | SQL |
|---|---|
| enum | `nvarchar(32)` (string name) |
| decimal | `decimal(10,2)` |
| string (no max set) | `nvarchar(256)` — use `HasMaxLength` or `nvarchar(max)` explicitly for long text |
| DateTime | `datetime2` (UTC) |
| DateOnly / TimeOnly | `date` / `time` |

## Keys and deletes
- **All FKs are `Restrict`** — enforced globally in `OnModelCreating`. Don't configure `Cascade`.
- Soft delete: entities implementing `ISoftDeletable` (`User`, `ChatConversation`) get a global query
  filter `DeletedAt == null`. `Remove()` is turned into an update by `SoftDeleteInterceptor`.
  Use `IgnoreQueryFilters()` when you need history (transcripts of deactivated students).
- Uniques that carry business rules (never remove): `Users.UniversityId`, `Users.Email`,
  `Departments.Code`, `Courses.Code`, `CartItems(StudentId, SectionId)`,
  `StudentGrades(EnrollmentId, ComponentId)`, `AttendanceSessions(MeetingId, SessionDate)`,
  `Sections(CourseId, TermId, SectionCode)`.

## Migrations
- EF migrations are the source of truth. Never edit the DB by hand in SSMS.
- From `backend/`:
  ```
  dotnet ef migrations add <PascalCaseName> -p src/SmartSis.Api -o Shared/Data/Migrations
  dotnet ef database update -p src/SmartSis.Api
  dotnet ef migrations script --idempotent -p src/SmartSis.Api -o ../database/02_schema.sql
  ```
- One migration per PR, named after the change (`AddSectionMeetings`, not `Update3`).
- Don't edit a migration that's already on `main`. Add a new one.
- Review the generated migration: no `Cascade`, no dropped columns you didn't mean to drop.

## Seeds
- Reference data that never changes (`GradeScales`) → `HasData` in its configuration.
- Dev data (simulated users, a term, courses) → `Shared/Data/Seed/DevSeeder.cs`, runs only in Development
  when the DB is empty. Simulated data only (SR-23).

## Embeddings
`KnowledgeChunks.Embedding` is `varbinary(max)` (float32 little-endian). Switch to `VECTOR` only after the
team confirms SQL Server 2025 (open item in PROJECT.md §11).
