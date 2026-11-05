import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, waitFor, within } from '@testing-library/react'
import type { UserEvent } from '@testing-library/user-event'
import { server } from '@/test/server'
import { lookups, paged, profileFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'
import type { Term } from '@/features/terms/types'
import type { SaveSectionRequest, Section } from '../types'

// PB-04 / T08: admin lists, creates and edits sections with weekly meetings. Simulated data (SR-23).

const term = (id: number, name: string, isCurrent: boolean): Term => ({
  id, name, isCurrent, startDate: '2026-09-01', endDate: '2026-12-20',
  enrollStart: '2026-08-15T08:00:00Z', enrollEnd: '2026-08-30T20:00:00Z',
  addDropStart: '2026-09-01T08:00:00Z', addDropEnd: '2026-09-10T20:00:00Z', currentWindow: 'Closed',
})
const fall = term(1, 'Fall 2026', true)
const spring = term(2, 'Spring 2027', false)

const courseList = [
  { id: 1, code: 'CSC 101', name: 'Introduction to Programming', credits: 3, departmentCode: 'CSC', departmentName: 'Computer Science' },
  { id: 4, code: 'MAT 201', name: 'Discrete Mathematics', credits: 3, departmentCode: 'MAT', departmentName: 'Mathematics' },
]

const csc101A: Section = {
  id: 1, courseId: 1, courseCode: 'CSC 101', courseName: 'Introduction to Programming', termId: 1, termName: 'Fall 2026',
  sectionCode: 'A', instructorId: 2, instructorName: 'Demo Instructor', capacity: 40, seatsTaken: 12,
  meetings: [
    { id: 11, dayOfWeek: 1, startTime: '08:00:00', endTime: '09:15:00', roomId: 1, roomName: 'B-101', type: 'Lecture' },
    { id: 12, dayOfWeek: 3, startTime: '10:00:00', endTime: '12:00:00', roomId: 3, roomName: 'Lab-3', type: 'Lab' },
  ],
}
const mat201B: Section = {
  ...csc101A, id: 2, courseId: 4, courseCode: 'MAT 201', courseName: 'Discrete Mathematics', sectionCode: 'B',
  instructorId: null, instructorName: null, capacity: 25, seatsTaken: 25,
  meetings: [{ id: 21, dayOfWeek: 2, startTime: '13:00:00', endTime: '14:15:00', roomId: 2, roomName: 'B-204', type: 'Lecture' }],
}

type Sent = { method: string; id?: string; body: SaveSectionRequest }

function mockSections(sections: Section[] = [csc101A, mat201B]) {
  const sent: Sent[] = []
  const queries: URLSearchParams[] = []
  server.use(
    http.get('/api/profile', () => HttpResponse.json(profileFor('Admin'))),
    http.get('/api/admin/lookups', () => HttpResponse.json(lookups)),
    http.get('/api/admin/terms', () => HttpResponse.json([spring, fall])),
    http.get('/api/admin/courses', () => HttpResponse.json(paged(courseList, 1, 100))),
    http.get('/api/admin/sections', ({ request }) => {
      const q = new URL(request.url).searchParams
      queries.push(q)
      const hits = sections.filter(
        (s) => (!q.get('termId') || s.termId === Number(q.get('termId'))) && (!q.get('courseId') || s.courseId === Number(q.get('courseId'))),
      )
      return HttpResponse.json(paged(hits))
    }),
    http.post('/api/admin/sections', async ({ request }) => {
      const body = (await request.json()) as SaveSectionRequest
      sent.push({ method: 'POST', body })
      return HttpResponse.json({ ...mat201B, id: 9, sectionCode: body.sectionCode.toUpperCase() }, { status: 201 })
    }),
    http.put('/api/admin/sections/:id', async ({ request, params }) => {
      const body = (await request.json()) as SaveSectionRequest
      sent.push({ method: 'PUT', id: String(params.id), body })
      return HttpResponse.json(csc101A)
    }),
  )
  return { sent, queries }
}

async function openNew(user: UserEvent) {
  await user.click((await screen.findAllByRole('button', { name: /New section/ }))[0]!)
  const dialog = screen.getByRole('dialog', { name: 'New section' })
  await waitFor(() => expect(within(dialog).getByLabelText('Course')).toBeEnabled())
  return dialog
}

const meeting = (dialog: HTMLElement, n: number) => within(within(dialog).getByRole('group', { name: `Meeting ${n}` }))

async function fillNewSection(user: UserEvent, dialog: HTMLElement) {
  await user.selectOptions(within(dialog).getByLabelText('Course'), 'MAT 201 · Discrete Mathematics')
  await user.type(within(dialog).getByLabelText('Section code'), 'c')
  await user.selectOptions(within(dialog).getByLabelText('Instructor'), 'Demo Instructor (2001)')
  await user.clear(within(dialog).getByLabelText('Capacity'))
  await user.type(within(dialog).getByLabelText('Capacity'), '35')
  const m1 = meeting(dialog, 1)
  await user.selectOptions(m1.getByLabelText('Day'), 'Saturday')
  await user.selectOptions(m1.getByLabelText('Starts'), '09:00')
  await user.selectOptions(m1.getByLabelText('Ends'), '10:15')
  await user.selectOptions(m1.getByLabelText('Room'), 'B-101 · 40 seats')
}

describe('SectionsPage', () => {
  it('lists the current term\'s sections with instructor, seats and meetings', async () => {
    const { queries } = mockSections()
    renderAt('/admin/sections', { as: 'Admin' })

    const row = (await screen.findByRole('rowheader', { name: /CSC 101 · A/ })).closest('tr')!
    expect(within(row).getByText('Demo Instructor')).toBeInTheDocument()
    expect(within(row).getByText('12 / 40')).toBeInTheDocument()
    const meetings = within(row).getByRole('list', { name: 'Meetings of CSC 101 A' })
    expect(within(meetings).getAllByRole('listitem').map((li) => li.textContent)).toEqual([
      'Mon08:00–09:15 · B-101',
      'Wed10:00–12:00 · Lab-3Lab',
    ])

    const full = screen.getByRole('rowheader', { name: /MAT 201 · B/ }).closest('tr')!
    expect(within(full).getByText('Full')).toBeInTheDocument()
    expect(within(full).getByText('Not assigned')).toBeInTheDocument()

    // The current term is chosen by default.
    expect(screen.getByLabelText('Term')).toHaveValue('1')
    expect(queries.every((q) => q.get('termId') === '1')).toBe(true)
  })

  it('filters by term and course', async () => {
    const { queries } = mockSections()
    const { user } = renderAt('/admin/sections', { as: 'Admin' })
    await screen.findByRole('rowheader', { name: /CSC 101 · A/ })

    await user.selectOptions(screen.getByLabelText('Course'), 'MAT 201 · Discrete Mathematics')
    await waitFor(() => expect(screen.queryByRole('rowheader', { name: /CSC 101 · A/ })).not.toBeInTheDocument())
    expect(queries.at(-1)?.get('courseId')).toBe('4')

    await user.selectOptions(screen.getByLabelText('Term'), 'Spring 2027')
    expect(await screen.findByText('No sections here yet')).toBeInTheDocument()
    expect(queries.at(-1)?.get('termId')).toBe('2')
  })

  it('shows an empty state with a way to create the first section', async () => {
    mockSections([])
    const { user } = renderAt('/admin/sections', { as: 'Admin' })
    await user.selectOptions(await screen.findByLabelText('Term'), 'All terms')

    expect(await screen.findByText('No sections yet')).toBeInTheDocument()
    expect(screen.getAllByRole('button', { name: /New section/ })).toHaveLength(2)
  })

  it('shows an error with a retry button when the list fails', async () => {
    mockSections()
    let calls = 0
    server.use(
      http.get('/api/admin/sections', () => {
        calls++
        return calls === 1 ? HttpResponse.json({ title: 'Something went wrong.' }, { status: 500 }) : HttpResponse.json(paged([csc101A]))
      }),
    )
    const { user } = renderAt('/admin/sections', { as: 'Admin' })

    expect(await screen.findByText('Something went wrong.')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Try again' }))
    expect(await screen.findByRole('rowheader', { name: /CSC 101 · A/ })).toBeInTheDocument()
  })

  it('creates a section with two meetings in the chosen term', async () => {
    const { sent } = mockSections()
    const { user } = renderAt('/admin/sections', { as: 'Admin' })
    const dialog = await openNew(user)
    expect(within(dialog).getByLabelText('Term')).toHaveValue('1') // preselected from the filter

    await fillNewSection(user, dialog)
    await user.click(within(dialog).getByRole('button', { name: 'Add meeting' }))
    const m2 = meeting(dialog, 2)
    await user.selectOptions(m2.getByLabelText('Day'), 'Saturday')
    await user.selectOptions(m2.getByLabelText('Starts'), '10:15') // back to back with meeting 1 is fine
    await user.selectOptions(m2.getByLabelText('Ends'), '12:00')
    await user.selectOptions(m2.getByLabelText('Room'), 'Lab-3 · 25 seats')
    await user.selectOptions(m2.getByLabelText('Type'), 'Lab')
    await user.click(within(dialog).getByRole('button', { name: 'Create section' }))

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(sent).toEqual([
      {
        method: 'POST',
        body: {
          courseId: 4, termId: 1, sectionCode: 'c', instructorId: 2, capacity: 35,
          meetings: [
            { id: null, dayOfWeek: 6, startTime: '09:00:00', endTime: '10:15:00', roomId: 1, type: 'Lecture' },
            { id: null, dayOfWeek: 6, startTime: '10:15:00', endTime: '12:00:00', roomId: 3, type: 'Lab' },
          ],
        },
      },
    ])
    expect(screen.getByText('MAT 201 C saved.')).toBeInTheDocument()
  })

  it('checks the fields and the meetings before sending', async () => {
    const { sent } = mockSections()
    const { user } = renderAt('/admin/sections', { as: 'Admin' })
    const dialog = await openNew(user)
    await user.clear(within(dialog).getByLabelText('Capacity'))
    await user.type(within(dialog).getByLabelText('Capacity'), '501')
    await user.type(within(dialog).getByLabelText('Section code'), 'A-1')
    // Ending at or before the start can't be picked.
    const m1 = meeting(dialog, 1)
    await user.selectOptions(m1.getByLabelText('Starts'), '10:00')
    expect(within(m1.getByLabelText('Ends')).getByRole('option', { name: '10:00' })).toBeDisabled()
    expect(within(m1.getByLabelText('Ends')).getByRole('option', { name: '10:05' })).toBeEnabled()

    await user.click(within(dialog).getByRole('button', { name: 'Create section' }))

    expect(await within(dialog).findByText('Choose a course.')).toBeInTheDocument()
    expect(within(dialog).getByText('Capacity must be between 1 and 500.')).toBeInTheDocument()
    expect(within(dialog).getByText('Use letters and digits only, e.g. "A" or "B2".')).toBeInTheDocument()
    expect(m1.getByText('The meeting must end after it starts.')).toBeInTheDocument()
    expect(m1.getByText('Choose a room.')).toBeInTheDocument()
    expect(sent).toHaveLength(0)
  })

  it('catches two meetings of the section that overlap', async () => {
    const { sent } = mockSections()
    const { user } = renderAt('/admin/sections', { as: 'Admin' })
    const dialog = await openNew(user)
    await fillNewSection(user, dialog)
    await user.click(within(dialog).getByRole('button', { name: 'Add meeting' }))
    const m2 = meeting(dialog, 2)
    await user.selectOptions(m2.getByLabelText('Day'), 'Saturday')
    await user.selectOptions(m2.getByLabelText('Starts'), '10:00')
    await user.selectOptions(m2.getByLabelText('Ends'), '11:00')
    await user.selectOptions(m2.getByLabelText('Room'), 'Lab-3 · 25 seats')

    await user.click(within(dialog).getByRole('button', { name: 'Create section' }))

    expect(await within(dialog).findByText('Meetings 1 and 2 of this section overlap on Saturday.')).toBeInTheDocument()
    expect(sent).toHaveLength(0)
  })

  it('shows a room clash from the API next to the meetings, and a duplicate code under the code', async () => {
    mockSections()
    let reply = { status: 409, title: 'Room B-101 is already used by CSC 101 A on Monday 08:00–09:15.', code: 'room_clash' }
    server.use(http.post('/api/admin/sections', () => HttpResponse.json(reply, { status: 409 })))
    const { user } = renderAt('/admin/sections', { as: 'Admin' })
    const dialog = await openNew(user)
    await fillNewSection(user, dialog)

    await user.click(within(dialog).getByRole('button', { name: 'Create section' }))
    const meetings = within(dialog).getByRole('group', { name: 'Weekly meetings' })
    expect(await within(meetings).findByText(reply.title)).toBeInTheDocument()

    reply = { status: 409, title: 'Section C already exists for this course in this term.', code: 'duplicate_section_code' }
    await user.click(within(dialog).getByRole('button', { name: 'Create section' }))
    const code = within(dialog).getByLabelText('Section code')
    await waitFor(() => expect(code).toHaveAttribute('aria-invalid', 'true'))
    expect(within(dialog).getByText(reply.title)).toBeInTheDocument()
  })

  it('puts the API\'s 400 meeting errors on the right meeting', async () => {
    mockSections()
    server.use(
      http.post('/api/admin/sections', () =>
        HttpResponse.json(
          { status: 400, title: 'Some fields are not valid.', errors: { 'meetings[1].roomId': ["Room 3 doesn't exist."] } },
          { status: 400 },
        ),
      ),
    )
    const { user } = renderAt('/admin/sections', { as: 'Admin' })
    const dialog = await openNew(user)
    await fillNewSection(user, dialog)
    await user.click(within(dialog).getByRole('button', { name: 'Add meeting' }))
    await user.selectOptions(meeting(dialog, 2).getByLabelText('Room'), 'Lab-3 · 25 seats')

    await user.click(within(dialog).getByRole('button', { name: 'Create section' }))

    expect(await meeting(dialog, 2).findByText("Room 3 doesn't exist.")).toBeInTheDocument()
  })

  it('edits a section keeping its meeting ids, and an instructor clash shows next to the meetings', async () => {
    const { sent } = mockSections()
    const clash = 'The instructor already teaches MAT 201 A on Monday 10:00–11:15.'
    const { user } = renderAt('/admin/sections', { as: 'Admin' })

    await user.click(await screen.findByRole('button', { name: 'Edit CSC 101 A' }))
    const dialog = screen.getByRole('dialog', { name: 'Edit CSC 101 A' })
    await waitFor(() => expect(within(dialog).getByLabelText('Course')).toHaveValue('1'))
    // Students hold seats: course and term are locked; the seats taken are shown under capacity.
    expect(within(dialog).getByLabelText('Course')).toBeDisabled()
    expect(within(dialog).getByText('12 seats taken')).toBeInTheDocument()
    expect(meeting(dialog, 1).getByLabelText('Starts')).toHaveValue('08:00')
    expect(meeting(dialog, 2).getByLabelText('Type')).toHaveValue('Lab')

    server.use(
      http.put('/api/admin/sections/1', () => HttpResponse.json({ status: 409, title: clash, code: 'instructor_clash' }, { status: 409 })),
    )
    await user.selectOptions(meeting(dialog, 1).getByLabelText('Ends'), '09:30')
    await user.click(within(dialog).getByRole('button', { name: 'Save changes' }))
    expect(await within(within(dialog).getByRole('group', { name: 'Weekly meetings' })).findByText(clash)).toBeInTheDocument()

    // Remove the lab and save: meeting 1 keeps its id, the removed one is left out.
    server.use(
      http.put('/api/admin/sections/1', async ({ request }) => {
        sent.push({ method: 'PUT', id: '1', body: (await request.json()) as SaveSectionRequest })
        return HttpResponse.json(csc101A)
      }),
    )
    await user.click(within(dialog).getByRole('button', { name: 'Remove meeting 2' }))
    expect(within(dialog).queryByText(clash)).not.toBeInTheDocument() // cleared on change
    await user.click(within(dialog).getByRole('button', { name: 'Save changes' }))
    await waitFor(() => expect(sent).toHaveLength(1))
    expect(sent[0]).toMatchObject({
      method: 'PUT',
      id: '1',
      body: {
        courseId: 1, termId: 1, sectionCode: 'A', instructorId: 2, capacity: 40,
        meetings: [{ id: 11, dayOfWeek: 1, startTime: '08:00:00', endTime: '09:30:00', roomId: 1, type: 'Lecture' }],
      },
    })
    expect(await screen.findByText('CSC 101 A saved.')).toBeInTheDocument()
  })
})
