# Team conventions

The short version for humans. The full, enforceable rules are in [`docs/rules/`](rules/) and are what
Claude reads (through `CLAUDE.md`).

| Topic | Rule file |
|---|---|
| How we work, branches, commits, definition of done | [00-workflow.md](rules/00-workflow.md) |
| Business rules and where each one is enforced | [business-rules.md](rules/business-rules.md) |
| Security (roles, tokens, ownership) | [security.md](rules/security.md) |
| Backend layers, adding a feature, errors, transactions | [backend.md](rules/backend.md) |
| Tables, types, migrations, seeds | [database.md](rules/database.md) |
| Tests and test-case mapping | [testing.md](rules/testing.md) |
| React structure, data, forms, UI | [frontend.md](rules/frontend.md) |
| Python AI service boundaries | [ai-service.md](rules/ai-service.md) |

## The eight rules everyone remembers
1. A feature owns its entities; others go through its service.
2. Controllers: validate → service → DTO. No DbContext.
3. Services hold flow and transactions; repositories hold queries.
4. Rules are pure and unit-tested.
5. Entities never leave the API.
6. Errors are exceptions → ProblemDetails (400/401/403/404/409).
7. Each feature registers itself in `<Feature>Module.cs`.
8. BRD vocabulary: Term, Section, Enrollment, Component, Charge, Payment.

## Changing a rule
Talk to the team, update `docs/PROJECT.md` and the matching rule file in the same PR as the code.
