import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, waitFor, within } from '@testing-library/react'
import { server } from '@/test/server'
import { paged, profileFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'
import type { CreateSupportRequest, SupportRequestDetail, SupportRequestSummary } from '../types'

// PB-13 / T51: the student's support requests and the new-request form. Simulated data (SR-23).

const categories = [
  { id: 2, name: 'Finance' },
  { id: 1, name: 'Registration' },
]

const summary = (id: number, subject: string, extra: Partial<SupportRequestSummary> = {}): SupportRequestSummary => ({
  id, subject, categoryId: 1, categoryName: 'Registration', status: 'Open',
  createdAt: '2026-11-01T10:00:00Z', lastActivityAt: '2026-11-02T10:00:00Z', messageCount: 0, ...extra,
})

const detail = (id: number, body: CreateSupportRequest): SupportRequestDetail => ({
  id, categoryId: body.categoryId, categoryName: 'Finance', subject: body.subject, description: body.description,
  status: 'Open', createdAt: '2026-11-03T10:00:00Z', resolvedAt: null, messages: [],
})

function mockSupport(items: SupportRequestSummary[]) {
  const queries: URLSearchParams[] = []
  const sent: CreateSupportRequest[] = []
  server.use(
    http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))),
    http.get('/api/support/categories', () => HttpResponse.json(categories)),
    http.get('/api/support/requests', ({ request }) => {
      const q = new URL(request.url).searchParams
      queries.push(q)
      return HttpResponse.json(paged(items.filter((i) => !q.get('status') || i.status === q.get('status'))))
    }),
    http.post('/api/support/requests', async ({ request }) => {
      const body = (await request.json()) as CreateSupportRequest
      sent.push(body)
      return HttpResponse.json(detail(42, body), { status: 201 })
    }),
    http.get('/api/support/requests/42', () =>
      HttpResponse.json(detail(42, { categoryId: 2, subject: 'Charge looks wrong', description: 'Simulated.' })),
    ),
  )
  return { queries, sent }
}

describe('StudentSupportPage', () => {
  it('lists my requests with status badges and filters by status', async () => {
    const { queries } = mockSupport([
      summary(1, 'Cannot add CSC 310', { status: 'InProgress', messageCount: 2 }),
      summary(2, 'Lab fee charged twice', { status: 'Resolved', categoryName: 'Finance' }),
    ])
    const { user } = renderAt('/student/support', { as: 'Student' })

    const list = await screen.findByRole('list', { name: 'Support requests' })
    const first = within(list).getByRole('link', { name: 'Cannot add CSC 310' }).closest('li')!
    expect(within(first).getByText('In progress')).toBeInTheDocument()
    expect(within(first).getByLabelText('2 replies')).toBeInTheDocument()
    expect(within(list).getByRole('link', { name: 'Lab fee charged twice' })).toHaveAttribute('href', '/student/support/2')
    expect(within(list).getByText('Resolved')).toBeInTheDocument()

    await user.selectOptions(screen.getByLabelText('Status'), 'Resolved')
    await waitFor(() => expect(screen.queryByRole('link', { name: 'Cannot add CSC 310' })).not.toBeInTheDocument())
    expect(queries.at(-1)?.get('status')).toBe('Resolved')
  })

  it('submits a new request and opens it', async () => {
    const { sent } = mockSupport([])
    const { user, router } = renderAt('/student/support', { as: 'Student' })
    await user.click((await screen.findAllByRole('button', { name: /New request/ }))[0]!)
    const dialog = screen.getByRole('dialog', { name: 'New support request' })
    await waitFor(() => expect(within(dialog).getByLabelText('Category')).toBeEnabled())

    await user.selectOptions(within(dialog).getByLabelText('Category'), 'Finance')
    await user.type(within(dialog).getByLabelText('Subject'), '  Charge looks wrong ')
    await user.type(within(dialog).getByLabelText('Description'), 'Simulated.')
    await user.click(within(dialog).getByRole('button', { name: 'Submit request' }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/student/support/42'))
    expect(sent).toEqual([{ categoryId: 2, subject: 'Charge looks wrong', description: 'Simulated.' }])
    expect(await screen.findByRole('heading', { name: 'Charge looks wrong' })).toBeInTheDocument()
    expect(screen.getByText(/Request submitted/)).toBeInTheDocument()
  })

  it('checks the fields before sending', async () => {
    const { sent } = mockSupport([])
    const { user } = renderAt('/student/support', { as: 'Student' })
    await user.click((await screen.findAllByRole('button', { name: /New request/ }))[0]!)
    const dialog = screen.getByRole('dialog', { name: 'New support request' })
    await user.type(within(dialog).getByLabelText('Subject'), '   ')
    await user.click(within(dialog).getByRole('button', { name: 'Submit request' }))

    expect(await within(dialog).findByText('Choose a category.')).toBeInTheDocument()
    expect(within(dialog).getByText('Enter a subject.')).toBeInTheDocument()
    expect(within(dialog).getByText('Describe the problem.')).toBeInTheDocument()
    expect(sent).toHaveLength(0)
  })

  it('shows the API’s 400 field error under the field', async () => {
    mockSupport([])
    server.use(
      http.post('/api/support/requests', () =>
        HttpResponse.json(
          { status: 400, title: 'Some fields are not valid.', errors: { categoryId: ['Choose a category from the list.'] } },
          { status: 400 },
        ),
      ),
    )
    const { user } = renderAt('/student/support', { as: 'Student' })
    await user.click((await screen.findAllByRole('button', { name: /New request/ }))[0]!)
    const dialog = screen.getByRole('dialog', { name: 'New support request' })
    await waitFor(() => expect(within(dialog).getByLabelText('Category')).toBeEnabled())
    await user.selectOptions(within(dialog).getByLabelText('Category'), 'Registration')
    await user.type(within(dialog).getByLabelText('Subject'), 'Question')
    await user.type(within(dialog).getByLabelText('Description'), 'Details')
    await user.click(within(dialog).getByRole('button', { name: 'Submit request' }))

    expect(await within(dialog).findByText('Choose a category from the list.')).toBeInTheDocument()
  })

  it('shows an empty state with a way to submit the first request', async () => {
    mockSupport([])
    renderAt('/student/support', { as: 'Student' })
    expect(await screen.findByText('No support requests yet')).toBeInTheDocument()
    expect(screen.getAllByRole('button', { name: /New request/ })).toHaveLength(2)
  })

  it('shows an error with a retry button when the list fails', async () => {
    mockSupport([])
    let calls = 0
    server.use(
      http.get('/api/support/requests', () => {
        calls++
        return calls === 1
          ? HttpResponse.json({ title: 'Something went wrong.' }, { status: 500 })
          : HttpResponse.json(paged([summary(1, 'Cannot add CSC 310')]))
      }),
    )
    const { user } = renderAt('/student/support', { as: 'Student' })
    expect(await screen.findByText('Something went wrong.')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Try again' }))
    expect(await screen.findByRole('link', { name: 'Cannot add CSC 310' })).toBeInTheDocument()
  })
})
