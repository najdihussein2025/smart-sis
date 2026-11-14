import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import { paged } from './fixtures'

/**
 * Requests every signed-in screen makes (the bell in RoleShell). They stay after `server.resetHandlers()`, so tests of
 * other pages don't need to mock them; a test overrides them with `server.use(...)` when it cares.
 */
export const defaultHandlers = [
  http.get('/api/notifications/unread-count', () => HttpResponse.json({ count: 0 })),
  http.get('/api/notifications', () => HttpResponse.json(paged([]))),
]

/**
 * Mock API for tests. Each test adds the handlers it needs with `server.use(...)`;
 * any request without a handler fails the test (see setup.ts), so a typo in a URL can't pass silently.
 */
export const server = setupServer(...defaultHandlers)
