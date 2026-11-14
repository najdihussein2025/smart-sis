import { useState } from 'react'
import { BookCheck, LogOut } from 'lucide-react'
import { useDrop, useMyEnrollments } from '@/features/enrollment/hooks'
import type { EnrolledSection } from '@/features/enrollment/types'
import { errorMessage } from '@/shared/api/problem'
import { Button } from '@/shared/ui/Button'
import { Card } from '@/shared/ui/Card'
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
      <div className="flex flex-wrap items-baseline justify-between gap-2 border-b border-line px-5 py-4">
        <h2 className="font-medium text-ink">Enrolled this term</h2>
        {mine.data && (
          <p className="text-sm text-muted">
            {mine.data.enrolledCredits} of {mine.data.maxCredits} credits
          </p>
        )}
      </div>
      {mine.isPending ? (
        <TableSkeleton label="Loading your courses" />
      ) : mine.isError ? (
        <QueryError error={mine.error} onRetry={() => mine.refetch()} />
      ) : mine.data.items.length === 0 ? (
        <EmptyState icon={BookCheck} title="No courses yet">
          Sections you enroll in appear here.
        </EmptyState>
      ) : (
        <>
          <ul className="divide-y divide-line" aria-label="Enrolled sections">
            {mine.data.items.map((e) => {
              const label = `${e.courseCode} ${e.sectionCode}`
              return (
                <li key={e.enrollmentId} className="flex flex-wrap items-start gap-3 px-5 py-4">
                  <div className="min-w-0 flex-1 space-y-1.5">
                    <p>
                      <span className="font-medium text-ink">{label}</span> <span className="text-ink">{e.courseName}</span>
                    </p>
                    <p className="text-sm text-muted">
                      {e.credits} credits · {e.instructorName ?? 'Instructor to be announced'}
                    </p>
                    <MeetingList meetings={e.meetings} label={`Meetings of ${label}`} />
                  </div>
                  <Button
                    variant="secondary"
                    className="h-8 px-2.5"
                    disabled={!canDrop}
                    onClick={() => setDropping(e)}
                    aria-label={`Drop ${label}`}
                  >
                    <LogOut className="size-4" aria-hidden /> Drop
                  </Button>
                </li>
              )
            })}
          </ul>
          {!canDrop && <p className="border-t border-line px-5 py-3 text-xs text-muted">You can drop a course only during the add/drop period.</p>}
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
