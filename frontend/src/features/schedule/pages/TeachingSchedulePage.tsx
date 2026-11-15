import { Link } from 'react-router'
import { CalendarDays, ClipboardCheck, ListChecks } from 'lucide-react'
import { Card } from '@/shared/ui/Card'
import { EmptyState } from '@/shared/ui/EmptyState'
import { PageHeader } from '@/shared/ui/PageHeader'
import { QueryError } from '@/shared/ui/QueryError'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { WeekSchedule } from '../components/WeekSchedule'
import { useTeachingSchedule } from '../hooks'
import type { ScheduleItem } from '../types'

const linkClass =
  'inline-flex h-11 items-center gap-2 rounded-lg px-3 text-sm font-medium transition-colors sm:h-9 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus'

/** Per class: take its attendance (deep link: section + meeting, the date defaults to its latest occurrence) and grades. */
function ClassLinks({ item }: { item: ScheduleItem }) {
  const name = `${item.courseCode} ${item.sectionCode}`
  return (
    <>
      <Link
        to={`/instructor/attendance?section=${item.sectionId}&meeting=${item.meetingId}`}
        aria-label={`Take attendance for ${name} ${item.type}`}
        className={`${linkClass} border border-line-strong/60 bg-surface text-ink shadow-sm hover:bg-canvas`}
      >
        <ClipboardCheck className="size-4" aria-hidden /> Take attendance
      </Link>
      <Link
        to={`/instructor/grading?section=${item.sectionId}`}
        aria-label={`Grades for ${name}`}
        className={`${linkClass} text-muted hover:bg-brand-50 hover:text-ink`}
      >
        <ListChecks className="size-4" aria-hidden /> Grades
      </Link>
    </>
  )
}

/** PB-06 / T22: the instructor's weekly lectures and labs for the current term. */
export function TeachingSchedulePage() {
  const schedule = useTeachingSchedule()
  const data = schedule.data

  return (
    <>
      <PageHeader eyebrow={data?.termName ?? 'Teaching'} title="Teaching schedule" icon={CalendarDays}>
        The sections you teach this week, with their lectures and labs.
      </PageHeader>

      <Card>
        {schedule.isPending ? (
          <TableSkeleton label="Loading schedule" rows={4} />
        ) : schedule.isError ? (
          <QueryError error={schedule.error} onRetry={() => schedule.refetch()} />
        ) : data?.termId == null ? (
          <EmptyState icon={CalendarDays} title="No current term">
            Your teaching schedule appears here once a term starts.
          </EmptyState>
        ) : data.items.length === 0 ? (
          <EmptyState icon={CalendarDays} title="You are not teaching this term">
            Sections the admin assigns to you appear here.
          </EmptyState>
        ) : (
          <WeekSchedule items={data.items} actions={(item) => <ClassLinks item={item} />} />
        )}
      </Card>
    </>
  )
}
