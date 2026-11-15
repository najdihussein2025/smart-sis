import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, waitFor, within } from '@testing-library/react'
import { server } from '@/test/server'
import { profileFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'
import type { Catalog, CatalogCourse, CatalogCourseDetail } from '../types'

// PB-04 / T15: the student browses the catalog and adds sections to the cart. Simulated data (SR-23).

const course = (id: number, code: string, name: string, type: CatalogCourse['type'], extra: Partial<CatalogCourse> = {}): CatalogCourse => ({
  id, code, name, credits: 3, departmentName: 'Computer Science', type, sectionCount: 1, seatsLeft: 30, status: 'Available', ...extra,
})
const courses = [
  course(1, 'CSC 101', 'Introduction to Programming', 'Major', { status: 'Enrolled' }),
  course(2, 'CSC 210', 'Data Structures', 'Major', { sectionCount: 2 }),
  course(4, 'MAT 201', 'Discrete Mathematics', 'Elective', { seatsLeft: 0 }),
]
const catalog = (list: CatalogCourse[], extra: Partial<Catalog> = {}): Catalog => ({
  termId: 1, termName: 'Fall 2026', window: 'Enrollment', majorId: 1, courses: list, ...extra,
})

const detail: CatalogCourseDetail = {
  id: 2, code: 'CSC 210', name: 'Data Structures', credits: 3, description: 'Lists, stacks, queues, trees and hash tables.',
  departmentName: 'Computer Science',
  prerequisites: [{ id: 1, code: 'CSC 101', name: 'Introduction to Programming', passed: true }],
  sections: [
    {
      id: 21, sectionCode: 'A', instructorName: 'Demo Instructor', capacity: 30, seatsTaken: 4, seatsLeft: 26, status: 'Available',
      meetings: [{ id: 1, dayOfWeek: 2, startTime: '10:00:00', endTime: '11:15:00', roomId: 2, roomName: 'B-204', type: 'Lecture' }],
    },
    {
      id: 22, sectionCode: 'B', instructorName: null, capacity: 30, seatsTaken: 30, seatsLeft: 0, status: 'InCart',
      meetings: [{ id: 2, dayOfWeek: 4, startTime: '10:00:00', endTime: '12:00:00', roomId: 3, roomName: 'Lab-3', type: 'Lab' }],
    },
  ],
}

function mockCatalog(list = courses) {
  const queries: URLSearchParams[] = []
  server.use(
    http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))),
    http.get('/api/catalog/majors', () => HttpResponse.json([{ id: 1, name: 'Computer Science' }, { id: 2, name: 'Mathematics' }])),
    http.get('/api/catalog/courses', ({ request }) => {
      const q = new URL(request.url).searchParams
      queries.push(q)
      const type = q.get('type')
      return HttpResponse.json(catalog(list.filter((c) => !type || c.type === type)))
    }),
  )
  return queries
}

describe('CatalogPage', () => {
  it('lists this term\'s courses with type, seats and the student\'s status', async () => {
    mockCatalog()
    renderAt('/student/courses', { as: 'Student' })

    const list = await screen.findByRole('list', { name: 'Courses' })
    const rows = within(list).getAllByRole('link')
    expect(rows).toHaveLength(3)
    expect(within(rows[0]!).getByText('Enrolled')).toBeInTheDocument()
    expect(within(rows[2]!).getByText('Elective')).toBeInTheDocument()
    expect(within(rows[2]!).getByText(/Full/)).toBeInTheDocument()
    expect(screen.getByText('Enrollment open')).toBeInTheDocument()
  })

  it('filters by the Elective tab', async () => {
    const queries = mockCatalog()
    const { user } = renderAt('/student/courses', { as: 'Student' })
    await screen.findByRole('list', { name: 'Courses' })

    await user.click(screen.getByRole('tab', { name: 'Elective' }))

    expect(await screen.findByRole('link', { name: /MAT 201/ })).toBeInTheDocument()
    expect(screen.queryByRole('link', { name: /CSC 210/ })).not.toBeInTheDocument()
    expect(queries.at(-1)?.get('type')).toBe('Elective')
  })

  it('labels the program picker apart from the Major tab, and picks another program', async () => {
    const queries = mockCatalog()
    const { user } = renderAt('/student/courses', { as: 'Student' })
    await screen.findByRole('list', { name: 'Courses' })

    expect(screen.getByRole('tab', { name: 'Major' })).toBeInTheDocument()
    await user.selectOptions(screen.getByLabelText('Program'), 'Mathematics')
    await waitFor(() => expect(queries.at(-1)?.get('majorId')).toBe('2'))
  })

  it('shows an empty state when nothing matches', async () => {
    mockCatalog([])
    renderAt('/student/courses', { as: 'Student' })
    expect(await screen.findByText('No courses here')).toBeInTheDocument()
  })

  it('shows the error with a retry button', async () => {
    mockCatalog()
    server.use(http.get('/api/catalog/courses', () => HttpResponse.json({ title: 'Database unavailable.' }, { status: 500 })))
    renderAt('/student/courses', { as: 'Student' })
    expect(await screen.findByText('Something went wrong on our side. Please try again in a moment.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })
})

describe('CourseDetailPage', () => {
  function mockDetail(onAdd: () => Response | Promise<Response>) {
    let current = detail
    server.use(
      http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))),
      http.get('/api/catalog/courses/2', () => HttpResponse.json(current)),
      http.post('/api/cart', async () => {
        const res = await onAdd()
        if (res.ok) current = { ...detail, sections: detail.sections.map((s) => (s.id === 21 ? { ...s, status: 'InCart' } : s)) }
        return res
      }),
    )
  }

  it('shows prerequisites and sections, and "Add to cart" turns into "In Cart"', async () => {
    mockDetail(() => HttpResponse.json({}))
    const { user } = renderAt('/student/courses/2', { as: 'Student' })

    expect(await screen.findByRole('heading', { name: 'Data Structures' })).toBeInTheDocument()
    expect(screen.getByText('Passed')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'CSC 210 B: In Cart' })).toBeDisabled()
    expect(screen.getByText('Full')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Add CSC 210 A to cart' }))

    expect(await screen.findByRole('button', { name: 'CSC 210 A: In Cart' })).toBeDisabled()
  })

  it('shows the API\'s 409 message', async () => {
    mockDetail(() => HttpResponse.json({ title: "You're already enrolled in CSC 210 A." }, { status: 409 }))
    const { user } = renderAt('/student/courses/2', { as: 'Student' })

    await user.click(await screen.findByRole('button', { name: 'Add CSC 210 A to cart' }))
    expect(await screen.findByText("You're already enrolled in CSC 210 A.")).toBeInTheDocument()
  })
})

// Final regression (T80): rows of the official test cases that had no web test yet.
describe('CourseDetailPage: official test cases', () => {
  const serve = (d: CatalogCourseDetail) =>
    server.use(
      http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))),
      http.get('/api/catalog/courses/2', () => HttpResponse.json(d)),
    )

  it('TC_12: each section shows its code, instructor, seats and weekly day, time and room', async () => {
    serve(detail)
    renderAt('/student/courses/2', { as: 'Student' })

    expect(await screen.findByText('Section A')).toBeInTheDocument()
    expect(screen.getByText('26 of 30 seats left')).toBeInTheDocument()
    expect(screen.getByText('Demo Instructor')).toBeInTheDocument()
    expect(screen.getByText('Instructor to be announced')).toBeInTheDocument()
    const meetings = screen.getByRole('list', { name: 'Meetings of CSC 210 A' })
    expect(meetings).toHaveTextContent('Tue')
    expect(meetings).toHaveTextContent('10:00–11:15')
    expect(meetings).toHaveTextContent('B-204')
  })

  it('TC_14 and TC_17: an enrolled section says "Enrolled", and the other sections warn about Swap', async () => {
    serve({ ...detail, sections: detail.sections.map((s) => (s.id === 21 ? { ...s, status: 'Enrolled' as const } : { ...s, status: 'Available' as const, seatsLeft: 5 })) })
    renderAt('/student/courses/2', { as: 'Student' })

    expect(await screen.findByRole('button', { name: 'CSC 210 A: Enrolled' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Add CSC 210 B to cart' })).toBeEnabled()
    const warning = screen.getByRole('status')
    expect(warning).toHaveTextContent("You're already enrolled in section A.")
    expect(warning).toHaveTextContent('use Swap')
  })

  it('TC_17: no warning when the student is not enrolled in any section of the course', async () => {
    serve(detail)
    renderAt('/student/courses/2', { as: 'Student' })
    await screen.findByText('Section A')
    expect(screen.queryByText(/already enrolled in section/)).not.toBeInTheDocument()
  })

  it('TC_16: a course with no sections says it is not offered this term', async () => {
    serve({ ...detail, sections: [] })
    renderAt('/student/courses/2', { as: 'Student' })
    expect(await screen.findByText('Not offered this term')).toBeInTheDocument()
    expect(screen.getByText(/no sections in the current term/)).toBeInTheDocument()
  })
})
