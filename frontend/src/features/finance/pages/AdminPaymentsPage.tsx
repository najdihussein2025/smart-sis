import { useState } from 'react'
import { Banknote, Layers, Plus, Receipt, UserPlus, X } from 'lucide-react'
import { formatDate, formatDateOnly, formatMoney } from '@/shared/lib/format'
import { useDebouncedValue } from '@/shared/lib/useDebouncedValue'
import { useSavedMessage } from '@/shared/lib/useSavedMessage'
import { Alert } from '@/shared/ui/Alert'
import { Button } from '@/shared/ui/Button'
import { Card } from '@/shared/ui/Card'
import { EmptyState } from '@/shared/ui/EmptyState'
import { PageHeader } from '@/shared/ui/PageHeader'
import { Pagination } from '@/shared/ui/Pagination'
import { QueryError } from '@/shared/ui/QueryError'
import { SelectField } from '@/shared/ui/SelectField'
import { TableScroll } from '@/shared/ui/TableScroll'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { Tabs } from '@/shared/ui/Tabs'
import { TextField } from '@/shared/ui/TextField'
import { BatchFormDialog } from '../components/BatchFormDialog'
import { ChargeFormDialog } from '../components/ChargeFormDialog'
import { ChargeStatusBadge, OverdueBadge } from '../components/ChargeStatusBadge'
import { PaymentDialog } from '../components/PaymentDialog'
import { useBatches, useCharges, useFinanceOptions } from '../hooks'
import type { AdminCharge, ChargeBatch, ChargeStatus, FinanceOptions } from '../types'

const PAGE_SIZE = 20

type Tab = 'charges' | 'batches'
type Open = 'none' | 'batch' | 'charge'

/** PB-18 / T58: charge a whole major at once, charge one student, list charges and record full or partial payments. */
export function AdminPaymentsPage() {
  const options = useFinanceOptions()
  const [tab, setTab] = useState<Tab>('charges')
  const [open, setOpen] = useState<Open>('none')
  const [paying, setPaying] = useState<AdminCharge | null>(null)
  const [batch, setBatch] = useState<ChargeBatch | null>(null)
  const saved = useSavedMessage()

  const showCharges = (b: ChargeBatch) => {
    setBatch(b)
    setTab('charges')
  }

  return (
    <>
      <PageHeader
        eyebrow="Administration"
        title="Payments"
        actions={
          <>
            <Button variant="secondary" onClick={() => setOpen('charge')}>
              <UserPlus className="size-4" aria-hidden /> Charge a student
            </Button>
            <Button onClick={() => setOpen('batch')}>
              <Plus className="size-4" aria-hidden /> Charge a major
            </Button>
          </>
        }
      >
        Charge every student of a major at once, and record full or partial payments.
      </PageHeader>

      {saved.message && (
        <div className="mb-4">
          <Alert tone="success">{saved.message}</Alert>
        </div>
      )}

      <div className="mb-4">
        <Tabs
          label="Payments views"
          value={tab}
          onChange={setTab}
          items={[
            { value: 'charges', label: 'Charges' },
            { value: 'batches', label: 'Batches' },
          ]}
        />
      </div>

      {tab === 'charges' ? (
        <ChargesTab options={options.data} batch={batch} onClearBatch={() => setBatch(null)} onPay={setPaying} />
      ) : (
        <BatchesTab options={options.data} onShowCharges={showCharges} onNew={() => setOpen('batch')} />
      )}

      <BatchFormDialog
        open={open === 'batch'}
        options={options.data}
        onClose={() => setOpen('none')}
        onSaved={(b) => {
          saved.show(`${b.title}: ${b.chargeCount} ${b.chargeCount === 1 ? 'student' : 'students'} charged.`)
          setOpen('none')
        }}
      />
      <ChargeFormDialog
        open={open === 'charge'}
        options={options.data}
        onClose={() => setOpen('none')}
        onSaved={(c) => {
          saved.show(`${c.title} charged to ${c.studentName}.`)
          setOpen('none')
        }}
      />
      <PaymentDialog
        charge={paying}
        onClose={() => setPaying(null)}
        onSaved={(c, amount) => {
          saved.show(
            c.status === 'Paid'
              ? `${formatMoney(amount)} recorded. ${c.title} for ${c.studentName} is now paid.`
              : `${formatMoney(amount)} recorded. Remaining balance: ${formatMoney(c.balance)}.`,
          )
          setPaying(null)
        }}
      />
    </>
  )
}

function TermMajorFilters({
  options,
  termId,
  majorId,
  onTerm,
  onMajor,
}: {
  options: FinanceOptions | undefined
  termId: string
  majorId: string
  onTerm: (v: string) => void
  onMajor: (v: string) => void
}) {
  return (
    <>
      <SelectField label="Term" value={termId} onChange={(e) => onTerm(e.target.value)}>
        <option value="">All terms</option>
        {options?.terms.map((t) => (
          <option key={t.id} value={t.id}>
            {t.name}
            {t.isCurrent ? ' (current)' : ''}
          </option>
        ))}
      </SelectField>
      <SelectField label="Major" value={majorId} onChange={(e) => onMajor(e.target.value)}>
        <option value="">All majors</option>
        {options?.majors.map((m) => (
          <option key={m.id} value={m.id}>
            {m.name}
          </option>
        ))}
      </SelectField>
    </>
  )
}

function ChargesTab({
  options,
  batch,
  onClearBatch,
  onPay,
}: {
  options: FinanceOptions | undefined
  batch: ChargeBatch | null
  onClearBatch: () => void
  onPay: (c: AdminCharge) => void
}) {
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState<ChargeStatus | ''>('')
  const [termId, setTermId] = useState('')
  const [majorId, setMajorId] = useState('')
  const [page, setPage] = useState(1)
  const debounced = useDebouncedValue(search.trim())

  const charges = useCharges({
    search: debounced || undefined,
    status: status || undefined,
    termId: termId ? Number(termId) : undefined,
    majorId: majorId ? Number(majorId) : undefined,
    batchId: batch?.id,
    page,
    pageSize: PAGE_SIZE,
  })
  const filtered = !!(debounced || status || termId || majorId || batch)
  const reset = <T,>(set: (v: T) => void) => (v: T) => {
    set(v)
    setPage(1)
  }

  return (
    <Card>
      <div className="grid gap-3 border-b border-line p-4 sm:grid-cols-2 lg:grid-cols-4">
        <TextField label="Student" placeholder="University ID or name" value={search} onChange={(e) => reset(setSearch)(e.target.value)} />
        <SelectField label="Status" value={status} onChange={(e) => reset(setStatus)(e.target.value as ChargeStatus | '')}>
          <option value="">All statuses</option>
          <option value="Pending">Pending</option>
          <option value="Partial">Partially paid</option>
          <option value="Paid">Paid</option>
        </SelectField>
        <TermMajorFilters options={options} termId={termId} majorId={majorId} onTerm={reset(setTermId)} onMajor={reset(setMajorId)} />
      </div>
      {batch && (
        <div className="flex flex-wrap items-center gap-2 border-b border-line px-4 py-2.5 text-sm">
          <span className="text-muted">Batch:</span>
          <span className="font-medium text-ink">
            {batch.title} · {batch.majorName}
          </span>
          <Button
            variant="ghost"
            className="h-8 px-2"
            onClick={() => {
              onClearBatch()
              setPage(1)
            }}
            aria-label="Clear batch filter"
          >
            <X className="size-4" aria-hidden /> Clear
          </Button>
        </div>
      )}

      {charges.isPending ? (
        <TableSkeleton label="Loading charges" />
      ) : charges.isError ? (
        <QueryError error={charges.error} onRetry={() => charges.refetch()} />
      ) : charges.data.items.length === 0 ? (
        <EmptyState icon={Receipt} title={filtered ? 'No charges match' : 'No charges yet'}>
          {filtered ? 'Change the filters to see more charges.' : 'Use "Charge a major" to charge every student of a major at once.'}
        </EmptyState>
      ) : (
        <>
          <TableScroll>
            <table className="w-full text-left text-sm md:min-w-[820px]">
              <thead className="border-b border-line text-xs uppercase tracking-wide text-muted">
                <tr>
                  <th scope="col" className="px-4 py-3 font-medium">Student</th>
                  <th scope="col" className="px-4 py-3 font-medium">Charge</th>
                  <th scope="col" className="px-4 py-3 font-medium">Due</th>
                  <th scope="col" className="px-4 py-3 text-right font-medium">Amount</th>
                  <th scope="col" className="px-4 py-3 text-right font-medium">Balance</th>
                  <th scope="col" className="px-4 py-3 font-medium">Status</th>
                  <th scope="col" className="px-4 py-3"><span className="sr-only">Actions</span></th>
                </tr>
              </thead>
              <tbody className="divide-y divide-line">
                {charges.data.items.map((c) => (
                  <tr key={c.id} className="align-top">
                    <th scope="row" className="px-4 py-3.5 font-normal">
                      <span className="block font-medium text-ink">{c.studentName}</span>
                      <span className="block text-muted">
                        {c.universityId} · {c.majorName}
                      </span>
                    </th>
                    <td className="px-4 py-3.5">
                      <span className="block text-ink">{c.title}</span>
                      <span className="block text-muted">
                        {c.paymentTypeName} · {c.termName}
                      </span>
                    </td>
                    <td className="whitespace-nowrap px-4 py-3.5 text-ink">
                      {formatDateOnly(c.dueDate)}
                      {c.isOverdue && (
                        <span className="mt-1 block">
                          <OverdueBadge />
                        </span>
                      )}
                    </td>
                    <td className="whitespace-nowrap px-4 py-3.5 text-right tabular-nums text-ink">{formatMoney(c.amount)}</td>
                    <td className="whitespace-nowrap px-4 py-3.5 text-right tabular-nums font-medium text-ink">{formatMoney(c.balance)}</td>
                    <td className="px-4 py-3.5">
                      <ChargeStatusBadge status={c.status} />
                    </td>
                    <td className="px-4 py-3.5 text-right">
                      {c.status !== 'Paid' && (
                        <Button
                          variant="secondary"
                          className="h-8 px-2.5"
                          onClick={() => onPay(c)}
                          aria-label={`Record payment for ${c.studentName}, ${c.title}`}
                        >
                          <Banknote className="size-4" aria-hidden /> Record payment
                        </Button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </TableScroll>
          <Pagination page={charges.data.page} totalPages={charges.data.totalPages} totalCount={charges.data.totalCount} noun="charge" onPage={setPage} />
        </>
      )}
    </Card>
  )
}

function BatchesTab({
  options,
  onShowCharges,
  onNew,
}: {
  options: FinanceOptions | undefined
  onShowCharges: (b: ChargeBatch) => void
  onNew: () => void
}) {
  const [termId, setTermId] = useState('')
  const [majorId, setMajorId] = useState('')
  const [page, setPage] = useState(1)
  const batches = useBatches({
    termId: termId ? Number(termId) : undefined,
    majorId: majorId ? Number(majorId) : undefined,
    page,
    pageSize: PAGE_SIZE,
  })
  const filtered = !!(termId || majorId)

  return (
    <Card>
      <div className="grid gap-3 border-b border-line p-4 sm:grid-cols-2">
        <TermMajorFilters
          options={options}
          termId={termId}
          majorId={majorId}
          onTerm={(v) => {
            setTermId(v)
            setPage(1)
          }}
          onMajor={(v) => {
            setMajorId(v)
            setPage(1)
          }}
        />
      </div>

      {batches.isPending ? (
        <TableSkeleton label="Loading batches" />
      ) : batches.isError ? (
        <QueryError error={batches.error} onRetry={() => batches.refetch()} />
      ) : batches.data.items.length === 0 ? (
        <EmptyState icon={Layers} title={filtered ? 'No batches match' : 'No batches yet'}>
          {filtered ? 'Change the filters to see more batches.' : 'Charge a whole major at once; each batch is listed here.'}
          {!filtered && (
            <div className="mt-4">
              <Button onClick={onNew}>
                <Plus className="size-4" aria-hidden /> Charge a major
              </Button>
            </div>
          )}
        </EmptyState>
      ) : (
        <>
          <TableScroll>
            <table className="w-full text-left text-sm md:min-w-[760px]">
              <thead className="border-b border-line text-xs uppercase tracking-wide text-muted">
                <tr>
                  <th scope="col" className="px-4 py-3 font-medium">Batch</th>
                  <th scope="col" className="px-4 py-3 font-medium">Major</th>
                  <th scope="col" className="px-4 py-3 font-medium">Due</th>
                  <th scope="col" className="px-4 py-3 text-right font-medium">Per student</th>
                  <th scope="col" className="px-4 py-3 font-medium">Paid</th>
                  <th scope="col" className="px-4 py-3"><span className="sr-only">Actions</span></th>
                </tr>
              </thead>
              <tbody className="divide-y divide-line">
                {batches.data.items.map((b) => (
                  <tr key={b.id} className="align-top">
                    <th scope="row" className="px-4 py-3.5 font-normal">
                      <span className="block font-medium text-ink">{b.title}</span>
                      <span className="block text-muted">
                        {b.paymentTypeName} · {b.termName} · created {formatDate(b.createdAt)}
                      </span>
                    </th>
                    <td className="px-4 py-3.5 text-ink">{b.majorName}</td>
                    <td className="whitespace-nowrap px-4 py-3.5 text-ink">{formatDateOnly(b.dueDate)}</td>
                    <td className="whitespace-nowrap px-4 py-3.5 text-right tabular-nums text-ink">{formatMoney(b.amount)}</td>
                    <td className="whitespace-nowrap px-4 py-3.5 tabular-nums text-ink">
                      {b.paidCount} / {b.chargeCount} paid
                    </td>
                    <td className="px-4 py-3.5 text-right">
                      <Button variant="ghost" className="h-8 px-2.5" onClick={() => onShowCharges(b)} aria-label={`Show charges of ${b.title}`}>
                        Show charges
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </TableScroll>
          <Pagination page={batches.data.page} totalPages={batches.data.totalPages} totalCount={batches.data.totalCount} noun="major charge" onPage={setPage} />
        </>
      )}
    </Card>
  )
}
