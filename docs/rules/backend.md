# Backend rules (ASP.NET Core, .NET 10)

## Shape of a feature
```
Features/<Feature>/
├── <Feature>Module.cs        public static IServiceCollection Add<Feature>Feature(this IServiceCollection s)
├── Controllers/              HTTP only
├── Interfaces/               I<Feature>Service, I<Feature>Repository
├── Services/                 flow + transactions
├── Repositories/             EF queries only
├── Models/Entities/          tables this feature owns
├── Models/Dtos/              request / response records
├── Enums/
├── Rules/                    pure rule classes (Enrollment, Grading, Attendance, Finance)
├── Validators/               FluentValidation, one per request DTO
├── Events/ Handlers/ Jobs/   only when needed
└── Data/                     IEntityTypeConfiguration<T> per entity
```
Create only the folders you use. Namespace = folder path: `SmartSis.Api.Features.Enrollment.Services`.

## Adding a feature (checklist)
1. Entities in `Models/Entities/` (inherit `BaseEntity`, or `AuditableEntity` for composite/natural keys).
2. One configuration per entity in `Data/` — table name (plural), keys, indexes, max lengths.
   Picked up automatically by `ApplyConfigurationsFromAssembly`.
3. DTOs as `sealed record`s in `Models/Dtos/`. Request DTOs end in `Request`, responses in `Response` or `Dto`.
4. Validator per request DTO in `Validators/`. Validation runs automatically via `FluentValidationFilter`.
5. Interfaces → Repository → Service → Controller.
6. Register everything in `<Feature>Module.cs`, add the call in `Shared/FeatureRegistration.cs`.
7. Migration if the schema changed.
8. Tests (see testing.md) and a row in `RoleAccessTests`.

## Layer rules
| Layer | Does | Never does |
|---|---|---|
| Controller | read route/body, call one service method, return DTO / status | touch DbContext or repositories, contain `if` business logic, catch exceptions |
| Service | orchestrate, ownership checks, call Rules, open transactions via `IUnitOfWork`, map to DTOs | write raw LINQ against `DbContext` (go through the repository) |
| Repository | EF queries, `Include`, projections, row-lock queries | call `SaveChanges`, contain business decisions |
| Rules | pure functions over plain inputs, throw `BusinessRuleException` or return a result | take a DbContext, `DateTime.Now` (pass the time in), call services |
| Entity | state + small invariants (`Charge.ApplyPayment`, `Enrollment.DropForAbsence`) | depend on services |

## Cross-feature calls
- Only the owning feature's service changes its entities. Others call its interface
  (e.g. Auth → `IUserAccountService.SetPasswordHashAsync`, Attendance → `IEnrollmentService.DropForAbsence`).
- Reading another feature's entities in a query (joins, `Include`) is fine.
- Avoid circular service dependencies; if two features need each other, raise a domain event.

## Errors
Throw, don't return error codes. `ExceptionMiddleware` maps:
| Exception | Status |
|---|---|
| `FluentValidation.ValidationException` | 400 ValidationProblemDetails |
| `UnauthorizedException` | 401 |
| `ForbiddenException` | 403 |
| `NotFoundException` | 404 |
| `BusinessRuleException` | 409 (message is shown to the user, so write it for humans) |
| anything else | 500, details only in Development |

## Transactions
```csharp
await uow.ExecuteInTransactionAsync(async () =>
{
    var section = await repo.LockSectionAsync(sectionId, ct);   // UPDLOCK, ROWLOCK
    EnrollmentPolicy.Check(...);
    repo.Add(enrollment);
    await uow.SaveChangesAsync(ct);
}, ct);
```
Single-step writes just call `uow.SaveChangesAsync(ct)`.

## Style
- File-scoped namespaces, primary constructors for DI, `sealed` by default, `async` all the way with `CancellationToken ct` last.
- DTOs are records; entities are classes with `{ get; set; }` (EF) and behaviour methods where rules live.
- Times are UTC (`DateTime.UtcNow`) in the database; convert in the frontend. Use `TimeProvider` (injected) in services so tests can freeze time.
- No AutoMapper; map by hand in the service (`ToDto()` extension next to the DTO is fine).
- Nullable reference types are on. Don't silence warnings with `!` unless EF requires it for navigations.
