import { CalendarDays } from 'lucide-react'
import { Card } from '@/shared/ui/Card'
import { EmptyState } from '@/shared/ui/EmptyState'
import { PageHeader } from '@/shared/ui/PageHeader'
import { QueryError } from '@/shared/ui/QueryError'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { WeekSchedule } from '../components/WeekSchedule'
import { useTeachingSchedule } from '../hooks'

/** PB-06 / T22: the instructor's weekly lectures and labs for the current term. */
export function TeachingSchedulePage() {
  const schedule = useTeachingSchedule()
  const data = schedule.data

  return (
    <>
      <PageHeader eyebrow={data?.termName ?? 'Schedule'} title="Teaching schedule">
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
          <WeekSchedule items={data.items} />
        )}
      </Card>
    </>
  )
}
