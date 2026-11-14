import { Link } from 'react-router'
import { CalendarDays } from 'lucide-react'
import { Card } from '@/shared/ui/Card'
import { EmptyState } from '@/shared/ui/EmptyState'
import { PageHeader } from '@/shared/ui/PageHeader'
import { QueryError } from '@/shared/ui/QueryError'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { WeekSchedule } from '../components/WeekSchedule'
import { useMySchedule } from '../hooks'

/** PB-06 / T22: the student's weekly classes for the current term, one day tab at a time. */
export function SchedulePage() {
  const schedule = useMySchedule()
  const data = schedule.data

  return (
    <>
      <PageHeader eyebrow={data?.termName ?? 'Schedule'} title="My schedule">
        Your classes this week. Pick a day to see when and where they meet.
      </PageHeader>

      <Card>
        {schedule.isPending ? (
          <TableSkeleton label="Loading schedule" rows={4} />
        ) : schedule.isError ? (
          <QueryError error={schedule.error} onRetry={() => schedule.refetch()} />
        ) : data?.termId == null ? (
          <EmptyState icon={CalendarDays} title="No current term">
            Your schedule appears here once a term starts.
          </EmptyState>
        ) : data.items.length === 0 ? (
          <EmptyState icon={CalendarDays} title="You have no classes this term">
            Browse the{' '}
            <Link to="/student/courses" className="font-medium text-brand underline">
              course catalog
            </Link>{' '}
            and enroll in sections to build your schedule.
          </EmptyState>
        ) : (
          <WeekSchedule items={data.items} showInstructor />
        )}
      </Card>
    </>
  )
}
