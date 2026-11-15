import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, within } from '@testing-library/react'
import { server } from '@/test/server'
import { profileFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'
import type { Schedule, ScheduleItem } from '../types'

// PB-06 / T22: the student's weekly schedule in day tabs. Simulated data (SR-23).

const item = (meetingId: number, courseCode: string, dayOfWeek: number, extra: Partial<ScheduleItem> = {}): ScheduleItem => ({
  sectionId: meetingId, meetingId, courseCode, courseName: `${courseCode} course`, sectionCode: 'A', dayOfWeek,
  startTime: '08:00:00', endTime: '09:15:00', roomName: 'B-101', instructorName: 'Demo Instructor', type: 'Lecture',
  ...extra,
})

const scheduleOf = (items: ScheduleItem[], extra: Partial<Schedule> = {}): Schedule => ({
  termId: 1, termName: 'Fall 2026', items, ...extra,
})

function mockSchedule(body: Schedule) {
  server.use(
    http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))),
    http.get('/api/schedule/my', () => HttpResponse.json(body)),
  )
}

const week = [
  item(1, 'CSC 101', 1),
  item(2, 'MAT 201', 3, { startTime: '11:00:00', endTime: '12:15:00', roomName: 'C-204' }),
  item(3, 'CSC 210', 3, { startTime: '09:30:00', endTime: '10:45:00', instructorName: null }),
]

describe('SchedulePage', () => {
  beforeEach(() => {
    // Wednesday 18 Nov 2026. Only Date is faked so MSW and user-event timers keep running.
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(2026, 10, 18, 10, 0))
  })
  afterEach(() => vi.useRealTimers())

  it("opens on today's tab with its classes in time order", async () => {
    mockSchedule(scheduleOf(week))
    renderAt('/student/schedule', { as: 'Student' })

    const list = await screen.findByRole('list', { name: 'Classes on Wednesday' })
    expect(screen.getByRole('tab', { name: 'Wednesday' })).toHaveAttribute('aria-selected', 'true')
    expect(screen.getByText('Fall 2026')).toBeInTheDocument()

    const rows = within(list).getAllByRole('listitem').filter((li) => li.parentElement === list)
    expect(rows).toHaveLength(2)
    expect(within(rows[0]!).getByText('CSC 210 A')).toBeInTheDocument()
    expect(within(rows[0]!).getByText('Instructor to be announced')).toBeInTheDocument()
    expect(within(rows[1]!).getByText('MAT 201 A')).toBeInTheDocument()
    expect(within(rows[1]!).getByText('MAT 201 course')).toBeInTheDocument()
    expect(within(rows[1]!).getByText('11:00–12:15')).toBeInTheDocument()
    expect(within(rows[1]!).getByText('C-204')).toBeInTheDocument()
    expect(within(rows[1]!).getByText('Demo Instructor')).toBeInTheDocument()
    expect(within(rows[1]!).getByText('Lecture')).toBeInTheDocument()
    // Read-only: no attendance or grade links for students, and the time shows once.
    expect(within(list).queryByRole('link')).not.toBeInTheDocument()
    expect(within(rows[1]!).getAllByText('11:00–12:15')).toHaveLength(1)
  })

  it('switches days and shows an empty day', async () => {
    mockSchedule(scheduleOf(week))
    const { user } = renderAt('/student/schedule', { as: 'Student' })
    await screen.findByRole('list', { name: 'Classes on Wednesday' })

    await user.click(screen.getByRole('tab', { name: 'Monday' }))
    const monday = screen.getByRole('list', { name: 'Classes on Monday' })
    expect(within(monday).getByText('CSC 101 A')).toBeInTheDocument()
    expect(screen.queryByText('MAT 201 A')).not.toBeInTheDocument()

    await user.click(screen.getByRole('tab', { name: 'Tuesday' }))
    expect(screen.getByText('No classes on Tuesday')).toBeInTheDocument()
  })

  it('opens on Monday on a Sunday', async () => {
    vi.setSystemTime(new Date(2026, 10, 15, 10, 0))
    mockSchedule(scheduleOf(week))
    renderAt('/student/schedule', { as: 'Student' })

    expect(await screen.findByRole('list', { name: 'Classes on Monday' })).toBeInTheDocument()
    expect(screen.getByRole('tab', { name: 'Monday' })).toHaveAttribute('aria-selected', 'true')
  })

  it('shows an empty week with a link to the catalog', async () => {
    mockSchedule(scheduleOf([]))
    renderAt('/student/schedule', { as: 'Student' })

    expect(await screen.findByText('You have no classes this term')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'course catalog' })).toHaveAttribute('href', '/student/courses')
    expect(screen.queryByRole('tablist')).not.toBeInTheDocument()
  })

  it('says when there is no current term', async () => {
    mockSchedule(scheduleOf([], { termId: null, termName: null }))
    renderAt('/student/schedule', { as: 'Student' })
    expect(await screen.findByText('No current term')).toBeInTheDocument()
  })

  it('shows the error with a retry button', async () => {
    let fail = true
    server.use(
      http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))),
      http.get('/api/schedule/my', () =>
        fail
          ? HttpResponse.json({ title: 'Could not load your schedule.' }, { status: 500 })
          : HttpResponse.json(scheduleOf(week)),
      ),
    )
    const { user } = renderAt('/student/schedule', { as: 'Student' })

    expect(await screen.findByText('Something went wrong on our side. Please try again in a moment.')).toBeInTheDocument()
    fail = false
    await user.click(screen.getByRole('button', { name: 'Try again' }))
    expect(await screen.findByRole('list', { name: 'Classes on Wednesday' })).toBeInTheDocument()
  })
})
