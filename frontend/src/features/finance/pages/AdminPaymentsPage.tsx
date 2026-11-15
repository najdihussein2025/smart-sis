import { useId, useState, type ReactNode } from 'react'
import { Banknote, CreditCard, Layers, Plus, Receipt, Search, SlidersHorizontal, UserPlus, X } from 'lucide-react'
import { cn } from '@/shared/lib/cn'
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
import { SearchField } from '@/shared/ui/SearchField'
import { SelectField } from '@/shared/ui/SelectField'
import { SortHeader, useSort } from '@/shared/ui/SortHeader'
import { TableScroll } from '@/shared/ui/TableScroll'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { Tabs } from '@/shared/ui/Tabs'
import { BatchFormDialog } from '../components/BatchFormDialog'
import { ChargeFormDialog } from '../components/ChargeFormDialog'
import { ChargeStatusBadge, OverdueBadge } from '../components/ChargeStatusBadge'
import { PaymentDialog } from '../components/PaymentDialog'
import { useBatches, useCharges, useFinanceOptions } from '../hooks'
import type { AdminCharge, ChargeBatch, ChargeStatus, FinanceOptions } from '../types'

const PAGE_SIZE = 20

type Tab = 'charges' | 'batches'
type Open = 'none' | 'batch' | 'charge'

const thead = 'border-b border-line bg-canvas/60 text-xs uppercase tracking-wide text-muted max-lg:hidden'

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
  const chargeMajor = (
    <Button onClick={() => setOpen('batch')}>
      <Plus className="size-4" aria-hidden /> Charge a major
    </Button>
  )

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Money"
        title="Payments"
        icon={CreditCard}
        actions={
          <>
            <Button variant="secondary" onClick={() => setOpen('charge')}>
              <UserPlus className="size-4" aria-hidden /> Charge a student
            </Button>
            {chargeMajor}
          </>
        }
      >
        Charge every student of a major at once, and record full or partial payments.
      </PageHeader>

      {saved.message && <Alert tone="success">{saved.message}</Alert>}

      <Tabs
        label="Payments views"
        value={tab}
        onChange={setTab}
        items={[
          { value: 'charges', label: 'Charges' },
          { value: 'batches', label: 'Batches' },
        ]}
      />

      {tab === 'charges' ? (
        <ChargesTab
          options={options.data}
          batch={batch}
          onClearBatch={() => setBatch(null)}
          onPay={setPaying}
          onNew={() => setOpen('batch')}
        />
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
    </div>
  )
}

/**
 * Filter selects: inline from `lg`; on phones and tablets they fold behind a "Filters (n)" toggle so the search box and
 * the list stay on screen.
 */
function FilterDisclosure({ active, children, className }: { active: number; children: ReactNode; className?: string }) {
  const [open, setOpen] = useState(false)
  const id = useId()
  return (
    <>
      <Button
        variant="secondary"
        size="sm"
        className="justify-self-start lg:hidden"
        aria-expanded={open}
        aria-controls={id}
        onClick={() => setOpen((o) => !o)}
      >
        <SlidersHorizontal className="size-4" aria-hidden /> Filters{active > 0 && ` (${active})`}
      </Button>
      <div id={id} className={cn('grid gap-3 lg:contents', !open && 'max-lg:hidden', className)}>
        {children}
      </div>
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

type ChargeSort = 'due' | 'balance'

function ChargesTab({
  options,
  batch,
  onClearBatch,
  onPay,
  onNew,
}: {
  options: FinanceOptions | undefined
  batch: ChargeBatch | null
  onClearBatch: () => void
  onPay: (c: AdminCharge) => void
  onNew: () => void
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
  const { sorted, sort, toggle } = useSort<AdminCharge, ChargeSort>(charges.data?.items, (c, key) =>
    key === 'due' ? c.dueDate : c.balance,
  )
  const filtered = !!(debounced || status || termId || majorId || batch)
  const activeFilters = [status, termId, majorId].filter(Boolean).length
  const reset = <T,>(set: (v: T) => void) => (v: T) => {
    set(v)
    setPage(1)
  }
  const clearFilters = () => {
    setSearch('')
    setStatus('')
    setTermId('')
    setMajorId('')
    onClearBatch()
    setPage(1)
  }

  return (
    <Card>
      <div className="grid gap-3 border-b border-line p-4 sm:px-5 lg:grid-cols-4 lg:items-end">
        <SearchField label="Student" placeholder="University ID or name" value={search} onChange={reset(setSearch)} />
        <FilterDisclosure active={activeFilters} className="sm:grid-cols-3">
          <SelectField label="Status" value={status} onChange={(e) => reset(setStatus)(e.target.value as ChargeStatus | '')}>
            <option value="">All statuses</option>
            <option value="Pending">Pending</option>
            <option value="Partial">Partially paid</option>
            <option value="Paid">Paid</option>
          </SelectField>
          <TermMajorFilters options={options} termId={termId} majorId={majorId} onTerm={reset(setTermId)} onMajor={reset(setMajorId)} />
        </FilterDisclosure>
      </div>
      {batch && (
        <div className="flex flex-wrap items-center gap-2 border-b border-line px-4 py-2.5 text-sm sm:px-5">
          <span className="text-muted">Batch:</span>
          <span className="font-medium text-ink">
            {batch.title} · {batch.majorName}
          </span>
          <Button
            variant="ghost"
            size="sm"
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
        filtered ? (
          <EmptyState
            icon={Search}
            title="No charges match"
            action={
              <Button variant="ghost" onClick={clearFilters}>
                Clear filters
              </Button>
            }
          >
            Change the search or the filters to see more charges.
          </EmptyState>
        ) : (
          <EmptyState
            icon={Receipt}
            title="No charges yet"
            action={
              <Button onClick={onNew}>
                <Plus className="size-4" aria-hidden /> Charge a major
              </Button>
            }
          >
            Charge every student of a major at once with “Charge a major”.
          </EmptyState>
        )
      ) : (
        <>
          <TableScroll>
            <table className="w-full text-left text-sm max-lg:block">
              <thead className={thead}>
                <tr>
                  <th scope="col" className="px-4 py-3 font-medium">Student</th>
                  <th scope="col" className="px-4 py-3 font-medium">Charge</th>
                  <SortHeader label="Due" sortKey="due" sort={sort} onSort={toggle} className="px-4" />
                  <th scope="col" className="px-4 py-3 text-right font-medium">Amount</th>
                  <SortHeader label="Balance" sortKey="balance" sort={sort} onSort={toggle} align="right" className="px-4" />
                  <th scope="col" className="px-4 py-3 font-medium">Status</th>
                  <th scope="col" className="px-4 py-3"><span className="sr-only">Actions</span></th>
                </tr>
              </thead>
              <tbody className="divide-y divide-line max-lg:block">
                {sorted.map((c) => (
                  <tr
                    key={c.id}
                    className="align-top transition-colors hover:bg-canvas/50 max-lg:grid max-lg:grid-cols-[minmax(0,1fr)_auto] max-lg:gap-x-3 max-lg:gap-y-1.5 max-lg:px-4 max-lg:py-4"
                  >
                    <th scope="row" className="px-4 py-3.5 font-normal max-lg:p-0">
                      <span className="block font-medium text-ink">{c.studentName}</span>
                      <span className="block text-muted">
                        <span className="tabular">{c.universityId}</span> · {c.majorName}
                      </span>
                    </th>
                    <td className="px-4 py-3.5 max-lg:col-span-2 max-lg:row-start-2 max-lg:p-0">
                      <span className="block text-ink">{c.title}</span>
                      <span className="block text-muted">
                        {c.paymentTypeName} · {c.termName}
                      </span>
                    </td>
                    <td className="whitespace-nowrap px-4 py-3.5 text-ink max-lg:col-span-2 max-lg:row-start-3 max-lg:p-0 max-lg:text-muted">
                      <span className="mr-1.5 lg:hidden">Due</span>
                      <span className="tabular">{formatDateOnly(c.dueDate)}</span>
                      {c.isOverdue && (
                        <span className="mt-1 block max-lg:ml-2 max-lg:mt-0 max-lg:inline-block">
                          <OverdueBadge />
                        </span>
                      )}
                    </td>
                    <td className="tabular whitespace-nowrap px-4 py-3.5 text-right text-ink max-lg:hidden">{formatMoney(c.amount)}</td>
                    <td className="tabular whitespace-nowrap px-4 py-3.5 text-right font-semibold text-ink max-lg:col-start-2 max-lg:row-start-1 max-lg:p-0 max-lg:text-base">
                      <span className="sr-only lg:hidden">Balance </span>
                      <span>{formatMoney(c.balance)}</span>
                    </td>
                    <td className="px-4 py-3.5 max-lg:col-start-2 max-lg:row-start-2 max-lg:p-0 max-lg:text-right">
                      <ChargeStatusBadge status={c.status} />
                    </td>
                    <td className={cn('px-4 py-3.5 text-right max-lg:col-span-2 max-lg:p-0', c.status === 'Paid' && 'max-lg:hidden')}>
                      {c.status !== 'Paid' && (
                        <Button
                          variant="secondary"
                          size="sm"
                          className="max-lg:mt-2 max-lg:w-full"
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

type BatchSort = 'due' | 'paid'

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
  const { sorted, sort, toggle } = useSort<ChargeBatch, BatchSort>(batches.data?.items, (b, key) =>
    key === 'due' ? b.dueDate : b.chargeCount === 0 ? 0 : b.paidCount / b.chargeCount,
  )
  const filtered = !!(termId || majorId)
  const clearFilters = () => {
    setTermId('')
    setMajorId('')
    setPage(1)
  }

  return (
    <Card>
      <div className="grid gap-3 border-b border-line p-4 sm:grid-cols-2 sm:px-5">
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
        filtered ? (
          <EmptyState
            icon={Search}
            title="No batches match"
            action={
              <Button variant="ghost" onClick={clearFilters}>
                Clear filters
              </Button>
            }
          >
            Change the filters to see more batches.
          </EmptyState>
        ) : (
          <EmptyState
            icon={Layers}
            title="No batches yet"
            action={
              <Button onClick={onNew}>
                <Plus className="size-4" aria-hidden /> Charge a major
              </Button>
            }
          >
            Charge a whole major at once; each batch is listed here.
          </EmptyState>
        )
      ) : (
        <>
          <TableScroll>
            <table className="w-full text-left text-sm max-lg:block">
              <thead className={thead}>
                <tr>
                  <th scope="col" className="px-4 py-3 font-medium">Batch</th>
                  <th scope="col" className="px-4 py-3 font-medium">Major</th>
                  <SortHeader label="Due" sortKey="due" sort={sort} onSort={toggle} className="px-4" />
                  <th scope="col" className="px-4 py-3 text-right font-medium">Per student</th>
                  <SortHeader label="Paid" sortKey="paid" sort={sort} onSort={toggle} align="right" className="px-4" />
                  <th scope="col" className="px-4 py-3"><span className="sr-only">Actions</span></th>
                </tr>
              </thead>
              <tbody className="divide-y divide-line max-lg:block">
                {sorted.map((b) => (
                  <tr
                    key={b.id}
                    className="align-top transition-colors hover:bg-canvas/50 max-lg:grid max-lg:grid-cols-[minmax(0,1fr)_auto] max-lg:gap-x-3 max-lg:gap-y-1.5 max-lg:px-4 max-lg:py-4"
                  >
                    <th scope="row" className="px-4 py-3.5 font-normal max-lg:p-0">
                      <span className="block font-medium text-ink">{b.title}</span>
                      <span className="block text-muted">
                        {b.paymentTypeName} · {b.termName} · created {formatDate(b.createdAt)}
                      </span>
                    </th>
                    <td className="px-4 py-3.5 text-ink max-lg:col-span-2 max-lg:row-start-2 max-lg:p-0">{b.majorName}</td>
                    <td className="tabular whitespace-nowrap px-4 py-3.5 text-ink max-lg:row-start-3 max-lg:p-0 max-lg:text-muted">
                      <span className="mr-1.5 lg:hidden">Due</span>
                      {formatDateOnly(b.dueDate)}
                    </td>
                    <td className="tabular whitespace-nowrap px-4 py-3.5 text-right text-ink max-lg:col-start-2 max-lg:row-start-1 max-lg:p-0 max-lg:font-semibold">
                      {formatMoney(b.amount)}
                      <span className="text-xs font-normal text-muted lg:hidden"> each</span>
                    </td>
                    <td className="px-4 py-3.5 text-right max-lg:col-span-2 max-lg:row-start-4 max-lg:p-0 max-lg:text-left">
                      <PaidProgress paid={b.paidCount} total={b.chargeCount} />
                    </td>
                    <td className="px-4 py-3.5 text-right max-lg:col-start-2 max-lg:row-start-3 max-lg:-mr-3 max-lg:-my-2 max-lg:p-0">
                      <Button variant="ghost" size="sm" onClick={() => onShowCharges(b)} aria-label={`Show charges of ${b.title}`}>
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

/** "1 / 6 paid" with a thin bar. The text carries the number; the bar only repeats it. */
function PaidProgress({ paid, total }: { paid: number; total: number }) {
  const pct = total > 0 ? Math.round((paid / total) * 100) : 0
  return (
    <span className="inline-flex w-full flex-col gap-1.5 lg:w-28 lg:items-end">
      <span className="tabular whitespace-nowrap text-ink">
        {paid} / {total} paid
      </span>
      <span aria-hidden className="block h-1.5 w-full overflow-hidden rounded-full bg-line">
        <span className={cn('block h-full rounded-full', pct === 100 ? 'bg-success' : 'bg-brand')} style={{ width: `${pct}%` }} />
      </span>
    </span>
  )
}
