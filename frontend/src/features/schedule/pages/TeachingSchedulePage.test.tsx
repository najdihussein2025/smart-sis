import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, within } from '@testing-library/react'
import { server } from '@/test/server'
import { profileFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'
import type { Schedule, ScheduleItem } from '../types'

// PB-06 / T22: the instructor's teaching schedule, same day tabs, Lecture / Lab labels. Simulated data (SR-23).

const item = (meetingId: number, dayOfWeek: number, extra: Partial<ScheduleItem> = {}): ScheduleItem => ({
  sectionId: 11, meetingId, courseCode: 'CSC 101', courseName: 'Intro to Programming', sectionCode: 'A', dayOfWeek,
  startTime: '08:00:00', endTime: '09:15:00', roomName: 'B-101', instructorName: null, type: 'Lecture', ...extra,
})

function mockTeaching(body: Schedule | 'error') {
  server.use(
    http.get('/api/profile', () => HttpResponse.json(profileFor('Instructor'))),
    http.get('/api/schedule/teaching', () =>
      body === 'error'
        ? HttpResponse.json({ title: 'Could not load your schedule.' }, { status: 500 })
        : HttpResponse.json(body),
    ),
  )
}

describe('TeachingSchedulePage', () => {
  beforeEach(() => {
    // Thursday 19 Nov 2026. Only Date is faked so MSW and user-event timers keep running.
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(2026, 10, 19, 10, 0))
  })
  afterEach(() => vi.useRealTimers())

  it('shows lectures and labs for the day without an instructor line', async () => {
    mockTeaching({
      termId: 1, termName: 'Fall 2026',
      items: [item(1, 4), item(2, 4, { startTime: '14:00:00', endTime: '16:00:00', roomName: 'Lab-3', type: 'Lab' })],
    })
    renderAt('/instructor/schedule', { as: 'Instructor' })

    const list = await screen.findByRole('list', { name: 'Classes on Thursday' })
    expect(screen.getByRole('heading', { name: 'Teaching schedule' })).toBeInTheDocument()
    expect(within(list).getByText('Lecture')).toBeInTheDocument()
    expect(within(list).getByText('Lab')).toBeInTheDocument()
    expect(within(list).getByText('14:00–16:00')).toBeInTheDocument()
    expect(within(list).getByText('Lab-3')).toBeInTheDocument()
    expect(within(list).queryByText('Instructor to be announced')).not.toBeInTheDocument()
  })

  it('shows an empty day', async () => {
    mockTeaching({ termId: 1, termName: 'Fall 2026', items: [item(1, 1)] })
    renderAt('/instructor/schedule', { as: 'Instructor' })
    expect(await screen.findByText('No classes on Thursday')).toBeInTheDocument()
  })

  it('shows an empty week', async () => {
    mockTeaching({ termId: 1, termName: 'Fall 2026', items: [] })
    renderAt('/instructor/schedule', { as: 'Instructor' })
    expect(await screen.findByText('You are not teaching this term')).toBeInTheDocument()
  })

  it('shows the error with a retry button', async () => {
    mockTeaching('error')
    renderAt('/instructor/schedule', { as: 'Instructor' })
    expect(await screen.findByText('Could not load your schedule.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })
})
