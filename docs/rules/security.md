# Security rules (SR-22 and friends)

- **Deny by default.** The API has a fallback policy that requires an authenticated user. Public
  endpoints must say `[AllowAnonymous]` explicitly (only login, forgot/reset password, health).
- **Every controller declares its role** with `[Authorize(Policy = Policies.Student | Instructor | Admin)]`
  at class level. Mixed controllers put the policy on each action.
- **Identity comes from the token, never from the request body.** Use `ICurrentUser.UserId`.
  A student endpoint never accepts `studentId` as input.
- **Ownership checks live in services.** Instructor grading/attendance/announcements → check the
  section's `InstructorId == currentUser.UserId`, else `ForbiddenException`.
- Passwords: BCrypt via `IPasswordHasher` (work factor 11). Never log, return or store plain passwords.
- Reset tokens: random 32 bytes, only the SHA-256 hash is stored, 30-minute expiry, single use.
  Forgot-password always returns 204 so it can't be used to discover valid IDs.
- Login failure message is always "Invalid University ID or password." — never say which part was wrong.
  Deactivated users get the same message.
- JWT signing key lives in user-secrets / environment (`Jwt__Key`), min 32 chars. The value in
  `appsettings.Development.json` is for local dev only.
- The AI service gets the **student's own JWT** and calls the API with it. It never gets a DB
  connection string to the core tables and never uses an admin token.
- No secrets in git: `.env`, user-secrets, connection strings with passwords.
- Every new endpoint gets a row in `Security/RoleAccessTests` (wrong role → 403, no token → 401).
