import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, waitFor, within } from '@testing-library/react'
import { sessionStore } from '@/shared/auth/session'
import { server } from '@/test/server'
import { profileFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'

// PB-01 / SR-3: each role sees its own dashboard. Stat numbers arrive with /api/dashboard (phase 3) and show "—" until then.
describe('role dashboards', () => {
  it('student dashboard shows name, ID, major and credit cap', async () => {
    server.use(http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))))
    renderAt('/student', { as: 'Student' })

    expect(await screen.findByRole('heading', { name: /, Demo$/ })).toBeInTheDocument()
    expect((await screen.findAllByText(/3001/)).length).toBeGreaterThan(0)
    expect(screen.getByText('Computer Science')).toBeInTheDocument()
    expect(screen.getByText('Cap 18')).toBeInTheDocument()
    expect(screen.getByText('Cumulative GPA')).toBeInTheDocument()
  })

  it('instructor dashboard shows department and teaching cards', async () => {
    server.use(http.get('/api/profile', () => HttpResponse.json(profileFor('Instructor'))))
    renderAt('/instructor', { as: 'Instructor' })

    expect(await screen.findByText('Computer Science')).toBeInTheDocument()
    expect(screen.getByText('Active courses')).toBeInTheDocument()
    expect(screen.getByText('Ungraded components')).toBeInTheDocument()
  })

  it('admin dashboard shows the admin cards and admin-only navigation', async () => {
    server.use(http.get('/api/profile', () => HttpResponse.json(profileFor('Admin'))))
    renderAt('/admin', { as: 'Admin' })

    expect(await screen.findByText('Active students')).toBeInTheDocument()
    const nav = screen.getByRole('navigation', { name: 'Admin navigation' })
    for (const item of ['Users', 'Terms', 'Courses', 'Sections']) {
      expect(within(nav).getByRole('link', { name: item })).toBeInTheDocument()
    }
  })

  it('student navigation has no admin links', async () => {
    server.use(http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))))
    renderAt('/student', { as: 'Student' })

    const nav = await screen.findByRole('navigation', { name: 'Student navigation' })
    expect(within(nav).queryByRole('link', { name: 'Users' })).not.toBeInTheDocument()
  })

  it('signs out when the API rejects the token', async () => {
    server.use(http.get('/api/profile', () => HttpResponse.json({ title: 'Unauthorized' }, { status: 401 })))
    const { router } = renderAt('/admin', { as: 'Admin' })

    await waitFor(() => expect(router.state.location.pathname).toBe('/login'))
    expect(sessionStore.get()).toBeNull()
  })

  it('sign out clears the session and returns to login', async () => {
    server.use(http.get('/api/profile', () => HttpResponse.json(profileFor('Admin'))))
    const { router, user } = renderAt('/admin', { as: 'Admin' })

    await user.click(await screen.findByRole('button', { name: 'Sign out' }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/login'))
    expect(sessionStore.get()).toBeNull()
  })
})
