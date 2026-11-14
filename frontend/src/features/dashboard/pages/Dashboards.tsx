import type { ReactNode } from 'react'
import {
  AlertTriangle, Bell, BookOpen, CalendarDays, ClipboardCheck, CreditCard, GraduationCap, Inbox, Layers, ListChecks, Users,
} from 'lucide-react'
import type { UseQueryResult } from '@tanstack/react-query'
import { useProfile } from '@/features/profile/hooks'
import { formatDateOnly, formatMoney } from '@/shared/lib/format'
import { Card } from '@/shared/ui/Card'
import { PageHeader } from '@/shared/ui/PageHeader'
import { QueryError } from '@/shared/ui/QueryError'
import { StatCard, StatCardSkeleton } from '@/shared/ui/StatCard'
import { useAdminDashboard, useInstructorDashboard, useStudentDashboard } from '../hooks'

function greeting() {
  const h = new Date().getHours()
  return h < 12 ? 'Good morning' : h < 18 ? 'Good afternoon' : 'Good evening'
}

function firstName(full?: string) {
  return full?.split(' ')[0] ?? ''
}

const plural = (n: number, word: string) => `${n} ${n === 1 ? word : `${word}s`}`

/** Loading → skeleton cards, error → message with retry, then the cards. */
function StatGrid<T>({ query, cards, columns, children }: {
  query: UseQueryResult<T>
  cards: number
  columns: string
  children: (data: T) => ReactNode
}) {
  const grid = `grid gap-4 sm:grid-cols-2 ${columns}`
  if (query.isPending) {
    return (
      <div className={grid} role="status" aria-busy="true" aria-label="Loading your numbers">
        {Array.from({ length: cards }, (_, i) => <StatCardSkeleton key={i} />)}
      </div>
    )
  }
  if (query.isError) {
    return (
      <Card>
        <QueryError error={query.error} onRetry={() => void query.refetch()} />
      </Card>
    )
  }
  return <div className={grid}>{children(query.data)}</div>
}

export function StudentDashboard() {
  const { data } = useProfile()
  const dashboard = useStudentDashboard()
  return (
    <>
      <PageHeader
        documentTitle="Dashboard"
        eyebrow={data?.student?.majorName ?? 'Student'}
        title={`${greeting()}, ${firstName(data?.fullName)}`}
      >
        ID {data?.universityId ?? '…'}
        {data?.student && ` · ${data.student.campusName}`}
        {dashboard.data?.termName && ` · ${dashboard.data.termName}`}
      </PageHeader>
      <StatGrid query={dashboard} cards={6} columns="lg:grid-cols-3">
        {(d) => (
          <>
            <StatCard
              label="Cumulative GPA"
              value={d.cumulativeGpa === null ? '—' : d.cumulativeGpa.toFixed(2)}
              hint={d.cumulativeGpa === null ? 'No finalized grades yet' : `Out of 4.0 · ${d.creditsEarned} credits earned`}
              icon={GraduationCap}
              to="/student/grades"
            />
            <StatCard
              label="Credits this term"
              value={`${d.enrolledCredits} / ${d.maxCredits}`}
              hint={
                d.enrolledCredits >= d.maxCredits
                  ? 'You have reached your credit cap'
                  : `${d.maxCredits - d.enrolledCredits} more credits allowed`
              }
              icon={BookOpen}
              to="/student/cart"
            />
            <StatCard
              label="Classes today"
              value={String(d.classesToday)}
              hint={d.classesToday === 0 ? 'Nothing on your timetable today' : 'See the times in your schedule'}
              icon={CalendarDays}
              to="/student/schedule"
            />
            <StatCard
              label="Outstanding balance"
              value={formatMoney(d.outstandingBalance)}
              hint={
                d.overdueBalance > 0
                  ? `${formatMoney(d.overdueBalance)} is overdue`
                  : d.nextDueDate
                    ? `Next due ${formatDateOnly(d.nextDueDate)}`
                    : 'Nothing to pay'
              }
              tone={d.overdueBalance > 0 ? 'danger' : 'default'}
              icon={CreditCard}
              to="/student/finance"
            />
            <StatCard
              label="Attendance warnings"
              value={String(d.attendanceWarnings)}
              hint={
                d.attendanceWarnings === 0
                  ? 'No absences this term'
                  : d.attendanceCritical > 0
                    ? `${plural(d.attendanceCritical, 'course')} close to the absence limit`
                    : 'Some absences, still under the limit'
              }
              tone={d.attendanceCritical > 0 ? 'danger' : d.attendanceWarnings > 0 ? 'warning' : 'default'}
              icon={d.attendanceCritical > 0 ? AlertTriangle : ClipboardCheck}
              to="/student/attendance"
            />
            <StatCard
              label="Unread notifications"
              value={String(d.unreadNotifications)}
              hint={d.unreadNotifications === 0 ? "You're all caught up" : 'Open the bell at the top to read them'}
              icon={Bell}
            />
          </>
        )}
      </StatGrid>
    </>
  )
}

export function InstructorDashboard() {
  const { data } = useProfile()
  const dashboard = useInstructorDashboard()
  return (
    <>
      <PageHeader
        documentTitle="Dashboard"
        eyebrow={data?.instructor?.departmentName ?? 'Instructor'}
        title={`${greeting()}, ${firstName(data?.fullName)}`}
      >
        ID {data?.universityId ?? '…'}
        {dashboard.data?.termName && ` · ${dashboard.data.termName}`}
      </PageHeader>
      <StatGrid query={dashboard} cards={4} columns="lg:grid-cols-4">
        {(d) => (
          <>
            <StatCard
              label="Active sections"
              value={String(d.activeSections)}
              hint={d.termName ?? 'No current term'}
              icon={Layers}
              to="/instructor/schedule"
            />
            <StatCard
              label="Classes today"
              value={String(d.classesToday)}
              hint={d.classesToday === 0 ? 'No classes today' : 'See the times in your schedule'}
              icon={CalendarDays}
              to="/instructor/schedule"
            />
            <StatCard
              label="Missing scores"
              value={String(d.missingScores)}
              hint={d.missingScores === 0 ? 'Every score is entered' : `In ${plural(d.sectionsWithMissingScores, 'section')}`}
              tone={d.missingScores > 0 ? 'warning' : 'default'}
              icon={ListChecks}
              to="/instructor/grading"
            />
            <StatCard
              label="Attendance to take"
              value={String(d.attendanceToTake)}
              hint={d.attendanceToTake === 0 ? 'Up to date this week' : 'Classes this week with no attendance yet'}
              tone={d.attendanceToTake > 0 ? 'warning' : 'default'}
              icon={ClipboardCheck}
              to="/instructor/attendance"
            />
          </>
        )}
      </StatGrid>
    </>
  )
}

export function AdminDashboard() {
  const { data } = useProfile()
  const dashboard = useAdminDashboard()
  return (
    <>
      <PageHeader documentTitle="Dashboard" eyebrow="Administration" title={`${greeting()}, ${firstName(data?.fullName)}`}>
        {dashboard.data?.termName && `Current term: ${dashboard.data.termName}`}
      </PageHeader>
      <StatGrid query={dashboard} cards={4} columns="lg:grid-cols-4">
        {(d) => (
          <>
            <StatCard
              label="Active students"
              value={String(d.activeStudents)}
              hint={plural(d.activeInstructors, 'active instructor')}
              icon={Users}
              to="/admin/users"
            />
            <StatCard
              label="Sections this term"
              value={String(d.sectionsThisTerm)}
              hint={d.termName ?? 'No current term'}
              icon={Layers}
              to="/admin/sections"
            />
            <StatCard
              label="Open support requests"
              value={String(d.openSupportRequests)}
              hint={d.newSupportRequests === 0 ? 'None waiting to be picked up' : `${d.newSupportRequests} new, not picked up yet`}
              tone={d.newSupportRequests > 0 ? 'warning' : 'default'}
              icon={Inbox}
              to="/admin/support"
            />
            <StatCard
              label="Unpaid charges"
              value={formatMoney(d.outstandingTotal)}
              hint={`${plural(d.unpaidCharges, 'charge')} pending or partly paid`}
              icon={CreditCard}
              to="/admin/payments"
            />
          </>
        )}
      </StatGrid>
    </>
  )
}
