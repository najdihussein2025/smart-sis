import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, waitFor, within } from '@testing-library/react'
import type { UserEvent } from '@testing-library/user-event'
import { server } from '@/test/server'
import { paged, profileFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'
import type { AdminCharge, ChargeBatch, CreateChargeBatchRequest, FinanceOptions, RecordPaymentRequest } from '../types'

// PB-18 / T58: admin charges a major, lists charges and records full or partial payments. Simulated data (SR-23).

const options: FinanceOptions = {
  terms: [
    { id: 2, name: 'Fall 2026', isCurrent: true },
    { id: 1, name: 'Spring 2026', isCurrent: false },
  ],
  majors: [
    { id: 1, name: 'Computer Science', activeStudents: 6 },
    { id: 2, name: 'Mathematics', activeStudents: 0 },
  ],
  paymentTypes: [
    { id: 3, name: 'Lab fee' },
    { id: 2, name: 'Registration' },
    { id: 1, name: 'Tuition' },
  ],
}

const charge = (id: number, extra: Partial<AdminCharge> = {}): AdminCharge => ({
  id, studentId: 10 + id, universityId: 3000 + id, studentName: `Demo Student ${id}`, majorName: 'Computer Science', batchId: 1,
  termId: 2, termName: 'Fall 2026', paymentTypeName: 'Tuition', title: 'Fall 2026 tuition',
  amount: 4500, paid: 2000, balance: 2500, dueDate: '2026-12-01', status: 'Partial', isOverdue: false,
  payments: [{ id: 1, amount: 2000, paidAt: '2026-11-01T10:00:00Z', note: 'Simulated receipt 0001' }],
  ...extra,
})
const partial = charge(1)
const overdue = charge(2, { title: 'CSC 210 lab fee', paymentTypeName: 'Lab fee', amount: 150, paid: 0, balance: 150, status: 'Pending', isOverdue: true, batchId: null, payments: [], dueDate: '2026-11-01' })
const paid = charge(3, { paid: 4500, balance: 0, status: 'Paid' })

const batch: ChargeBatch = {
  id: 1, title: 'Fall 2026 tuition', majorId: 1, majorName: 'Computer Science', termId: 2, termName: 'Fall 2026',
  paymentTypeId: 1, paymentTypeName: 'Tuition', amount: 4500, dueDate: '2026-12-01', chargeCount: 6, paidCount: 1,
  createdAt: '2026-11-01T09:00:00Z',
}

function mock({ charges = [partial, overdue, paid], batches = [batch] }: { charges?: AdminCharge[]; batches?: ChargeBatch[] } = {}) {
  const queries: URLSearchParams[] = []
  const payments: { id: string; body: RecordPaymentRequest }[] = []
  const created: CreateChargeBatchRequest[] = []
  server.use(
    http.get('/api/profile', () => HttpResponse.json(profileFor('Admin'))),
    http.get('/api/admin/finance/options', () => HttpResponse.json(options)),
    http.get('/api/admin/finance/charges', ({ request }) => {
      const q = new URL(request.url).searchParams
      queries.push(q)
      const hits = charges.filter((c) => (!q.get('status') || c.status === q.get('status')) && (!q.get('batchId') || c.batchId === Number(q.get('batchId'))))
      return HttpResponse.json(paged(hits))
    }),
    http.get('/api/admin/finance/batches', () => HttpResponse.json(paged(batches))),
    http.post('/api/admin/finance/charges/:id/payments', async ({ request, params }) => {
      const body = (await request.json()) as RecordPaymentRequest
      payments.push({ id: String(params.id), body })
      const c = charges.find((x) => x.id === Number(params.id))!
      const nowPaid = c.paid + body.amount
      return HttpResponse.json({ ...c, paid: nowPaid, balance: c.amount - nowPaid, status: nowPaid >= c.amount ? 'Paid' : 'Partial' })
    }),
    http.post('/api/admin/finance/batches', async ({ request }) => {
      const body = (await request.json()) as CreateChargeBatchRequest
      created.push(body)
      return HttpResponse.json({ ...batch, id: 9, title: body.title, chargeCount: 6, paidCount: 0 }, { status: 201 })
    }),
  )
  return { queries, payments, created }
}

const rowOf = (name: string) => screen.getByRole('rowheader', { name: new RegExp(name) }).closest('tr')!

async function openPayment(user: UserEvent, studentName: string, title: string) {
  await user.click(await screen.findByRole('button', { name: `Record payment for ${studentName}, ${title}` }))
  return screen.getByRole('dialog', { name: 'Record payment' })
}

async function typeDate(user: UserEvent, scope: HTMLElement, label: string, dmy: string) {
  const input = within(scope).getByLabelText(label)
  await user.clear(input)
  await user.type(input, dmy)
  await user.tab()
}

describe('AdminPaymentsPage', () => {
  it('lists charges with status, balance and overdue; paid charges have no payment button', async () => {
    mock()
    renderAt('/admin/payments', { as: 'Admin' })

    await screen.findByRole('rowheader', { name: /Demo Student 1/ })
    expect(within(rowOf('Demo Student 1')).getByText('Partially paid')).toBeInTheDocument()
    expect(within(rowOf('Demo Student 1')).getByText('$2,500.00')).toBeInTheDocument()
    expect(within(rowOf('Demo Student 2')).getByText('Overdue')).toBeInTheDocument()
    expect(within(rowOf('Demo Student 2')).getByText('Pending')).toBeInTheDocument()
    expect(within(rowOf('Demo Student 3')).getByText('Paid')).toBeInTheDocument()
    expect(within(rowOf('Demo Student 3')).queryByRole('button')).not.toBeInTheDocument()
  })

  it('filters by status', async () => {
    const { queries } = mock()
    const { user } = renderAt('/admin/payments', { as: 'Admin' })
    await screen.findByRole('rowheader', { name: /Demo Student 1/ })

    await user.selectOptions(screen.getByLabelText('Status'), 'Paid')
    await waitFor(() => expect(screen.queryByRole('rowheader', { name: /Demo Student 1/ })).not.toBeInTheDocument())
    expect(queries.at(-1)?.get('status')).toBe('Paid')
    expect(screen.getByRole('rowheader', { name: /Demo Student 3/ })).toBeInTheDocument()
  })

  it('records a partial payment and shows the remaining balance', async () => {
    const { payments } = mock()
    const { user } = renderAt('/admin/payments', { as: 'Admin' })

    const dialog = await openPayment(user, 'Demo Student 1', 'Fall 2026 tuition')
    expect(within(dialog).getByText('Remaining balance')).toBeInTheDocument()
    expect(within(dialog).getByText('$2,500.00')).toBeInTheDocument()
    await user.type(within(dialog).getByLabelText('Payment amount'), '400.50')
    await user.type(within(dialog).getByLabelText('Note (optional)'), 'Receipt 7')
    await user.click(within(dialog).getByRole('button', { name: 'Record payment' }))

    expect(await screen.findByText('$400.50 recorded. Remaining balance: $2,099.50.')).toBeInTheDocument()
    expect(payments).toEqual([{ id: '1', body: { amount: 400.5, note: 'Receipt 7' } }])
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('pays the full balance in one click', async () => {
    const { payments } = mock()
    const { user } = renderAt('/admin/payments', { as: 'Admin' })

    const dialog = await openPayment(user, 'Demo Student 2', 'CSC 210 lab fee')
    await user.click(within(dialog).getByRole('button', { name: 'Pay the full balance of $150.00' }))
    await user.click(within(dialog).getByRole('button', { name: 'Record payment' }))

    expect(await screen.findByText('$150.00 recorded. CSC 210 lab fee for Demo Student 2 is now paid.')).toBeInTheDocument()
    expect(payments[0]?.body).toEqual({ amount: 150, note: null })
  })

  it('blocks an overpayment before sending it', async () => {
    const { payments } = mock()
    const { user } = renderAt('/admin/payments', { as: 'Admin' })

    const dialog = await openPayment(user, 'Demo Student 1', 'Fall 2026 tuition')
    await user.type(within(dialog).getByLabelText('Payment amount'), '2500.01')
    await user.click(within(dialog).getByRole('button', { name: 'Record payment' }))

    expect(await within(dialog).findByText("Payment can't be more than the remaining balance ($2,500.00).")).toBeInTheDocument()
    await user.clear(within(dialog).getByLabelText('Payment amount'))
    await user.type(within(dialog).getByLabelText('Payment amount'), '12.345')
    await user.click(within(dialog).getByRole('button', { name: 'Record payment' }))
    expect(await within(dialog).findByText('Enter an amount like 1500 or 1500.50.')).toBeInTheDocument()
    expect(payments).toHaveLength(0)
  })

  it('shows the 409 from the API in the payment dialog', async () => {
    mock()
    server.use(
      http.post('/api/admin/finance/charges/:id/payments', () =>
        HttpResponse.json({ status: 409, title: 'Payment is more than the remaining balance (100.00).', code: 'overpayment' }, { status: 409 }),
      ),
    )
    const { user } = renderAt('/admin/payments', { as: 'Admin' })

    const dialog = await openPayment(user, 'Demo Student 1', 'Fall 2026 tuition')
    await user.type(within(dialog).getByLabelText('Payment amount'), '200')
    await user.click(within(dialog).getByRole('button', { name: 'Record payment' }))

    expect(await within(dialog).findByRole('alert')).toHaveTextContent('Payment is more than the remaining balance (100.00).')
  })

  it('charges a whole major', async () => {
    const { created } = mock()
    const { user } = renderAt('/admin/payments', { as: 'Admin' })

    await user.click(await screen.findByRole('button', { name: 'Charge a major' }))
    const dialog = screen.getByRole('dialog', { name: 'Charge a major' })
    await waitFor(() => expect(within(dialog).getByLabelText('Term')).toHaveValue('2')) // current term by default
    await user.selectOptions(within(dialog).getByLabelText('Major'), '1')
    expect(within(dialog).getByText('6 active students will be charged.')).toBeInTheDocument()
    await user.selectOptions(within(dialog).getByLabelText('Payment type'), '1')
    await user.type(within(dialog).getByLabelText('Title'), 'Spring 2027 tuition')
    await user.type(within(dialog).getByLabelText('Amount per student'), '4500')
    await typeDate(user, dialog, 'Due date', '15/01/2027')
    await user.click(within(dialog).getByRole('button', { name: 'Create charges' }))

    expect(await screen.findByText('Spring 2027 tuition: 6 students charged.')).toBeInTheDocument()
    expect(created).toEqual([{ majorId: 1, termId: 2, paymentTypeId: 1, title: 'Spring 2027 tuition', amount: 4500, dueDate: '2027-01-15' }])
  })

  it('TC_95: an amount that is negative, zero or not a number is refused before sending', async () => {
    const { created } = mock()
    const { user } = renderAt('/admin/payments', { as: 'Admin' })

    await user.click(await screen.findByRole('button', { name: 'Charge a major' }))
    const dialog = screen.getByRole('dialog', { name: 'Charge a major' })
    const amount = within(dialog).getByLabelText('Amount per student')
    for (const [typed, message] of [['abc', 'Enter an amount like 1500 or 1500.50.'], ['-50', 'Enter an amount like 1500 or 1500.50.'], ['0', 'Enter an amount greater than zero.']] as const) {
      await user.clear(amount)
      await user.type(amount, typed)
      await user.click(within(dialog).getByRole('button', { name: 'Create charges' }))
      expect(await within(dialog).findByText(message)).toBeInTheDocument()
    }
    expect(created).toHaveLength(0)
  })

  it('shows field errors and the 409 when the major has nobody to charge', async () => {
    const { created } = mock()
    server.use(
      http.post('/api/admin/finance/batches', () =>
        HttpResponse.json({ status: 409, title: 'Mathematics has no active students to charge.', code: 'no_active_students' }, { status: 409 }),
      ),
    )
    const { user } = renderAt('/admin/payments', { as: 'Admin' })

    await user.click(await screen.findByRole('button', { name: 'Charge a major' }))
    const dialog = screen.getByRole('dialog', { name: 'Charge a major' })
    await user.click(within(dialog).getByRole('button', { name: 'Create charges' }))
    expect(await within(dialog).findByText('Choose a major.')).toBeInTheDocument()
    expect(within(dialog).getByText('Enter a title.')).toBeInTheDocument()
    expect(within(dialog).getByText('Choose a due date.')).toBeInTheDocument()
    expect(created).toHaveLength(0)

    await user.selectOptions(within(dialog).getByLabelText('Major'), '2')
    await user.selectOptions(within(dialog).getByLabelText('Payment type'), '3')
    await user.type(within(dialog).getByLabelText('Title'), 'Lab fee')
    await user.type(within(dialog).getByLabelText('Amount per student'), '50')
    await typeDate(user, dialog, 'Due date', '15/01/2027')
    await user.click(within(dialog).getByRole('button', { name: 'Create charges' }))

    expect(await within(dialog).findByRole('alert')).toHaveTextContent('Mathematics has no active students to charge.')
  })

  it('lists batches and shows the charges of one', async () => {
    const { queries } = mock()
    const { user } = renderAt('/admin/payments', { as: 'Admin' })
    await screen.findByRole('rowheader', { name: /Demo Student 1/ })

    await user.click(screen.getByRole('tab', { name: 'Batches' }))
    expect(await screen.findByText('1 / 6 paid')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Show charges of Fall 2026 tuition' }))

    await waitFor(() => expect(queries.at(-1)?.get('batchId')).toBe('1'))
    expect(await screen.findByRole('rowheader', { name: /Demo Student 1/ })).toBeInTheDocument()
    expect(screen.queryByRole('rowheader', { name: /Demo Student 2/ })).not.toBeInTheDocument()
  })

  it('shows the empty state', async () => {
    mock({ charges: [] })
    renderAt('/admin/payments', { as: 'Admin' })
    expect(await screen.findByText('No charges yet')).toBeInTheDocument()
    // The empty state carries the button it names (plus the one in the page header).
    expect(screen.getAllByRole('button', { name: 'Charge a major' })).toHaveLength(2)
  })

  it('offers "Clear filters" when no charge matches, and sorts by balance', async () => {
    mock()
    const { user } = renderAt('/admin/payments', { as: 'Admin' })
    await screen.findByRole('rowheader', { name: /Demo Student 1/ })
    const students = () => screen.getAllByRole('rowheader').map((h) => h.querySelector('span')?.textContent)

    await user.click(screen.getByRole('button', { name: 'Balance' }))
    expect(screen.getByRole('columnheader', { name: /Balance/ })).toHaveAttribute('aria-sort', 'ascending')
    expect(students()).toEqual(['Demo Student 3', 'Demo Student 2', 'Demo Student 1'])

    // The phone filter toggle reports how many filters are on.
    expect(screen.getByRole('button', { name: 'Filters' })).toHaveAttribute('aria-expanded', 'false')
    await user.selectOptions(screen.getByLabelText('Major'), 'Mathematics')
    server.use(http.get('/api/admin/finance/charges', ({ request }) =>
      HttpResponse.json(paged(new URL(request.url).searchParams.get('majorId') ? [] : [partial, overdue, paid]))))
    await user.selectOptions(screen.getByLabelText('Status'), 'Pending')
    expect(await screen.findByText('No charges match')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Filters (2)' })).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Clear filters' }))
    expect(await screen.findByRole('rowheader', { name: /Demo Student 1/ })).toBeInTheDocument()
    expect(screen.getByLabelText('Status')).toHaveValue('')
    expect(screen.getByLabelText('Major')).toHaveValue('')
  })

  it('shows a load error with retry', async () => {
    mock()
    server.use(http.get('/api/admin/finance/charges', () => HttpResponse.json({ title: 'Server is down.' }, { status: 500 })))
    renderAt('/admin/payments', { as: 'Admin' })
    expect(await screen.findByText('Something went wrong on our side. Please try again in a moment.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })
})
