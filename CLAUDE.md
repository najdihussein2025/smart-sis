# Smart SIS — working rules

Web rebuild of the Antonine University Student Information System (HMH-SIS).
Team: Hussein Najdi, Hussein Haydar, Mahdi Marmar.

**Before any change, read `docs/PROJECT.md`.** It is the single source of truth for scope,
business rules, database and architecture. If a decision changes, update PROJECT.md in the
same commit as the code.

## Always-on rules

@docs/rules/00-workflow.md
@docs/rules/business-rules.md
@docs/rules/security.md

## Area rules (loaded only when needed)

Each area has its own `CLAUDE.md` that imports its rules, so they load only when you work there.
Don't `@`-import area rules here: every `@` import loads in every session and costs tokens.

| Working in | Rules (via that folder's CLAUDE.md) |
|---|---|
| `backend/` | docs/rules/backend.md, database.md, testing.md |
| `frontend/` | docs/rules/frontend.md |
| `ai-service/` | docs/rules/ai-service.md |
| `database/` | docs/rules/database.md (read it yourself) |
| Dockerfiles, `docker-compose.yml`, `.github/` | docs/rules/docker.md (read it yourself) |

## Quick map

```
backend/src/SmartSis.Api/Features/<Feature>/   one vertical slice per feature (whole flow inside)
backend/src/SmartSis.Api/Shared/               plumbing only, no business logic
backend/tests/SmartSis.Tests/                  mirrors Features/
frontend/src/features/<feature>/               pages, components, api.ts, hooks.ts, types.ts
ai-service/app/                                FastAPI, AI only, never writes core tables
database/                                      SSMS scripts generated from EF migrations
```

## Commands

```bash
# whole stack in Docker (from repo root)
docker compose up --build                         # web :3000, api :5080, sql :14333
docker compose down -v                            # stop + wipe database

# backend (from backend/)
dotnet build
dotnet test
dotnet ef migrations add <Name> -p src/SmartSis.Api -o Shared/Data/Migrations
dotnet ef database update -p src/SmartSis.Api
dotnet run --project src/SmartSis.Api            # http://localhost:5080, docs at /scalar

# frontend (from frontend/)
npm install && npm run dev                        # http://localhost:5173
npm run build                                     # type-check + build
npm run gen:api                                   # regenerate API types (API must be running)

# ai-service (from ai-service/)
python -m venv .venv && .venv\Scripts\activate && pip install -r requirements.txt
uvicorn app.main:app --reload --port 8000
```
