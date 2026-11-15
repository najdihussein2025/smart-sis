import { useId, useState, type ReactNode } from 'react'
import { Link } from 'react-router'
import { Inbox, MessageSquareReply, Search, SlidersHorizontal } from 'lucide-react'
import { cn } from '@/shared/lib/cn'
import { formatDateTime } from '@/shared/lib/format'
import { useDebouncedValue } from '@/shared/lib/useDebouncedValue'
import { Badge } from '@/shared/ui/Badge'
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
import { CategoriesPanel } from '../components/CategoriesPanel'
import { StatusBadge, statusLabels } from '../components/StatusBadge'
import { useAdminSupportCategories, useAdminSupportRequests } from '../hooks'
import { supportStatuses, type AdminSupportRequest, type SupportAssignment, type SupportStatus } from '../types'

const PAGE_SIZE = 20
type Tab = 'inbox' | 'categories'

/** "Needs reply": open requests nobody has picked up. The inbox starts here. */
const NEEDS_REPLY = { status: 'Open' as const, assigned: 'Unassigned' as const }

/** PB-14 / T54: the support inbox every admin works from (rule C3), and the support categories. */
export function AdminSupportPage() {
  const [tab, setTab] = useState<Tab>('inbox')
  // Same query as the inbox's default view, so the count costs no extra request.
  const needsReply = useAdminSupportRequests({ ...NEEDS_REPLY, page: 1, pageSize: PAGE_SIZE })
  return (
    <div className="space-y-6">
      <PageHeader eyebrow="Communication" title="Support inbox" icon={Inbox}>
        Student requests about registration, grades, payments and more. Any admin can pick one up.
      </PageHeader>
      <Tabs
        label="Support"
        value={tab}
        onChange={setTab}
        items={[
          { value: 'inbox', label: 'Requests', count: needsReply.data?.totalCount, ariaLabel: needsReply.data ? `Requests, ${needsReply.data.totalCount} need a reply` : undefined },
          { value: 'categories', label: 'Categories' },
        ]}
      />
      {tab === 'inbox' ? <InboxPanel /> : <CategoriesPanel />}
    </div>
  )
}

/** Filter selects: inline from `lg`; on phones and tablets behind a "Filters (n)" toggle so the list stays on screen. */
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

/** "waiting 2d" for a request still open: time since its last activity. */
function waitingFor(iso: string, now = Date.now()): string {
  const minutes = Math.max(0, Math.floor((now - new Date(iso).getTime()) / 60_000))
  if (minutes < 60) return `waiting ${Math.max(1, minutes)}m`
  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `waiting ${hours}h`
  return `waiting ${Math.floor(hours / 24)}d`
}

type SortKey = 'activity'

function InboxPanel() {
  const categories = useAdminSupportCategories()
  const [status, setStatus] = useState<SupportStatus | ''>(NEEDS_REPLY.status)
  const [categoryId, setCategoryId] = useState('')
  const [assigned, setAssigned] = useState<SupportAssignment>(NEEDS_REPLY.assigned)
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const debounced = useDebouncedValue(search.trim())
  const requests = useAdminSupportRequests({
    status: status || undefined,
    categoryId: categoryId ? Number(categoryId) : undefined,
    assigned: assigned === 'Any' ? undefined : assigned,
    search: debounced || undefined,
    page,
    pageSize: PAGE_SIZE,
  })
  const { sorted, sort, toggle } = useSort<AdminSupportRequest, SortKey>(requests.data?.items, (r) => r.lastActivityAt)
  const needsReplyView = status === NEEDS_REPLY.status && assigned === NEEDS_REPLY.assigned && !categoryId && !debounced
  const filtered = !!status || !!categoryId || assigned !== 'Any' || !!debounced
  const activeFilters = [status, categoryId, assigned !== 'Any' ? assigned : ''].filter(Boolean).length

  const reset = <T,>(set: (v: T) => void) => (v: T) => {
    set(v)
    setPage(1)
  }
  const showAll = () => {
    setStatus('')
    setCategoryId('')
    setAssigned('Any')
    setSearch('')
    setPage(1)
  }
  const showNeedsReply = () => {
    showAll()
    setStatus(NEEDS_REPLY.status)
    setAssigned(NEEDS_REPLY.assigned)
  }

  return (
    <Card>
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-line px-4 py-3 sm:px-5">
        <p className="flex items-center gap-2 text-sm text-ink">
          <MessageSquareReply className="size-4 text-muted" aria-hidden />
          {needsReplyView ? (
            <>
              <span className="font-medium">Needs reply</span>
              <span className="text-muted">· open and not picked up yet</span>
            </>
          ) : (
            <span className="font-medium">{filtered ? 'Filtered requests' : 'All requests'}</span>
          )}
        </p>
        {needsReplyView ? (
          <Button variant="ghost" size="sm" onClick={showAll}>
            Show all requests
          </Button>
        ) : (
          <Button variant="ghost" size="sm" onClick={showNeedsReply}>
            Show needs reply
          </Button>
        )}
      </div>
      <div className="grid gap-3 border-b border-line p-4 sm:px-5 lg:grid-cols-4 lg:items-end">
        <SearchField label="Search" placeholder="Subject, student name or ID" value={search} onChange={reset(setSearch)} />
        <FilterDisclosure active={activeFilters} className="sm:grid-cols-3">
          <SelectField label="Status" value={status} onChange={(e) => reset(setStatus)(e.target.value as SupportStatus | '')}>
            <option value="">All statuses</option>
            {supportStatuses.map((s) => (
              <option key={s} value={s}>
                {statusLabels[s]}
              </option>
            ))}
          </SelectField>
          <SelectField label="Category" value={categoryId} onChange={(e) => reset(setCategoryId)(e.target.value)}>
            <option value="">All categories</option>
            {categories.data?.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </SelectField>
          <SelectField label="Assigned" value={assigned} onChange={(e) => reset(setAssigned)(e.target.value as SupportAssignment)}>
            <option value="Any">Anyone</option>
            <option value="Me">Assigned to me</option>
            <option value="Unassigned">Unassigned</option>
          </SelectField>
        </FilterDisclosure>
      </div>

      {requests.isPending ? (
        <TableSkeleton label="Loading requests" />
      ) : requests.isError ? (
        <QueryError error={requests.error} onRetry={() => requests.refetch()} />
      ) : requests.data.items.length === 0 ? (
        needsReplyView ? (
          <EmptyState
            icon={Inbox}
            title="Nothing needs a reply"
            action={
              <Button variant="ghost" onClick={showAll}>
                Show all requests
              </Button>
            }
          >
            Every open request has been picked up. New requests from students land here.
          </EmptyState>
        ) : filtered ? (
          <EmptyState
            icon={Search}
            title="No requests match"
            action={
              <Button variant="ghost" onClick={showAll}>
                Clear filters
              </Button>
            }
          >
            Change the filters or clear the search to see other requests.
          </EmptyState>
        ) : (
          <EmptyState icon={Inbox} title="The inbox is empty">
            Requests students submit will appear here.
          </EmptyState>
        )
      ) : (
        <>
          <TableScroll>
            <table className="w-full text-left text-sm max-md:block md:min-w-[760px]">
              <thead className="border-b border-line bg-canvas/60 text-xs uppercase tracking-wide text-muted max-md:hidden">
                <tr>
                  <th scope="col" className="px-4 py-3 font-medium">Request</th>
                  <th scope="col" className="px-4 py-3 font-medium">Student</th>
                  <th scope="col" className="px-4 py-3 font-medium">Status</th>
                  <th scope="col" className="px-4 py-3 font-medium">Assigned</th>
                  <SortHeader label="Last activity" sortKey="activity" sort={sort} onSort={toggle} className="px-4" />
                </tr>
              </thead>
              <tbody className="divide-y divide-line max-md:block">
                {sorted.map((r) => (
                  <tr key={r.id} className="align-top transition-colors hover:bg-canvas/50 max-md:grid max-md:gap-1.5 max-md:px-4 max-md:py-4">
                    <th scope="row" className="px-4 py-3.5 font-normal max-md:p-0">
                      <Link to={`/admin/support/${r.id}`} className="block font-medium text-ink hover:text-brand hover:underline">
                        {r.subject}
                      </Link>
                      <span className="block text-muted">
                        {r.categoryName} · {r.messageCount} {r.messageCount === 1 ? 'reply' : 'replies'}
                      </span>
                    </th>
                    <td className="px-4 py-3.5 max-md:p-0">
                      <span className="block text-ink">{r.studentName}</span>
                      <span className="tabular block text-xs text-muted">{r.studentUniversityId}</span>
                    </td>
                    <td className="px-4 py-3.5 max-md:p-0">
                      <StatusBadge status={r.status} />
                    </td>
                    <td className="px-4 py-3.5 text-muted max-md:p-0">
                      <span className="mr-1.5 font-medium text-ink md:hidden">Assigned</span>
                      {r.assignedAdminName ?? <Badge tone="warning" dot>Unassigned</Badge>}
                    </td>
                    <td className="whitespace-nowrap px-4 py-3.5 text-muted max-md:p-0">
                      <span className="tabular">{formatDateTime(r.lastActivityAt)}</span>
                      {r.status === 'Open' && (
                        <span className="block text-xs font-medium text-warning max-md:ml-2 max-md:inline">{waitingFor(r.lastActivityAt)}</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </TableScroll>
          <Pagination
            page={requests.data.page}
            totalPages={requests.data.totalPages}
            totalCount={requests.data.totalCount}
            noun="request"
            onPage={setPage}
          />
        </>
      )}
    </Card>
  )
}
