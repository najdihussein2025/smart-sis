import { useState } from 'react'
import { Link } from 'react-router'
import { ChevronDown, ClipboardCheck } from 'lucide-react'
import { cn } from '@/shared/lib/cn'
import { formatDateOnly } from '@/shared/lib/format'
import { hm, shortDays } from '@/shared/lib/schedule'
import { Badge } from '@/shared/ui/Badge'
import { Card } from '@/shared/ui/Card'
import { EmptyState } from '@/shared/ui/EmptyState'
import { PageHeader } from '@/shared/ui/PageHeader'
import { QueryError } from '@/shared/ui/QueryError'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { StandingBadge } from '../components/StandingBadge'
import { useMyAttendance } from '../hooks'
import type { AbsenceStanding, CourseAttendance } from '../types'

/**
 * PB-11 / T44: the student's absences per course this term with the colour rule (A5): green "Perfect" (none),
 * orange "Warning", red "Critical" (one away from the limit or more). Each course opens to the dates missed.
 */
export function StudentAttendancePage() {
  const mine = useMyAttendance()

  return (
    <>
      <PageHeader eyebrow={mine.data?.termName ?? 'This term'} title="Attendance">
        Each course allows 3 absences per credit (9 for a 3-credit course). One absence over the limit drops the course with
        an F that counts in your GPA.
      </PageHeader>

      {mine.isPending ? (
        <Card>
          <TableSkeleton label="Loading attendance" />
        </Card>
      ) : mine.isError ? (
        <Card>
          <QueryError error={mine.error} onRetry={() => mine.refetch()} />
        </Card>
      ) : mine.data.courses.length === 0 ? (
        <Card>
          <EmptyState icon={ClipboardCheck} title="No courses this term">
            Your attendance shows here once you are enrolled. Enroll from your{' '}
            <Link to="/student/cart" className="font-medium text-brand underline">
              cart
            </Link>
            .
          </EmptyState>
        </Card>
      ) : (
        <ul className="grid gap-4 md:grid-cols-2" aria-label="Attendance per course">
          {mine.data.courses.map((c) => (
            <CourseCard key={c.sectionId} course={c} />
          ))}
        </ul>
      )}
    </>
  )
}

const bar: Record<AbsenceStanding, string> = { Perfect: 'bg-success', Warning: 'bg-warning', Critical: 'bg-danger' }

function CourseCard({ course: c }: { course: CourseAttendance }) {
  const [open, setOpen] = useState(false)
  const label = `${c.courseCode} ${c.sectionCode}`
  const dropped = c.enrollmentStatus === 'DroppedAbsence'
  const datesId = `absences-${c.sectionId}`

  return (
    <li>
      <Card className="h-full">
        <div className="space-y-3 p-5">
          <div className="flex items-start justify-between gap-3">
            <div className="min-w-0">
              <h2 className="font-display text-lg font-semibold text-ink">{label}</h2>
              <p className="text-sm text-muted">{c.courseName}</p>
            </div>
            <div className="flex shrink-0 flex-wrap justify-end gap-1.5">
              {dropped && <Badge tone="danger">Dropped (F)</Badge>}
              <StandingBadge standing={c.standing} />
            </div>
          </div>

          <div>
            <p className="text-sm text-ink">
              <span className="font-display text-2xl font-semibold">{c.absences}</span>
              <span className="text-muted"> of {c.limit} absences</span>
            </p>
            <div className="mt-2 h-2 overflow-hidden rounded-full bg-canvas" aria-hidden>
              <div className={cn('h-full rounded-full', bar[c.standing])} style={{ width: `${Math.min(100, (c.absences / c.limit) * 100)}%` }} />
            </div>
            <p className="mt-2 text-sm text-muted">{summary(c)}</p>
          </div>
        </div>

        <div className="border-t border-line">
          <button
            type="button"
            className="flex w-full items-center justify-between px-5 py-3 text-sm font-medium text-ink hover:bg-canvas"
            aria-expanded={open}
            aria-controls={datesId}
            onClick={() => setOpen((o) => !o)}
          >
            {open ? 'Hide' : 'Show'} absence dates for {label}
            <ChevronDown className={cn('size-4 text-muted transition-transform', open && 'rotate-180')} aria-hidden />
          </button>
          {open && (
            <div id={datesId} className="px-5 pb-4">
              {c.absenceDates.length === 0 ? (
                <p className="text-sm text-muted">No absences.</p>
              ) : (
                <ul className="space-y-1 text-sm" aria-label={`Absence dates for ${label}`}>
                  {c.absenceDates.map((d) => (
                    <li key={`${d.date}-${d.startTime}`} className="flex justify-between gap-3">
                      <span className="text-ink">
                        {shortDays[d.dayOfWeek]} {formatDateOnly(d.date)}
                      </span>
                      <span className="text-muted">
                        {hm(d.startTime)} · {d.type}
                      </span>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          )}
        </div>
      </Card>
    </li>
  )
}

function summary(c: CourseAttendance): string {
  if (c.enrollmentStatus === 'DroppedAbsence') return 'You were dropped from this course for going over the absence limit.'
  if (c.absences === 0) return `No absences in ${c.sessionsRecorded} recorded ${c.sessionsRecorded === 1 ? 'class' : 'classes'}.`
  if (c.remaining === 0) return 'You are at the limit. One more absence drops the course.'
  return `${c.remaining} ${c.remaining === 1 ? 'absence' : 'absences'} left before the limit.`
}
