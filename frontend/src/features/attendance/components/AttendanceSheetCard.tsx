import { useState } from 'react'
import { CheckCheck, Lock } from 'lucide-react'
import { errorMessage } from '@/shared/api/problem'
import { cn } from '@/shared/lib/cn'
import { isoToDmy } from '@/shared/lib/dates'
import { formatDateTime } from '@/shared/lib/format'
import { days, hm } from '@/shared/lib/schedule'
import { Alert } from '@/shared/ui/Alert'
import { Badge } from '@/shared/ui/Badge'
import { Button } from '@/shared/ui/Button'
import { Card } from '@/shared/ui/Card'
import { useSubmitAttendance } from '../hooks'
import type { AbsenceAlert, AttendanceSheet, AttendanceSheetRow, AttendanceStatus } from '../types'

interface Props {
  sheet: AttendanceSheet
  onSubmitted: (message: string, alerts: AbsenceAlert[]) => void
  /** Called when the marks change, so an old result stops showing. */
  onEdit: () => void
}

/**
 * The students of one meeting on one date. Before submission everyone starts Present and can be marked Absent;
 * after submission the recorded marks are shown read-only (attendance is taken once per meeting and date).
 */
export function AttendanceSheetCard({ sheet, onSubmitted, onEdit }: Props) {
  const submit = useSubmitAttendance()
  const [marks, setMarks] = useState<Record<number, AttendanceStatus>>(() =>
    Object.fromEntries(sheet.students.map((s) => [s.enrollmentId, 'Present' as const])),
  )
  const taken = sheet.sessionId !== null
  const label = `${sheet.courseCode} ${sheet.sectionCode}`
  const when = `${days[sheet.dayOfWeek]} ${isoToDmy(sheet.sessionDate)} at ${hm(sheet.startTime)}`
  const absent = taken
    ? sheet.students.filter((s) => s.status === 'Absent').length
    : Object.values(marks).filter((m) => m === 'Absent').length
  const present = sheet.students.length - absent

  const mark = (id: number, status: AttendanceStatus) => {
    submit.reset()
    onEdit()
    setMarks((m) => ({ ...m, [id]: status }))
  }
  const markAll = (status: AttendanceStatus) => {
    submit.reset()
    onEdit()
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
        onSuccess: (r) =>
          onSubmitted(`Attendance saved for ${label} on ${isoToDmy(sheet.sessionDate)}: ${r.present} present, ${r.absent} absent.`, r.alerts),
      },
    )

  return (
    <Card>
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-line px-5 py-4">
        <div>
          <h2 className="font-display text-lg font-semibold text-ink">
            {label} · {sheet.type}
          </h2>
          <p className="text-sm text-muted">
            {when} · {present} present, {absent} absent · limit {sheet.absenceLimit} absences
          </p>
        </div>
        {taken ? (
          <Badge tone="neutral">
            <Lock className="mr-1 size-3" aria-hidden /> Submitted {formatDateTime(sheet.submittedAt)}
          </Badge>
        ) : (
          <div className="flex flex-wrap gap-2">
            <Button variant="secondary" className="h-9" onClick={() => markAll('Present')}>
              <CheckCheck className="size-4" aria-hidden /> Mark all present
            </Button>
            <Button variant="secondary" className="h-9" onClick={() => markAll('Absent')}>
              Mark all absent
            </Button>
          </div>
        )}
      </div>

      {taken && (
        <p className="border-b border-line bg-canvas px-5 py-2.5 text-sm text-muted">
          Attendance for this session was already submitted. It can’t be taken twice.
        </p>
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

      {!taken && (
        <div className="space-y-3 border-t border-line p-5">
          {submit.isError && <Alert>{errorMessage(submit.error)}</Alert>}
          <div className="flex flex-wrap items-center justify-between gap-3">
            <p className="text-sm text-muted">Absences can’t be changed after submitting.</p>
            <Button onClick={send} loading={submit.isPending}>
              Submit attendance
            </Button>
          </div>
        </div>
      )}
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

function StudentRow({ student, limit, status, readOnly, onChange }: RowProps) {
  return (
    <li className="flex flex-wrap items-center justify-between gap-3 px-5 py-3">
      <div className="min-w-0">
        <p className="font-medium text-ink">{student.fullName}</p>
        <p className="text-sm text-muted">
          {student.universityId} · {student.absences} of {limit} absences so far
        </p>
      </div>
      {readOnly ? (
        <Badge tone={status === 'Absent' ? 'danger' : 'success'}>{status}</Badge>
      ) : (
        <fieldset className="flex rounded-lg border border-line bg-canvas p-0.5">
          <legend className="sr-only">Attendance of {student.fullName}</legend>
          {(['Present', 'Absent'] as const).map((option) => (
            <label
              key={option}
              className={cn(
                'cursor-pointer rounded-md px-3 py-1.5 text-sm font-medium transition-colors has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-accent/40',
                status === option
                  ? option === 'Present'
                    ? 'bg-success-soft text-success'
                    : 'bg-danger-soft text-danger'
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
              {option}
            </label>
          ))}
        </fieldset>
      )}
    </li>
  )
}
