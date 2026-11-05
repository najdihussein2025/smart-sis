# Frontend rules (React + Vite + TypeScript)

## Structure
```
src/
├── app/          App.tsx, router.tsx, providers.tsx, RequireRole.tsx
├── layouts/      AuthLayout, StudentLayout, InstructorLayout, AdminLayout (share RoleShell)
├── shared/       api/ (client, generated types), ui/ (Button, Input, Card, StatCard, EmptyState…),
│                 auth/ (token store, useAuth), lib/ (format, cn)
└── features/<feature>/
    ├── pages/        route components, one per screen
    ├── components/   pieces used only by this feature
    ├── api.ts        plain functions calling the API client
    ├── hooks.ts      TanStack Query hooks wrapping api.ts
    └── types.ts      feature types (prefer the generated API types)
```
A feature never imports another feature's `components/`. If two features need it, move it to `shared/ui`.

## Data
- **All server data goes through TanStack Query.** No `useEffect` + `fetch`. Query keys: `['<feature>', ...params]`.
- After a mutation, invalidate the affected keys (`enroll` → `['cart']`, `['enrollment']`, `['schedule']`).
- API calls only through `shared/api/client.ts` (axios instance that adds the JWT and logs out on 401).
- API types come from `npm run gen:api` (openapi-typescript → `shared/api/schema.d.ts`). Don't hand-write
  DTO shapes that the backend already defines.
- Show backend errors from ProblemDetails (`title`/`detail`) — the 409 messages are written for users.

## Forms
- React Hook Form + Zod (`zodResolver`). One schema per form, next to the form.
- Validation in the browser is for UX only; the backend validates again.
- Field errors under the field; server error at the top of the form; clear it when the user types (login rule).

## Routing and roles
- `/login`, `/forgot-password`, `/reset-password` are public.
- `/student/*`, `/instructor/*`, `/admin/*` are wrapped in `<RequireRole role="...">`. Wrong role → redirect to
  the user's own dashboard.
- Hiding a button is not security. The API enforces roles; the UI just mirrors it.

## UI
- Tailwind only, using the design tokens in `src/index.css` (`bg-surface`, `text-ink`, `brand`, …). No
  hex colours in components.
- Every list screen handles three states: loading (skeleton), empty (`EmptyState` with a sentence that says
  what to do), error (message + retry).
- Wide tables sit in `TableScroll` (scrolls sideways inside the card, never the page). Loading uses `TableSkeleton`,
  paged lists `Pagination`, dropdowns `SelectField`.
- "Saved" messages use `useSavedMessage()` (`shared/lib`): they hide themselves after 5 s.
- Destructive actions (drop, deactivate) go through `ConfirmDialog` (`shared/ui`).
- Status colours follow the business rules: attendance green/orange/red, charge Pending/Partial/Paid.
- Works at 360 px wide; sidebar collapses to a top bar on mobile.
- Accessible: labels on every input, buttons are `<button>`, focus visible, colour is never the only signal.

## Style
- Function components, named exports, no default exports except route lazies.
- `strict` TypeScript; no `any`. Use `unknown` and narrow.
- Dates from the API are UTC ISO strings; format with `shared/lib/format.ts`.
- Date inputs use `DateField` / `DateTimeField` from `shared/ui` (DD/MM/YYYY, 24-hour times, `min`/`max` grey out
  days that aren't allowed). The field keeps a grey DD/MM/YYYY template that fills in as you type, refuses an
  impossible digit as it is typed (day 01–31, month 01–12, year 1900–2099) and has month/year dropdowns in the calendar. Never a native `type="date"` / `datetime-local`: those follow the computer's locale
  (MM/DD/YYYY on many machines).

## Tests (Vitest + React Testing Library + MSW)
- `npm test` runs once (CI), `npm run test:watch` while working. Test files sit next to the code: `LoginPage.test.tsx`.
- Render through `renderAt(path, { as: 'Admin' })` from `src/test/render.tsx`: it mounts the **real routes** with a fresh
  query cache, the real `AuthProvider` and a memory router, optionally signed in. Assert on what the user sees
  (`getByRole`, `getByLabelText`, `findByText`) and on `router.state.location.pathname` for redirects.
- Mock the API with MSW handlers per test: `server.use(http.get('/api/...', () => HttpResponse.json(...)))`.
  A request with no handler **fails the test**, so a wrong URL can't pass silently. Never mock axios or hooks directly.
- Test data comes from `src/test/fixtures.ts` (the same simulated users as DevSeeder). No real data (SR-23).
- Each page test covers: the happy path, a field error, a 409/400 from the API shown to the user, and the empty/error state.
