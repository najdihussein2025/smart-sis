# Performance results (T76, AB#143)

Load test of the Smart SIS API on a developer machine. Tool: `tools/load/loadtest.mjs` (autocannon 7.15.0).
Raw numbers: `tools/load/results/*.json`. Simulated data only (SR-23).

## Method
- API: `dotnet run -c Release --no-launch-profile` on `127.0.0.1`, SQL Server LocalDB, fresh database per run
  (migrations + dev seed, `AiService:BaseUrl` pointed at an unused port).
- Client and server ran on the **same machine** (Windows 11, i7-13620H 16 threads, 32 GB), so they compete for CPU.
  Use `127.0.0.1`, not `localhost` (IPv6 fallback adds seconds on Windows).
- The script creates N simulated students (`4100001…`, `load<id>@smartsis.local`) via the admin API and logs them in.
- Each endpoint: 2 s warm-up, then 15 s of keep-alive connections. Every request uses a random student's token
  (admin lists use the admin token). Login uses at most 10 connections because BCrypt (work factor 11) is CPU-bound by design.
- autocannon reports p50 / p90 / p99, not p95.
- Run: `cd tools/load && npm install && node loadtest.mjs --base http://127.0.0.1:5080 --users 60 --connections 25 --duration 15 --out results/run.json`.
  It mutates the database; use a throw-away one.

## Scenarios
1. **Seat race (rule E6):** a section with 5 (or 10) seats, one enroll per student fired at the same moment (60 or 100 students).
   Pass = exactly `capacity` succeed, `seatsTaken == capacity`, no 5xx.
2. **Read endpoints:** login, catalog, cart, my enrollments, schedule, grades, attendance, finance, announcements,
   notifications (+ unread count), admin users page, admin charges, admin support.

## Seat race result
| Build | Students / seats | Succeeded | seatsTaken | 5xx | Wall time |
|---|---|---:|---:|---:|---:|
| Before T75 | 60 / 5 | 5 | 5 | 0 | 372 ms |
| Current main | 60 / 5 | 5 | 5 | 0 | 2306 ms |
| Before T75 | 100 / 10 | 10 | 10 | 0 | 237 ms |
| Current main | 100 / 10 | 10 | 10 | 0 | 178 ms |

Capacity is never exceeded. The 60-student wall time on main (2.3 s) is the first request burst after a cold start
(connection pool and JIT warm-up) and is not a regression: the 100-student run on the same build took 178 ms.

## T75 effect: before vs after (25 connections, 60 students)
Before = commit `cadd778` (parent of T75), after = current main. Release builds, each on its own fresh LocalDB database.

| Endpoint | req/s before | req/s after | p50 ms before | p50 ms after | p99 ms before | p99 ms after |
|---|---:|---:|---:|---:|---:|---:|
| `POST /api/auth/login` (10 conn) | 37 | 23 | 254 | 345 | 409 | 2635 |
| `GET /api/catalog/courses` | 2203 | 1750 | 9 | 11 | 30 | 42 |
| `GET /api/cart` | 3229 | 3048 | 6 | 7 | 29 | 22 |
| `GET /api/enrollment/my` | 4447 | 3717 | 4 | 5 | 18 | 21 |
| `GET /api/schedule/my` | 5113 | 4017 | 4 | 5 | 14 | 19 |
| `GET /api/grades/my` | 3588 | 2373 | 6 | 8 | 19 | 39 |
| `GET /api/attendance/my` | 4597 | 2860 | 4 | 7 | 13 | 28 |
| `GET /api/finance/my` | 5801 | 5219 | 3 | 3 | 16 | 18 |
| `GET /api/announcements` | 2911 | 2571 | 7 | 8 | 21 | 26 |
| `GET /api/notifications` | 6227 | 4028 | 3 | 4 | 8 | 23 |
| `GET /api/notifications/unread-count` | 7753 | 6688 | 2 | 3 | 8 | 12 |
| `GET /api/admin/users?page=2` | 3396 | 2792 | 6 | 7 | 20 | 25 |
| `GET /api/admin/finance/charges` | 3739 | 3409 | 6 | 6 | 17 | 18 |
| `GET /api/admin/support` | 3880 | 3120 | 5 | 6 | 17 | 28 |

**Reading this honestly:** on the small dev dataset T75 gives **no measurable improvement**; the "after" run is 5-40 %
slower on most rows. We do not claim T75 made these endpoints faster. Likely reasons: with only dozens of rows per table
SQL Server scans are already cheap, so indexes and `AsNoTracking` cannot help much; the "before" run was done first and
the "after" database/JIT state differed; single runs on a shared desktop vary by 10-30 %. T75's value (bounded page size,
no unbounded lists, indexes for large tables) is aimed at production-size data, which we did not generate. All rows
had 0 % errors in both builds.

## 50 connections, 100 students (current main vs before T75)
| Endpoint | Conn. | req/s | p50 ms | p90 ms | p99 ms | Errors |
|---|---:|---:|---:|---:|---:|---:|
| `POST /api/auth/login` | 10 | 31 | 311 | 412 | 600 | 0 % |
| `GET /api/catalog/courses` | 50 | 2036 | 21 | 34 | 59 | 0 % |
| `GET /api/cart` | 50 | 3383 | 13 | 21 | 35 | 0 % |
| `GET /api/enrollment/my` | 50 | 3332 | 11 | 20 | 60 | 0 % |
| `GET /api/schedule/my` | 50 | 3693 | 11 | 20 | 44 | 0 % |
| `GET /api/grades/my` | 50 | 3276 | 13 | 21 | 37 | 0 % |
| `GET /api/attendance/my` | 50 | 4018 | 10 | 17 | 36 | 0 % |
| `GET /api/finance/my` | 50 | 6671 | 6 | 10 | 19 | 0 % |
| `GET /api/announcements` | 50 | 3162 | 13 | 22 | 45 | 0 % |
| `GET /api/notifications` | 50 | 5397 | 8 | 13 | 26 | 0 % |
| `GET /api/notifications/unread-count` | 50 | 6349 | 6 | 11 | 29 | 0 % |
| `GET /api/admin/users?page=2` | 50 | 2361 | 17 | 32 | 87 | 0 % |
| `GET /api/admin/finance/charges` | 50 | 3285 | 13 | 21 | 38 | 0 % |
| `GET /api/admin/support` | 50 | 3617 | 12 | 20 | 39 | 0 % |

No errors, no non-2xx, every read endpoint p99 below 90 ms at 50 concurrent connections. The pre-T75 build at 50
connections was within the same range (see `results/pre-t75-50conn.json`; e.g. admin users p99 32 ms vs 87 ms).
Login is bound by BCrypt, about 30 logins per second on this machine, which is intended.

## Caveats
- Same-machine client and server, one run per configuration, no repeats: differences below ~30 % are noise.
- Small dataset (seeded dev data plus 60-100 load students). Not production volume; not tested with thousands of
  students, so scaling behaviour with data size is **unmeasured**.
- LocalDB, not the Docker SQL Server container; no network between client and API; Release build, no TLS.
- Warm cache: students request the same few tables repeatedly. Write paths other than the seat race (grading, attendance,
  payments) and the AI service are not load tested.
- Earlier exploratory runs (a previous session, `before/after.json`) showed numbers about 10x slower (p50 30-100 ms) and
  an apparent T75 gain; they could not be reproduced under a controlled Release build and were discarded.
- A requirement target (e.g. NFR response time) is not stated in the repository, so this report gives measurements
  and does not claim pass/fail against one.
