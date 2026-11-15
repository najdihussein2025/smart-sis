import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, waitFor, within } from '@testing-library/react'
import { server } from '@/test/server'
import { profileFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'
import type { AttendanceSheet, SessionSummary, SubmitAttendance, SubmitAttendanceResult, TeachingSections } from '../types'

// PB-10 / T41: take attendance for a section the instructor teaches. Simulated data (SR-23).
// "Today" on the server is Wednesday 11/11/2026. CSC 101 A meets Monday (meeting 11) and Wednesday (meeting 12).

const teaching: TeachingSections = {
  termId: 1,
  termName: 'Fall 2026',
  today: '2026-11-11',
  sections: [
    {
      sectionId: 1, courseCode: 'CSC 101', courseName: 'Introduction to Programming', sectionCode: 'A', credits: 3, absenceLimit: 9,
      termStart: '2026-10-01', termEnd: '2027-01-29', enrolledCount: 3,
      meetings: [
        { id: 11, dayOfWeek: 1, startTime: '08:00:00', endTime: '09:15:00', roomName: 'B-101', type: 'Lecture' },
        { id: 12, dayOfWeek: 3, startTime: '08:00:00', endTime: '09:15:00', roomName: 'B-101', type: 'Lecture' },
      ],
    },
  ],
}

const students = [
  { enrollmentId: 101, universityId: 3004, fullName: 'Demo Student 4', status: null, absences: 0 },
  { enrollmentId: 102, universityId: 3005, fullName: 'Demo Student 5', status: null, absences: 7 },
  { enrollmentId: 103, universityId: 3006, fullName: 'Demo Student 6', status: null, absences: 2 },
]

const sheetFor = (date: string, extra: Partial<AttendanceSheet> = {}): AttendanceSheet => ({
  sectionId: 1, courseCode: 'CSC 101', sectionCode: 'A', meetingId: 11, dayOfWeek: 1, startTime: '08:00:00', type: 'Lecture',
  sessionDate: date, sessionId: null, submittedAt: null, absenceLimit: 9, students, ...extra,
})

function mockApi({ data = teaching, sessions = [] as SessionSummary[], sheet = sheetFor } = {}) {
  const requested: string[] = []
  server.use(
    http.get('/api/profile', () => HttpResponse.json(profileFor('Instructor'))),
    http.get('/api/attendance/teaching', () => HttpResponse.json(data)),
    http.get('/api/attendance/sections/:id/sessions', () => HttpResponse.json(sessions)),
    http.get('/api/attendance/sheet', ({ request }) => {
      const url = new URL(request.url)
      requested.push(`${url.searchParams.get('meetingId')}@${url.searchParams.get('date')}`)
      return HttpResponse.json(sheet(url.searchParams.get('date')!))
    }),
  )
  return requested
}

async function openMondaySession() {
  const view = renderAt('/instructor/attendance', { as: 'Instructor' })
  const { user } = view
  await user.selectOptions(await screen.findByLabelText('Course and section'), '1')
  await user.selectOptions(screen.getByLabelText('Session'), '11')
  return view
}

/** Submit asks first (attendance can't be changed afterwards); confirm in the dialog. */
async function submitAndConfirm(user: Awaited<ReturnType<typeof openMondaySession>>['user']) {
  await user.click(screen.getByRole('button', { name: 'Submit attendance' }))
  const dialog = screen.getByRole('dialog', { name: 'Submit attendance for CSC 101 A?' })
  await user.click(within(dialog).getByRole('button', { name: 'Submit' }))
  return dialog
}

describe('InstructorAttendancePage', () => {
  it('loads the roster for the latest Monday, everyone present by default, and submits the marks', async () => {
    const requested = mockApi()
    let sent: SubmitAttendance | null = null
    const result: SubmitAttendanceResult = {
      sessionId: 50, present: 2, absent: 1,
      alerts: [{ enrollmentId: 102, fullName: 'Demo Student 5', level: 'Warning', absences: 8, limit: 9 }],
    }
    server.use(
      http.post('/api/attendance/sessions', async ({ request }) => {
        sent = (await request.json()) as SubmitAttendance
        return HttpResponse.json(result, { status: 201 })
      }),
    )
    const { user } = await openMondaySession()

    expect(screen.getByLabelText('Date')).toHaveValue('09/11/2026')
    const list = await screen.findByRole('list', { name: 'Students of CSC 101 A' })
    expect(requested).toContain('11@2026-11-09')
    expect(within(list).getByText('Demo Student 5')).toBeInTheDocument()
    expect(within(list).getByText(/7 of 9 absences so far/)).toBeInTheDocument()
    const row5 = within(list).getByRole('group', { name: 'Attendance of Demo Student 5' })
    expect(within(row5).getByRole('radio', { name: 'Present' })).toBeChecked()

    await user.click(within(row5).getByRole('radio', { name: 'Absent' }))
    expect(screen.getByText(/2 present, 1 absent/)).toBeInTheDocument()
    expect(within(list).getByText('Absent today puts this student one away from the limit.')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Submit attendance' }))
    const dialog = screen.getByRole('dialog', { name: 'Submit attendance for CSC 101 A?' })
    expect(within(dialog).getByText(/2 present, 1 absent/)).toBeInTheDocument()
    expect(within(dialog).getByText('Demo Student 5').closest('li')).toHaveTextContent('Demo Student 5 gets a warning (8 of 9 absences)')
    await user.click(within(dialog).getByRole('button', { name: 'Submit' }))

    expect(await screen.findByText('Attendance saved for CSC 101 A on 09/11/2026: 2 present, 1 absent.')).toBeInTheDocument()
    expect(sent).toEqual({
      meetingId: 11,
      sessionDate: '2026-11-09',
      records: [
        { enrollmentId: 101, status: 'Present' },
        { enrollmentId: 102, status: 'Absent' },
        { enrollmentId: 103, status: 'Present' },
      ],
    })
    const alerts = screen.getByRole('region', { name: 'Absence alerts' })
    expect(within(alerts).getByText('Demo Student 5')).toBeInTheDocument()
    expect(within(alerts).getByText(/8 of 9 absences/)).toBeInTheDocument()
  })

  it('marks everyone absent at once', async () => {
    mockApi()
    let sent: SubmitAttendance | null = null
    server.use(
      http.post('/api/attendance/sessions', async ({ request }) => {
        sent = (await request.json()) as SubmitAttendance
        return HttpResponse.json({ sessionId: 51, present: 0, absent: 3, alerts: [] }, { status: 201 })
      }),
    )
    const { user } = await openMondaySession()
    await screen.findByRole('list', { name: 'Students of CSC 101 A' })

    await user.click(screen.getByRole('button', { name: 'Mark all absent' }))
    await submitAndConfirm(user)

    await waitFor(() => expect(sent?.records.every((r) => r.status === 'Absent')).toBe(true))
  })

  it('refuses a date that is not on the session’s weekday, without asking the server', async () => {
    const requested = mockApi()
    const { user } = await openMondaySession()
    await screen.findByRole('list', { name: 'Students of CSC 101 A' })

    const date = screen.getByLabelText('Date')
    await user.clear(date)
    await user.type(date, '10/11/2026')
    await user.tab()

    expect(await screen.findByText('This session is on Monday. Choose a Monday.')).toBeInTheDocument()
    expect(screen.queryByRole('list', { name: 'Students of CSC 101 A' })).not.toBeInTheDocument()
    expect(requested).not.toContain('11@2026-11-10')
  })

  it('shows the API’s 409 when attendance was already submitted for this session', async () => {
    mockApi()
    server.use(
      http.post('/api/attendance/sessions', () =>
        HttpResponse.json(
          { title: 'Attendance for CSC 101 A on 09/11/2026 at 08:00 was already submitted.', code: 'attendance_already_taken' },
          { status: 409 },
        )),
    )
    const { user } = await openMondaySession()
    await screen.findByRole('list', { name: 'Students of CSC 101 A' })

    const dialog = await submitAndConfirm(user)

    expect(await within(dialog).findByText('Attendance for CSC 101 A on 09/11/2026 at 08:00 was already submitted.')).toBeInTheDocument()
  })

  it('shows a session that was already taken read-only, and opens it from the history', async () => {
    const taken = sheetFor('2026-11-02', {
      sessionId: 40, submittedAt: '2026-11-02T09:20:00Z',
      students: students.map((s, i) => ({ ...s, status: i === 1 ? 'Absent' : 'Present' })),
    })
    mockApi({
      sessions: [{ sessionId: 40, meetingId: 11, sessionDate: '2026-11-02', dayOfWeek: 1, startTime: '08:00:00', type: 'Lecture', present: 2, absent: 1, submittedAt: '2026-11-02T09:20:00Z' }],
      sheet: (date) => (date === '2026-11-02' ? taken : sheetFor(date)),
    })
    const { user } = renderAt('/instructor/attendance', { as: 'Instructor' })
    await user.selectOptions(await screen.findByLabelText('Course and section'), '1')

    const history = await screen.findByRole('list', { name: 'Submitted sessions of CSC 101 A' })
    expect(within(history).getByText('1 absent', { exact: false })).toBeInTheDocument()
    await user.click(within(history).getByRole('button', { name: 'View 02/11/2026 at 08:00' }))

    expect(await screen.findByText(/already submitted/)).toBeInTheDocument()
    const list = screen.getByRole('list', { name: 'Students of CSC 101 A' })
    expect(within(list).getByText('Absent')).toBeInTheDocument()
    expect(within(list).queryByRole('radio')).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Submit attendance' })).not.toBeInTheDocument()
  })

  it('warns in the confirm, by name, about a student this submission drops from the course', async () => {
    mockApi({
      sheet: (date) => sheetFor(date, { students: [...students.slice(0, 2), { ...students[2]!, absences: 9 }] }),
    })
    const { user } = await openMondaySession()
    const list = await screen.findByRole('list', { name: 'Students of CSC 101 A' })
    const row6 = within(list).getByRole('group', { name: 'Attendance of Demo Student 6' })
    // A5 standing per row: 9 of 9 is Critical.
    expect(within(list).getAllByText('Critical').length).toBeGreaterThan(0)

    await user.click(within(row6).getByRole('radio', { name: 'Absent' }))
    expect(within(list).getByText('Absent today drops this student from the course (F).')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Submit attendance' }))

    const dialog = screen.getByRole('dialog', { name: 'Submit attendance for CSC 101 A?' })
    const warnings = within(dialog).getByRole('region', { name: 'Absence alerts this submission sends' })
    expect(within(warnings).getAllByRole('listitem')).toHaveLength(1)
    expect(within(warnings).getByRole('listitem')).toHaveTextContent('Demo Student 6 is dropped from the course with an F (10 of 9)')

    await user.click(within(dialog).getByRole('button', { name: 'Cancel' }))
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('asks before switching session when marks are not submitted', async () => {
    const requested = mockApi()
    const { user } = await openMondaySession()
    const list = await screen.findByRole('list', { name: 'Students of CSC 101 A' })
    await user.click(within(within(list).getByRole('group', { name: 'Attendance of Demo Student 4' })).getByRole('radio', { name: 'Absent' }))

    await user.selectOptions(screen.getByLabelText('Session'), '12')
    const dialog = screen.getByRole('dialog', { name: 'Discard unsaved attendance?' })
    await user.click(within(dialog).getByRole('button', { name: 'Cancel' }))

    expect(screen.getByLabelText('Session')).toHaveValue('11')
    expect(within(screen.getByRole('group', { name: 'Attendance of Demo Student 4' })).getByRole('radio', { name: 'Absent' })).toBeChecked()
    expect(requested).not.toContain('12@2026-11-11')

    await user.selectOptions(screen.getByLabelText('Session'), '12')
    await user.click(within(screen.getByRole('dialog', { name: 'Discard unsaved attendance?' })).getByRole('button', { name: 'Discard marks' }))
    expect(screen.getByLabelText('Session')).toHaveValue('12')
    await waitFor(() => expect(requested).toContain('12@2026-11-11'))
  })

  it('opens the session and date from the URL', async () => {
    const requested = mockApi()
    const { router } = renderAt('/instructor/attendance?section=1&meeting=11&date=2026-11-02', { as: 'Instructor' })

    expect(await screen.findByRole('list', { name: 'Students of CSC 101 A' })).toBeInTheDocument()
    expect(screen.getByLabelText('Session')).toHaveValue('11')
    expect(screen.getByLabelText('Date')).toHaveValue('02/11/2026')
    expect(requested).toEqual(['11@2026-11-02'])
    expect(router.state.location.search).toBe('?section=1&meeting=11&date=2026-11-02')
  })

  it('picks the only section, and defaults the date of a linked session to its latest one', async () => {
    const requested = mockApi()
    const { user, router } = renderAt('/instructor/attendance?meeting=12', { as: 'Instructor' })

    expect(await screen.findByLabelText('Course and section')).toHaveValue('1')
    expect(screen.getByLabelText('Session')).toHaveValue('12')
    expect(screen.getByLabelText('Date')).toHaveValue('11/11/2026')
    await waitFor(() => expect(requested).toContain('12@2026-11-11'))

    await user.selectOptions(screen.getByLabelText('Session'), '11')
    const search = new URLSearchParams(router.state.location.search)
    expect([search.get('section'), search.get('meeting'), search.get('date')]).toEqual(['1', '11', null])
  })

  it('says so when the section has no students', async () => {
    mockApi({ sheet: (date) => sheetFor(date, { students: [] }) })
    await openMondaySession()
    expect(await screen.findByText('No students enrolled')).toBeInTheDocument()
  })

  it('says so when the instructor teaches nothing this term', async () => {
    mockApi({ data: { ...teaching, sections: [] } })
    renderAt('/instructor/attendance', { as: 'Instructor' })
    expect(await screen.findByText('No sections this term')).toBeInTheDocument()
  })

  it('shows a load error with a retry button', async () => {
    server.use(
      http.get('/api/profile', () => HttpResponse.json(profileFor('Instructor'))),
      http.get('/api/attendance/teaching', () => HttpResponse.json({ title: 'Could not load your sections.' }, { status: 500 })),
    )
    renderAt('/instructor/attendance', { as: 'Instructor' })
    expect(await screen.findByText('Something went wrong on our side. Please try again in a moment.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })
})
