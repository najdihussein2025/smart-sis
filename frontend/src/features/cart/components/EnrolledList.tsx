import { useState } from 'react'
import { BookCheck, XCircle } from 'lucide-react'
import { useDrop, useMyEnrollments } from '@/features/enrollment/hooks'
import type { EnrolledSection } from '@/features/enrollment/types'
import { errorMessage } from '@/shared/api/problem'
import { Button } from '@/shared/ui/Button'
import { Card } from '@/shared/ui/Card'
import { CardHeader } from '@/shared/ui/CardHeader'
import { ConfirmDialog } from '@/shared/ui/ConfirmDialog'
import { EmptyState } from '@/shared/ui/EmptyState'
import { MeetingList } from '@/shared/ui/MeetingList'
import { QueryError } from '@/shared/ui/QueryError'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'

/** PB-07 / T25: the sections the student is enrolled in, with Drop (add/drop period only, after a confirmation). */
export function EnrolledList({ onDropped }: { onDropped: (message: string) => void }) {
  const mine = useMyEnrollments()
  const drop = useDrop()
  const [dropping, setDropping] = useState<EnrolledSection | null>(null)
  const canDrop = mine.data?.window === 'AddDrop'

  const close = () => {
    drop.reset()
    setDropping(null)
  }

  return (
    <Card>
      <CardHeader
        title="Enrolled this term"
        icon={BookCheck}
        description={
          mine.data && mine.data.items.length > 0
            ? `${mine.data.items.length} ${mine.data.items.length === 1 ? 'section' : 'sections'}`
            : undefined
        }
      />
      {mine.isPending ? (
        <TableSkeleton label="Loading your courses" />
      ) : mine.isError ? (
        <QueryError error={mine.error} onRetry={() => mine.refetch()} />
      ) : mine.data.items.length === 0 ? (
        <EmptyState icon={BookCheck} title="No courses yet">
          Sections you enroll in appear here, with Drop during the add/drop period.
        </EmptyState>
      ) : (
        <>
          <ul className="divide-y divide-line" aria-label="Enrolled sections">
            {mine.data.items.map((e) => {
              const label = `${e.courseCode} ${e.sectionCode}`
              return (
                <li key={e.enrollmentId} className="flex flex-wrap items-start gap-3 px-4 py-4 sm:px-5">
                  <div className="min-w-0 flex-1 space-y-1.5">
                    <p className="text-ink">
                      <span className="font-semibold">{label}</span> <span>{e.courseName}</span>
                    </p>
                    <p className="tabular text-sm text-muted">
                      {e.credits} credits · {e.instructorName ?? 'Instructor to be announced'}
                    </p>
                    <MeetingList meetings={e.meetings} label={`Meetings of ${label}`} />
                  </div>
                  <Button
                    variant="danger-ghost"
                    size="sm"
                    disabled={!canDrop}
                    onClick={() => setDropping(e)}
                    aria-label={`Drop ${label}`}
                  >
                    <XCircle className="size-4" aria-hidden /> Drop
                  </Button>
                </li>
              )
            })}
          </ul>
          {!canDrop && <p className="border-t border-line px-4 py-3 text-xs text-muted sm:px-5">You can drop a course only during the add/drop period.</p>}
        </>
      )}

      <ConfirmDialog
        open={dropping !== null}
        title={dropping ? `Drop ${dropping.courseCode} ${dropping.sectionCode}?` : 'Drop course?'}
        confirmLabel="Drop course"
        loading={drop.isPending}
        error={drop.isError ? errorMessage(drop.error) : null}
        onClose={close}
        onConfirm={() =>
          dropping &&
          drop.mutate(dropping.sectionId, {
            onSuccess: () => {
              onDropped(`Dropped ${dropping.courseCode} ${dropping.sectionCode}. Your seat went back to the section.`)
              close()
            },
          })
        }
      >
        You will lose your seat in this section. You can enroll again later only if a seat is free.
      </ConfirmDialog>
    </Card>
  )
}
