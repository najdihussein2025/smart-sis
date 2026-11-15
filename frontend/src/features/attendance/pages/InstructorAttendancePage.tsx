import { useState } from 'react'
import { useSearchParams } from 'react-router'
import { ChevronRight, ClipboardCheck, History, Users } from 'lucide-react'
import { errorMessage } from '@/shared/api/problem'
import { cn } from '@/shared/lib/cn'
import { isoToDmy } from '@/shared/lib/dates'
import { formatDateOnly } from '@/shared/lib/format'
import { days, hm, shortDays } from '@/shared/lib/schedule'
import { useUnsavedChanges } from '@/shared/lib/useUnsavedChanges'
import { Alert } from '@/shared/ui/Alert'
import { Button } from '@/shared/ui/Button'
import { Card } from '@/shared/ui/Card'
import { CardHeader } from '@/shared/ui/CardHeader'
import { ConfirmDialog } from '@/shared/ui/ConfirmDialog'
import { DateField } from '@/shared/ui/DateField'
import { EmptyState } from '@/shared/ui/EmptyState'
import { PageHeader } from '@/shared/ui/PageHeader'
import { QueryError } from '@/shared/ui/QueryError'
import { SelectField } from '@/shared/ui/SelectField'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { AttendanceSheetCard } from '../components/AttendanceSheetCard'
import { dateProblem, latestOn } from '../dates'
import { useAttendanceSheet, useSessions, useTeachingSections } from '../hooks'
import type { AttendanceMeeting, TeachingSection } from '../types'

const meetingLabel = (m: AttendanceMeeting) => `${days[m.dayOfWeek]} ${hm(m.startTime)}–${hm(m.endTime)} · ${m.type} · ${m.roomName}`

/** URL search params, so other screens can link straight to a session: `?section=1&meeting=11&date=2026-11-09`. */
const attendanceParams = { section: 'section', meeting: 'meeting', date: 'date' } as const

type Selection = { section: string; meeting: string; date: string | null }

/**
 * PB-10 / T41: the instructor picks a section they teach, then a session (weekly meeting) and a date, marks each
 * enrolled student Present or Absent and submits. A meeting already taken on that date shows what was recorded.
 *
 * The choice lives in the URL. A missing param falls back: the only section or meeting is chosen for you, and the
 * date defaults to the session's latest occurrence up to today. An empty param ("section=") means "none chosen".
 */
export function InstructorAttendancePage() {
  const teaching = useTeachingSections()
  const [params, setParams] = useSearchParams()
  const [dirty, setDirty] = useState(false)
  const [returnTo, setReturnTo] = useState<Selection | null>(null)
  // Remounts the date field when a guarded change is cancelled, so it shows the date still in use.
  const [dateKey, setDateKey] = useState(0)
  const unsaved = useUnsavedChanges(dirty)

  const data = teaching.data
  const sectionParam = params.get(attendanceParams.section)
  const sectionId = sectionParam ?? (data?.sections.length === 1 ? String(data.sections[0]!.sectionId) : '')
  const section = data?.sections.find((s) => s.sectionId === Number(sectionId))
  const meetingParam = params.get(attendanceParams.meeting)
  const meetingId = meetingParam ?? (section?.meetings.length === 1 ? String(section.meetings[0]!.id) : '')
  const meeting = section?.meetings.find((m) => m.id === Number(meetingId))
  // Attendance can be taken from the term's first day up to today (or the term's last day, if earlier).
  const min = section?.termStart ?? ''
  const max = section && data ? (data.today < section.termEnd ? data.today : section.termEnd) : ''
  const dateParam = params.get(attendanceParams.date)
  const date = dateParam ?? (meeting ? (latestOn(meeting.dayOfWeek, min, max) ?? '') : '')
  const problem = meeting && date ? dateProblem(date, meeting.dayOfWeek, min, max) : null
  const ready = !!meeting && !!date && !problem
  const sheet = useAttendanceSheet(ready ? meeting.id : null, ready ? date : null)

  const select = (next: Selection) =>
    setParams(
      (p) => {
        const out = new URLSearchParams(p)
        out.set(attendanceParams.section, next.section)
        out.set(attendanceParams.meeting, next.meeting)
        if (next.date === null) out.delete(attendanceParams.date)
        else out.set(attendanceParams.date, next.date)
        return out
      },
      { replace: true },
    )

  const chooseSection = (id: string) =>
    unsaved.guard(() => {
      setReturnTo(null)
      const s = data?.sections.find((x) => x.sectionId === Number(id))
      // A section with one meeting picks it (the meeting param stays out so the fallback applies).
      if (s && s.meetings.length === 1) setParams({ [attendanceParams.section]: id }, { replace: true })
      else select({ section: id, meeting: '', date: null })
    })
  // Choosing a session fills in its latest date, the usual case (today's class or the last one).
  const chooseMeeting = (id: string) =>
    unsaved.guard(() => {
      setReturnTo(null)
      select({ section: sectionId, meeting: id, date: null })
    })
  const chooseDate = (iso: string) =>
    iso !== date &&
    unsaved.guard(() => {
      setReturnTo(null)
      select({ section: sectionId, meeting: meetingId, date: iso })
    })
  const view = (s: { meetingId: number; sessionDate: string }) =>
    unsaved.guard(() => {
      if (!returnTo) setReturnTo({ section: sectionId, meeting: meetingId, date })
      select({ section: sectionId, meeting: String(s.meetingId), date: s.sessionDate })
    })
  const back = returnTo
    ? () => {
        select(returnTo)
        setReturnTo(null)
      }
    : undefined

  return (
    <>
      <PageHeader eyebrow={data?.termName ?? 'Teaching'} title="Attendance" icon={ClipboardCheck}>
        Choose a section, the session and its date, then mark each student present or absent.
      </PageHeader>

      {teaching.isPending ? (
        <Card>
          <TableSkeleton label="Loading your sections" />
        </Card>
      ) : teaching.isError ? (
        <Card>
          <QueryError error={teaching.error} onRetry={() => teaching.refetch()} />
        </Card>
      ) : teaching.data.sections.length === 0 ? (
        <Card>
          <EmptyState icon={ClipboardCheck} title="No sections this term">
            You aren’t assigned to teach a section in the current term. Ask the registrar if this is wrong.
          </EmptyState>
        </Card>
      ) : (
        <div className="grid gap-6 lg:grid-cols-[1fr_320px]">
          <div className="min-w-0 space-y-6">
            <Card className="grid gap-4 p-4 sm:grid-cols-3 sm:p-5">
              <SelectField label="Course and section" value={sectionId} onChange={(e) => chooseSection(e.target.value)}>
                <option value="">Choose a section</option>
                {teaching.data.sections.map((s) => (
                  <option key={s.sectionId} value={s.sectionId}>
                    {s.courseCode} {s.sectionCode} · {s.courseName}
                  </option>
                ))}
              </SelectField>
              <SelectField
                label="Session"
                value={meeting ? meetingId : ''}
                disabled={!section}
                hint={section ? undefined : 'Choose a section first.'}
                onChange={(e) => chooseMeeting(e.target.value)}
              >
                <option value="">{section ? 'Choose a session' : '—'}</option>
                {section?.meetings.map((m) => (
                  <option key={m.id} value={m.id}>
                    {meetingLabel(m)}
                  </option>
                ))}
              </SelectField>
              {meeting ? (
                <DateField
                  key={dateKey}
                  label="Date"
                  value={date}
                  onChange={chooseDate}
                  min={min}
                  max={max}
                  error={problem ?? undefined}
                  hint={`${days[meeting.dayOfWeek]}s only, up to today.`}
                />
              ) : (
                <SelectField label="Date" value="" disabled hint="Choose a session first." onChange={() => undefined}>
                  <option value="">—</option>
                </SelectField>
              )}
            </Card>

            {ready && <SheetArea sheet={sheet} onDirtyChange={setDirty} onBack={back} />}
          </div>

          {section && <SessionHistory section={section} current={ready ? { meetingId: meeting.id, date } : null} onView={view} />}
        </div>
      )}

      <ConfirmDialog
        open={unsaved.pending}
        title="Discard unsaved attendance?"
        confirmLabel="Discard marks"
        onConfirm={unsaved.confirm}
        onClose={() => {
          unsaved.cancel()
          setDateKey((k) => k + 1)
        }}
      >
        The Absent marks on this roster haven’t been submitted. Switching now loses them.
      </ConfirmDialog>
    </>
  )
}

function SheetArea({
  sheet,
  onDirtyChange,
  onBack,
}: {
  sheet: ReturnType<typeof useAttendanceSheet>
  onDirtyChange: (dirty: boolean) => void
  onBack?: () => void
}) {
  if (sheet.isPending)
    return (
      <Card>
        <TableSkeleton label="Loading students" rows={4} />
      </Card>
    )
  if (sheet.isError)
    return (
      <Card className="space-y-3 p-5">
        <Alert>{errorMessage(sheet.error, 'Could not load the students.')}</Alert>
        <Button variant="secondary" onClick={() => sheet.refetch()}>
          Try again
        </Button>
      </Card>
    )
  if (sheet.data.students.length === 0)
    return (
      <Card>
        <EmptyState icon={Users} title="No students enrolled">
          Nobody is enrolled in this section, so there is no attendance to take.
        </EmptyState>
      </Card>
    )
  // A new key per meeting and date starts the marks again (everyone Present).
  return (
    <AttendanceSheetCard
      key={`${sheet.data.meetingId}-${sheet.data.sessionDate}`}
      sheet={sheet.data}
      onDirtyChange={onDirtyChange}
      onBack={onBack}
    />
  )
}

function SessionHistory({
  section,
  current,
  onView,
}: {
  section: TeachingSection
  current: { meetingId: number; date: string } | null
  onView: (s: { meetingId: number; sessionDate: string }) => void
}) {
  const sessions = useSessions(section.sectionId)
  const label = `${section.courseCode} ${section.sectionCode}`
  return (
    <Card className="h-fit">
      <CardHeader icon={History} title={`Taken for ${label}`} description="Open a session to see what was recorded." />
      {sessions.isPending ? (
        <TableSkeleton label="Loading submitted sessions" rows={2} />
      ) : sessions.isError ? (
        <QueryError error={sessions.error} onRetry={() => sessions.refetch()} />
      ) : sessions.data.length === 0 ? (
        <p className="px-5 py-4 text-sm text-muted">No attendance taken yet for this section.</p>
      ) : (
        <ul className="max-h-[420px] divide-y divide-line overflow-y-auto" aria-label={`Submitted sessions of ${label}`}>
          {sessions.data.map((s) => {
            const selected = current?.meetingId === s.meetingId && current.date === s.sessionDate
            return (
              <li key={s.sessionId}>
                <button
                  type="button"
                  onClick={() => onView(s)}
                  aria-label={`View ${isoToDmy(s.sessionDate)} at ${hm(s.startTime)}`}
                  aria-current={selected ? 'true' : undefined}
                  className={cn(
                    'flex min-h-11 w-full cursor-pointer items-center justify-between gap-3 px-5 py-2.5 text-left text-sm transition-colors',
                    'hover:bg-canvas focus-visible:bg-canvas focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-focus',
                    selected && 'bg-brand-50',
                  )}
                >
                  <span className="min-w-0">
                    <span className="block font-medium text-ink">
                      {shortDays[s.dayOfWeek]} {formatDateOnly(s.sessionDate)}
                    </span>
                    <span className="tabular block text-muted">
                      {hm(s.startTime)} · {s.present} present, {s.absent} absent
                    </span>
                  </span>
                  <span className="inline-flex items-center gap-1 text-sm font-medium text-brand">
                    {selected ? 'Viewing' : 'View'} <ChevronRight className="size-4" aria-hidden />
                  </span>
                </button>
              </li>
            )
          })}
        </ul>
      )}
    </Card>
  )
}
