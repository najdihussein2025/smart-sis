# database/

SQL scripts for working with the database in SSMS. **EF Core migrations are the source of truth**
(`backend/src/SmartSis.Api/Shared/Data/Migrations`). Never change tables by hand.

| File | What | How it's made |
|---|---|---|
| `01_create_database.sql` | Creates the empty `SmartSis` database | hand-written |
| `02_schema.sql` | Full schema, idempotent | generated, see below |
| `03_views.sql` | Read-only reporting views for SSMS | hand-written (later) |

Generate `02_schema.sql` after every migration (from `backend/`):

```
dotnet ef migrations script --idempotent -p src/SmartSis.Api -o ../database/02_schema.sql
```

Simulated dev data is inserted by `DevSeeder` when the API starts in Development with an empty
database. The grade scale is seeded by the migration itself.
