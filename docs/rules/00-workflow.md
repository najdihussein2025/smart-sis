# Workflow rules

## Before writing code
1. Read `docs/PROJECT.md` sections that touch the task (§4 rules, §5 tables, §7 structure).
2. Check the build order (§10). Don't build a feature whose data depends on a phase that isn't done.
3. If the task contradicts PROJECT.md, stop and ask. Don't silently pick one.

## While working
- Stay inside the feature you are changing. If you need data from another feature, call that
  feature's service interface. Never edit another feature's entities directly.
- Keep `Program.cs` short. New wiring goes in the feature's `<Feature>Module.cs`.
- Use the BRD vocabulary: Term, Section, Enrollment, Component, Charge, Payment. Not "class",
  "semester", "registration", "fee".
- No placeholder logic that pretends to work (fake GPA, hardcoded schedule). Throw
  `NotImplementedException` or leave a `// TODO(phase-N):` with the phase number.
- Simulated data only (SR-23). Never paste real student names, IDs, emails or grades.

## When done
- `dotnet build` and `dotnet test` pass; `npm run build` passes if frontend was touched.
- Every new `Rules/` class has unit tests.
- Schema changed → new migration + regenerate `database/02_schema.sql`.
- Business decision changed → update `docs/PROJECT.md` (and this rules folder if it affects how we work).
- Map the work to test case IDs (TC_xx) or SR numbers in the PR description when they exist.

## Git
- Branch from `main`: `feature/<phase>-<short-name>` (e.g. `feature/p4-enrollment`), `fix/<short-name>`.
- Commits: imperative, scoped prefix — `backend: add EnrollmentPolicy`, `frontend: login page`,
  `db: add Sections migration`, `docs: update grading rules`.
- One feature per PR. Don't mix refactors with features.
- Never commit `appsettings.Development.json` secrets, `.env`, `bin/`, `obj/`, `node_modules/`, `.venv/`.
