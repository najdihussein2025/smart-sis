import { act } from 'react'
import { describe, expect, it, vi } from 'vitest'
import { http, HttpResponse } from 'msw'
import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import type { UserEvent } from '@testing-library/user-event'
import { server } from '@/test/server'
import { profileFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'
import type { SaveTermRequest, Term } from '../types'
import { SAVED_MESSAGE_MS } from './TermsPage'

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

/** Dates are typed DD/MM/YYYY and checked when the field loses focus. */
async function typeDate(user: UserEvent, scope: HTMLElement, label: string, dmy: string) {
  const input = within(scope).getByLabelText(label)
  await user.clear(input)
  await user.type(input, dmy)
  await user.tab()
}

async function setDateTime(user: UserEvent, scope: HTMLElement, label: string, dmy: string, time: string) {
  await typeDate(user, scope, `${label} date`, dmy)
  await user.selectOptions(within(scope).getByLabelText(`${label} time`), time)
}

async function fillValidTerm(user: UserEvent, dialog: HTMLElement, name: string) {
  await user.type(within(dialog).getByLabelText('Name'), name)
  await typeDate(user, dialog, 'First day', '20/01/2028')
  await typeDate(user, dialog, 'Last day', '30/05/2028')
  await setDateTime(user, dialog, 'Enrollment opens', '05/01/2028', '08:00')
  await setDateTime(user, dialog, 'Enrollment closes', '15/01/2028', '20:00')
  await setDateTime(user, dialog, 'Add/drop opens', '20/01/2028', '08:00')
  await setDateTime(user, dialog, 'Add/drop closes', '30/01/2028', '20:00')
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

    // The table scrolls inside a positioned box: otherwise the sr-only "Actions" header escaped it and the whole
    // page scrolled sideways at 375 px.
    expect(screen.getByRole('table').parentElement).toHaveClass('relative', 'overflow-x-auto')
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

    await fillValidTerm(user, dialog, '  Spring 2028 ')
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

  it('refuses a last day that is not after the first day, and keeps the field empty', async () => {
    mockTerms([fall])
    const { user } = renderAt('/admin/terms', { as: 'Admin' })
    await user.click(await screen.findByRole('button', { name: /New term/ }))
    const dialog = screen.getByRole('dialog', { name: 'New term' })

    await typeDate(user, dialog, 'First day', '20/01/2028')
    await typeDate(user, dialog, 'Last day', '20/01/2028')

    expect(within(dialog).getByText('Choose 21/01/2028 or later.')).toBeInTheDocument()
    expect(within(dialog).getByLabelText('Last day')).toHaveValue('')
  })

  it('greys out earlier days in the calendar and fills the field in DD/MM/YYYY', async () => {
    mockTerms([fall])
    const { user } = renderAt('/admin/terms', { as: 'Admin' })
    await user.click(await screen.findByRole('button', { name: /New term/ }))
    const dialog = screen.getByRole('dialog', { name: 'New term' })
    await typeDate(user, dialog, 'First day', '20/01/2028')

    await user.click(within(dialog).getByRole('button', { name: 'Open calendar for Last day' }))
    const calendar = within(dialog).getByRole('dialog', { name: 'Choose Last day' })
    expect(within(calendar).getByRole('button', { name: /January 20th, 2028/ })).toBeDisabled()
    await user.click(within(calendar).getByRole('button', { name: /January 21st, 2028/ }))

    expect(within(dialog).getByLabelText('Last day')).toHaveValue('21/01/2028')
    expect(within(dialog).queryByRole('dialog', { name: 'Choose Last day' })).not.toBeInTheDocument()
  })

  it('refuses a date that is not a real DD/MM/YYYY date', async () => {
    mockTerms([fall])
    const { user } = renderAt('/admin/terms', { as: 'Admin' })
    await user.click(await screen.findByRole('button', { name: /New term/ }))
    const dialog = screen.getByRole('dialog', { name: 'New term' })

    await typeDate(user, dialog, 'First day', '31/02/2028')

    expect(within(dialog).getByText('Use DD/MM/YYYY, e.g. 20/01/2028.')).toBeInTheDocument()
    expect(within(dialog).getByLabelText('First day')).toHaveValue('')
  })

  it('blocks add/drop from opening before enrollment closes (days and times)', async () => {
    mockTerms([fall])
    const { user } = renderAt('/admin/terms', { as: 'Admin' })
    await user.click(await screen.findByRole('button', { name: /New term/ }))
    const dialog = screen.getByRole('dialog', { name: 'New term' })
    await setDateTime(user, dialog, 'Enrollment opens', '05/01/2028', '08:00')
    await setDateTime(user, dialog, 'Enrollment closes', '15/01/2028', '20:00')

    // An earlier day is refused…
    await typeDate(user, dialog, 'Add/drop opens date', '14/01/2028')
    expect(within(dialog).getByText('Choose 15/01/2028 or later.')).toBeInTheDocument()
    expect(within(dialog).getByLabelText('Add/drop opens date')).toHaveValue('')

    // …and on the same day, earlier times can't be picked. Add/drop may open exactly when enrollment closes.
    await typeDate(user, dialog, 'Add/drop opens date', '15/01/2028')
    const time = within(dialog).getByLabelText('Add/drop opens time')
    expect(time).toHaveValue('20:00')
    expect(within(time).getByRole('option', { name: '19:45' })).toBeDisabled()
    expect(within(time).getByRole('option', { name: '20:00' })).toBeEnabled()

    // A window must end after it starts, so the same time isn't allowed for "closes".
    const closes = within(dialog).getByLabelText('Enrollment closes time')
    expect(within(closes).getByRole('option', { name: '08:00' })).toBeEnabled() // different day
    await typeDate(user, dialog, 'Enrollment closes date', '05/01/2028')
    expect(within(closes).getByRole('option', { name: '08:00' })).toBeDisabled()
    expect(within(closes).getByRole('option', { name: '08:15' })).toBeEnabled()
  })

  it('flags a later date straight away when an earlier one moves past it', async () => {
    mockTerms([fall])
    const { user } = renderAt('/admin/terms', { as: 'Admin' })
    await user.click(await screen.findByRole('button', { name: /New term/ }))
    const dialog = screen.getByRole('dialog', { name: 'New term' })
    await typeDate(user, dialog, 'First day', '20/01/2028')
    await typeDate(user, dialog, 'Last day', '30/05/2028')

    await typeDate(user, dialog, 'First day', '01/06/2028')

    expect(await within(dialog).findByText('The term must end after it starts.')).toBeInTheDocument()
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

    await fillValidTerm(user, dialog, 'Fall 2026')
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
    expect(within(dialog).getByLabelText('First day')).toHaveValue('20/01/2099')
    expect(within(dialog).getByLabelText('Enrollment opens date')).toHaveValue('15/08/2026')
    expect(within(dialog).getByLabelText('Enrollment opens time')).toHaveValue('08:00')

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

  it('closes only the calendar with Escape, keeping the dialog open', async () => {
    mockTerms([fall])
    const { user } = renderAt('/admin/terms', { as: 'Admin' })
    await user.click(await screen.findByRole('button', { name: /New term/ }))
    const dialog = screen.getByRole('dialog', { name: 'New term' })

    await user.click(within(dialog).getByRole('button', { name: 'Open calendar for First day' }))
    expect(within(dialog).getByRole('dialog', { name: 'Choose First day' })).toBeInTheDocument()
    await user.keyboard('{Escape}')

    expect(within(dialog).queryByRole('dialog', { name: 'Choose First day' })).not.toBeInTheDocument()
    expect(screen.getByRole('dialog', { name: 'New term' })).toBeInTheDocument()
  })

  it('keeps the calendar inside the screen when the field is near the bottom of a short screen', async () => {
    mockTerms([fall])
    const { user } = renderAt('/admin/terms', { as: 'Admin' })
    await user.click(await screen.findByRole('button', { name: /New term/ }))
    const dialog = screen.getByRole('dialog', { name: 'New term' })
    vi.spyOn(window, 'innerHeight', 'get').mockReturnValue(400)
    // The field sits at 300–344 px: no room for the calendar (360 px) below or above it.
    vi.spyOn(HTMLElement.prototype, 'getBoundingClientRect').mockReturnValue(
      { top: 300, bottom: 344, left: 16, right: 300, width: 284, height: 44, x: 16, y: 300, toJSON: () => ({}) } as DOMRect,
    )

    await user.click(within(dialog).getByRole('button', { name: 'Open calendar for Add/drop closes date' }))

    const calendar = within(dialog).getByRole('dialog', { name: 'Choose Add/drop closes date' })
    // 400 − 360 − 8: the whole calendar fits on screen (it used to open below the field and run off the bottom).
    expect(calendar.style.top).toBe('32px')
  })

  it('places the calendar before it appears, so it never pushes the dialog content and closes itself', async () => {
    mockTerms([fall])
    const { user } = renderAt('/admin/terms', { as: 'Admin' })
    await user.click(await screen.findByRole('button', { name: /New term/ }))
    const dialog = screen.getByRole('dialog', { name: 'New term' })

    // Unplaced, the calendar sat in the dialog's flow for a moment, scrolled the dialog on small screens, and that
    // scroll closed it again (seen at 375 px wide). It must enter the page already fixed, not be moved afterwards.
    const moved: string[] = []
    const observer = new MutationObserver((records) => {
      for (const r of records)
        if (r.target instanceof HTMLElement && r.target.getAttribute('role') === 'dialog' && r.target !== dialog)
          moved.push(r.oldValue ?? '(no style)')
    })
    observer.observe(dialog, { subtree: true, attributes: true, attributeFilter: ['style'], attributeOldValue: true })
    await user.click(within(dialog).getByRole('button', { name: 'Open calendar for Add/drop closes date' }))
    observer.disconnect()

    expect(moved).toEqual([])
    expect(within(dialog).getByRole('dialog', { name: 'Choose Add/drop closes date' })).toHaveStyle({ position: 'fixed' })
  })

  it('keeps the calendar open and next to its field while the dialog scrolls, and closes it once the field is off screen', async () => {
    mockTerms([fall])
    const { user } = renderAt('/admin/terms', { as: 'Admin' })
    await user.click(await screen.findByRole('button', { name: /New term/ }))
    const dialog = screen.getByRole('dialog', { name: 'New term' })
    vi.spyOn(window, 'innerHeight', 'get').mockReturnValue(667)
    const rect = (top: number) => ({ top, bottom: top + 44, left: 16, right: 300, width: 284, height: 44, x: 16, y: top, toJSON: () => ({}) }) as DOMRect
    const fieldRect = vi.spyOn(HTMLElement.prototype, 'getBoundingClientRect').mockReturnValue(rect(100))
    await user.click(within(dialog).getByRole('button', { name: 'Open calendar for First day' }))
    const calendar = within(dialog).getByRole('dialog', { name: 'Choose First day' })
    expect(calendar.style.top).toBe('148px')

    // A scroll that is still running when the calendar opens (a phone's momentum scroll) used to close it at once.
    fieldRect.mockReturnValue(rect(60))
    fireEvent.scroll(dialog.querySelector('.overflow-y-auto')!)
    expect(calendar).toBeInTheDocument()
    expect(calendar.style.top).toBe('108px')

    fieldRect.mockReturnValue(rect(-100))
    fireEvent.scroll(dialog.querySelector('.overflow-y-auto')!)
    expect(within(dialog).queryByRole('dialog', { name: 'Choose First day' })).not.toBeInTheDocument()
  })

  it('hides the "saved" message after 5 seconds when a term is created', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true })
    try {
      mockTerms([fall])
      const { user } = renderAt('/admin/terms', { as: 'Admin' })
      await user.click(await screen.findByRole('button', { name: /New term/ }))
      const dialog = screen.getByRole('dialog', { name: 'New term' })
      await fillValidTerm(user, dialog, 'Spring 2028')
      await user.click(within(dialog).getByRole('button', { name: 'Create term' }))
      expect(await screen.findByText('Spring 2028 saved.')).toBeInTheDocument()

      act(() => vi.advanceTimersByTime(SAVED_MESSAGE_MS + 100))
      expect(screen.queryByText('Spring 2028 saved.')).not.toBeInTheDocument()
    } finally {
      vi.useRealTimers()
    }
  })

  it('hides the "saved" message after 5 seconds when a term is edited', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true })
    try {
      mockTerms([fall, spring])
      const { user } = renderAt('/admin/terms', { as: 'Admin' })
      await user.click(await screen.findByRole('button', { name: 'Edit Spring 2027' }))
      await user.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Save changes' }))
      expect(await screen.findByText('Spring 2027 saved.')).toBeInTheDocument()

      act(() => vi.advanceTimersByTime(SAVED_MESSAGE_MS - 500))
      expect(screen.getByText('Spring 2027 saved.')).toBeInTheDocument()
      act(() => vi.advanceTimersByTime(600))
      expect(screen.queryByText('Spring 2027 saved.')).not.toBeInTheDocument()
    } finally {
      vi.useRealTimers()
    }
  })
})
