# Docker flow

## Local stack
```
copy .env.example .env          # once; set SA_PASSWORD and JWT_KEY
docker compose up --build       # db + api + web
docker compose --profile ai up --build   # + AI service
docker compose down             # stop (keeps data)
docker compose down -v          # stop + wipe the database
```
| Service | Image / build | Port on your PC | Inside the network |
|---|---|---|---|
| `db` | `mcr.microsoft.com/mssql/server:2022-latest` | 14333 (SSMS: `localhost,14333`) | `db:1433` |
| `api` | `backend/Dockerfile` | 5080 (`/scalar`, `/health`) | `api:8080` |
| `web` | `frontend/Dockerfile` (nginx) | 3000 | proxies `/api/` → `api:8080` |
| `ai` | `ai-service/Dockerfile` (profile `ai`) | 8000 | `ai:8000` |

Start order: `db` healthy → `api` (applies EF migrations, seeds simulated data) → `web`.

## Rules
- Config goes through environment variables in `docker-compose.yml` (`Section__Key`), values from `.env`.
  Never bake secrets into an image or commit `.env`.
- The API container needs a **committed EF migration**; it never creates tables any other way.
- New config key in `appsettings.json` → add it to `docker-compose.yml` if Docker needs a different value.
- New service → Dockerfile + `.dockerignore` in its folder, a service in compose, a row in the table above.
- Images run as non-root (`$APP_UID` for .NET, `appuser` for Python).
- Pin base image majors (`sdk:10.0`, `node:22-alpine`, `nginx:1.27-alpine`, `python:3.13-slim`).

## CI (`.github/workflows/ci.yml`)
On every push to `main` and every PR:
1. **backend** — `dotnet build` + `dotnet test`
2. **frontend** — `npm ci` + `npm run build`
3. **ai-service** — `pytest`
4. **docker** — builds all images, starts the stack, and smoke-tests web → API → SQL Server
   (SPA routes, login, profile, wrong password → 401). Compose logs are printed if it fails.

A red CI blocks merging. Fix it before starting the next feature.
