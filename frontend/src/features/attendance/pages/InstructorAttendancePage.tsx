import { useState } from 'react'
import { ClipboardCheck, History, Users } from 'lucide-react'
import { errorMessage } from '@/shared/api/problem'
import { isoToDmy } from '@/shared/lib/dates'
import { formatDateOnly } from '@/shared/lib/format'
import { days, hm, shortDays } from '@/shared/lib/schedule'
import { useSavedMessage } from '@/shared/lib/useSavedMessage'
import { Alert } from '@/shared/ui/Alert'
import { Card } from '@/shared/ui/Card'
import { DateField } from '@/shared/ui/DateField'
import { EmptyState } from '@/shared/ui/EmptyState'
import { PageHeader } from '@/shared/ui/PageHeader'
import { QueryError } from '@/shared/ui/QueryError'
import { SelectField } from '@/shared/ui/SelectField'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { Button } from '@/shared/ui/Button'
import { AttendanceSheetCard } from '../components/AttendanceSheetCard'
import { AlertsSummary } from '../components/AlertsSummary'
import { dateProblem, latestOn } from '../dates'
import { useAttendanceSheet, useSessions, useTeachingSections } from '../hooks'
import type { AbsenceAlert, AttendanceMeeting, TeachingSection } from '../types'

const meetingLabel = (m: AttendanceMeeting) => `${days[m.dayOfWeek]} ${hm(m.startTime)}–${hm(m.endTime)} · ${m.type} · ${m.roomName}`

/**
 * PB-10 / T41: the instructor picks a section they teach, then a session (weekly meeting) and a date, marks each
 * enrolled student Present or Absent and submits. A meeting already taken on that date shows what was recorded.
 */
export function InstructorAttendancePage() {
  const teaching = useTeachingSections()
  const [sectionId, setSectionId] = useState('')
  const [meetingId, setMeetingId] = useState('')
  const [date, setDate] = useState('')
  const [alerts, setAlerts] = useState<AbsenceAlert[]>([])
  const saved = useSavedMessage()

  const data = teaching.data
  const section = data?.sections.find((s) => s.sectionId === Number(sectionId))
  const meeting = section?.meetings.find((m) => m.id === Number(meetingId))
  // Attendance can be taken from the term's first day up to today (or the term's last day, if earlier).
  const min = section?.termStart ?? ''
  const max = section && data ? (data.today < section.termEnd ? data.today : section.termEnd) : ''
  const problem = meeting && date ? dateProblem(date, meeting.dayOfWeek, min, max) : null
  const ready = !!meeting && !!date && !problem
  const sheet = useAttendanceSheet(ready ? meeting.id : null, ready ? date : null)

  const chooseSection = (id: string) => {
    setSectionId(id)
    setMeetingId('')
    setDate('')
  }
  // Choosing a session fills in its latest date, the usual case (today's class or the last one).
  const chooseMeeting = (id: string) => {
    setMeetingId(id)
    const m = section?.meetings.find((x) => x.id === Number(id))
    setDate(m ? (latestOn(m.dayOfWeek, min, max) ?? '') : '')
  }
  const view = (s: { meetingId: number; sessionDate: string }) => {
    setMeetingId(String(s.meetingId))
    setDate(s.sessionDate)
  }

  return (
    <>
      <PageHeader eyebrow={data?.termName ?? 'Teaching'} title="Attendance">
        Choose a section, the session and its date, then mark each student present or absent.
      </PageHeader>

      <div className="mb-4 space-y-3 empty:hidden">
        {saved.message && <Alert tone="success">{saved.message}</Alert>}
        {alerts.length > 0 && <AlertsSummary alerts={alerts} />}
      </div>

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
            <Card className="grid gap-4 p-5 sm:grid-cols-3">
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
                value={meetingId}
                disabled={!section}
                onChange={(e) => chooseMeeting(e.target.value)}
              >
                <option value="">{section ? 'Choose a session' : 'Choose a section first'}</option>
                {section?.meetings.map((m) => (
                  <option key={m.id} value={m.id}>
                    {meetingLabel(m)}
                  </option>
                ))}
              </SelectField>
              {meeting ? (
                <DateField
                  label="Date"
                  value={date}
                  onChange={setDate}
                  min={min}
                  max={max}
                  error={problem ?? undefined}
                  hint={`${days[meeting.dayOfWeek]}s only, up to today.`}
                />
              ) : (
                <div className="space-y-1.5">
                  <p className="text-sm font-medium text-ink">Date</p>
                  <p className="flex h-11 items-center text-sm text-muted">Choose a session first</p>
                </div>
              )}
            </Card>

            {ready && (
              <SheetArea
                sheet={sheet}
                onSubmitted={(text, list) => {
                  saved.show(text)
                  setAlerts(list)
                }}
                onEdit={() => setAlerts([])}
              />
            )}
          </div>

          {section && <SessionHistory section={section} onView={view} />}
        </div>
      )}
    </>
  )
}

function SheetArea({
  sheet,
  onSubmitted,
  onEdit,
}: {
  sheet: ReturnType<typeof useAttendanceSheet>
  onSubmitted: (message: string, alerts: AbsenceAlert[]) => void
  onEdit: () => void
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
  return <AttendanceSheetCard key={`${sheet.data.meetingId}-${sheet.data.sessionDate}`} sheet={sheet.data} onSubmitted={onSubmitted} onEdit={onEdit} />
}

function SessionHistory({ section, onView }: { section: TeachingSection; onView: (s: { meetingId: number; sessionDate: string }) => void }) {
  const sessions = useSessions(section.sectionId)
  const label = `${section.courseCode} ${section.sectionCode}`
  return (
    <Card className="h-fit">
      <h2 className="flex items-center gap-2 border-b border-line px-5 py-3 font-display font-semibold text-ink">
        <History className="size-4 text-muted" aria-hidden /> Taken for {label}
      </h2>
      {sessions.isPending ? (
        <TableSkeleton label="Loading submitted sessions" rows={2} />
      ) : sessions.isError ? (
        <QueryError error={sessions.error} onRetry={() => sessions.refetch()} />
      ) : sessions.data.length === 0 ? (
        <p className="px-5 py-4 text-sm text-muted">No attendance taken yet for this section.</p>
      ) : (
        <ul className="max-h-[420px] divide-y divide-line overflow-y-auto" aria-label={`Submitted sessions of ${label}`}>
          {sessions.data.map((s) => (
            <li key={s.sessionId} className="flex items-center justify-between gap-3 px-5 py-2.5 text-sm">
              <div>
                <p className="font-medium text-ink">
                  {shortDays[s.dayOfWeek]} {formatDateOnly(s.sessionDate)}
                </p>
                <p className="text-muted">
                  {hm(s.startTime)} · {s.present} present, {s.absent} absent
                </p>
              </div>
              <Button
                variant="ghost"
                className="h-8 px-2.5"
                onClick={() => onView(s)}
                aria-label={`View ${isoToDmy(s.sessionDate)} at ${hm(s.startTime)}`}
              >
                View
              </Button>
            </li>
          ))}
        </ul>
      )}
    </Card>
  )
}
