import { describe, expect, it, vi } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, waitFor, within } from '@testing-library/react'
import { sessionStore } from '@/shared/auth/session'
import { server } from '@/test/server'
import { dashboards, paged, profileFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'

/** Simulated timetable rows (SR-23). 11 Nov 2026 is a Wednesday (dayOfWeek 3). */
const meeting = (over: Partial<Record<string, unknown>> = {}) => ({
  sectionId: 10, meetingId: 100, courseCode: 'CSC 210', courseName: 'Data Structures', sectionCode: 'A',
  dayOfWeek: 3, startTime: '08:00:00', endTime: '09:15:00', roomName: 'B-101', instructorName: 'Demo Instructor', type: 'Lecture',
  ...over,
})
const studentWeek = [
  meeting(),
  meeting({ sectionId: 11, meetingId: 101, courseCode: 'MAT 201', courseName: 'Linear Algebra', startTime: '13:00:00', endTime: '14:15:00', roomName: 'B-204' }),
  meeting({ sectionId: 12, meetingId: 102, courseCode: 'CSC 220', courseName: 'Databases', dayOfWeek: 4, startTime: '10:00:00', endTime: '11:15:00', roomName: 'Lab-3', type: 'Lab' }),
]
const schedule = (items: unknown[]) => ({ termId: 1, termName: 'Fall 2026', items })

/** Only Date is faked, so MSW, React Query and user-event keep their real timers. */
function at(local: Date) {
  vi.useFakeTimers({ toFake: ['Date'] })
  vi.setSystemTime(local)
}

const gradingSection = (over: Partial<Record<string, unknown>> = {}) => ({
  sectionId: 10, courseCode: 'CSC 210', courseName: 'Data Structures', sectionCode: 'A', termName: 'Fall 2026',
  studentCount: 20, componentCount: 3, totalWeight: 100, notGraded: 0, isFinalized: false, ...over,
})

// PB-01 / SR-3: each role sees its own dashboard. PB-22: the numbers come from /api/dashboard/<role> and link to their pages.
describe('student dashboard', () => {
  it('shows name, ID, major, the numbers and their links', async () => {
    server.use(http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))))
    renderAt('/student', { as: 'Student' })

    expect(await screen.findByRole('heading', { name: /, Demo$/ })).toBeInTheDocument()
    expect((await screen.findAllByText(/3001/)).length).toBeGreaterThan(0)
    expect(screen.getByText('Computer Science')).toBeInTheDocument()

    expect(await screen.findByRole('link', { name: /Cumulative GPA: 3\.25/ })).toHaveAttribute('href', '/student/grades')
    expect(screen.getByRole('link', { name: /Credits this term: 9 \/ 18\. 9 more credits allowed/ })).toHaveAttribute('href', '/student/cart')
    expect(screen.getByRole('link', { name: /Outstanding balance: \$1,500\.00\. Next due 01 Dec 2026/ })).toHaveAttribute('href', '/student/finance')
    expect(screen.getByRole('link', { name: /Attendance warnings: 1\. Some absences/ })).toHaveAttribute('href', '/student/attendance')
    expect(document.title).toBe('Dashboard · Smart SIS')
  })

  it('lists what needs attention, each linking to its page (unread notifications → announcements)', async () => {
    server.use(
      http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))),
      http.get('/api/cart', () =>
        HttpResponse.json({
          termId: 1, termName: 'Fall 2026', window: 'AddDrop', cartCredits: 3, enrolledCredits: 9, maxCredits: 18,
          items: [{ sectionId: 12, courseId: 3, courseCode: 'CSC 220', courseName: 'Databases', credits: 3, sectionCode: 'A',
            instructorName: null, capacity: 30, seatsLeft: 5, meetings: [], addedAt: '2026-09-02T08:00:00Z' }],
        }),
      ),
    )
    renderAt('/student', { as: 'Student' })

    expect(await screen.findByRole('link', { name: /Note: 3 unread notifications/ })).toHaveAttribute('href', '/student/announcements')
    expect(screen.getByRole('link', { name: /Soon: 1 section in your cart, not enrolled yet/ })).toHaveAttribute('href', '/student/cart')
    expect(screen.getByRole('link', { name: /Note: Payment due 01 Dec 2026/ })).toHaveAttribute('href', '/student/finance')
    expect(screen.getByRole('link', { name: /Soon: Absences in 1 course/ })).toHaveAttribute('href', '/student/attendance')
    expect(screen.getByText('Add/drop open')).toBeInTheDocument()
  })

  it('says when money is overdue and a course is close to the absence limit', async () => {
    server.use(
      http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))),
      http.get('/api/dashboard/student', () =>
        HttpResponse.json({ ...dashboards.student, cumulativeGpa: null, overdueBalance: 400, attendanceWarnings: 2, attendanceCritical: 1 }),
      ),
    )
    renderAt('/student', { as: 'Student' })

    expect(await screen.findByText('No finalized grades yet')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /Action needed: \$400\.00 is overdue/ })).toHaveAttribute('href', '/student/finance')
    expect(screen.getByRole('link', { name: /Action needed: 1 course close to the absence limit/ })).toBeInTheDocument()
  })

  it('shows an all-clear when nothing needs attention', async () => {
    server.use(
      http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))),
      http.get('/api/dashboard/student', () =>
        HttpResponse.json({ ...dashboards.student, outstandingBalance: 0, nextDueDate: null, unreadNotifications: 0, attendanceWarnings: 0 }),
      ),
    )
    renderAt('/student', { as: 'Student' })

    expect(await screen.findByText("Nothing needs your attention. You're all set.")).toBeInTheDocument()
  })

  it('shows the next class today with room, instructor and the rest of the day', async () => {
    at(new Date(2026, 10, 11, 7, 30))
    server.use(
      http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))),
      http.get('/api/schedule/my', () => HttpResponse.json(schedule(studentWeek))),
    )
    renderAt('/student', { as: 'Student' })

    expect(await screen.findByRole('heading', { name: 'CSC 210 · Data Structures' })).toBeInTheDocument()
    expect(screen.getByText('Next up today')).toBeInTheDocument()
    expect(screen.getByText('Today 08:00–09:15')).toBeInTheDocument()
    expect(screen.getByText('B-101')).toBeInTheDocument()
    expect(screen.getByText('Demo Instructor')).toBeInTheDocument()
    expect(screen.getByText('Later today')).toBeInTheDocument()
    expect(screen.getByText('MAT 201 · B-204')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Full schedule' })).toHaveAttribute('href', '/student/schedule')
  })

  it('says there are no more classes today and shows the next one', async () => {
    at(new Date(2026, 10, 11, 18, 0))
    server.use(
      http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))),
      http.get('/api/schedule/my', () => HttpResponse.json(schedule(studentWeek))),
    )
    renderAt('/student', { as: 'Student' })

    expect(await screen.findByText('No more classes today · next up')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'CSC 220 · Databases' })).toBeInTheDocument()
    expect(screen.getByText('Thursday 10:00–11:15')).toBeInTheDocument()
  })

  it('points to the catalog when the timetable is empty', async () => {
    server.use(http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))))
    renderAt('/student', { as: 'Student' })

    expect(await screen.findByRole('heading', { name: 'No classes on your timetable yet' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Browse courses' })).toHaveAttribute('href', '/student/courses')
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
    expect(screen.getByLabelText('Checking what needs attention')).toBeInTheDocument()
    expect(await screen.findByText(/Something went wrong/)).toBeInTheDocument()
    expect(screen.queryByText(/System\.Exception/)).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Try again' }))
    expect(await screen.findByRole('link', { name: /Cumulative GPA: 3\.25/ })).toBeInTheDocument()
  })

  it('student navigation has no admin links', async () => {
    server.use(http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))))
    renderAt('/student', { as: 'Student' })

    const nav = await screen.findByRole('navigation', { name: 'Student navigation' })
    expect(within(nav).queryByRole('link', { name: 'Users' })).not.toBeInTheDocument()
  })
})

describe('instructor dashboard', () => {
  it('shows department and teaching cards', async () => {
    server.use(http.get('/api/profile', () => HttpResponse.json(profileFor('Instructor'))))
    renderAt('/instructor', { as: 'Instructor' })

    expect(await screen.findByText('Computer Science')).toBeInTheDocument()
    expect(await screen.findByRole('link', { name: /Active sections: 5/ })).toHaveAttribute('href', '/instructor/schedule')
    expect(screen.getByRole('link', { name: /Missing scores: 12\. In 2 sections/ })).toHaveAttribute('href', '/instructor/grading')
    expect(screen.getByRole('link', { name: /Attendance to take: 1/ })).toHaveAttribute('href', '/instructor/attendance')
    expect(screen.getByRole('link', { name: /1 attendance session still to take/ })).toHaveAttribute('href', '/instructor/attendance')
  })

  it("lists today's classes with a direct link to the attendance sheet", async () => {
    server.use(
      http.get('/api/profile', () => HttpResponse.json(profileFor('Instructor'))),
      http.get('/api/schedule/teaching', () =>
        HttpResponse.json(schedule([meeting({ instructorName: null }), meeting({ sectionId: 12, meetingId: 102, courseCode: 'CSC 220', dayOfWeek: 4 })])),
      ),
    )
    renderAt('/instructor', { as: 'Instructor' })

    // The dashboard's server date is 11 Nov 2026, a Wednesday: only the Wednesday class is today.
    expect(await screen.findByRole('link', { name: 'Take attendance for CSC 210 A at 08:00' })).toHaveAttribute(
      'href', '/instructor/attendance?section=10&meeting=100&date=2026-11-11',
    )
    expect(screen.getByRole('link', { name: 'Grades for CSC 210 A' })).toHaveAttribute('href', '/instructor/grading')
    expect(screen.queryByRole('link', { name: /Take attendance for CSC 220/ })).not.toBeInTheDocument()
    expect(screen.getByText('11 Nov 2026 · 1 class')).toBeInTheDocument()
  })

  it("doesn't claim every score is entered when a section has no grade components", async () => {
    server.use(
      http.get('/api/profile', () => HttpResponse.json(profileFor('Instructor'))),
      http.get('/api/dashboard/instructor', () =>
        HttpResponse.json({ ...dashboards.instructor, missingScores: 0, sectionsWithMissingScores: 0, attendanceToTake: 0 }),
      ),
      http.get('/api/instructor/grading/sections', () =>
        HttpResponse.json([gradingSection({ componentCount: 0, totalWeight: 0 }), gradingSection({ sectionId: 11, sectionCode: 'B', totalWeight: 80 })]),
      ),
    )
    renderAt('/instructor', { as: 'Instructor' })

    expect(await screen.findByRole('link', { name: /Missing scores: 0\. 1 section with no grade components yet/ })).toBeInTheDocument()
    expect(screen.queryByText('Every score is entered')).not.toBeInTheDocument()
    expect(screen.getByRole('link', { name: /CSC 210 A has no grade components/ })).toHaveAttribute('href', '/instructor/grading')
    expect(screen.getByRole('link', { name: /CSC 210 B weights add up to 80 of 100/ })).toBeInTheDocument()
  })

  it('shows an all-clear and "every score is entered" when every section is complete', async () => {
    server.use(
      http.get('/api/profile', () => HttpResponse.json(profileFor('Instructor'))),
      http.get('/api/dashboard/instructor', () =>
        HttpResponse.json({ ...dashboards.instructor, missingScores: 0, sectionsWithMissingScores: 0, attendanceToTake: 0 }),
      ),
      http.get('/api/instructor/grading/sections', () => HttpResponse.json([gradingSection()])),
    )
    renderAt('/instructor', { as: 'Instructor' })

    expect(await screen.findByText('Attendance and scores are up to date.')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /Missing scores: 0\. Every score is entered/ })).toBeInTheDocument()
  })
})

describe('admin dashboard', () => {
  it('shows the admin cards and admin-only navigation', async () => {
    server.use(http.get('/api/profile', () => HttpResponse.json(profileFor('Admin'))))
    renderAt('/admin', { as: 'Admin' })

    expect(await screen.findByRole('link', { name: /Active students: 42/ })).toHaveAttribute('href', '/admin/users')
    expect(screen.getByRole('link', { name: /Active instructors: 6/ })).toHaveAttribute('href', '/admin/users')
    expect(screen.getByRole('link', { name: /Open support requests: 4\. 1 new/ })).toHaveAttribute('href', '/admin/support')
    expect(screen.getByRole('link', { name: /Unpaid charges: \$10,250\.00\. 7 charges pending/ })).toHaveAttribute('href', '/admin/payments')
    expect(screen.getByRole('link', { name: /Sections this term: 18/ })).toHaveAttribute('href', '/admin/sections')
    expect(await screen.findByRole('heading', { name: 'Fall 2026' })).toBeInTheDocument()
    const nav = screen.getByRole('navigation', { name: 'Admin navigation' })
    for (const item of ['Users', 'Terms', 'Courses', 'Sections']) {
      expect(within(nav).getByRole('link', { name: item })).toBeInTheDocument()
    }
  })

  it('flags a switched-off career quiz and links to the question bank', async () => {
    server.use(
      http.get('/api/profile', () => HttpResponse.json(profileFor('Admin'))),
      http.get('/api/admin/career/questions', () =>
        HttpResponse.json({ activeCount: 12, minQuestions: 20, maxQuestions: 30, questions: [] }),
      ),
    )
    renderAt('/admin', { as: 'Admin' })

    expect(await screen.findByRole('link', { name: /Action needed: Career quiz is switched off.*12 of 20 active questions needed/ })).toHaveAttribute(
      'href', '/admin/career',
    )
  })

  it('flags no current term, an empty knowledge base and unassigned support requests', async () => {
    const supportQueries: URLSearchParams[] = []
    server.use(
      http.get('/api/profile', () => HttpResponse.json(profileFor('Admin'))),
      http.get('/api/admin/terms', () => HttpResponse.json([])),
      http.get('/api/admin/knowledge', () => HttpResponse.json([])),
      http.get('/api/admin/support', ({ request }) => {
        supportQueries.push(new URL(request.url).searchParams)
        return HttpResponse.json(paged([], 1, 1, 3))
      }),
    )
    renderAt('/admin', { as: 'Admin' })

    expect(await screen.findByRole('link', { name: /No current term/ })).toHaveAttribute('href', '/admin/terms')
    expect(screen.getByRole('link', { name: /Knowledge base is empty/ })).toHaveAttribute('href', '/admin/knowledge')
    expect(screen.getByRole('link', { name: /3 new support requests with nobody assigned/ })).toHaveAttribute('href', '/admin/support')
    expect(supportQueries[0]?.get('assigned')).toBe('Unassigned')
    expect(supportQueries[0]?.get('pageSize')).toBe('1')
  })

  it('shows an all-clear when nothing is blocking', async () => {
    server.use(
      http.get('/api/profile', () => HttpResponse.json(profileFor('Admin'))),
      http.get('/api/dashboard/admin', () => HttpResponse.json({ ...dashboards.admin, unpaidCharges: 0, outstandingTotal: 0 })),
    )
    renderAt('/admin', { as: 'Admin' })

    expect(await screen.findByText('Nothing is blocking students or staff.')).toBeInTheDocument()
  })

  it('says a check failed instead of showing an all-clear', async () => {
    server.use(
      http.get('/api/profile', () => HttpResponse.json(profileFor('Admin'))),
      http.get('/api/dashboard/admin', () => HttpResponse.json({ ...dashboards.admin, unpaidCharges: 0 })),
      http.get('/api/admin/knowledge', () => HttpResponse.json({ title: 'Something went wrong.' }, { status: 500 })),
    )
    renderAt('/admin', { as: 'Admin' })

    expect(await screen.findByRole('link', { name: /Couldn't check the knowledge base/ })).toHaveAttribute('href', '/admin/knowledge')
    expect(screen.queryByText('Nothing is blocking students or staff.')).not.toBeInTheDocument()
  })
})

describe('session', () => {
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
