import { Link, useLocation, useParams } from 'react-router'
import { ArrowLeft } from 'lucide-react'
import { formatDateTime } from '@/shared/lib/format'
import { Alert } from '@/shared/ui/Alert'
import { Card } from '@/shared/ui/Card'
import { PageHeader } from '@/shared/ui/PageHeader'
import { QueryError } from '@/shared/ui/QueryError'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { ReplyForm } from '../components/ReplyForm'
import { StatusBadge } from '../components/StatusBadge'
import { Thread } from '../components/Thread'
import { useReplyToSupportRequest, useSupportRequest } from '../hooks'

/** PB-13 / T51: one of the student's requests, its thread and a reply box (none once it is closed). */
export function StudentSupportRequestPage() {
  const id = Number(useParams().id)
  const location = useLocation()
  const submitted = (location.state as { submitted?: boolean } | null)?.submitted === true
  const request = useSupportRequest(id)
  const reply = useReplyToSupportRequest(id)

  const back = (
    <Link to="/student/support" className="mb-4 inline-flex items-center gap-1.5 text-sm text-muted hover:text-ink">
      <ArrowLeft className="size-4" aria-hidden /> All requests
    </Link>
  )

  if (request.isPending || request.isError)
    return (
      <>
        {back}
        <Card>
          {request.isPending ? (
            <TableSkeleton label="Loading request" />
          ) : (
            <QueryError error={request.error} onRetry={() => request.refetch()} />
          )}
        </Card>
      </>
    )

  const r = request.data
  return (
    <>
      {back}
      <PageHeader eyebrow={r.categoryName} title={r.subject}>
        <span className="inline-flex flex-wrap items-center gap-2">
          <StatusBadge status={r.status} />
          <span>Submitted {formatDateTime(r.createdAt)}</span>
        </span>
      </PageHeader>

      {submitted && (
        <div className="mb-4">
          <Alert tone="success">Request submitted. You’ll get a notification and an email when an administrator answers.</Alert>
        </div>
      )}

      <Card className="space-y-6 p-5">
        <Thread
          description={r.description}
          createdAt={r.createdAt}
          submittedBy="You"
          messages={r.messages}
          studentLabel={() => 'You'}
          staffLabel={(m) => `${m.authorName} · Support`}
        />
        <div className="border-t border-line pt-5">
          {r.status === 'Closed' ? (
            <p className="text-sm text-muted">This request is closed. Submit a new request if you still need help.</p>
          ) : (
            <ReplyForm
              send={(body) => reply.mutateAsync(body)}
              pending={reply.isPending}
              hint={r.status === 'Resolved' ? 'Replying reopens this request.' : undefined}
            />
          )}
        </div>
      </Card>
    </>
  )
}
