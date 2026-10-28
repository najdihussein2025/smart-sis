# Testing rules (xUnit)

`backend/tests/SmartSis.Tests/` mirrors `Features/`:
```
Features/<Feature>/<Class>Tests.cs     unit tests (Rules/ classes, entity behaviour)
Features/<Feature>/<Feature>ApiTests.cs integration tests (WebApplicationFactory)
Security/RoleAccessTests.cs            SR-22: wrong role → 403, no token → 401
Infrastructure/                         SisApiFactory, TestData builders, auth helpers
```

## Rules
- **Every `Rules/` class has a test class.** Cover each rule branch and the boundaries
  (credit cap exactly 18 vs 19, absences at limit−1 / limit / limit+1, 59.5% rounding to 60 = D).
- Name tests `Method_Scenario_Expected` (`Check_SectionFull_Throws`).
- Put the test case ID in a trait when one exists: `[Trait("TC", "TC_54")]`. This is how we show the
  mobile acceptance tests are covered.
- Rules tests use plain objects — no DbContext, no mocks.
- Freeze time with `FakeTimeProvider` (Microsoft.Extensions.TimeProvider.Testing). Never depend on today's date.
- Integration tests use `SisApiFactory`, which swaps SQL Server for EF InMemory and seeds its own
  simulated data. Tests that need real SQL behaviour (row locks, unique indexes) are tagged
  `[Trait("Category", "SqlServer")]` and run against LocalDB.
- No real student data in fixtures (SR-23).
- `dotnet test` must pass before a PR is opened.
