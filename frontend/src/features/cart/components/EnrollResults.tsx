import { CheckCircle2, XCircle } from 'lucide-react'
import type { EnrollResult } from '@/features/enrollment/types'
import { cn } from '@/shared/lib/cn'

const ruleNames: Record<string, string> = {
  window_closed: 'Registration closed',
  credit_cap: 'Credit limit',
  prerequisite_missing: 'Prerequisite',
  same_course: 'Same course',
  schedule_conflict: 'Time conflict',
  section_full: 'Section full',
  not_current_term: 'Not this term',
  not_found: 'Not found',
}

/** T19: one line per section tried, so every rule failure is shown with its own reason. */
export function EnrollResults({ result }: { result: EnrollResult }) {
  const failed = result.results.length - result.enrolledCount
  return (
    <section
      aria-label="Enrollment results"
      aria-live="polite"
      className={cn('rounded-xl border p-4', failed === 0 ? 'border-success/30 bg-success-soft' : 'border-line bg-surface')}
    >
      <h2 className="font-medium text-ink">
        {result.enrolledCount > 0 && `Enrolled in ${result.enrolledCount} ${result.enrolledCount === 1 ? 'section' : 'sections'}. `}
        {failed > 0 && `${failed} could not be enrolled.`}
      </h2>
      <ul className="mt-3 space-y-2 text-sm">
        {result.results.map((r) => (
          <li key={r.sectionId} className="flex items-start gap-2">
            {r.success ? (
              <CheckCircle2 className="mt-0.5 size-4 shrink-0 text-success" aria-hidden />
            ) : (
              <XCircle className="mt-0.5 size-4 shrink-0 text-danger" aria-hidden />
            )}
            <span>
              <span className="font-medium text-ink">{r.label ?? `Section ${r.sectionId}`}</span>
              {r.success ? (
                <span className="text-success"> — enrolled</span>
              ) : (
                <>
                  {r.code && ruleNames[r.code] && <span className="text-danger"> — {ruleNames[r.code]}: </span>}
                  <span className="text-ink">{r.message}</span>
                </>
              )}
            </span>
          </li>
        ))}
      </ul>
    </section>
  )
}
