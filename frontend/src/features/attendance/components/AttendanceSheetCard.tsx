import { useEffect, useState } from 'react'
import { ArrowLeft, Check, CheckCheck, ClipboardCheck, Lock, TriangleAlert } from 'lucide-react'
import { errorMessage } from '@/shared/api/problem'
import { cn } from '@/shared/lib/cn'
import { isoToDmy } from '@/shared/lib/dates'
import { formatDateTime } from '@/shared/lib/format'
import { days, hm } from '@/shared/lib/schedule'
import { useSavedMessage } from '@/shared/lib/useSavedMessage'
import { Alert } from '@/shared/ui/Alert'
import { Badge } from '@/shared/ui/Badge'
import { Button } from '@/shared/ui/Button'
import { Card } from '@/shared/ui/Card'
import { CardHeader } from '@/shared/ui/CardHeader'
import { ConfirmDialog } from '@/shared/ui/ConfirmDialog'
import { useSubmitAttendance } from '../hooks'
import { alertLevelAt, projectedAlerts, standingOf, type ProjectedAlert } from '../standing'
import type { AbsenceAlert, AbsenceAlertLevel, AttendanceSheet, AttendanceSheetRow, AttendanceStatus } from '../types'
import { AlertsSummary } from './AlertsSummary'
import { StandingBadge } from './StandingBadge'

interface Props {
  sheet: AttendanceSheet
  /** Called after a successful submission, with the saved message and the alerts it caused. */
  onSubmitted?: (message: string, alerts: AbsenceAlert[]) => void
  /** Called when the marks change, so an old result stops showing. */
  onEdit?: () => void
  /** True while there are marks that were not submitted yet, so the page can guard leaving them. */
  onDirtyChange?: (dirty: boolean) => void
  /** Shown on a submitted sheet opened from the history: goes back to the session the instructor was taking. */
  onBack?: () => void
}

const projectedText: Record<AbsenceAlertLevel, (a: ProjectedAlert, limit: number) => string> = {
  Warning: (a, limit) => `gets a warning (${a.absences} of ${limit} absences)`,
  Limit: (a, limit) => `reaches the absence limit (${a.absences} of ${limit})`,
  AutoDropped: (a, limit) => `is dropped from the course with an F (${a.absences} of ${limit})`,
}

/**
 * The students of one meeting on one date. Before submission everyone starts Present and can be marked Absent;
 * after submission the recorded marks are shown read-only (attendance is taken once per meeting and date).
 */
export function AttendanceSheetCard({ sheet, onSubmitted, onEdit, onDirtyChange, onBack }: Props) {
  const submit = useSubmitAttendance()
  const saved = useSavedMessage()
  const [marks, setMarks] = useState<Record<number, AttendanceStatus>>(() =>
    Object.fromEntries(sheet.students.map((s) => [s.enrollmentId, 'Present' as const])),
  )
  const [confirming, setConfirming] = useState(false)
  const [alerts, setAlerts] = useState<AbsenceAlert[]>([])
  const taken = sheet.sessionId !== null
  const label = `${sheet.courseCode} ${sheet.sectionCode}`
  const when = `${days[sheet.dayOfWeek]} ${isoToDmy(sheet.sessionDate)} at ${hm(sheet.startTime)}`
  const absent = taken
    ? sheet.students.filter((s) => s.status === 'Absent').length
    : Object.values(marks).filter((m) => m === 'Absent').length
  const present = sheet.students.length - absent
  const projected = taken ? [] : projectedAlerts(sheet.students, marks, sheet.absenceLimit)

  // Everyone starts Present, so only an Absent mark is work that would be lost.
  const dirty = !taken && !submit.isSuccess && absent > 0
  useEffect(() => onDirtyChange?.(dirty), [dirty, onDirtyChange])
  useEffect(() => () => onDirtyChange?.(false), [onDirtyChange])

  const edited = () => {
    submit.reset()
    setAlerts([])
    onEdit?.()
  }
  const mark = (id: number, status: AttendanceStatus) => {
    edited()
    setMarks((m) => ({ ...m, [id]: status }))
  }
  const markAll = (status: AttendanceStatus) => {
    edited()
    setMarks(Object.fromEntries(sheet.students.map((s) => [s.enrollmentId, status])))
  }

  const send = () =>
    submit.mutate(
      {
        meetingId: sheet.meetingId,
        sessionDate: sheet.sessionDate,
        records: sheet.students.map((s) => ({ enrollmentId: s.enrollmentId, status: marks[s.enrollmentId] ?? 'Present' })),
      },
      {
        onSuccess: (r) => {
          const text = `Attendance saved for ${label} on ${isoToDmy(sheet.sessionDate)}: ${r.present} present, ${r.absent} absent.`
          setConfirming(false)
          setAlerts(r.alerts)
          saved.show(text)
          onSubmitted?.(text, r.alerts)
        },
      },
    )

  const showFooter = !taken || saved.message !== null || alerts.length > 0

  return (
    <Card>
      <CardHeader
        icon={ClipboardCheck}
        title={`${label} · ${sheet.type}`}
        description={
          <span className="tabular">
            {when} · limit {sheet.absenceLimit} absences
          </span>
        }
        actions={
          taken ? (
            <Badge tone="neutral">
              <Lock className="size-3" aria-hidden /> Submitted {formatDateTime(sheet.submittedAt)}
            </Badge>
          ) : (
            <>
              <Button variant="secondary" size="sm" onClick={() => markAll('Present')}>
                <CheckCheck className="size-4" aria-hidden /> Mark all present
              </Button>
              <Button variant="ghost" size="sm" onClick={() => markAll('Absent')}>
                Mark all absent
              </Button>
            </>
          )
        }
      />

      {taken && (
        <div className="flex flex-wrap items-center justify-between gap-3 border-b border-line bg-info-soft px-4 py-2.5 text-sm text-info sm:px-5">
          <p>
            <span className="font-semibold">Viewing submitted session</span> · Attendance for this session was already submitted. It
            can’t be taken twice.
          </p>
          {onBack && (
            <Button variant="secondary" size="sm" onClick={onBack}>
              <ArrowLeft className="size-4" aria-hidden /> Back
            </Button>
          )}
        </div>
      )}

      <ul className="divide-y divide-line" aria-label={`Students of ${label}`}>
        {sheet.students.map((s) => (
          <StudentRow
            key={s.enrollmentId}
            student={s}
            limit={sheet.absenceLimit}
            status={taken ? s.status : (marks[s.enrollmentId] ?? 'Present')}
            readOnly={taken}
            onChange={(status) => mark(s.enrollmentId, status)}
          />
        ))}
      </ul>

      {showFooter && (
        <div className="sticky bottom-0 z-10 space-y-3 rounded-b-[var(--radius-card)] border-t border-line bg-surface px-4 py-3 shadow-[0_-4px_12px_-8px_rgb(0_0_0/0.15)] sm:px-5">
          {saved.message && <Alert tone="success">{saved.message}</Alert>}
          {alerts.length > 0 && (
            <div className="max-h-40 overflow-y-auto">
              <AlertsSummary alerts={alerts} />
            </div>
          )}
          {!taken && (
            <div className="flex flex-wrap items-center justify-between gap-3">
              <p className="text-sm text-muted" aria-live="polite">
                <span className="tabular font-medium text-ink">
                  {present} present, {absent} absent
                </span>{' '}
                · can’t be changed after submitting
              </p>
              <Button
                onClick={() => {
                  submit.reset()
                  setConfirming(true)
                }}
              >
                Submit attendance
              </Button>
            </div>
          )}
        </div>
      )}

      <ConfirmDialog
        open={confirming}
        title={`Submit attendance for ${label}?`}
        confirmLabel="Submit"
        loading={submit.isPending}
        error={submit.isError ? errorMessage(submit.error) : null}
        onConfirm={send}
        onClose={() => setConfirming(false)}
      >
        <p>
          {when}: <span className="tabular font-semibold">{present} present, {absent} absent</span>. Attendance can’t be changed
          after submitting.
        </p>
        {projected.length > 0 && (
          <section aria-label="Absence alerts this submission sends" className="mt-3 rounded-lg bg-warning-soft px-3.5 py-3 text-warning">
            <p className="flex items-center gap-2 font-medium">
              <TriangleAlert className="size-4" aria-hidden /> Each student below is emailed and notified
            </p>
            <ul className="mt-1.5 list-disc space-y-0.5 pl-9">
              {projected.map((a) => (
                <li key={a.enrollmentId}>
                  <span className="font-medium">{a.fullName}</span> {projectedText[a.level](a, sheet.absenceLimit)}
                </li>
              ))}
            </ul>
          </section>
        )}
      </ConfirmDialog>
    </Card>
  )
}

interface RowProps {
  student: AttendanceSheetRow
  limit: number
  status: AttendanceStatus | null
  readOnly: boolean
  onChange: (status: AttendanceStatus) => void
}

const rowWarning: Record<AbsenceAlertLevel, string> = {
  Warning: 'Absent today puts this student one away from the limit.',
  Limit: 'Absent today reaches the absence limit.',
  AutoDropped: 'Absent today drops this student from the course (F).',
}

function StudentRow({ student, limit, status, readOnly, onChange }: RowProps) {
  const warning = !readOnly && status === 'Absent' ? alertLevelAt(student.absences + 1, limit) : null
  return (
    <li className="flex scroll-mb-28 flex-wrap items-center justify-between gap-3 px-4 py-3 sm:px-5">
      <div className="min-w-0">
        <p className="font-medium text-ink">{student.fullName}</p>
        <p className="flex flex-wrap items-center gap-x-2 gap-y-1 text-sm text-muted">
          <span className="tabular">
            {student.universityId} · {student.absences} of {limit} absences so far
          </span>
          <StandingBadge standing={standingOf(student.absences, limit)} />
        </p>
        {warning && (
          <p className={cn('mt-1 flex items-center gap-1.5 text-[13px] font-medium', warning === 'Warning' ? 'text-warning' : 'text-danger')}>
            <TriangleAlert className="size-3.5 shrink-0" aria-hidden /> {rowWarning[warning]}
          </p>
        )}
      </div>
      {readOnly ? (
        <Badge tone={status === 'Absent' ? 'danger' : 'success'} dot>
          {status}
        </Badge>
      ) : (
        <fieldset className="flex rounded-lg border border-line bg-canvas p-0.5">
          <legend className="sr-only">Attendance of {student.fullName}</legend>
          {(['Present', 'Absent'] as const).map((option) => (
            <label
              key={option}
              className={cn(
                'inline-flex min-h-11 min-w-24 cursor-pointer items-center justify-center gap-1 rounded-md px-3 text-sm font-medium transition-colors sm:min-h-9 sm:min-w-20',
                'has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-focus',
                status === option
                  ? option === 'Present'
                    ? 'bg-success-soft text-success shadow-sm'
                    : 'bg-danger-soft text-danger shadow-sm'
                  : 'text-muted hover:text-ink',
              )}
            >
              <input
                type="radio"
                className="sr-only"
                name={`mark-${student.enrollmentId}`}
                value={option}
                checked={status === option}
                onChange={() => onChange(option)}
              />
              {/* The tick shows the choice without relying on colour. */}
              {status === option && <Check className="size-3.5" aria-hidden />}
              {option}
            </label>
          ))}
        </fieldset>
      )}
    </li>
  )
}
