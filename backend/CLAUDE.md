# backend/ — read before editing

@../docs/rules/backend.md
@../docs/rules/database.md
@../docs/rules/testing.md

- One API project: `src/SmartSis.Api`. Features in `Features/<Feature>/`, plumbing in `Shared/`.
- The feature folder is **`Enrollments`** (plural), not `Enrollment`: a namespace named `Enrollment`
  would clash with the `Enrollment` entity class.
- Already working: Auth (login, me, forgot/reset password), Users (profile + `IUserAccountService`),
  Notifications (`IEmailQueue`, `INotificationService`, `EmailOutboxJob`). Every other feature has its
  entities + configurations and an empty `<Feature>Module.cs` with a `TODO(phase-N)`.
- Seeded dev accounts (password `Pass@1234`): admin `1001`, instructor `2001`, student `3001`.
