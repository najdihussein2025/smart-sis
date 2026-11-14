import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, waitFor, within } from '@testing-library/react'
import type { UserEvent } from '@testing-library/user-event'
import { server } from '@/test/server'
import { paged, profileFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'
import type { Term } from '@/features/terms/types'
import type { CreateGradeComponentRequest, GradeComponent, GradeComponentList } from '../types'

// PB-08 / T31, T34: admin grade components per course per term. Simulated data (SR-23).

const term = (id: number, name: string, isCurrent: boolean): Term => ({
  id, name, isCurrent, startDate: '2026-09-01', endDate: '2026-12-20',
  enrollStart: '2026-08-15T08:00:00Z', enrollEnd: '2026-08-30T20:00:00Z',
  addDropStart: '2026-09-01T08:00:00Z', addDropEnd: '2026-09-10T20:00:00Z', currentWindow: 'Closed',
})
const courseList = [
  { id: 1, code: 'CSC 101', name: 'Introduction to Programming', credits: 3, departmentCode: 'CSC', departmentName: 'Computer Science' },
  { id: 4, code: 'MAT 201', name: 'Discrete Mathematics', credits: 3, departmentCode: 'MAT', departmentName: 'Mathematics' },
]
const comp = (id: number, name: string, maxGrade: number, weight: number, scoreCount = 0): GradeComponent => ({
  id, courseId: 1, termId: 1, name, maxGrade, weight, scoreCount,
})

function mockComponents(items: GradeComponent[] = [comp(1, 'Midterm', 50, 30, 4), comp(2, 'Project', 100, 20)], isFinalized = false) {
  const sent: { method: string; id?: string; body?: CreateGradeComponentRequest }[] = []
  const queries: URLSearchParams[] = []
  const list = (): GradeComponentList => ({ courseId: 1, termId: 1, totalWeight: items.reduce((n, c) => n + c.weight, 0), isFinalized, items })
  server.use(
    http.get('/api/profile', () => HttpResponse.json(profileFor('Admin'))),
    http.get('/api/admin/terms', () => HttpResponse.json([term(2, 'Spring 2027', false), term(1, 'Fall 2026', true)])),
    http.get('/api/admin/courses', () => HttpResponse.json(paged(courseList, 1, 100))),
    http.get('/api/admin/grade-components', ({ request }) => {
      queries.push(new URL(request.url).searchParams)
      return HttpResponse.json(list())
    }),
    http.post('/api/admin/grade-components', async ({ request }) => {
      const body = (await request.json()) as CreateGradeComponentRequest
      sent.push({ method: 'POST', body })
      const created = comp(9, body.name, body.maxGrade, body.weight)
      items = [...items, created]
      return HttpResponse.json(created, { status: 201 })
    }),
    http.delete('/api/admin/grade-components/:id', ({ params }) => {
      sent.push({ method: 'DELETE', id: String(params.id) })
      items = items.filter((c) => c.id !== Number(params.id))
      return new HttpResponse(null, { status: 204 })
    }),
  )
  return { sent, queries }
}

async function chooseCourse(user: UserEvent) {
  await waitFor(() => expect(screen.getByLabelText('Course')).toBeEnabled())
  await user.selectOptions(screen.getByLabelText('Course'), 'CSC 101 · Introduction to Programming')
}

async function openNew(user: UserEvent) {
  await screen.findByRole('rowheader', { name: 'Midterm' })
  await user.click(screen.getByRole('button', { name: /New component/ }))
  return screen.getByRole('dialog', { name: 'New component for CSC 101' })
}

describe('GradeComponentsPage', () => {
  it('asks for a course first, then lists its components with the total weight', async () => {
    const { queries } = mockComponents()
    const { user } = renderAt('/admin/grade-components', { as: 'Admin' })

    expect(await screen.findByText('Choose a course')).toBeInTheDocument()
    await chooseCourse(user)

    const row = (await screen.findByRole('rowheader', { name: 'Midterm' })).closest('tr')!
    expect(within(row).getByText('50')).toBeInTheDocument()
    expect(within(row).getByText('30%')).toBeInTheDocument()
    expect(within(row).getByText('4 entered')).toBeInTheDocument()
    expect(screen.getByText('Total weight 50% of 100%')).toBeInTheDocument()
    expect(screen.getByText('50% left to assign')).toBeInTheDocument()
    expect(screen.getByRole('progressbar', { name: 'Total weight' })).toHaveAttribute('aria-valuenow', '50')
    // The current term is chosen by default.
    expect(screen.getByLabelText('Term')).toHaveValue('1')
    expect(queries.at(-1)?.get('termId')).toBe('1')
    expect(queries.at(-1)?.get('courseId')).toBe('1')
  })

  it('adds a component', async () => {
    const { sent } = mockComponents()
    const { user } = renderAt('/admin/grade-components', { as: 'Admin' })
    await chooseCourse(user)
    const dialog = await openNew(user)

    expect(within(dialog).getByText('50% is still free.')).toBeInTheDocument()
    await user.type(within(dialog).getByLabelText('Name'), 'Final')
    await user.type(within(dialog).getByLabelText('Weight (%)'), '50')
    await user.click(within(dialog).getByRole('button', { name: 'Add component' }))

    expect(await screen.findByText('Final saved.')).toBeInTheDocument()
    expect(sent).toEqual([{ method: 'POST', body: { courseId: 1, termId: 1, name: 'Final', maxGrade: 100, weight: 50 } }])
    expect(await screen.findByRole('rowheader', { name: 'Final' })).toBeInTheDocument()
  })

  it('shows field errors without calling the API', async () => {
    const { sent } = mockComponents()
    const { user } = renderAt('/admin/grade-components', { as: 'Admin' })
    await chooseCourse(user)
    const dialog = await openNew(user)

    await user.clear(within(dialog).getByLabelText('Max grade'))
    await user.type(within(dialog).getByLabelText('Weight (%)'), '12.345')
    await user.click(within(dialog).getByRole('button', { name: 'Add component' }))

    expect(await within(dialog).findByText('Enter a name, e.g. "Midterm".')).toBeInTheDocument()
    expect(within(dialog).getByText('Max grade must be more than 0 and at most 1000 (2 decimals).')).toBeInTheDocument()
    expect(within(dialog).getByText('Weight must be more than 0% and at most 100% (2 decimals).')).toBeInTheDocument()
    expect(sent).toEqual([])
  })

  it('shows the 409 over-100% message under Weight', async () => {
    mockComponents()
    server.use(
      http.post('/api/admin/grade-components', () =>
        HttpResponse.json(
          { status: 409, title: 'The weights would add up to 110%, but a course can\'t go over 100%. 50% is left.', code: 'weight_over_100' },
          { status: 409 },
        ),
      ),
    )
    const { user } = renderAt('/admin/grade-components', { as: 'Admin' })
    await chooseCourse(user)
    const dialog = await openNew(user)

    await user.type(within(dialog).getByLabelText('Name'), 'Final')
    await user.type(within(dialog).getByLabelText('Weight (%)'), '60')
    await user.click(within(dialog).getByRole('button', { name: 'Add component' }))

    expect(await within(dialog).findByText(/can't go over 100%\. 50% is left\./)).toBeInTheDocument()
    expect(within(dialog).getByLabelText('Weight (%)')).toHaveAttribute('aria-invalid', 'true')
  })

  it('deletes after confirming, and shows a 409 when the component has scores', async () => {
    const { sent } = mockComponents()
    server.use(
      http.delete('/api/admin/grade-components/1', () =>
        HttpResponse.json({ status: 409, title: 'Midterm already has 4 scores, so it can\'t be deleted.', code: 'component_has_scores' }, { status: 409 }),
      ),
    )
    const { user } = renderAt('/admin/grade-components', { as: 'Admin' })
    await chooseCourse(user)
    await screen.findByRole('rowheader', { name: 'Midterm' })

    await user.click(screen.getByRole('button', { name: 'Delete Midterm' }))
    let dialog = screen.getByRole('dialog', { name: 'Delete Midterm?' })
    await user.click(within(dialog).getByRole('button', { name: 'Delete' }))
    expect(await within(dialog).findByText('Midterm already has 4 scores, so it can\'t be deleted.')).toBeInTheDocument()
    await user.click(within(dialog).getByRole('button', { name: 'Cancel' }))

    await user.click(screen.getByRole('button', { name: 'Delete Project' }))
    dialog = screen.getByRole('dialog', { name: 'Delete Project?' })
    await user.click(within(dialog).getByRole('button', { name: 'Delete' }))
    expect(await screen.findByText('Project deleted.')).toBeInTheDocument()
    expect(sent).toEqual([{ method: 'DELETE', id: '2' }])
    await waitFor(() => expect(screen.queryByRole('rowheader', { name: 'Project' })).not.toBeInTheDocument())
  })

  it('locks a finalized course', async () => {
    mockComponents(undefined, true)
    const { user } = renderAt('/admin/grade-components', { as: 'Admin' })
    await chooseCourse(user)

    expect(await screen.findByText(/components are locked/)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Edit Midterm' })).toBeDisabled()
    expect(screen.getByRole('button', { name: /New component/ })).toBeDisabled()
  })

  it('shows an empty state for a course without components', async () => {
    mockComponents([])
    const { user } = renderAt('/admin/grade-components', { as: 'Admin' })
    await chooseCourse(user)

    expect(await screen.findByText('No components yet')).toBeInTheDocument()
  })

  it('shows an error with retry when the list fails', async () => {
    mockComponents()
    server.use(http.get('/api/admin/grade-components', () => HttpResponse.json({ title: 'Server error' }, { status: 500 })))
    const { user } = renderAt('/admin/grade-components', { as: 'Admin' })
    await chooseCourse(user)

    expect(await screen.findByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })
})
