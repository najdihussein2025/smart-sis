import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, waitFor } from '@testing-library/react'
import type { Role } from '@/shared/auth/session'
import { sessionStore } from '@/shared/auth/session'
import { server } from '@/test/server'
import { profileFor, sessionFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'

// PB-01 / UC-01 / SD-01: log in and land on the role dashboard.
describe('LoginPage', () => {
  const fill = async (user: ReturnType<typeof renderAt>['user'], id: string, password: string) => {
    if (id) await user.type(screen.getByLabelText('University ID'), id)
    if (password) await user.type(screen.getByLabelText('Password'), password)
    await user.click(screen.getByRole('button', { name: 'Sign in' }))
  }

  it.each([
    ['Admin', 1001, '/admin'],
    ['Instructor', 2001, '/instructor'],
    ['Student', 3001, '/student'],
  ] as const)('signs in a %s (ID %i) and opens %s', async (role: Role, universityId, home) => {
    let sent: unknown
    server.use(
      http.post('/api/auth/login', async ({ request }) => {
        sent = await request.json()
        return HttpResponse.json(sessionFor(role))
      }),
      http.get('/api/profile', () => HttpResponse.json(profileFor(role))),
    )
    const { router, user } = renderAt('/login')

    await fill(user, String(universityId), 'Pass@1234')

    await waitFor(() => expect(router.state.location.pathname).toBe(home))
    expect(sent).toEqual({ universityId, password: 'Pass@1234' })
    expect(sessionStore.get()?.user.role).toBe(role)
  })

  it('returns to the page the user first asked for, inside their own area', async () => {
    server.use(
      http.post('/api/auth/login', () => HttpResponse.json(sessionFor('Admin'))),
      http.get('/api/profile', () => HttpResponse.json(profileFor('Admin'))),
    )
    const { router, user } = renderAt('/admin/terms')
    await waitFor(() => expect(router.state.location.pathname).toBe('/login'))

    await fill(user, '1001', 'Pass@1234')

    await waitFor(() => expect(router.state.location.pathname).toBe('/admin/terms'))
  })

  it('shows field errors and does not call the API when fields are empty or not numeric', async () => {
    const { user } = renderAt('/login')

    await fill(user, '', '')
    expect(await screen.findByText('Enter your University ID.')).toBeInTheDocument()
    expect(screen.getByText('Enter your password.')).toBeInTheDocument()

    await user.type(screen.getByLabelText('University ID'), 'abc')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))
    expect(await screen.findByText('University ID contains numbers only.')).toBeInTheDocument()
    // No handler is registered for /api/auth/login: a call would fail the test (onUnhandledRequest: 'error').
  })

  it('shows the server message on bad credentials and clears it when the user types', async () => {
    server.use(
      http.post('/api/auth/login', () =>
        HttpResponse.json({ status: 401, title: 'Invalid University ID or password.' }, { status: 401 }),
      ),
    )
    const { router, user } = renderAt('/login')

    await fill(user, '1001', 'wrong')

    expect(await screen.findByText('Invalid University ID or password.')).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/login')
    expect(sessionStore.get()).toBeNull()

    await user.type(screen.getByLabelText('Password'), 'x')
    expect(screen.queryByText('Invalid University ID or password.')).not.toBeInTheDocument()
  })

  it('says when the server cannot be reached', async () => {
    server.use(http.post('/api/auth/login', () => HttpResponse.error()))
    const { user } = renderAt('/login')

    await fill(user, '1001', 'Pass@1234')

    expect(await screen.findByText('Cannot reach the server. Check your connection.')).toBeInTheDocument()
  })

  it('skips the form when already signed in', async () => {
    server.use(http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))))
    const { router } = renderAt('/login', { as: 'Student' })

    await waitFor(() => expect(router.state.location.pathname).toBe('/student'))
  })
})
