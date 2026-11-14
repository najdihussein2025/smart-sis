import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, within } from '@testing-library/react'
import { server } from '@/test/server'
import { profileFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'
import type { CourseAttendance, MyAttendance } from '../types'

// PB-11 / T44, T45: the student's absences per course with the colour rule. Simulated data (SR-23).

const course = (code: string, absences: number, extra: Partial<CourseAttendance> = {}): CourseAttendance => ({
  sectionId: code.length + absences, courseCode: code, courseName: `${code} course`, sectionCode: 'A', credits: 3,
  absences, limit: 9, remaining: Math.max(0, 9 - absences),
  standing: absences === 0 ? 'Perfect' : absences >= 8 ? 'Critical' : 'Warning',
  enrollmentStatus: 'Enrolled', sessionsRecorded: 10, absenceDates: [], ...extra,
})

const mineOf = (courses: CourseAttendance[]): MyAttendance => ({ termId: 1, termName: 'Fall 2026', courses })

function mock(body: MyAttendance | Response) {
  server.use(
    http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))),
    http.get('/api/attendance/my', () => (body instanceof Response ? body : HttpResponse.json(body))),
  )
}

const card = (label: string) => screen.getByRole('heading', { name: label }).closest('li')!

describe('StudentAttendancePage', () => {
  it('shows each course with its colour status in words', async () => {
    mock(mineOf([
      course('CSC 210', 0),
      course('MAT 201', 3, {
        absenceDates: [
          { date: '2026-11-04', dayOfWeek: 3, startTime: '10:00:00', type: 'Lecture' },
          { date: '2026-10-26', dayOfWeek: 1, startTime: '10:00:00', type: 'Lecture' },
        ],
      }),
      course('CSC 310', 8, { sectionId: 99 }),
    ]))
    renderAt('/student/attendance', { as: 'Student' })

    await screen.findByRole('list', { name: 'Attendance per course' })
    expect(within(card('CSC 210 A')).getByText('Perfect')).toBeInTheDocument()
    expect(within(card('CSC 210 A')).getByText('No absences in 10 recorded classes.')).toBeInTheDocument()
    expect(within(card('MAT 201 A')).getByText('Warning')).toBeInTheDocument()
    expect(within(card('MAT 201 A')).getByText('6 absences left before the limit.')).toBeInTheDocument()
    expect(within(card('CSC 310 A')).getByText('Critical')).toBeInTheDocument()
    expect(within(card('CSC 310 A')).getByText('1 absence left before the limit.')).toBeInTheDocument()
    expect(screen.getByText('Fall 2026')).toBeInTheDocument()
  })

  it('drills down to the dates missed', async () => {
    mock(mineOf([
      course('MAT 201', 2, {
        absenceDates: [
          { date: '2026-11-04', dayOfWeek: 3, startTime: '10:00:00', type: 'Lecture' },
          { date: '2026-10-26', dayOfWeek: 1, startTime: '10:00:00', type: 'Lab' },
        ],
      }),
    ]))
    const { user } = renderAt('/student/attendance', { as: 'Student' })

    const toggle = await screen.findByRole('button', { name: 'Show absence dates for MAT 201 A' })
    expect(toggle).toHaveAttribute('aria-expanded', 'false')
    await user.click(toggle)

    const dates = screen.getByRole('list', { name: 'Absence dates for MAT 201 A' })
    const items = within(dates).getAllByRole('listitem')
    expect(items).toHaveLength(2)
    expect(items[0]).toHaveTextContent('Wed 04 Nov 2026')
    expect(items[1]).toHaveTextContent('Mon 26 Oct 2026')
    expect(items[1]).toHaveTextContent('10:00 · Lab')

    await user.click(screen.getByRole('button', { name: 'Hide absence dates for MAT 201 A' }))
    expect(screen.queryByRole('list', { name: 'Absence dates for MAT 201 A' })).not.toBeInTheDocument()
  })

  it('says "No absences" when opening a perfect course', async () => {
    mock(mineOf([course('CSC 210', 0)]))
    const { user } = renderAt('/student/attendance', { as: 'Student' })
    await user.click(await screen.findByRole('button', { name: 'Show absence dates for CSC 210 A' }))
    expect(screen.getByText('No absences.')).toBeInTheDocument()
  })

  it('marks a course dropped for absences', async () => {
    mock(mineOf([course('CSC 101', 10, { enrollmentStatus: 'DroppedAbsence', standing: 'Critical' })]))
    renderAt('/student/attendance', { as: 'Student' })
    expect(await screen.findByText('Dropped (F)')).toBeInTheDocument()
    expect(screen.getByText('You were dropped from this course for going over the absence limit.')).toBeInTheDocument()
  })

  it('shows the empty state with a link to the cart', async () => {
    mock(mineOf([]))
    renderAt('/student/attendance', { as: 'Student' })
    expect(await screen.findByText('No courses this term')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'cart' })).toHaveAttribute('href', '/student/cart')
  })

  it('shows the error with a retry button', async () => {
    mock(HttpResponse.json({ title: 'Could not load your attendance.' }, { status: 500 }))
    renderAt('/student/attendance', { as: 'Student' })
    expect(await screen.findByText('Something went wrong on our side. Please try again in a moment.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })
})
