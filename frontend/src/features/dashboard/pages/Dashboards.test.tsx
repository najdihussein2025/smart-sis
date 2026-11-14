import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, waitFor, within } from '@testing-library/react'
import { sessionStore } from '@/shared/auth/session'
import { server } from '@/test/server'
import { dashboards, profileFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'

// PB-01 / SR-3: each role sees its own dashboard. PB-22: the numbers come from /api/dashboard/<role> and link to their pages.
describe('role dashboards', () => {
  it('student dashboard shows name, ID, major and credit cap', async () => {
    server.use(http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))))
    renderAt('/student', { as: 'Student' })

    expect(await screen.findByRole('heading', { name: /, Demo$/ })).toBeInTheDocument()
    expect((await screen.findAllByText(/3001/)).length).toBeGreaterThan(0)
    expect(screen.getByText('Computer Science')).toBeInTheDocument()

    expect(await screen.findByRole('link', { name: /Cumulative GPA: 3\.25/ })).toHaveAttribute('href', '/student/grades')
    expect(screen.getByRole('link', { name: /Credits this term: 9 \/ 18\. 9 more credits allowed/ })).toHaveAttribute('href', '/student/cart')
    expect(screen.getByRole('link', { name: /Classes today: 2/ })).toHaveAttribute('href', '/student/schedule')
    expect(screen.getByRole('link', { name: /Outstanding balance: \$1,500\.00\. Next due 01 Dec 2026/ })).toHaveAttribute('href', '/student/finance')
    expect(screen.getByRole('link', { name: /Attendance warnings: 1\. Some absences/ })).toHaveAttribute('href', '/student/attendance')
    expect(screen.getByText('Unread notifications')).toBeInTheDocument()
    expect(document.title).toBe('Dashboard · Smart SIS')
  })

  it('student dashboard says when money is overdue and a course is close to the absence limit', async () => {
    server.use(
      http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))),
      http.get('/api/dashboard/student', () =>
        HttpResponse.json({ ...dashboards.student, cumulativeGpa: null, overdueBalance: 400, attendanceWarnings: 2, attendanceCritical: 1 }),
      ),
    )
    renderAt('/student', { as: 'Student' })

    expect(await screen.findByText('No finalized grades yet')).toBeInTheDocument()
    expect(screen.getByText('$400.00 is overdue')).toBeInTheDocument()
    expect(screen.getByText('1 course close to the absence limit')).toBeInTheDocument()
  })

  it('shows skeletons while loading, then a friendly error with retry', async () => {
    let calls = 0
    server.use(
      http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))),
      http.get('/api/dashboard/student', () => {
        calls++
        return calls === 1
          ? HttpResponse.json({ title: 'Something went wrong.', detail: 'System.Exception: boom' }, { status: 500 })
          : HttpResponse.json(dashboards.student)
      }),
    )
    const { user } = renderAt('/student', { as: 'Student' })

    expect(screen.getByLabelText('Loading your numbers')).toBeInTheDocument()
    expect(await screen.findByText(/Something went wrong/)).toBeInTheDocument()
    expect(screen.queryByText(/System\.Exception/)).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Try again' }))
    expect(await screen.findByRole('link', { name: /Cumulative GPA: 3\.25/ })).toBeInTheDocument()
  })

  it('instructor dashboard shows department and teaching cards', async () => {
    server.use(http.get('/api/profile', () => HttpResponse.json(profileFor('Instructor'))))
    renderAt('/instructor', { as: 'Instructor' })

    expect(await screen.findByText('Computer Science')).toBeInTheDocument()
    expect(await screen.findByRole('link', { name: /Active sections: 5/ })).toHaveAttribute('href', '/instructor/schedule')
    expect(screen.getByRole('link', { name: /Missing scores: 12\. In 2 sections/ })).toHaveAttribute('href', '/instructor/grading')
    expect(screen.getByRole('link', { name: /Attendance to take: 1/ })).toHaveAttribute('href', '/instructor/attendance')
  })

  it('admin dashboard shows the admin cards and admin-only navigation', async () => {
    server.use(http.get('/api/profile', () => HttpResponse.json(profileFor('Admin'))))
    renderAt('/admin', { as: 'Admin' })

    expect(await screen.findByRole('link', { name: /Active students: 42\. 6 active instructors/ })).toHaveAttribute('href', '/admin/users')
    expect(screen.getByRole('link', { name: /Open support requests: 4\. 1 new/ })).toHaveAttribute('href', '/admin/support')
    expect(screen.getByRole('link', { name: /Unpaid charges: \$10,250\.00\. 7 charges pending/ })).toHaveAttribute('href', '/admin/payments')
    expect(screen.getByRole('link', { name: /Sections this term: 18/ })).toHaveAttribute('href', '/admin/sections')
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
