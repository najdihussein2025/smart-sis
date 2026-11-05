import { render } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { createMemoryRouter, RouterProvider, type RouteObject } from 'react-router'
import { AuthProvider } from '@/shared/auth/AuthProvider'
import { sessionStore, type Role } from '@/shared/auth/session'
import { routes as appRoutes } from '@/app/router'
import { sessionFor } from './fixtures'

interface Options {
  /** Signed in as this role before the first render. Omit for a signed-out visitor. */
  as?: Role
  /** Defaults to the app's real routes. */
  routes?: RouteObject[]
}

/**
 * Renders the app (or the given routes) at `path` with a fresh query cache, the real AuthProvider and a memory router.
 * Returns the router so tests can assert on `router.state.location.pathname`.
 */
export function renderAt(path: string, { as, routes = appRoutes }: Options = {}) {
  if (as) sessionStore.set(sessionFor(as))

  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false, refetchOnWindowFocus: false }, mutations: { retry: false } },
  })
  const router = createMemoryRouter(routes, { initialEntries: [path] })
  const user = userEvent.setup()

  const result = render(
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <RouterProvider router={router} />
      </AuthProvider>
    </QueryClientProvider>,
  )
  return { ...result, router, user, queryClient }
}
