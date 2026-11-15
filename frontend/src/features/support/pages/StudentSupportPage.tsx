import { useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { LifeBuoy, MessageSquare, Plus } from 'lucide-react'
import { formatDateTime } from '@/shared/lib/format'
import { Button } from '@/shared/ui/Button'
import { Card } from '@/shared/ui/Card'
import { EmptyState } from '@/shared/ui/EmptyState'
import { PageHeader } from '@/shared/ui/PageHeader'
import { Pagination } from '@/shared/ui/Pagination'
import { QueryError } from '@/shared/ui/QueryError'
import { SelectField } from '@/shared/ui/SelectField'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { NewRequestDialog } from '../components/NewRequestDialog'
import { StatusBadge, statusLabels } from '../components/StatusBadge'
import { useMySupportRequests } from '../hooks'
import { supportStatuses, type SupportStatus } from '../types'

const PAGE_SIZE = 20

/** PB-13 / T51: the student's support requests with their status, and a form to submit a new one. */
export function StudentSupportPage() {
  const navigate = useNavigate()
  const [status, setStatus] = useState<SupportStatus | ''>('')
  const [page, setPage] = useState(1)
  const [dialogOpen, setDialogOpen] = useState(false)
  const requests = useMySupportRequests({
    status: status || undefined,
    page,
    pageSize: PAGE_SIZE,
  })

  const newButton = (
    <Button onClick={() => setDialogOpen(true)}>
      <Plus className="size-4" aria-hidden /> New request
    </Button>
  )
  // No requests at all (not just none for this filter): the empty state carries the only "New request" button, and a
  // status filter over nothing is noise.
  const noneAtAll = !status && requests.data?.totalCount === 0
  const showHeaderButton = !requests.isPending && !noneAtAll
  const showFilter = !requests.isPending && !noneAtAll && !requests.isError

  return (
    <>
      <PageHeader eyebrow="Help" title="Support" icon={LifeBuoy} actions={showHeaderButton ? newButton : undefined}>
        Ask the administration about registration, grades, payments or anything else, and follow the answer here.
      </PageHeader>

      <Card>
        {showFilter && (
          <div className="border-b border-line p-4 sm:px-5">
            <div className="sm:max-w-xs">
              <SelectField
                label="Status"
                value={status}
                onChange={(e) => {
                  setStatus(e.target.value as SupportStatus | '')
                  setPage(1)
                }}
              >
                <option value="">All statuses</option>
                {supportStatuses.map((s) => (
                  <option key={s} value={s}>
                    {statusLabels[s]}
                  </option>
                ))}
              </SelectField>
            </div>
          </div>
        )}

        {requests.isPending ? (
          <TableSkeleton label="Loading requests" />
        ) : requests.isError ? (
          <QueryError error={requests.error} onRetry={() => requests.refetch()} />
        ) : requests.data.items.length === 0 ? (
          <EmptyState
            icon={LifeBuoy}
            title={status ? `No ${statusLabels[status].toLowerCase()} requests` : 'No support requests yet'}
            action={
              status ? (
                <Button
                  variant="secondary"
                  onClick={() => {
                    setStatus('')
                    setPage(1)
                  }}
                >
                  Show all statuses
                </Button>
              ) : (
                newButton
              )
            }
          >
            {status
              ? 'Choose another status to see your other requests.'
              : 'When something isn’t working, submit a request and an administrator will answer you here.'}
          </EmptyState>
        ) : (
          <>
            <ul className="divide-y divide-line" aria-label="Support requests">
              {requests.data.items.map((r) => (
                <li key={r.id} className="flex flex-wrap items-start justify-between gap-x-4 gap-y-2 px-4 py-4 sm:px-5">
                  <div className="min-w-0">
                    <Link to={`/student/support/${r.id}`} className="font-medium text-ink hover:text-brand hover:underline">
                      {r.subject}
                    </Link>
                    <p className="tabular mt-0.5 text-sm text-muted">
                      {r.categoryName} · updated {formatDateTime(r.lastActivityAt)}
                    </p>
                  </div>
                  <div className="flex items-center gap-3 text-sm text-muted">
                    <span className="tabular inline-flex items-center gap-1" aria-label={`${r.messageCount} replies`}>
                      <MessageSquare className="size-4" aria-hidden />
                      {r.messageCount}
                    </span>
                    <StatusBadge status={r.status} />
                  </div>
                </li>
              ))}
            </ul>
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

      <NewRequestDialog
        open={dialogOpen}
        onClose={() => setDialogOpen(false)}
        onCreated={(r) => {
          setDialogOpen(false)
          void navigate(`/student/support/${r.id}`, {
            state: { submitted: true },
          })
        }}
      />
    </>
  )
}
