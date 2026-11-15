import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, waitFor, within } from '@testing-library/react'
import { server } from '@/test/server'
import { paged, profileFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'
import type { AdminSupportCategory, AdminSupportRequest } from '../types'

// PB-14 / T54: the admin support inbox and the support categories tab. Simulated data (SR-23).

const row = (id: number, subject: string, extra: Partial<AdminSupportRequest> = {}): AdminSupportRequest => ({
  id, subject, categoryId: 1, categoryName: 'Registration', status: 'Open', studentId: 3, studentUniversityId: 3001,
  studentName: 'Demo Student', assignedAdminId: null, assignedAdminName: null,
  createdAt: '2026-11-01T10:00:00Z', lastActivityAt: '2026-11-02T10:00:00Z', messageCount: 0, ...extra,
})

const categories: AdminSupportCategory[] = [
  { id: 2, name: 'Finance', requestCount: 1 },
  { id: 1, name: 'Registration', requestCount: 2 },
  { id: 5, name: 'Other', requestCount: 0 },
]

function mockInbox(items: AdminSupportRequest[] = [row(1, 'Cannot add CSC 310'), row(2, 'Lab fee charged twice', {
  status: 'InProgress', categoryId: 2, categoryName: 'Finance', assignedAdminId: 1, assignedAdminName: 'Demo Admin', messageCount: 1,
})]) {
  const queries: URLSearchParams[] = []
  let cats = [...categories]
  const sent: { method: string; body?: unknown; id?: string }[] = []
  server.use(
    http.get('/api/profile', () => HttpResponse.json(profileFor('Admin'))),
    http.get('/api/admin/support', ({ request }) => {
      const q = new URL(request.url).searchParams
      queries.push(q)
      const hits = items.filter(
        (i) =>
          (!q.get('status') || i.status === q.get('status')) &&
          (!q.get('categoryId') || i.categoryId === Number(q.get('categoryId'))) &&
          (q.get('assigned') !== 'Unassigned' || i.assignedAdminId === null) &&
          (!q.get('search') || i.subject.toLowerCase().includes(q.get('search')!.toLowerCase())),
      )
      return HttpResponse.json(paged(hits))
    }),
    http.get('/api/admin/support/categories', () => HttpResponse.json(cats)),
    http.post('/api/admin/support/categories', async ({ request }) => {
      const body = (await request.json()) as { name: string }
      sent.push({ method: 'POST', body })
      const created = { id: 9, name: body.name, requestCount: 0 }
      cats = [...cats, created]
      return HttpResponse.json(created, { status: 201 })
    }),
    http.put('/api/admin/support/categories/:id', async ({ request, params }) => {
      const body = (await request.json()) as { name: string }
      sent.push({ method: 'PUT', id: String(params.id), body })
      cats = cats.map((c) => (c.id === Number(params.id) ? { ...c, name: body.name } : c))
      return HttpResponse.json(cats.find((c) => c.id === Number(params.id)))
    }),
    http.delete('/api/admin/support/categories/:id', ({ params }) => {
      sent.push({ method: 'DELETE', id: String(params.id) })
      cats = cats.filter((c) => c.id !== Number(params.id))
      return new HttpResponse(null, { status: 204 })
    }),
  )
  return { queries, sent }
}

describe('AdminSupportPage — inbox', () => {
  it('opens on "Needs reply" (open and unassigned) with its count on the Requests tab', async () => {
    const { queries } = mockInbox()
    renderAt('/admin/support', { as: 'Admin' })

    const first = (await screen.findByRole('link', { name: 'Cannot add CSC 310' })).closest('tr')!
    expect(screen.queryByRole('link', { name: 'Lab fee charged twice' })).not.toBeInTheDocument()
    expect(queries[0]?.get('status')).toBe('Open')
    expect(queries[0]?.get('assigned')).toBe('Unassigned')
    // One request for the list and the tab count.
    expect(queries).toHaveLength(1)
    expect(screen.getByRole('tab', { name: 'Requests, 1 need a reply' })).toBeInTheDocument()
    expect(screen.getByLabelText('Status')).toHaveValue('Open')
    // How long the student has been waiting, from the last activity.
    expect(within(first).getByText(/^waiting \d+(m|h|d)$/)).toBeInTheDocument()
  })

  it('lists requests with student, status and assignee', async () => {
    mockInbox()
    const { user } = renderAt('/admin/support', { as: 'Admin' })
    await user.click(await screen.findByRole('button', { name: 'Show all requests' }))

    const first = (await screen.findByRole('link', { name: 'Cannot add CSC 310' })).closest('tr')!
    expect(first.querySelector('a')).toHaveAttribute('href', '/admin/support/1')
    expect(within(first).getByText('Demo Student')).toBeInTheDocument()
    expect(within(first).getByText('3001')).toBeInTheDocument()
    expect(within(first).getByText('Open')).toBeInTheDocument()
    expect(within(first).getByText('Unassigned')).toBeInTheDocument()

    const second = (await screen.findByRole('link', { name: 'Lab fee charged twice' })).closest('tr')!
    expect(within(second).getByText('In progress')).toBeInTheDocument()
    expect(within(second).getByText('Demo Admin')).toBeInTheDocument()
  })

  it('filters by status, category, assignee and search', async () => {
    const { queries } = mockInbox()
    const { user } = renderAt('/admin/support', { as: 'Admin' })
    await screen.findByRole('link', { name: 'Cannot add CSC 310' })
    await user.click(screen.getByRole('button', { name: 'Show all requests' }))

    await user.selectOptions(screen.getByLabelText('Status'), 'In progress')
    await waitFor(() => expect(screen.queryByRole('link', { name: 'Cannot add CSC 310' })).not.toBeInTheDocument())
    expect(queries.at(-1)?.get('status')).toBe('InProgress')

    await user.selectOptions(screen.getByLabelText('Status'), 'All statuses')
    await user.selectOptions(await screen.findByLabelText('Category'), 'Finance')
    await waitFor(() => expect(queries.at(-1)?.get('categoryId')).toBe('2'))

    await user.selectOptions(screen.getByLabelText('Category'), 'All categories')
    await user.selectOptions(screen.getByLabelText('Assigned'), 'Assigned to me')
    await waitFor(() => expect(queries.at(-1)?.get('assigned')).toBe('Me'))

    await user.selectOptions(screen.getByLabelText('Assigned'), 'Anyone')
    await user.type(screen.getByLabelText('Search'), 'lab fee')
    await waitFor(() => expect(queries.at(-1)?.get('search')).toBe('lab fee'))
    expect(await screen.findByRole('link', { name: 'Lab fee charged twice' })).toBeInTheDocument()
    await waitFor(() => expect(screen.queryByRole('link', { name: 'Cannot add CSC 310' })).not.toBeInTheDocument())
  })

  it('shows an empty inbox', async () => {
    mockInbox([])
    const { user } = renderAt('/admin/support', { as: 'Admin' })
    expect(await screen.findByText('Nothing needs a reply')).toBeInTheDocument()
    await user.click(screen.getAllByRole('button', { name: 'Show all requests' })[0]!)
    expect(await screen.findByText('The inbox is empty')).toBeInTheDocument()
  })

  it('offers "Clear filters" when nothing matches the filters', async () => {
    mockInbox()
    const { user } = renderAt('/admin/support', { as: 'Admin' })
    await screen.findByRole('link', { name: 'Cannot add CSC 310' })

    await user.type(screen.getByLabelText('Search'), 'zzz')
    expect(await screen.findByText('No requests match')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Clear filters' }))

    expect(await screen.findByRole('link', { name: 'Lab fee charged twice' })).toBeInTheDocument()
    expect(screen.getByLabelText('Search')).toHaveValue('')
    expect(screen.getByLabelText('Status')).toHaveValue('')
  })

  it('shows an error with a retry button when the list fails', async () => {
    mockInbox()
    let calls = 0
    server.use(
      http.get('/api/admin/support', () => {
        calls++
        return calls === 1 ? HttpResponse.json({ title: 'Something went wrong.' }, { status: 500 }) : HttpResponse.json(paged([row(1, 'Cannot add CSC 310')]))
      }),
    )
    const { user } = renderAt('/admin/support', { as: 'Admin' })
    expect(await screen.findByText('Something went wrong on our side. Please try again in a moment.')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Try again' }))
    expect(await screen.findByRole('link', { name: 'Cannot add CSC 310' })).toBeInTheDocument()
  })
})

describe('AdminSupportPage — categories', () => {
  async function openCategories() {
    const rendered = renderAt('/admin/support', { as: 'Admin' })
    await rendered.user.click(await screen.findByRole('tab', { name: 'Categories' }))
    await screen.findByRole('list', { name: 'Support categories' })
    return rendered
  }

  it('lists categories with request counts and adds one', async () => {
    const { sent } = mockInbox()
    const { user } = await openCategories()
    const list = screen.getByRole('list', { name: 'Support categories' })
    expect(within(within(list).getByText('Registration').closest('li')!).getByText('2 requests')).toBeInTheDocument()

    await user.type(screen.getByLabelText('New category'), ' Housing ')
    await user.click(screen.getByRole('button', { name: 'Add' }))

    expect(await within(list).findByText('Housing')).toBeInTheDocument()
    expect(sent).toEqual([{ method: 'POST', body: { name: 'Housing' } }])
    expect(screen.getByText('“Housing” added.')).toBeInTheDocument()
    expect(screen.getByLabelText('New category')).toHaveValue('')
  })

  it('asks for a name before adding', async () => {
    const { sent } = mockInbox()
    const { user } = await openCategories()
    await user.click(screen.getByRole('button', { name: 'Add' }))
    expect(await screen.findByText('Enter a name.')).toBeInTheDocument()
    expect(sent).toHaveLength(0)
  })

  it('shows a duplicate name (409) under the field', async () => {
    mockInbox()
    server.use(
      http.post('/api/admin/support/categories', () =>
        HttpResponse.json({ status: 409, title: 'A category named "finance" already exists.', code: 'duplicate_category' }, { status: 409 }),
      ),
    )
    const { user } = await openCategories()
    await user.type(screen.getByLabelText('New category'), 'finance')
    await user.click(screen.getByRole('button', { name: 'Add' }))
    expect(await screen.findByText('A category named "finance" already exists.')).toBeInTheDocument()
    expect(screen.getByLabelText('New category')).toHaveAttribute('aria-invalid', 'true')
  })

  it('renames a category', async () => {
    const { sent } = mockInbox()
    const { user } = await openCategories()
    await user.click(screen.getByRole('button', { name: 'Rename Other' }))
    const dialog = screen.getByRole('dialog', { name: 'Rename category' })
    await user.clear(within(dialog).getByLabelText('Name'))
    await user.type(within(dialog).getByLabelText('Name'), 'General')
    await user.click(within(dialog).getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(sent).toEqual([{ method: 'PUT', id: '5', body: { name: 'General' } }])
    expect(await screen.findByText('Renamed to “General”.')).toBeInTheDocument()
  })

  it('deletes an unused category after confirming', async () => {
    const { sent } = mockInbox()
    const { user } = await openCategories()
    await user.click(screen.getByRole('button', { name: 'Delete Other' }))
    await user.click(within(screen.getByRole('dialog', { name: 'Delete category?' })).getByRole('button', { name: 'Delete' }))

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(sent).toEqual([{ method: 'DELETE', id: '5' }])
    await waitFor(() => expect(within(screen.getByRole('list', { name: 'Support categories' })).queryByText('Other')).not.toBeInTheDocument())
  })

  it('shows why a category in use can’t be deleted (409)', async () => {
    mockInbox()
    server.use(
      http.delete('/api/admin/support/categories/:id', () =>
        HttpResponse.json(
          { status: 409, title: '"Registration" is used by 2 support requests, so it can\'t be deleted. Rename it instead.', code: 'category_in_use' },
          { status: 409 },
        ),
      ),
    )
    const { user } = await openCategories()
    await user.click(screen.getByRole('button', { name: 'Delete Registration' }))
    const dialog = screen.getByRole('dialog', { name: 'Delete category?' })
    await user.click(within(dialog).getByRole('button', { name: 'Delete' }))
    expect(await within(dialog).findByRole('alert')).toHaveTextContent('is used by 2 support requests')
  })

  it('shows an error with a retry when categories fail to load', async () => {
    mockInbox()
    server.use(http.get('/api/admin/support/categories', () => HttpResponse.json({ title: 'Could not load.' }, { status: 500 })))
    const { user } = renderAt('/admin/support', { as: 'Admin' })
    await user.click(await screen.findByRole('tab', { name: 'Categories' }))
    expect(await screen.findByText('Something went wrong on our side. Please try again in a moment.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })
})
