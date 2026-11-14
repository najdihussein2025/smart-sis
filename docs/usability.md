# Usability check (PB-22: T77 AB#144, T78 AB#145)

Sprint 6 hardening pass over every screen of the web app. Decisions are recorded in `docs/decisions.md` → "PB-22 Usability".

## How it was checked
- **Code audit** of every route component under `frontend/src/features/**/pages` and the shared UI (`src/shared/ui`, `src/layouts`).
- **Automated sweep** (Playwright, against the real API on LocalDB with the dev seed): every route of every role
  (12 student, 6 instructor, 12 admin) at **360, 768, 1024 and 1440 px** = 120 page loads. For each it measured
  `scrollWidth − clientWidth` of the page (horizontal page scroll) and read `document.title`; at 360 px it saved a
  full-page screenshot that was reviewed by eye.
- **Keyboard script** at 360 px: open the mobile menu, Tab 20 times, Escape; open "New section" dialog, Tab 40 times, Escape.
- **Unit tests** (Vitest + RTL) for the behaviour that changed: `shared/ui/a11y.test.tsx` (dialog focus trap / Escape /
  focus return, tabs arrows + Home/End, mobile menu, skip link), `shared/api/problem.test.ts` (error sentences),
  `Dashboards.test.tsx` (cards, links, skeleton, error + retry).

## Pages: loading / empty / error
| Area | Pages | Loading | Empty (says what to do) | Error (message + retry) |
|---|---|---|---|---|
| Student | Dashboard, Schedule, Catalog, Course detail, Cart, Grades, Attendance, Finance, Announcements, Support (+ request), Assistant, Career | skeleton | `EmptyState` | `QueryError` |
| Instructor | Dashboard, Teaching schedule, Attendance, Grading, Announcements | skeleton | `EmptyState` | `QueryError` |
| Admin | Dashboard, Users, Terms, Courses, Sections, Grade components, Payments, Announcements, Support inbox (+ request), Knowledge base, Career | `TableSkeleton` | `EmptyState` | `QueryError` |
| All | Profile | **fixed**: was a bare spinner + ad-hoc alert → skeleton + `QueryError` | — | **fixed** |
| All | Dashboards | **new**: skeleton cards | n/a (numbers, 0 is a value; hints say "Nothing to pay", "Up to date this week"…) | **new** |

Lookups is still a "coming soon" placeholder (phase 2 route, out of scope).

## Error messages
`errorMessage()` is the only place errors become text (all pages, `QueryError`, form top errors):

| Situation | Shown |
|---|---|
| No response (offline, API down) | "Cannot reach the server. Check your connection and try again." |
| Timeout | "The server took too long to answer. Please try again." |
| Any 5xx (incl. proxy HTML pages) | "Something went wrong on our side. Please try again in a moment." (never the dev `detail` / stack) |
| 4xx with our ProblemDetails title (409 rules, 404, 403…) | the title, written for people ("Section is full") |
| 4xx with a framework title or empty body | a sentence per status (400 / 401 / 403 / 404 / 408 / 413 / 429) |

"Request failed with status code …" can no longer reach the screen.

## Accessibility checklist
| Check | Result |
|---|---|
| Every interactive element is a `button` / `a` / form control (no click-only `div`) | Pass. Only the two modal backdrops have `onClick`, and both have keyboard equivalents (Escape, close button). |
| Visible focus | Pass. Global `:focus-visible` outline in the new `focus` colour (3:1+ on every surface); form controls also switch to a brand border. |
| Dialogs trap focus, close on Esc, return focus | **Fixed**: `Dialog` only moved focus in and handled Esc; Tab could leave it. Now `useFocusTrap`. |
| Mobile menu (drawer) | **Fixed**: had no Esc, no focus move, no trap and the close control was the backdrop. Now a labelled modal (`role="dialog"`, "Menu") with a Close button, trap, Esc, focus return; the menu button has `aria-expanded` / `aria-controls`. |
| Tabs use arrow keys | Pass (already); **added** Home / End. |
| Skip link | **Added** "Skip to main content" → `<main id="main">`. |
| Labels on inputs | Pass. All shared fields have `<label>`; raw inputs (composer, score cells, file upload, prerequisite search, radios) have `label`/`aria-label`/`legend`. |
| Colour never the only signal | Badges always carry text. **Fixed**: attendance Present/Absent choice now also shows a tick. Dashboard tones always come with words. |
| `aria-live` for async results | Alerts are `alert`/`status`; assistant log and enroll results were already polite. **Added** `role="status"` to `TableSkeleton`, the dashboard skeleton and the pagination count (announces the new count after a search). |
| Page `<title>` per route | **Added** via `PageHeader` ("Grades · Smart SIS"), dashboards = "Dashboard", auth pages and 404. Sweep: all 30 signed-in routes have a title. |
| 404 page | **Fixed**: now a `<main>` with an `<h1>`. |

## Contrast (WCAG 2.1 AA: 4.5:1 text, 3:1 large text / UI)
| Pair | Before | After |
|---|---|---|
| `ink` on `surface` | 16.5 | 16.5 |
| `muted` on `surface` / `canvas` / `brand-50` | 5.35 / 4.91 / 4.73 | unchanged (pass) |
| `success` on `success-soft` (badges) | **4.37 fail** (#2f7d4f) | 5.57 (#276b43) |
| `warning` on `warning-soft` (badges) | **3.50 fail** (#b86e12) | 4.77 (#9a5b0c) |
| `danger` on `danger-soft` / white on `danger` | 4.98 / 6.03 | unchanged |
| Gold text on light (page eyebrows, assistant icon, schedule counts) `accent` on `canvas` | **2.44 fail** | 5.00 with new `accent-ink` (#8a6220); 4.66 on `accent-soft` |
| Assistant reply bubble text (`accent` on `accent-soft`) | **2.27 fail** | 14.05 (now `ink`) |
| `accent` on `brand` (sidebar area label) | 6.00 | unchanged |
| Sidebar inactive links (white/65 on `brand`) | 7.4 | 9.5 (white/75) |
| Auth footer (white/50 on `brand`) | 4.99 | 8.4 (white/70) |
| Input edge (`line` on `surface`) | **1.30 fail** (UI) | 3.44 with new `line-strong` (#8f8a7f) |
| Focus outline (`accent` on `canvas` / `surface`) | **2.44 / 2.66 fail** | 3.14 / 3.42 with new `focus` (#b08332); 4.67 on `brand` |
| Placeholders (`muted/70` on `surface`) | **~2.9 fail** | 5.35 (`muted`) |
| Links hover (`accent` on `surface`) | **2.66 fail** | `brand-600` + underline |

`line` stays for dividers and card edges (decorative, not required to meet 3:1).

## Responsive (T78)
| Width | Result |
|---|---|
| 360 px | No horizontal page scroll on any of the 30 routes. Sidebar → top bar ("Smart SIS" + menu button) + drawer (max 85 vw). Tables sit in `TableScroll`; admin lists switch to cards; forms stack; tab lists scroll sideways; dialogs fill the width with a 16 px margin, the body scrolls inside and the footer buttons wrap. |
| 768 px | No page scroll; two-column stat grids and forms. |
| 1024 px | No page scroll; sidebar visible from `lg`. |
| 1440 px | No page scroll; content capped at `max-w-6xl`. |

Fixes: the notifications dropdown was anchored to the bell and, at 360 px, ran ~30 px off the left edge → now a full-width
panel under the top bar below `sm`. `PageHeader` actions wrap instead of refusing to shrink; long titles break.
`StatCard` values break instead of overflowing. Dialog padding is 16 px on phones.

## Known gaps
- Lookups admin screen is still a placeholder (phase 2).
- No automated axe run in CI (no axe dependency added); the sweep script lives outside the repo.
- Dates in dashboard counts use the UTC day (same as attendance); between 00:00 and 02:00/03:00 Beirut time "classes today"
  still shows the previous day.
- The schedule day tabs don't scroll the selected tab into view on first render at 360 px (Thu–Sat start off-screen; arrows
  and swipe reach them).
- Screen-reader testing was done by reading roles/names in tests, not with NVDA/VoiceOver.
