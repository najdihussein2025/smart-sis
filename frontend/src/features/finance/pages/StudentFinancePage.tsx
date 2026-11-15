import { useState } from 'react'
import { AlertTriangle, CalendarClock, CheckCircle2, CreditCard, Receipt, Wallet } from 'lucide-react'
import { formatDateOnly, formatMoney } from '@/shared/lib/format'
import { Card } from '@/shared/ui/Card'
import { CardHeader } from '@/shared/ui/CardHeader'
import { EmptyState } from '@/shared/ui/EmptyState'
import { PageHeader } from '@/shared/ui/PageHeader'
import { QueryError } from '@/shared/ui/QueryError'
import { StatCard, StatCardGrid } from '@/shared/ui/StatCard'
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
      <PageHeader eyebrow="Money" title="Financial account" icon={CreditCard}>
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
          <div className="mb-6">
            <StatCardGrid columns="sm:grid-cols-3" aria-label="Account summary">
              <StatCard
                label="Outstanding balance"
                value={formatMoney(account.data.outstandingBalance)}
                hint={
                  account.data.totalCharged === 0
                    ? 'No charges yet'
                    : account.data.overdueBalance > 0
                      ? `${formatMoney(account.data.overdueBalance)} overdue`
                      : 'Nothing overdue'
                }
                tone={account.data.overdueBalance > 0 ? 'danger' : 'default'}
                icon={Wallet}
              />
              <StatCard
                label="Total paid"
                value={formatMoney(account.data.totalPaid)}
                hint={account.data.totalPaid === 0 ? 'No payments yet' : `of ${formatMoney(account.data.totalCharged)} charged`}
                icon={Receipt}
              />
              <StatCard
                label="Next due date"
                value={account.data.nextDueDate ? formatDateOnly(account.data.nextDueDate) : '—'}
                hint={account.data.nextDueDate ? undefined : 'No upcoming payment'}
                icon={CalendarClock}
              />
            </StatCardGrid>
          </div>

          <div className="mb-6">
            <Tabs
              label="Charges"
              value={tab}
              onChange={setTab}
              items={[
                { value: 'pending', label: 'Pending', count: pending.length },
                { value: 'completed', label: 'Completed', count: completed.length },
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
            <ul aria-label={tab === 'pending' ? 'Pending charges' : 'Completed charges'} className="space-y-6">
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
      <Card>
        <CardHeader
          title={c.title}
          description={`${c.paymentTypeName} · ${c.termName}`}
          actions={
            <>
              {c.isOverdue && <OverdueBadge />}
              <ChargeStatusBadge status={c.status} />
            </>
          }
        />
        <div className="p-4 sm:p-5">
          <dl className="grid grid-cols-2 gap-3 text-sm sm:grid-cols-4">
            <Fact label="Amount" value={formatMoney(c.amount)} />
            <Fact label="Paid" value={formatMoney(c.paid)} />
            <Fact label="Balance" value={formatMoney(c.balance)} />
            <div>
              <dt className="text-muted">Due date</dt>
              <dd className={c.isOverdue ? 'tabular flex items-center gap-1 font-medium text-danger' : 'tabular font-medium text-ink'}>
                {c.isOverdue && <AlertTriangle className="size-4" aria-hidden />}
                {formatDateOnly(c.dueDate)}
              </dd>
            </div>
          </dl>

          <div className="mt-4 border-t border-line pt-3">
            <h3 className="mb-1.5 text-sm font-semibold text-ink">Payments</h3>
            <PaymentHistory payments={c.payments} label={`Payments for ${c.title}`} />
          </div>
        </div>
      </Card>
    </li>
  )
}

function Fact({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-muted">{label}</dt>
      <dd className="tabular font-medium text-ink">{value}</dd>
    </div>
  )
}
