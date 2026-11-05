import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, waitFor, within } from '@testing-library/react'
import { server } from '@/test/server'
import { profileFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'
import type { SaveTermRequest, Term } from '../types'

// PB-02 / UC-16 / SD-16 step 1: admin creates and edits terms. Tests run with TZ=UTC (vite.config.ts).

const fall: Term = {
  id: 1,
  name: 'Fall 2026',
  startDate: '2026-09-01',
  endDate: '2026-12-20',
  enrollStart: '2026-08-15T08:00:00Z',
  enrollEnd: '2026-08-30T20:00:00Z',
  addDropStart: '2026-09-01T08:00:00Z',
  addDropEnd: '2026-09-10T20:00:00Z',
  isCurrent: true,
  currentWindow: 'AddDrop',
}
const spring: Term = { ...fall, id: 2, name: 'Spring 2027', startDate: '2099-01-20', endDate: '2099-05-30', isCurrent: false, currentWindow: 'Closed' }

function mockTerms(initial: Term[]) {
  let terms = [...initial]
  const sent: { method: string; id?: string; body: SaveTermRequest }[] = []
  server.use(
    http.get('/api/profile', () => HttpResponse.json(profileFor('Admin'))),
    http.get('/api/admin/terms', () => HttpResponse.json(terms)),
    http.post('/api/admin/terms', async ({ request }) => {
      const body = (await request.json()) as SaveTermRequest
      sent.push({ method: 'POST', body })
      const created: Term = { ...body, id: 99, currentWindow: 'Closed' }
      terms = [created, ...terms]
      return HttpResponse.json(created, { status: 201 })
    }),
    http.put('/api/admin/terms/:id', async ({ request, params }) => {
      const body = (await request.json()) as SaveTermRequest
      sent.push({ method: 'PUT', id: String(params.id), body })
      terms = terms.map((t) => (t.id === Number(params.id) ? { ...t, ...body } : t))
      return HttpResponse.json(terms.find((t) => t.id === Number(params.id)))
    }),
  )
  return sent
}

describe('TermsPage', () => {
  it('lists terms with dates, windows and status', async () => {
    mockTerms([fall, spring])
    renderAt('/admin/terms', { as: 'Admin' })

    const row = (await screen.findByRole('rowheader', { name: 'Fall 2026' })).closest('tr')!
    expect(within(row).getByText('01 Sept 2026 – 20 Dec 2026')).toBeInTheDocument()
    expect(within(row).getByText('Current')).toBeInTheDocument()
    expect(within(row).getByText('Add/drop open')).toBeInTheDocument()

    const springRow = screen.getByRole('rowheader', { name: 'Spring 2027' }).closest('tr')!
    expect(within(springRow).getByText('Upcoming')).toBeInTheDocument()
  })

  it('shows an empty state with a way to create the first term', async () => {
    mockTerms([])
    renderAt('/admin/terms', { as: 'Admin' })

    expect(await screen.findByText('No terms yet')).toBeInTheDocument()
    expect(screen.getAllByRole('button', { name: /New term/ })).toHaveLength(2)
  })

  it('shows an error with a retry button when the list fails', async () => {
    let calls = 0
    server.use(
      http.get('/api/profile', () => HttpResponse.json(profileFor('Admin'))),
      http.get('/api/admin/terms', () => {
        calls++
        return calls === 1 ? HttpResponse.json({ title: 'Something went wrong.' }, { status: 500 }) : HttpResponse.json([fall])
      }),
    )
    const { user } = renderAt('/admin/terms', { as: 'Admin' })

    expect(await screen.findByText('Something went wrong.')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Try again' }))
    expect(await screen.findByRole('rowheader', { name: 'Fall 2026' })).toBeInTheDocument()
  })

  it('creates a term, sending window times as UTC, and shows it in the list', async () => {
    const sent = mockTerms([fall])
    const { user } = renderAt('/admin/terms', { as: 'Admin' })
    await user.click(await screen.findByRole('button', { name: /New term/ }))
    const dialog = screen.getByRole('dialog', { name: 'New term' })

    await user.type(within(dialog).getByLabelText('Name'), '  Spring 2028 ')
    await user.type(within(dialog).getByLabelText('First day'), '2028-01-20')
    await user.type(within(dialog).getByLabelText('Last day'), '2028-05-30')
    await user.type(within(dialog).getByLabelText('Enrollment opens'), '2028-01-05T08:00')
    await user.type(within(dialog).getByLabelText('Enrollment closes'), '2028-01-15T20:00')
    await user.type(within(dialog).getByLabelText('Add/drop opens'), '2028-01-20T08:00')
    await user.type(within(dialog).getByLabelText('Add/drop closes'), '2028-01-30T20:00')
    await user.click(within(dialog).getByRole('button', { name: 'Create term' }))

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(sent).toEqual([
      {
        method: 'POST',
        body: {
          name: 'Spring 2028',
          startDate: '2028-01-20',
          endDate: '2028-05-30',
          enrollStart: '2028-01-05T08:00:00.000Z',
          enrollEnd: '2028-01-15T20:00:00.000Z',
          addDropStart: '2028-01-20T08:00:00.000Z',
          addDropEnd: '2028-01-30T20:00:00.000Z',
          isCurrent: false,
        },
      },
    ])
    expect(await screen.findByRole('rowheader', { name: 'Spring 2028' })).toBeInTheDocument()
    expect(screen.getByText('Spring 2028 saved.')).toBeInTheDocument()
  })

  it('checks the window order before sending (same rules as the API)', async () => {
    const sent = mockTerms([fall])
    const { user } = renderAt('/admin/terms', { as: 'Admin' })
    await user.click(await screen.findByRole('button', { name: /New term/ }))
    const dialog = screen.getByRole('dialog', { name: 'New term' })

    await user.type(within(dialog).getByLabelText('Name'), 'Bad windows')
    await user.type(within(dialog).getByLabelText('First day'), '2028-01-20')
    await user.type(within(dialog).getByLabelText('Last day'), '2028-01-10')
    await user.type(within(dialog).getByLabelText('Enrollment opens'), '2028-01-05T08:00')
    await user.type(within(dialog).getByLabelText('Enrollment closes'), '2028-01-15T20:00')
    await user.type(within(dialog).getByLabelText('Add/drop opens'), '2028-01-14T08:00')
    await user.type(within(dialog).getByLabelText('Add/drop closes'), '2028-02-28T20:00')
    await user.click(within(dialog).getByRole('button', { name: 'Create term' }))

    expect(await within(dialog).findByText('The term must end after it starts.')).toBeInTheDocument()
    expect(within(dialog).getByText("Add/drop can't open before the enrollment window closes.")).toBeInTheDocument()
    expect(sent).toHaveLength(0)
  })

  it('shows a duplicate name from the API under the name field', async () => {
    mockTerms([fall])
    server.use(
      http.post('/api/admin/terms', () =>
        HttpResponse.json({ status: 409, title: "A term named 'Fall 2026' already exists.", code: 'duplicate_term_name' }, { status: 409 }),
      ),
    )
    const { user } = renderAt('/admin/terms', { as: 'Admin' })
    await user.click(await screen.findByRole('button', { name: /New term/ }))
    const dialog = screen.getByRole('dialog', { name: 'New term' })

    await user.type(within(dialog).getByLabelText('Name'), 'Fall 2026')
    await user.type(within(dialog).getByLabelText('First day'), '2026-09-01')
    await user.type(within(dialog).getByLabelText('Last day'), '2026-12-20')
    await user.type(within(dialog).getByLabelText('Enrollment opens'), '2026-08-15T08:00')
    await user.type(within(dialog).getByLabelText('Enrollment closes'), '2026-08-30T20:00')
    await user.type(within(dialog).getByLabelText('Add/drop opens'), '2026-09-01T08:00')
    await user.type(within(dialog).getByLabelText('Add/drop closes'), '2026-09-10T20:00')
    await user.click(within(dialog).getByRole('button', { name: 'Create term' }))

    const name = within(dialog).getByLabelText('Name')
    await waitFor(() => expect(name).toHaveAttribute('aria-invalid', 'true'))
    expect(within(dialog).getByText("A term named 'Fall 2026' already exists.")).toBeInTheDocument()
    expect(screen.getByRole('dialog')).toBeInTheDocument()
  })

  it('edits a term with its current values filled in', async () => {
    const sent = mockTerms([fall, spring])
    const { user } = renderAt('/admin/terms', { as: 'Admin' })

    await user.click(await screen.findByRole('button', { name: 'Edit Spring 2027' }))
    const dialog = screen.getByRole('dialog', { name: 'Edit Spring 2027' })
    expect(within(dialog).getByLabelText('First day')).toHaveValue('2099-01-20')
    expect(within(dialog).getByLabelText('Enrollment opens')).toHaveValue('2026-08-15T08:00')

    await user.click(within(dialog).getByLabelText('Current term'))
    await user.click(within(dialog).getByRole('button', { name: 'Save changes' }))

    await waitFor(() => expect(sent).toHaveLength(1))
    expect(sent[0]).toMatchObject({ method: 'PUT', id: '2', body: { name: 'Spring 2027', isCurrent: true } })
  })

  it('closes the dialog with Escape without saving', async () => {
    const sent = mockTerms([fall])
    const { user } = renderAt('/admin/terms', { as: 'Admin' })
    await user.click(await screen.findByRole('button', { name: /New term/ }))

    await user.keyboard('{Escape}')

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(sent).toHaveLength(0)
  })
})
