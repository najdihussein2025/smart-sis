import { useState } from 'react'
import { Link } from 'react-router'
import { Inbox } from 'lucide-react'
import { formatDateTime } from '@/shared/lib/format'
import { useDebouncedValue } from '@/shared/lib/useDebouncedValue'
import { Badge } from '@/shared/ui/Badge'
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
import { CategoriesPanel } from '../components/CategoriesPanel'
import { StatusBadge, statusLabels } from '../components/StatusBadge'
import { useAdminSupportCategories, useAdminSupportRequests } from '../hooks'
import { supportStatuses, type SupportAssignment, type SupportStatus } from '../types'

const PAGE_SIZE = 20
type Tab = 'inbox' | 'categories'

/** PB-14 / T54: the support inbox every admin works from (rule C3), and the support categories. */
export function AdminSupportPage() {
  const [tab, setTab] = useState<Tab>('inbox')
  return (
    <>
      <PageHeader eyebrow="Administration" title="Support inbox">
        Student requests about registration, grades, payments and more. Any admin can pick one up.
      </PageHeader>
      <div className="mb-4">
        <Tabs
          label="Support"
          value={tab}
          onChange={setTab}
          items={[
            { value: 'inbox', label: 'Requests' },
            { value: 'categories', label: 'Categories' },
          ]}
        />
      </div>
      {tab === 'inbox' ? <InboxPanel /> : <CategoriesPanel />}
    </>
  )
}

function InboxPanel() {
  const categories = useAdminSupportCategories()
  const [status, setStatus] = useState<SupportStatus | ''>('')
  const [categoryId, setCategoryId] = useState('')
  const [assigned, setAssigned] = useState<SupportAssignment>('Any')
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
  const filtered = !!status || !!categoryId || assigned !== 'Any' || !!debounced

  const reset = <T,>(set: (v: T) => void) => (v: T) => {
    set(v)
    setPage(1)
  }

  return (
    <Card>
      <div className="grid gap-3 border-b border-line p-4 sm:grid-cols-2 lg:grid-cols-4">
        <TextField
          label="Search"
          type="search"
          placeholder="Subject, student name or ID"
          value={search}
          onChange={(e) => reset(setSearch)(e.target.value)}
        />
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
      </div>

      {requests.isPending ? (
        <TableSkeleton label="Loading requests" />
      ) : requests.isError ? (
        <QueryError error={requests.error} onRetry={() => requests.refetch()} />
      ) : requests.data.items.length === 0 ? (
        <EmptyState icon={Inbox} title={filtered ? 'No requests match' : 'The inbox is empty'}>
          {filtered ? 'Change the filters or clear the search to see other requests.' : 'Requests students submit will appear here.'}
        </EmptyState>
      ) : (
        <>
          <TableScroll>
            <table className="w-full text-left text-sm max-md:block md:min-w-[760px]">
              <thead className="border-b border-line text-xs uppercase tracking-wide text-muted max-md:hidden">
                <tr>
                  <th scope="col" className="px-4 py-3 font-medium">Request</th>
                  <th scope="col" className="px-4 py-3 font-medium">Student</th>
                  <th scope="col" className="px-4 py-3 font-medium">Status</th>
                  <th scope="col" className="px-4 py-3 font-medium">Assigned</th>
                  <th scope="col" className="px-4 py-3 font-medium">Last activity</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-line max-md:block">
                {requests.data.items.map((r) => (
                  <tr key={r.id} className="align-top max-md:grid max-md:gap-1.5 max-md:px-4 max-md:py-4">
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
                      <span className="block text-xs text-muted">{r.studentUniversityId}</span>
                    </td>
                    <td className="px-4 py-3.5 max-md:p-0">
                      <StatusBadge status={r.status} />
                    </td>
                    <td className="px-4 py-3.5 text-muted max-md:p-0">
                      <span className="mr-1.5 font-medium text-ink md:hidden">Assigned</span>
                      {r.assignedAdminName ?? <Badge>Unassigned</Badge>}
                    </td>
                    <td className="whitespace-nowrap px-4 py-3.5 text-muted max-md:p-0">{formatDateTime(r.lastActivityAt)}</td>
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
