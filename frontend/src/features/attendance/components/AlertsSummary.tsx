import { TriangleAlert } from 'lucide-react'
import type { AbsenceAlert } from '../types'

const text: Record<AbsenceAlert['level'], (a: AbsenceAlert) => string> = {
  Warning: (a) => `warned: ${a.absences} of ${a.limit} absences, one away from the limit`,
  Limit: (a) => `reached the absence limit (${a.absences} of ${a.limit})`,
  AutoDropped: (a) => `dropped from the course for absences (${a.absences} of ${a.limit}), recorded as F`,
}

/** Absence alerts a submission caused. Each student was emailed and notified. */
export function AlertsSummary({ alerts }: { alerts: AbsenceAlert[] }) {
  return (
    <section aria-label="Absence alerts" className="rounded-lg bg-warning-soft px-3.5 py-3 text-sm text-warning">
      <p className="flex items-center gap-2 font-medium">
        <TriangleAlert className="size-4" aria-hidden /> Absence alerts sent by email and notification
      </p>
      <ul className="mt-1.5 list-disc space-y-0.5 pl-9">
        {alerts.map((a) => (
          <li key={`${a.enrollmentId}-${a.level}`}>
            <span className="font-medium">{a.fullName}</span> {text[a.level](a)}
          </li>
        ))}
      </ul>
    </section>
  )
}
