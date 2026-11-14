import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, within } from '@testing-library/react'
import { server } from '@/test/server'
import { profileFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'
import type { MyFinance, StudentCharge } from '../types'

// PB-18 / T57: the student's financial account. Simulated data (SR-23).

const charge = (id: number, title: string, extra: Partial<StudentCharge> = {}): StudentCharge => ({
  id, title, termId: 1, termName: 'Fall 2026', paymentTypeName: 'Tuition', amount: 4500, paid: 0, balance: 4500,
  dueDate: '2026-12-01', status: 'Pending', isOverdue: false, payments: [], ...extra,
})

const tuition = charge(1, 'Fall 2026 tuition', {
  paid: 2000, balance: 2500, status: 'Partial',
  payments: [{ id: 1, amount: 2000, paidAt: '2026-11-01T10:00:00Z', note: 'Simulated receipt 0001' }],
})
const labFee = charge(2, 'CSC 210 lab fee', { paymentTypeName: 'Lab fee', amount: 150, balance: 150, dueDate: '2026-11-01', isOverdue: true })
const registration = charge(3, 'Spring 2026 registration fee', {
  termName: 'Spring 2026', paymentTypeName: 'Registration', amount: 250, paid: 250, balance: 0, status: 'Paid', dueDate: '2026-02-01',
  payments: [{ id: 2, amount: 250, paidAt: '2026-01-20T10:00:00Z', note: null }],
})

const account = (charges: StudentCharge[], extra: Partial<MyFinance> = {}): MyFinance => ({
  totalCharged: charges.reduce((n, c) => n + c.amount, 0),
  totalPaid: charges.reduce((n, c) => n + c.paid, 0),
  outstandingBalance: charges.reduce((n, c) => n + c.balance, 0),
  overdueBalance: charges.filter((c) => c.isOverdue).reduce((n, c) => n + c.balance, 0),
  nextDueDate: '2026-12-01',
  charges,
  ...extra,
})

function mock(body: MyFinance | (() => Response)) {
  server.use(
    http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))),
    http.get('/api/finance/my', () => (typeof body === 'function' ? body() : HttpResponse.json(body))),
  )
}

describe('StudentFinancePage', () => {
  it('shows the outstanding balance, pending charges with due dates, overdue and payments', async () => {
    mock(account([tuition, labFee, registration]))
    renderAt('/student/finance', { as: 'Student' })

    const list = await screen.findByRole('list', { name: 'Pending charges' })
    expect(screen.getByText('$2,650.00')).toBeInTheDocument() // outstanding
    expect(screen.getByText('$150.00 overdue')).toBeInTheDocument()
    expect(screen.getByText('01 Dec 2026', { selector: 'p' })).toBeInTheDocument() // next due date card

    const items = within(list).getAllByRole('listitem').filter((li) => li.parentElement === list)
    expect(items).toHaveLength(2)
    const tuitionCard = within(list).getByRole('heading', { name: 'Fall 2026 tuition' }).closest('li')!
    expect(within(tuitionCard).getByText('Partially paid')).toBeInTheDocument()
    expect(within(within(tuitionCard).getByRole('list', { name: 'Payments for Fall 2026 tuition' })).getByText('$2,000.00')).toBeInTheDocument()
    const labCard = within(list).getByRole('heading', { name: 'CSC 210 lab fee' }).closest('li')!
    expect(within(labCard).getByText('Overdue')).toBeInTheDocument()
    expect(within(labCard).getByText('Pending')).toBeInTheDocument()
    expect(within(labCard).getByText('No payments yet.')).toBeInTheDocument()
  })

  it('lists paid charges under Completed', async () => {
    mock(account([tuition, registration]))
    const { user } = renderAt('/student/finance', { as: 'Student' })

    await user.click(await screen.findByRole('tab', { name: 'Completed (1)' }))
    const list = screen.getByRole('list', { name: 'Completed charges' })
    expect(within(list).getByRole('heading', { name: 'Spring 2026 registration fee' })).toBeInTheDocument()
    expect(within(list).getByText('Paid', { selector: 'span' })).toBeInTheDocument()
    expect(within(list).queryByText('Fall 2026 tuition')).not.toBeInTheDocument()
  })

  it('shows empty states when there is nothing to pay', async () => {
    mock(account([], { nextDueDate: null }))
    const { user } = renderAt('/student/finance', { as: 'Student' })

    expect(await screen.findByText('Nothing to pay')).toBeInTheDocument()
    expect(screen.getByText('Nothing overdue')).toBeInTheDocument()
    expect(screen.getByText('No upcoming payment')).toBeInTheDocument()
    await user.click(screen.getByRole('tab', { name: 'Completed (0)' }))
    expect(screen.getByText('No completed charges yet')).toBeInTheDocument()
  })

  it('shows the error and retries', async () => {
    let fail = true
    mock(() =>
      fail ? HttpResponse.json({ title: 'Server is down.' }, { status: 500 }) : HttpResponse.json(account([labFee])),
    )
    const { user } = renderAt('/student/finance', { as: 'Student' })

    expect(await screen.findByText('Server is down.')).toBeInTheDocument()
    fail = false
    await user.click(screen.getByRole('button', { name: 'Try again' }))
    expect(await screen.findByRole('heading', { name: 'CSC 210 lab fee' })).toBeInTheDocument()
  })
})
