import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, waitFor } from '@testing-library/react'
import type { Role } from '@/shared/auth/session'
import { server } from '@/test/server'
import { profileFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'

// PB-01 / SR-2, SR-3: each role reaches only its own area. (The API enforces this too; the UI mirrors it.)
describe('role guard', () => {
  const useProfile = (role: Role) => server.use(http.get('/api/profile', () => HttpResponse.json(profileFor(role))))

  it('sends a signed-out visitor to the login page', async () => {
    const { router } = renderAt('/admin/users')

    await waitFor(() => expect(router.state.location.pathname).toBe('/login'))
    expect(router.state.location.state).toEqual({ from: '/admin/users' })
  })

  it.each([
    ['Student', '/admin', '/student'],
    ['Student', '/instructor', '/student'],
    ['Instructor', '/admin/users', '/instructor'],
    ['Instructor', '/student', '/instructor'],
    ['Admin', '/student/schedule', '/admin'],
    ['Admin', '/instructor', '/admin'],
  ] as const)('sends a %s who opens %s back to %s', async (role, path, home) => {
    useProfile(role)
    const { router } = renderAt(path, { as: role })

    await waitFor(() => expect(router.state.location.pathname).toBe(home))
  })

  it.each(['Student', 'Instructor', 'Admin'] as const)('lets a %s into their own area', async (role) => {
    useProfile(role)
    const home = `/${role.toLowerCase()}`
    const { router } = renderAt(home, { as: role })

    expect(await screen.findByRole('navigation', { name: `${role} navigation` })).toBeInTheDocument()
    expect(router.state.location.pathname).toBe(home)
  })

  it('sends a signed-in user from / to their dashboard', async () => {
    useProfile('Instructor')
    const { router } = renderAt('/', { as: 'Instructor' })

    await waitFor(() => expect(router.state.location.pathname).toBe('/instructor'))
  })
})
