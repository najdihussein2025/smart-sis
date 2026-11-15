import { Link } from 'react-router'
import {
  AlertTriangle, Bell, BookOpen, CalendarClock, CalendarDays, CalendarRange, ClipboardCheck, Clock, CreditCard, FileWarning,
  GraduationCap, Inbox, Layers, Library, ListChecks, MapPin, Scale, ShoppingCart, Sparkles, User, UserCog, Users,
} from 'lucide-react'
import { useMySchedule, useTeachingSchedule } from '@/features/schedule/hooks'
import type { ScheduleItem } from '@/features/schedule/types'
import { useCart } from '@/features/cart/hooks'
import { useTeachingSections } from '@/features/grading/hooks'
import { useTerms } from '@/features/terms/hooks'
import type { Term } from '@/features/terms/types'
import { useQuestionBank } from '@/features/career/hooks'
import { useKnowledgeDocuments } from '@/features/knowledge/hooks'
import { useAdminSupportRequests } from '@/features/support/hooks'
import { useProfile } from '@/features/profile/hooks'
import { formatDateOnly, formatDateTime, formatMoney } from '@/shared/lib/format'
import { days, hm } from '@/shared/lib/schedule'
import type { AttentionItem } from '@/shared/ui/AttentionList'
import { Badge } from '@/shared/ui/Badge'
import { Card } from '@/shared/ui/Card'
import { CardHeader } from '@/shared/ui/CardHeader'
import { PageHeader } from '@/shared/ui/PageHeader'
import { QueryError } from '@/shared/ui/QueryError'
import { StatCard } from '@/shared/ui/StatCard'
import { WindowBadge } from '@/shared/ui/WindowBadge'
import {
  AttentionPanel, couldNotCheck, DashboardColumns, Fact, HeroCard, HeroSkeleton, linkButton, plural, StatGrid,
} from '../components/DashboardParts'
import { useAdminDashboard, useInstructorDashboard, useStudentDashboard } from '../hooks'
import { apiDayOf, apiDayOfIso, clockOf, planDay } from '../lib/nextClass'

function greeting() {
  const h = new Date().getHours()
  return h < 12 ? 'Good morning' : h < 18 ? 'Good afternoon' : 'Good evening'
}

function firstName(full?: string) {
  return full?.split(' ')[0] ?? ''
}

const timeRange = (i: ScheduleItem) => `${hm(i.startTime)}–${hm(i.endTime)}`

// ---------------------------------------------------------------------------------------------------------------------
// Student
// ---------------------------------------------------------------------------------------------------------------------

function StudentNextUp() {
  const schedule = useMySchedule()
  if (schedule.isPending) return <HeroSkeleton label="Loading your timetable" />
  if (schedule.isError) {
    return (
      <Card>
        <CardHeader title="Next up" icon={CalendarClock} />
        <QueryError error={schedule.error} onRetry={() => void schedule.refetch()} />
      </Card>
    )
  }

  const { termId, items } = schedule.data
  if (termId === null) {
    return (
      <HeroCard eyebrow="Your week" title="No current term" icon={CalendarClock}>
        Your timetable shows up here once a term starts.
      </HeroCard>
    )
  }
  if (items.length === 0) {
    return (
      <HeroCard
        eyebrow="Your week"
        title="No classes on your timetable yet"
        icon={CalendarClock}
        actions={<Link to="/student/courses" className={linkButton.onBrand}>Browse courses</Link>}
      >
        Add sections to your cart from the course catalog, then enroll in them.
      </HeroCard>
    )
  }

  const now = new Date()
  const plan = planDay(items, apiDayOf(now), clockOf(now))
  const next = plan.next!
  const { item } = next
  const eyebrow =
    next.when === 'now'
      ? 'Happening now'
      : next.when === 'today'
        ? 'Next up today'
        : plan.today.length === 0
          ? 'No classes today · next up'
          : 'No more classes today · next up'
  const when =
    next.when === 'now' ? `Now, until ${hm(item.endTime)}` : next.when === 'today' ? `Today ${timeRange(item)}` : `${days[item.dayOfWeek]} ${timeRange(item)}`
  const laterToday = plan.remainingToday.filter((i) => i !== item)

  return (
    <HeroCard
      eyebrow={eyebrow}
      title={`${item.courseCode} · ${item.courseName}`}
      icon={CalendarClock}
      actions={
        <>
          <Link to="/student/schedule" className={linkButton.onBrand}>Full schedule</Link>
          <Link to="/student/attendance" className={linkButton.onBrandGhost}>My attendance</Link>
        </>
      }
    >
      <p className="flex flex-wrap gap-x-5 gap-y-1.5">
        <Fact icon={Clock} className="tabular font-medium text-white">{when}</Fact>
        <Fact icon={MapPin}>{item.roomName}</Fact>
        {item.instructorName && <Fact icon={User}>{item.instructorName}</Fact>}
        <Fact icon={BookOpen}>
          {item.type} · Section {item.sectionCode}
        </Fact>
      </p>
      {laterToday.length > 0 && (
        <div className="mt-4 border-t border-white/15 pt-3">
          <p className="text-xs font-semibold tracking-[0.14em] text-white/60 uppercase">Later today</p>
          <ul className="mt-1.5 space-y-1">
            {laterToday.map((i) => (
              <li key={i.meetingId} className="flex flex-wrap gap-x-3">
                <span className="tabular w-24 shrink-0 text-white">{timeRange(i)}</span>
                <span className="min-w-0">
                  {i.courseCode} · {i.roomName}
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}
    </HeroCard>
  )
}

export function StudentDashboard() {
  const { data } = useProfile()
  const dashboard = useStudentDashboard()
  const cart = useCart()

  const items: AttentionItem[] = []
  const d = dashboard.data
  if (d) {
    if (d.overdueBalance > 0) {
      items.push({
        id: 'overdue', icon: CreditCard, tone: 'danger', to: '/student/finance',
        title: `${formatMoney(d.overdueBalance)} is overdue`,
        detail: `${formatMoney(d.outstandingBalance)} outstanding in total`,
      })
    } else if (d.outstandingBalance > 0) {
      items.push({
        id: 'balance', icon: CreditCard, tone: 'info', to: '/student/finance',
        title: d.nextDueDate ? `Payment due ${formatDateOnly(d.nextDueDate)}` : 'You have a balance to pay',
        detail: `${formatMoney(d.outstandingBalance)} outstanding`,
      })
    }
    if (d.attendanceCritical > 0) {
      items.push({
        id: 'attendance-critical', icon: AlertTriangle, tone: 'danger', to: '/student/attendance',
        title: `${plural(d.attendanceCritical, 'course')} close to the absence limit`,
        detail: 'Going over the limit drops you from the course with an F',
      })
    }
    const warningOnly = d.attendanceWarnings - d.attendanceCritical
    if (warningOnly > 0) {
      items.push({
        id: 'attendance-warning', icon: ClipboardCheck, tone: 'warning', to: '/student/attendance',
        title: `Absences in ${plural(warningOnly, 'course')}`,
        detail: 'Still under the limit',
      })
    }
  }
  if (cart.data && cart.data.items.length > 0) {
    const n = cart.data.items.length
    const open = cart.data.window !== 'Closed'
    items.push({
      id: 'cart', icon: ShoppingCart, tone: open ? 'warning' : 'info', to: '/student/cart',
      title: `${plural(n, 'section')} in your cart, not enrolled yet`,
      detail: open ? 'Enroll before the window closes' : 'Registration is closed right now',
    })
  } else if (cart.isError) {
    items.push(couldNotCheck('cart', 'your cart', '/student/cart'))
  }
  if (d && d.unreadNotifications > 0) {
    items.push({
      id: 'unread', icon: Bell, tone: 'info', to: '/student/announcements',
      title: `${plural(d.unreadNotifications, 'unread notification')}`,
      detail: 'See the latest announcements',
    })
  }

  return (
    <>
      <PageHeader
        documentTitle="Dashboard"
        eyebrow={data?.student?.majorName ?? 'Student'}
        title={`${greeting()}, ${firstName(data?.fullName)}`}
        actions={cart.data?.termId != null ? <WindowBadge window={cart.data.window} /> : undefined}
      >
        ID {data?.universityId ?? '…'}
        {data?.student && ` · ${data.student.campusName}`}
        {d?.termName && ` · ${d.termName}`}
      </PageHeader>
      <DashboardColumns
        main={<StudentNextUp />}
        attention={
          <AttentionPanel
            loading={dashboard.isPending || cart.isPending}
            items={items}
            emptyText="Nothing needs your attention. You're all set."
          />
        }
        stats={
          <StatGrid query={dashboard} cards={4} columns="lg:grid-cols-4 xl:grid-cols-2">
            {(s) => (
              <>
                <StatCard
                  label="Cumulative GPA"
                  value={s.cumulativeGpa === null ? '—' : s.cumulativeGpa.toFixed(2)}
                  hint={s.cumulativeGpa === null ? 'No finalized grades yet' : `Out of 4.0 · ${s.creditsEarned} credits earned`}
                  icon={GraduationCap}
                  to="/student/grades"
                />
                <StatCard
                  label="Credits this term"
                  value={`${s.enrolledCredits} / ${s.maxCredits}`}
                  hint={
                    s.enrolledCredits >= s.maxCredits
                      ? 'You have reached your credit cap'
                      : `${s.maxCredits - s.enrolledCredits} more credits allowed`
                  }
                  icon={BookOpen}
                  to="/student/cart"
                />
                <StatCard
                  label="Outstanding balance"
                  value={formatMoney(s.outstandingBalance)}
                  hint={
                    s.overdueBalance > 0
                      ? `${formatMoney(s.overdueBalance)} is overdue`
                      : s.nextDueDate
                        ? `Next due ${formatDateOnly(s.nextDueDate)}`
                        : 'Nothing to pay'
                  }
                  tone={s.overdueBalance > 0 ? 'danger' : 'default'}
                  icon={CreditCard}
                  to="/student/finance"
                />
                <StatCard
                  label="Attendance warnings"
                  value={String(s.attendanceWarnings)}
                  hint={
                    s.attendanceWarnings === 0
                      ? 'No absence warnings this term'
                      : s.attendanceCritical > 0
                        ? `${plural(s.attendanceCritical, 'course')} close to the absence limit`
                        : 'Some absences, still under the limit'
                  }
                  tone={s.attendanceCritical > 0 ? 'danger' : s.attendanceWarnings > 0 ? 'warning' : 'default'}
                  icon={s.attendanceCritical > 0 ? AlertTriangle : ClipboardCheck}
                  to="/student/attendance"
                />
              </>
            )}
          </StatGrid>
        }
      />
    </>
  )
}

// ---------------------------------------------------------------------------------------------------------------------
// Instructor
// ---------------------------------------------------------------------------------------------------------------------

/** Today's classes, each with its attendance sheet and grades one click away. `today` is the server's date. */
function TodaysClasses({ today }: { today: string | undefined }) {
  const schedule = useTeachingSchedule()
  const header = (description?: string) => (
    <CardHeader
      title="Today's classes"
      description={description}
      icon={CalendarDays}
      actions={<Link to="/instructor/schedule" className={linkButton.ghost}>Full schedule</Link>}
    />
  )

  if (schedule.isPending || today === undefined) {
    return (
      <Card role="status" aria-busy="true" aria-label="Loading today's classes">
        {header()}
        <div className="space-y-3 p-4 sm:p-5">
          {[0, 1].map((i) => <div key={i} className="h-14 animate-pulse rounded-lg bg-canvas" />)}
        </div>
      </Card>
    )
  }
  if (schedule.isError) {
    return (
      <Card>
        {header()}
        <QueryError error={schedule.error} onRetry={() => void schedule.refetch()} />
      </Card>
    )
  }

  const { termId, items } = schedule.data
  const day = apiDayOfIso(today)
  // The server's date has no time of day, so "next" here is the first class on a later weekday.
  const plan = planDay(items, day, '00:00:00')
  const dateLabel = formatDateOnly(today)

  if (plan.today.length === 0) {
    const next = plan.next?.item
    return (
      <Card>
        {header(dateLabel)}
        <div className="px-4 py-6 sm:px-5">
          <p className="font-display text-lg font-semibold text-ink">
            {termId === null ? 'No current term' : items.length === 0 ? 'No sections this term' : 'No classes today'}
          </p>
          {next && (
            <p className="mt-1 text-sm text-muted">
              Next: {days[next.dayOfWeek]} {timeRange(next)} · {next.courseCode} {next.sectionCode} in {next.roomName}
            </p>
          )}
        </div>
      </Card>
    )
  }

  return (
    <Card>
      {header(`${dateLabel} · ${plural(plan.today.length, 'class', 'classes')}`)}
      <ul className="divide-y divide-line">
        {plan.today.map((i) => (
          <li key={i.meetingId} className="flex flex-wrap items-center gap-x-4 gap-y-3 px-4 py-4 sm:px-5">
            <div className="tabular w-[6.5rem] shrink-0 font-display text-base font-semibold text-ink">{timeRange(i)}</div>
            <div className="min-w-0 flex-1 basis-48">
              <p className="font-medium text-ink">
                {i.courseCode} {i.sectionCode} · {i.courseName}
              </p>
              <p className="flex flex-wrap gap-x-4 text-sm text-muted">
                <Fact icon={MapPin}>{i.roomName}</Fact>
                <span>{i.type}</span>
              </p>
            </div>
            <div className="flex flex-wrap gap-2">
              <Link
                to={`/instructor/attendance?section=${i.sectionId}&meeting=${i.meetingId}&date=${today}`}
                className={linkButton.secondary}
                aria-label={`Take attendance for ${i.courseCode} ${i.sectionCode} at ${hm(i.startTime)}`}
              >
                <ClipboardCheck className="size-4" aria-hidden />
                Take attendance
              </Link>
              <Link
                to="/instructor/grading"
                className={linkButton.ghost}
                aria-label={`Grades for ${i.courseCode} ${i.sectionCode}`}
              >
                <ListChecks className="size-4" aria-hidden />
                Grades
              </Link>
            </div>
          </li>
        ))}
      </ul>
    </Card>
  )
}

export function InstructorDashboard() {
  const { data } = useProfile()
  const dashboard = useInstructorDashboard()
  const grading = useTeachingSections()

  const d = dashboard.data
  const sections = grading.data?.filter((s) => !s.isFinalized) ?? []
  const noComponents = sections.filter((s) => s.componentCount === 0)

  const items: AttentionItem[] = []
  if (d && d.attendanceToTake > 0) {
    items.push({
      id: 'attendance', icon: ClipboardCheck, tone: 'warning', to: '/instructor/attendance',
      title: `${plural(d.attendanceToTake, 'attendance session')} still to take`,
      detail: 'Classes since Monday with no attendance yet',
    })
  }
  for (const s of sections) {
    const name = `${s.courseCode} ${s.sectionCode}`
    if (s.componentCount === 0) {
      items.push({
        id: `no-components-${s.sectionId}`, icon: FileWarning, tone: 'info', to: '/instructor/grading',
        title: `${name} has no grade components`,
        detail: 'Scores can be entered once an admin adds them',
      })
      continue
    }
    if (s.notGraded > 0) {
      items.push({
        id: `missing-${s.sectionId}`, icon: ListChecks, tone: 'warning', to: '/instructor/grading',
        title: `${plural(s.notGraded, 'score')} missing in ${name}`,
        detail: s.courseName,
      })
    }
    if (s.totalWeight !== 100) {
      items.push({
        id: `weights-${s.sectionId}`, icon: Scale, tone: 'info', to: '/instructor/grading',
        title: `${name} weights add up to ${s.totalWeight} of 100`,
        detail: "Grades can't be finalized until they reach 100",
      })
    }
  }
  if (grading.isError) items.push(couldNotCheck('grading', 'your grade sheets', '/instructor/grading'))

  const missingHint = (s: { missingScores: number; sectionsWithMissingScores: number }) => {
    if (s.missingScores > 0) return `In ${plural(s.sectionsWithMissingScores, 'section')}`
    if (noComponents.length > 0) return `${plural(noComponents.length, 'section')} with no grade components yet`
    if (grading.isSuccess) return 'Every score is entered'
    return 'None missing in sections that have grade components'
  }

  return (
    <>
      <PageHeader
        documentTitle="Dashboard"
        eyebrow={data?.instructor?.departmentName ?? 'Instructor'}
        title={`${greeting()}, ${firstName(data?.fullName)}`}
      >
        ID {data?.universityId ?? '…'}
        {d?.termName && ` · ${d.termName}`}
      </PageHeader>
      <DashboardColumns
        main={<TodaysClasses today={d?.today} />}
        attention={
          <AttentionPanel
            loading={dashboard.isPending || grading.isPending}
            items={items}
            emptyText="Attendance and scores are up to date."
          />
        }
        stats={
          <StatGrid query={dashboard} cards={4} columns="lg:grid-cols-4 xl:grid-cols-2">
            {(s) => (
              <>
                <StatCard
                  label="Active sections"
                  value={String(s.activeSections)}
                  hint={s.termName ?? 'No current term'}
                  icon={Layers}
                  to="/instructor/schedule"
                />
                <StatCard
                  label="Classes today"
                  value={String(s.classesToday)}
                  hint={s.classesToday === 0 ? 'No classes today' : 'See the times in your schedule'}
                  icon={CalendarDays}
                  to="/instructor/schedule"
                />
                <StatCard
                  label="Missing scores"
                  value={String(s.missingScores)}
                  hint={missingHint(s)}
                  tone={s.missingScores > 0 || noComponents.length > 0 ? 'warning' : 'default'}
                  icon={ListChecks}
                  to="/instructor/grading"
                />
                <StatCard
                  label="Attendance to take"
                  value={String(s.attendanceToTake)}
                  hint={s.attendanceToTake === 0 ? 'Up to date this week' : 'Classes this week with no attendance yet'}
                  tone={s.attendanceToTake > 0 ? 'warning' : 'default'}
                  icon={ClipboardCheck}
                  to="/instructor/attendance"
                />
              </>
            )}
          </StatGrid>
        }
      />
    </>
  )
}

// ---------------------------------------------------------------------------------------------------------------------
// Admin
// ---------------------------------------------------------------------------------------------------------------------

function windowState(start: string, end: string, now: number): { text: string; tone: 'success' | 'info' | 'neutral' } {
  if (now < Date.parse(start)) return { text: 'Upcoming', tone: 'info' }
  if (now <= Date.parse(end)) return { text: 'Open now', tone: 'success' }
  return { text: 'Ended', tone: 'neutral' }
}

function WindowRow({ label, start, end, now }: { label: string; start: string; end: string; now: number }) {
  const state = windowState(start, end, now)
  return (
    <div className="flex flex-wrap items-center justify-between gap-x-4 gap-y-1 px-4 py-3 sm:px-5">
      <dt className="font-medium text-ink">{label}</dt>
      <dd className="flex flex-wrap items-center gap-x-3 gap-y-1 text-sm text-muted">
        <span className="tabular">
          {formatDateTime(start)} – {formatDateTime(end)}
        </span>
        <Badge tone={state.tone} dot>{state.text}</Badge>
      </dd>
    </div>
  )
}

function CurrentTermCard({ terms }: { terms: ReturnType<typeof useTerms> }) {
  const manage = <Link to="/admin/terms" className={linkButton.ghost}>Manage terms</Link>
  if (terms.isPending) {
    return (
      <Card role="status" aria-busy="true" aria-label="Loading the current term">
        <CardHeader title="Current term" icon={CalendarRange} />
        <div className="space-y-3 p-4 sm:p-5">
          {[0, 1].map((i) => <div key={i} className="h-10 animate-pulse rounded-lg bg-canvas" />)}
        </div>
      </Card>
    )
  }
  if (terms.isError) {
    return (
      <Card>
        <CardHeader title="Current term" icon={CalendarRange} actions={manage} />
        <QueryError error={terms.error} onRetry={() => void terms.refetch()} />
      </Card>
    )
  }
  const term = terms.data.find((t) => t.isCurrent)
  if (!term) {
    return (
      <Card>
        <CardHeader title="Current term" icon={CalendarRange} actions={manage} />
        <p className="px-4 py-6 text-sm text-muted sm:px-5">
          No term is marked as current, so students can't enroll and schedules are empty.
        </p>
      </Card>
    )
  }
  const now = Date.now()
  return (
    <Card>
      <CardHeader
        title={term.name}
        description={`${formatDateOnly(term.startDate)} – ${formatDateOnly(term.endDate)}`}
        icon={CalendarRange}
        actions={
          <>
            <WindowBadge window={term.currentWindow} />
            {manage}
          </>
        }
      />
      <dl className="divide-y divide-line">
        <WindowRow label="Enrollment" start={term.enrollStart} end={term.enrollEnd} now={now} />
        <WindowRow label="Add/drop" start={term.addDropStart} end={term.addDropEnd} now={now} />
      </dl>
    </Card>
  )
}

function termAttention(terms: Term[]): AttentionItem | null {
  const term = terms.find((t) => t.isCurrent)
  if (!term) {
    return {
      id: 'no-term', icon: CalendarRange, tone: 'danger', to: '/admin/terms',
      title: 'No current term', detail: "Students can't enroll until a term is marked as current",
    }
  }
  if (Date.parse(`${term.endDate}T23:59:59Z`) < Date.now()) {
    return {
      id: 'term-ended', icon: CalendarRange, tone: 'warning', to: '/admin/terms',
      title: `${term.name} has ended`, detail: 'Mark the next term as current',
    }
  }
  return null
}

export function AdminDashboard() {
  const { data } = useProfile()
  const dashboard = useAdminDashboard()
  const terms = useTerms()
  const bank = useQuestionBank()
  const knowledge = useKnowledgeDocuments()
  // One row is enough: only the total is read.
  const unassigned = useAdminSupportRequests({ status: 'Open', assigned: 'Unassigned', page: 1, pageSize: 1 })

  const items: AttentionItem[] = []
  if (terms.data) {
    const t = termAttention(terms.data)
    if (t) items.push(t)
  } else if (terms.isError) items.push(couldNotCheck('terms', 'the current term', '/admin/terms'))

  if (bank.data) {
    if (bank.data.activeCount < bank.data.minQuestions) {
      items.push({
        id: 'career-quiz', icon: Sparkles, tone: 'danger', to: '/admin/career',
        title: 'Career quiz is switched off',
        detail: `${bank.data.activeCount} of ${bank.data.minQuestions} active questions needed`,
      })
    }
  } else if (bank.isError) items.push(couldNotCheck('career', 'the career quiz', '/admin/career'))

  if (knowledge.data) {
    const ready = knowledge.data.filter((k) => k.status === 'Ready').length
    const failed = knowledge.data.filter((k) => k.status === 'Failed').length
    if (ready === 0) {
      items.push({
        id: 'knowledge-empty', icon: Library, tone: 'warning', to: '/admin/knowledge',
        title: 'Knowledge base is empty',
        detail: knowledge.data.length === 0 ? 'The assistant has no documents to answer from' : 'No document has finished processing',
      })
    }
    if (failed > 0) {
      items.push({
        id: 'knowledge-failed', icon: FileWarning, tone: 'warning', to: '/admin/knowledge',
        title: `${plural(failed, 'document')} failed to process`, detail: 'Re-ingest or replace them',
      })
    }
  } else if (knowledge.isError) items.push(couldNotCheck('knowledge', 'the knowledge base', '/admin/knowledge'))

  if (unassigned.data) {
    const n = unassigned.data.totalCount
    if (n > 0) {
      items.push({
        id: 'support', icon: Inbox, tone: 'warning', to: '/admin/support',
        title: `${plural(n, 'new support request')} with nobody assigned`, detail: 'Pick them up or assign an admin',
      })
    }
  } else if (unassigned.isError) items.push(couldNotCheck('support', 'support requests', '/admin/support'))

  const d = dashboard.data
  if (d && d.unpaidCharges > 0) {
    items.push({
      id: 'unpaid', icon: CreditCard, tone: 'info', to: '/admin/payments',
      title: `${plural(d.unpaidCharges, 'unpaid charge')}`, detail: `${formatMoney(d.outstandingTotal)} still owed`,
    })
  }

  const loading = [dashboard, terms, bank, knowledge, unassigned].some((q) => q.isPending)

  return (
    <>
      <PageHeader documentTitle="Dashboard" eyebrow="Administration" title={`${greeting()}, ${firstName(data?.fullName)}`}>
        {d?.termName ? `Current term: ${d.termName}` : d ? 'No current term' : null}
      </PageHeader>
      <DashboardColumns
        main={<CurrentTermCard terms={terms} />}
        attention={<AttentionPanel loading={loading} items={items} emptyText="Nothing is blocking students or staff." />}
        stats={
          <StatGrid query={dashboard} cards={6} columns="lg:grid-cols-3">
            {(s) => (
              <>
                <StatCard label="Active students" value={String(s.activeStudents)} hint="Can sign in" icon={Users} to="/admin/users" />
                <StatCard label="Active instructors" value={String(s.activeInstructors)} hint="Can sign in" icon={UserCog} to="/admin/users" />
                <StatCard
                  label="Sections this term"
                  value={String(s.sectionsThisTerm)}
                  hint={s.termName ?? 'No current term'}
                  icon={Layers}
                  to="/admin/sections"
                />
                <StatCard
                  label="Open support requests"
                  value={String(s.openSupportRequests)}
                  hint={s.newSupportRequests === 0 ? 'None waiting to be picked up' : `${s.newSupportRequests} new, not picked up yet`}
                  tone={s.newSupportRequests > 0 ? 'warning' : 'default'}
                  icon={Inbox}
                  to="/admin/support"
                />
                <StatCard
                  label="Unpaid charges"
                  value={formatMoney(s.outstandingTotal)}
                  hint={`${plural(s.unpaidCharges, 'charge')} pending or partly paid`}
                  icon={CreditCard}
                  to="/admin/payments"
                />
                <StatCard
                  label="Career quiz questions"
                  value={bank.data ? String(bank.data.activeCount) : '—'}
                  hint={
                    bank.data
                      ? bank.data.activeCount < bank.data.minQuestions
                        ? `${bank.data.minQuestions} needed to open the quiz`
                        : 'Active, the quiz is open'
                      : bank.isError
                        ? "Couldn't load the question bank"
                        : 'Loading…'
                  }
                  tone={bank.data && bank.data.activeCount < bank.data.minQuestions ? 'danger' : 'default'}
                  icon={Sparkles}
                  to="/admin/career"
                />
              </>
            )}
          </StatGrid>
        }
      />
    </>
  )
}
