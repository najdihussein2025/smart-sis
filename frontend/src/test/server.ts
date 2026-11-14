import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import { dashboards, paged } from './fixtures'

/**
 * Requests every signed-in screen makes (the bell in RoleShell) and the dashboard numbers (every sign-in lands there).
 * They stay after `server.resetHandlers()`, so tests of other pages don't need to mock them; a test overrides them with
 * `server.use(...)` when it cares.
 */
export const defaultHandlers = [
  http.get('/api/notifications/unread-count', () => HttpResponse.json({ count: 0 })),
  http.get('/api/notifications', () => HttpResponse.json(paged([]))),
  http.get('/api/dashboard/student', () => HttpResponse.json(dashboards.student)),
  http.get('/api/dashboard/instructor', () => HttpResponse.json(dashboards.instructor)),
  http.get('/api/dashboard/admin', () => HttpResponse.json(dashboards.admin)),
]

/**
 * Mock API for tests. Each test adds the handlers it needs with `server.use(...)`;
 * any request without a handler fails the test (see setup.ts), so a typo in a URL can't pass silently.
 */
export const server = setupServer(...defaultHandlers)
