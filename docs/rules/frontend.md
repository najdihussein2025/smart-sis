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
- Destructive actions (drop, deactivate) go through `ConfirmDialog`.
- Status colours follow the business rules: attendance green/orange/red, charge Pending/Partial/Paid.
- Works at 360 px wide; sidebar collapses to a top bar on mobile.
- Accessible: labels on every input, buttons are `<button>`, focus visible, colour is never the only signal.

## Style
- Function components, named exports, no default exports except route lazies.
- `strict` TypeScript; no `any`. Use `unknown` and narrow.
- Dates from the API are UTC ISO strings; format with `shared/lib/format.ts`.
