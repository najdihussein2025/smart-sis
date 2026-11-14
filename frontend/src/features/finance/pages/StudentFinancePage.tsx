import { useState } from 'react'
import { AlertTriangle, CalendarClock, CheckCircle2, Receipt, Wallet } from 'lucide-react'
import { formatDateOnly, formatMoney } from '@/shared/lib/format'
import { Card } from '@/shared/ui/Card'
import { EmptyState } from '@/shared/ui/EmptyState'
import { PageHeader } from '@/shared/ui/PageHeader'
import { QueryError } from '@/shared/ui/QueryError'
import { StatCard } from '@/shared/ui/StatCard'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { Tabs } from '@/shared/ui/Tabs'
import { ChargeStatusBadge, OverdueBadge } from '../components/ChargeStatusBadge'
import { PaymentHistory } from '../components/PaymentHistory'
import { useMyFinance } from '../hooks'
import type { StudentCharge } from '../types'

type Tab = 'pending' | 'completed'

/** PB-18 / T57: the student's charges, what is still owed, due dates and the payments made (partial payments allowed). */
export function StudentFinancePage() {
  const account = useMyFinance()
  const [tab, setTab] = useState<Tab>('pending')

  const charges = account.data?.charges ?? []
  const pending = charges.filter((c) => c.status !== 'Paid')
  const completed = charges.filter((c) => c.status === 'Paid')
  const shown = tab === 'pending' ? pending : completed

  return (
    <>
      <PageHeader eyebrow="Finance" title="Financial account">
        Your charges, due dates and the payments recorded by the finance office.
      </PageHeader>

      {account.isPending ? (
        <Card>
          <TableSkeleton label="Loading your account" />
        </Card>
      ) : account.isError ? (
        <Card>
          <QueryError error={account.error} onRetry={() => account.refetch()} />
        </Card>
      ) : (
        <>
          <div className="mb-6 grid gap-4 sm:grid-cols-3">
            <StatCard
              label="Outstanding balance"
              value={formatMoney(account.data.outstandingBalance)}
              hint={account.data.overdueBalance > 0 ? `${formatMoney(account.data.overdueBalance)} overdue` : 'Nothing overdue'}
              icon={Wallet}
            />
            <StatCard label="Total paid" value={formatMoney(account.data.totalPaid)} hint={`of ${formatMoney(account.data.totalCharged)} charged`} icon={Receipt} />
            <StatCard
              label="Next due date"
              value={account.data.nextDueDate ? formatDateOnly(account.data.nextDueDate) : '—'}
              hint={account.data.nextDueDate ? undefined : 'No upcoming payment'}
              icon={CalendarClock}
            />
          </div>

          <div className="mb-4">
            <Tabs
              label="Charges"
              value={tab}
              onChange={setTab}
              items={[
                { value: 'pending', label: `Pending (${pending.length})` },
                { value: 'completed', label: `Completed (${completed.length})` },
              ]}
            />
          </div>

          {shown.length === 0 ? (
            <Card>
              {tab === 'pending' ? (
                <EmptyState icon={CheckCircle2} title="Nothing to pay">
                  You have no pending charges. New charges appear here with their due date.
                </EmptyState>
              ) : (
                <EmptyState icon={Receipt} title="No completed charges yet">
                  Charges move here once they are fully paid.
                </EmptyState>
              )}
            </Card>
          ) : (
            <ul aria-label={tab === 'pending' ? 'Pending charges' : 'Completed charges'} className="space-y-4">
              {shown.map((c) => (
                <ChargeCard key={c.id} charge={c} />
              ))}
            </ul>
          )}
        </>
      )}
    </>
  )
}

function ChargeCard({ charge: c }: { charge: StudentCharge }) {
  return (
    <li>
      <Card className="p-5">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div className="min-w-0">
            <h2 className="font-display text-lg font-semibold text-ink">{c.title}</h2>
            <p className="text-sm text-muted">
              {c.paymentTypeName} · {c.termName}
            </p>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            {c.isOverdue && <OverdueBadge />}
            <ChargeStatusBadge status={c.status} />
          </div>
        </div>

        <dl className="mt-4 grid grid-cols-2 gap-3 text-sm sm:grid-cols-4">
          <Fact label="Amount" value={formatMoney(c.amount)} />
          <Fact label="Paid" value={formatMoney(c.paid)} />
          <Fact label="Balance" value={formatMoney(c.balance)} />
          <div>
            <dt className="text-muted">Due date</dt>
            <dd className={c.isOverdue ? 'flex items-center gap-1 font-medium text-danger' : 'font-medium text-ink'}>
              {c.isOverdue && <AlertTriangle className="size-4" aria-hidden />}
              {formatDateOnly(c.dueDate)}
            </dd>
          </div>
        </dl>

        <div className="mt-4 border-t border-line pt-3">
          <h3 className="mb-1.5 text-sm font-semibold text-ink">Payments</h3>
          <PaymentHistory payments={c.payments} label={`Payments for ${c.title}`} />
        </div>
      </Card>
    </li>
  )
}

function Fact({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-muted">{label}</dt>
      <dd className="font-medium tabular-nums text-ink">{value}</dd>
    </div>
  )
}
