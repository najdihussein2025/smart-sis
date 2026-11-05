import { setupServer } from 'msw/node'

/**
 * Mock API for tests. Each test adds the handlers it needs with `server.use(...)`;
 * any request without a handler fails the test (see setup.ts), so a typo in a URL can't pass silently.
 */
export const server = setupServer()
