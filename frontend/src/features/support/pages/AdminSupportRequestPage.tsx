import { Link, useParams } from 'react-router'
import { ArrowLeft } from 'lucide-react'
import { useAuth } from '@/shared/auth/AuthProvider'
import { errorMessage } from '@/shared/api/problem'
import { formatDateTime } from '@/shared/lib/format'
import { useSavedMessage } from '@/shared/lib/useSavedMessage'
import { Alert } from '@/shared/ui/Alert'
import { Button } from '@/shared/ui/Button'
import { Card } from '@/shared/ui/Card'
import { PageHeader } from '@/shared/ui/PageHeader'
import { QueryError } from '@/shared/ui/QueryError'
import { SelectField } from '@/shared/ui/SelectField'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { ReplyForm } from '../components/ReplyForm'
import { StatusBadge, statusLabels } from '../components/StatusBadge'
import { Thread } from '../components/Thread'
import {
  useAdminReply, useAdminSupportRequest, useAssignSupportRequest, useSetSupportStatus, useSupportAdmins,
} from '../hooks'
import { supportStatuses, type SupportStatus } from '../types'

/** PB-14 / T54: one request — the student, status, assignee, the thread and the admin's reply box. */
export function AdminSupportRequestPage() {
  const id = Number(useParams().id)
  const { user } = useAuth()
  const request = useAdminSupportRequest(id)
  const admins = useSupportAdmins()
  const setStatus = useSetSupportStatus(id)
  const assign = useAssignSupportRequest(id)
  const reply = useAdminReply(id)
  const saved = useSavedMessage()

  const back = (
    <Link to="/admin/support" className="mb-4 inline-flex items-center gap-1.5 text-sm text-muted hover:text-ink">
      <ArrowLeft className="size-4" aria-hidden /> Support inbox
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
  const actionError = setStatus.isError ? setStatus.error : assign.isError ? assign.error : null
  const assignTo = (adminId: number | null) => {
    setStatus.reset()
    assign.mutate(adminId, {
      onSuccess: (d) => saved.show(d.assignedAdminName ? `Assigned to ${d.assignedAdminName}.` : 'Request unassigned.'),
    })
  }

  return (
    <>
      {back}
      <PageHeader eyebrow={r.categoryName} title={r.subject}>
        <span className="inline-flex flex-wrap items-center gap-2">
          <StatusBadge status={r.status} />
          <span>Submitted {formatDateTime(r.createdAt)}</span>
          {r.resolvedAt && <span>· resolved {formatDateTime(r.resolvedAt)}</span>}
        </span>
      </PageHeader>

      {saved.message && (
        <div className="mb-4">
          <Alert tone="success">{saved.message}</Alert>
        </div>
      )}

      <div className="grid gap-6 lg:grid-cols-[1fr_300px]">
        <Card className="space-y-6 p-5 lg:order-2 lg:self-start">
          <section aria-label="Student" className="text-sm">
            <h2 className="font-medium text-ink">Student</h2>
            <p className="mt-1 text-ink">{r.studentName}</p>
            <p className="text-muted">{r.studentUniversityId}</p>
            <a className="break-all text-brand hover:underline" href={`mailto:${r.studentEmail}`}>
              {r.studentEmail}
            </a>
          </section>
          {actionError && <Alert>{errorMessage(actionError)}</Alert>}
          <SelectField
            label="Status"
            value={r.status}
            disabled={setStatus.isPending}
            hint="The student is notified and emailed."
            onChange={(e) => {
              assign.reset()
              setStatus.mutate(e.target.value as SupportStatus, {
                onSuccess: (d) => saved.show(`Status changed to ${statusLabels[d.status]}.`),
              })
            }}
          >
            {supportStatuses.map((s) => (
              <option key={s} value={s}>
                {statusLabels[s]}
              </option>
            ))}
          </SelectField>
          <div className="space-y-2">
            <SelectField
              label="Assigned to"
              value={r.assignedAdminId ?? ''}
              disabled={assign.isPending || !admins.data}
              onChange={(e) => assignTo(e.target.value ? Number(e.target.value) : null)}
            >
              <option value="">Unassigned</option>
              {/* Keep the current assignee even if they are no longer an active admin. */}
              {r.assignedAdminId !== null && !admins.data?.some((a) => a.id === r.assignedAdminId) && (
                <option value={r.assignedAdminId}>{r.assignedAdminName}</option>
              )}
              {admins.data?.map((a) => (
                <option key={a.id} value={a.id}>
                  {a.fullName}
                </option>
              ))}
            </SelectField>
            {user && r.assignedAdminId !== user.id && (
              <Button variant="secondary" className="w-full" loading={assign.isPending} onClick={() => assignTo(user.id)}>
                Assign to me
              </Button>
            )}
          </div>
        </Card>

        <Card className="space-y-6 p-5">
          <Thread
            description={r.description}
            createdAt={r.createdAt}
            submittedBy={r.studentName}
            messages={r.messages}
            staffLabel={(m) => (user && m.authorId === user.id ? 'You' : `${m.authorName} · Admin`)}
          />
          <div className="border-t border-line pt-5">
            {r.status === 'Closed' ? (
              <p className="text-sm text-muted">This request is closed. Change its status to reply again.</p>
            ) : (
              <ReplyForm
                send={(body) => reply.mutateAsync(body)}
                pending={reply.isPending}
                label="Reply to the student"
                hint={
                  r.status === 'Open'
                    ? 'The student is notified and emailed. The request moves to In progress.'
                    : 'The student is notified and emailed.'
                }
              />
            )}
          </div>
        </Card>
      </div>
    </>
  )
}
