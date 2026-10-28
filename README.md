# Smart SIS

AI-Assisted Student Information System for Antonine University (HMH-SIS).
Web rebuild of the earlier mobile app. Team: Hussein Najdi, Hussein Haydar, Mahdi Marmar.

**Start here:** [`docs/PROJECT.md`](docs/PROJECT.md) (scope, rules, database, architecture) and
[`docs/conventions.md`](docs/conventions.md) (how we work). Claude reads [`CLAUDE.md`](CLAUDE.md).

| Part | Stack | Folder |
|---|---|---|
| API | ASP.NET Core (.NET 10), EF Core 10, SQL Server | `backend/` |
| Web | React 19 + Vite + TypeScript, TanStack Query, Tailwind 4 | `frontend/` |
| AI | Python, FastAPI | `ai-service/` |
| SQL | SSMS scripts generated from migrations | `database/` |

## Run it locally (Windows, no Docker)

**Prerequisites:** .NET 10 SDK, SQL Server (Express/Developer) + SSMS, Node 20+, Python 3.12+.

### 1. Database + API
```powershell
cd backend
dotnet tool restore
dotnet restore
# first time only: create the initial migration
dotnet ef migrations add InitialCreate -p src/SmartSis.Api -o Shared/Data/Migrations
dotnet run --project src/SmartSis.Api
```
The API applies migrations and seeds simulated data on start (Development). API docs: http://localhost:5080/scalar

The connection string in `appsettings.Development.json` uses `Server=localhost` with Windows auth.
For SQL Express use `Server=localhost\SQLEXPRESS`. Override without editing the file:
```powershell
dotnet user-secrets set "ConnectionStrings:Sis" "Server=...;Database=SmartSis;..." --project src/SmartSis.Api
```

### 2. Web
```powershell
cd frontend
npm install
npm run dev
```
Open http://localhost:5173 and sign in with a seeded account (password `Pass@1234`):

| Role | University ID |
|---|---|
| Admin | 1001 |
| Instructor | 2001 |
| Student | 3001 |

### 3. AI service (phase 7)
```powershell
cd ai-service
python -m venv .venv; .venv\Scripts\activate
pip install -r requirements.txt
copy .env.example .env
uvicorn app.main:app --reload --port 8000
```

## Tests
```powershell
cd backend; dotnet test
cd frontend; npm run build
cd ai-service; pytest
```

## Status
Phase 1 (foundation) scaffolded: all 38 entities and configurations, JWT login, forgot/reset password,
email outbox, role layouts. See PROJECT.md §10 for the build order.
