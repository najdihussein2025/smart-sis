import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterAll, afterEach, beforeAll } from 'vitest'
import { server } from './server'

/**
 * jsdom replaces AbortController with its own, but Request is still Node's, and newer Node versions (24+) reject a
 * jsdom AbortSignal ("Expected signal to be an instance of AbortSignal"). React Router builds a Request with such a
 * signal on every navigation, so redirects silently never happen. Tests don't abort navigations, so when Node won't
 * take the signal we drop it.
 */
const NodeRequest = globalThis.Request
function nodeAcceptsSignal(signal: AbortSignal): boolean {
  try {
    new NodeRequest('http://localhost/', { signal })
    return true
  } catch {
    return false
  }
}
class TestRequest extends NodeRequest {
  constructor(input: RequestInfo | URL, init?: RequestInit) {
    if (init?.signal && !nodeAcceptsSignal(init.signal)) {
      const { signal: _dropped, ...rest } = init
      void _dropped
      super(input, rest)
    } else {
      super(input, init)
    }
  }
}
globalThis.Request = TestRequest

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
afterEach(() => {
  cleanup()
  server.resetHandlers()
  localStorage.clear()
})
afterAll(() => server.close())