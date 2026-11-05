import { act } from 'react'
import { describe, expect, it, vi } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, waitFor, within } from '@testing-library/react'
import type { UserEvent } from '@testing-library/user-event'
import { server } from '@/test/server'
import { lookups, paged, profileFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'
import { SAVED_MESSAGE_MS } from '@/shared/lib/useSavedMessage'
import type { CourseDetail, CourseListItem, SaveCourseRequest } from '../types'

// PB-03 / T08: admin searches, creates and edits courses. Simulated catalog matching the DevSeeder (SR-23).

const item = (id: number, code: string, name: string, dept = 'CSC'): CourseListItem => ({
  id, code, name, credits: 3, departmentCode: dept, departmentName: dept === 'CSC' ? 'Computer Science' : 'Mathematics',
})
const csc101 = item(1, 'CSC 101', 'Introduction to Programming')
const csc210 = item(2, 'CSC 210', 'Data Structures')
const csc310 = item(3, 'CSC 310', 'Database Systems')
const mat201 = item(4, 'MAT 201', 'Linear Algebra', 'MAT')
const catalog = [csc101, csc210, csc310, mat201]

const csc310Detail: CourseDetail = {
  id: 3,
  code: 'CSC 310',
  name: 'Database Systems',
  credits: 3,
  description: 'Relational model and SQL.',
  departmentId: 1,
  departmentName: 'Computer Science',
  prerequisites: [{ id: 2, code: 'CSC 210', name: 'Data Structures' }],
  majors: [{ majorId: 1, majorName: 'Computer Science', type: 'Major' }],
}

type Sent = { method: string; id?: string; body: SaveCourseRequest }

/** Mock API: searches the catalog like the backend (code or name, department, paging). */
function mockCourses(courses: CourseListItem[] = catalog) {
  const sent: Sent[] = []
  const searches: URLSearchParams[] = []
  server.use(
    http.get('/api/profile', () => HttpResponse.json(profileFor('Admin'))),
    http.get('/api/admin/lookups', () => HttpResponse.json(lookups)),
    http.get('/api/admin/courses', ({ request }) => {
      const q = new URL(request.url).searchParams
      searches.push(q)
      const s = (q.get('search') ?? '').toLowerCase()
      const dept = q.get('departmentId')
      const page = Number(q.get('page') ?? 1)
      const size = Number(q.get('pageSize') ?? 20)
      const hits = courses.filter(
        (c) => (!s || `${c.code} ${c.name}`.toLowerCase().includes(s)) && (!dept || (dept === '1') === (c.departmentCode === 'CSC')),
      )
      return HttpResponse.json(paged(hits.slice((page - 1) * size, page * size), page, size, hits.length))
    }),
    http.get('/api/admin/courses/3', () => HttpResponse.json(csc310Detail)),
    http.post('/api/admin/courses', async ({ request }) => {
      const body = (await request.json()) as SaveCourseRequest
      sent.push({ method: 'POST', body })
      return HttpResponse.json({ ...csc310Detail, id: 99, code: 'CSC 401', name: body.name }, { status: 201 })
    }),
    http.put('/api/admin/courses/:id', async ({ request, params }) => {
      const body = (await request.json()) as SaveCourseRequest
      sent.push({ method: 'PUT', id: String(params.id), body })
      return HttpResponse.json({ ...csc310Detail, name: body.name })
    }),
  )
  return { sent, searches }
}

async function openNew(user: UserEvent) {
  await user.click((await screen.findAllByRole('button', { name: /New course/ }))[0]!)
  return screen.getByRole('dialog', { name: 'New course' })
}

async function fillBasics(user: UserEvent, dialog: HTMLElement, code = 'csc401') {
  await user.type(within(dialog).getByLabelText('Code'), code)
  await user.type(within(dialog).getByLabelText('Name'), 'Software Engineering')
  const department = within(dialog).getByLabelText('Department')
  await waitFor(() => expect(department).toBeEnabled())
  await user.selectOptions(department, 'CSC · Computer Science')
}

async function addPrerequisite(user: UserEvent, dialog: HTMLElement, search: string, code: string, name: string) {
  await user.type(within(dialog).getByLabelText('Search courses to add as prerequisites'), search)
  await user.click(await within(dialog).findByRole('button', { name: `Add ${code} ${name}` }))
}

describe('CoursesPage', () => {
  it('lists courses with code, name, department and credits', async () => {
    mockCourses()
    renderAt('/admin/courses', { as: 'Admin' })

    const row = (await screen.findByRole('rowheader', { name: 'CSC 210' })).closest('tr')!
    expect(within(row).getByText('Data Structures')).toBeInTheDocument()
    expect(within(row).getByText('CSC')).toBeInTheDocument()
    expect(within(row).getByText('3')).toBeInTheDocument()
    expect(screen.getByText('4 courses')).toBeInTheDocument()
  })

  it('searches by code or name and filters by department, back on page 1', async () => {
    const { searches } = mockCourses()
    const { user } = renderAt('/admin/courses', { as: 'Admin' })
    await screen.findByRole('rowheader', { name: 'CSC 101' })

    await user.type(screen.getByLabelText('Search'), 'linear')
    expect(await screen.findByRole('rowheader', { name: 'MAT 201' })).toBeInTheDocument()
    await waitFor(() => expect(screen.queryByRole('rowheader', { name: 'CSC 101' })).not.toBeInTheDocument())
    expect(searches.at(-1)?.get('search')).toBe('linear')

    await user.clear(screen.getByLabelText('Search'))
    await user.selectOptions(screen.getByLabelText('Department'), 'MAT · Mathematics')
    await waitFor(() => expect(searches.at(-1)?.get('departmentId')).toBe('2'))
    expect(searches.at(-1)?.get('page')).toBe('1')
  })

  it('pages through long lists', async () => {
    const many = Array.from({ length: 25 }, (_, i) => item(100 + i, `CSC ${500 + i}`, `Simulated Course ${i + 1}`))
    const { searches } = mockCourses(many)
    const { user } = renderAt('/admin/courses', { as: 'Admin' })
    await screen.findByRole('rowheader', { name: 'CSC 500' })
    expect(screen.getByText('25 courses · page 1 of 2')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: /Next/ }))

    expect(await screen.findByRole('rowheader', { name: 'CSC 520' })).toBeInTheDocument()
    expect(searches.at(-1)?.get('page')).toBe('2')
    expect(screen.getByRole('button', { name: /Next/ })).toBeDisabled()
  })

  it('shows an empty state with a way to create the first course', async () => {
    mockCourses([])
    renderAt('/admin/courses', { as: 'Admin' })

    expect(await screen.findByText('No courses yet')).toBeInTheDocument()
    expect(screen.getAllByRole('button', { name: /New course/ })).toHaveLength(2)
  })

  it('says when nothing matches the search', async () => {
    mockCourses()
    const { user } = renderAt('/admin/courses', { as: 'Admin' })
    await screen.findByRole('rowheader', { name: 'CSC 101' })

    await user.type(screen.getByLabelText('Search'), 'zzz')

    expect(await screen.findByText('No courses match')).toBeInTheDocument()
  })

  it('shows an error with a retry button when the list fails', async () => {
    let calls = 0
    server.use(
      http.get('/api/profile', () => HttpResponse.json(profileFor('Admin'))),
      http.get('/api/admin/lookups', () => HttpResponse.json(lookups)),
      http.get('/api/admin/courses', () => {
        calls++
        return calls === 1 ? HttpResponse.json({ title: 'Something went wrong.' }, { status: 500 }) : HttpResponse.json(paged([csc101]))
      }),
    )
    const { user } = renderAt('/admin/courses', { as: 'Admin' })

    expect(await screen.findByText('Something went wrong.')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Try again' }))
    expect(await screen.findByRole('rowheader', { name: 'CSC 101' })).toBeInTheDocument()
  })

  it('creates a course with prerequisites and majors, and hides the saved message after 5 seconds', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true })
    const { sent } = mockCourses()
    const { user } = renderAt('/admin/courses', { as: 'Admin' })
    const dialog = await openNew(user)

    await fillBasics(user, dialog)
    await user.clear(within(dialog).getByLabelText('Credits'))
    await user.type(within(dialog).getByLabelText('Credits'), '4')
    await addPrerequisite(user, dialog, 'data', 'CSC 210', 'Data Structures')
    await addPrerequisite(user, dialog, '310', 'CSC 310', 'Database Systems')
    // Picking a course empties the search and its results at once (no stale "no match" box).
    expect(within(dialog).getByLabelText('Search courses to add as prerequisites')).toHaveValue('')
    expect(within(dialog).queryByText(/No other course matches/)).not.toBeInTheDocument()
    await user.click(within(dialog).getByRole('button', { name: 'Remove CSC 310' }))
    await user.selectOptions(within(dialog).getByLabelText('Computer Science'), 'Major (required)')
    await user.selectOptions(within(dialog).getByLabelText('Mathematics'), 'Elective')
    await user.click(within(dialog).getByRole('button', { name: 'Create course' }))

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(sent).toEqual([
      {
        method: 'POST',
        body: {
          code: 'csc401',
          name: 'Software Engineering',
          credits: 4,
          description: null,
          departmentId: 1,
          prerequisiteIds: [2],
          majors: [
            { majorId: 1, type: 'Major' },
            { majorId: 2, type: 'Elective' },
          ],
        },
      },
    ])

    expect(screen.getByText('CSC 401 saved.')).toBeInTheDocument()
    act(() => vi.advanceTimersByTime(SAVED_MESSAGE_MS - 500))
    expect(screen.getByText('CSC 401 saved.')).toBeInTheDocument()
    act(() => vi.advanceTimersByTime(600))
    expect(screen.queryByText('CSC 401 saved.')).not.toBeInTheDocument()
    vi.useRealTimers()
  })

  it('checks the fields before sending', async () => {
    const { sent } = mockCourses()
    const { user } = renderAt('/admin/courses', { as: 'Admin' })
    const dialog = await openNew(user)

    await user.type(within(dialog).getByLabelText('Code'), 'CS1')
    await user.clear(within(dialog).getByLabelText('Credits'))
    await user.type(within(dialog).getByLabelText('Credits'), '13')
    await user.click(within(dialog).getByRole('button', { name: 'Create course' }))

    expect(await within(dialog).findByText('Use a code like "CSC 101": 2–5 letters, 3 digits, optional letter.')).toBeInTheDocument()
    expect(within(dialog).getByText('Enter a course name.')).toBeInTheDocument()
    expect(within(dialog).getByText('Credits must be between 0 and 12.')).toBeInTheDocument()
    expect(within(dialog).getByText('Choose a department.')).toBeInTheDocument()
    expect(sent).toHaveLength(0)
  })

  it('shows field errors from the API under their fields', async () => {
    mockCourses()
    server.use(
      http.post('/api/admin/courses', () =>
        HttpResponse.json(
          { status: 400, title: 'Some fields are not valid.', errors: { 'majors[0].majorId': ["Major 1 doesn't exist."] } },
          { status: 400 },
        ),
      ),
    )
    const { user } = renderAt('/admin/courses', { as: 'Admin' })
    const dialog = await openNew(user)
    await fillBasics(user, dialog)
    await user.selectOptions(within(dialog).getByLabelText('Computer Science'), 'Major (required)')

    await user.click(within(dialog).getByRole('button', { name: 'Create course' }))

    expect(await within(dialog).findByText("Major 1 doesn't exist.")).toBeInTheDocument()
    expect(within(dialog).queryByRole('alert', { name: /Some fields/ })).not.toBeInTheDocument()
  })

  it('shows a duplicate code from the API under the code field', async () => {
    mockCourses()
    server.use(
      http.post('/api/admin/courses', () =>
        HttpResponse.json({ status: 409, title: 'Course code CSC 101 already exists.', code: 'duplicate_course_code' }, { status: 409 }),
      ),
    )
    const { user } = renderAt('/admin/courses', { as: 'Admin' })
    const dialog = await openNew(user)
    await fillBasics(user, dialog, 'csc101')

    await user.click(within(dialog).getByRole('button', { name: 'Create course' }))

    const code = within(dialog).getByLabelText('Code')
    await waitFor(() => expect(code).toHaveAttribute('aria-invalid', 'true'))
    expect(within(dialog).getByText('Course code CSC 101 already exists.')).toBeInTheDocument()
  })

  it('edits a course with its saved values, and shows a prerequisite cycle under the prerequisites', async () => {
    const { sent } = mockCourses()
    const cycle = 'CSC 310 can\'t require CSC 101: CSC 101 already depends on CSC 310 (CSC 101 → CSC 310).'
    const { user } = renderAt('/admin/courses', { as: 'Admin' })

    await user.click(await screen.findByRole('button', { name: 'Edit CSC 310' }))
    const dialog = await screen.findByRole('dialog', { name: 'Edit CSC 310' })
    expect(await within(dialog).findByLabelText('Code')).toHaveValue('CSC 310')
    expect(within(dialog).getByLabelText('Description (optional)')).toHaveValue('Relational model and SQL.')
    expect(within(dialog).getByRole('button', { name: 'Remove CSC 210' })).toBeInTheDocument()
    expect(within(dialog).getByLabelText('Computer Science')).toHaveValue('Major')
    await waitFor(() => expect(within(dialog).getByLabelText('Department')).toHaveValue('1'))

    // The course itself is never offered as its own prerequisite.
    await user.type(within(dialog).getByLabelText('Search courses to add as prerequisites'), 'CSC 3')
    expect(await within(dialog).findByText('No other course matches “CSC 3”.')).toBeInTheDocument()
    await user.clear(within(dialog).getByLabelText('Search courses to add as prerequisites'))

    server.use(
      http.put('/api/admin/courses/3', () =>
        HttpResponse.json({ status: 409, title: cycle, code: 'prerequisite_cycle' }, { status: 409 }),
      ),
    )
    await addPrerequisite(user, dialog, '101', 'CSC 101', 'Introduction to Programming')
    await user.click(within(dialog).getByRole('button', { name: 'Save changes' }))
    expect(await within(dialog).findByText(cycle)).toBeInTheDocument()

    // Removing the offending prerequisite and saving again goes through.
    server.use(
      http.put('/api/admin/courses/3', async ({ request }) => {
        sent.push({ method: 'PUT', id: '3', body: (await request.json()) as SaveCourseRequest })
        return HttpResponse.json(csc310Detail)
      }),
    )
    await user.click(within(dialog).getByRole('button', { name: 'Remove CSC 101' }))
    await user.click(within(dialog).getByRole('button', { name: 'Save changes' }))
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(sent).toMatchObject([{ method: 'PUT', id: '3', body: { code: 'CSC 310', prerequisiteIds: [2], majors: [{ majorId: 1, type: 'Major' }] } }])
    expect(screen.getByText('CSC 310 saved.')).toBeInTheDocument()
  })
})
