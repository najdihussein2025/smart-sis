import { ScrollText } from 'lucide-react'
import { Badge } from '@/shared/ui/Badge'
import { Card } from '@/shared/ui/Card'
import { CardHeader } from '@/shared/ui/CardHeader'
import { EmptyState } from '@/shared/ui/EmptyState'
import { QueryError } from '@/shared/ui/QueryError'
import { TableScroll } from '@/shared/ui/TableScroll'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { useTranscript } from '../hooks'
import type { TranscriptCourse } from '../types'
import { gpa } from './gpa'

function Grade({ course }: { course: TranscriptCourse }) {
  if (course.status === 'Enrolled')
    return (
      <Badge tone="info" dot>
        In progress
      </Badge>
    )
  return (
    <span className="inline-flex items-center gap-2">
      <span className="font-medium text-ink">{course.letterGrade}</span>
      {course.status === 'DroppedAbsence' && (
        <Badge tone="danger" dot>
          Absences
        </Badge>
      )}
      {!course.countsInGpa && <Badge tone="outline">Retaken</Badge>}
    </span>
  )
}

/** Every term with its courses, letters, term GPA, cumulative GPA and credits. */
export function TranscriptView() {
  const transcript = useTranscript()

  if (transcript.isPending)
    return (
      <Card>
        <TableSkeleton label="Loading transcript" />
      </Card>
    )
  if (transcript.isError)
    return (
      <Card>
        <QueryError error={transcript.error} onRetry={() => transcript.refetch()} />
      </Card>
    )
  const t = transcript.data
  if (t.terms.length === 0)
    return (
      <Card>
        <EmptyState icon={ScrollText} title="Nothing on your transcript yet">
          Your courses appear here once you enroll, and their grades once your instructors finalize them.
        </EmptyState>
      </Card>
    )

  return (
    <div className="space-y-6">
      <p className="tabular text-sm text-muted">
        Cumulative GPA <span className="font-medium text-ink">{gpa(t.cumulativeGpa)}</span> · {t.creditsEarned} of{' '}
        {t.creditsAttempted} credits earned
      </p>
      {t.terms.map((term) => (
        <Card key={term.termId}>
          <section aria-label={term.termName}>
            <CardHeader
              title={
                <span className="flex flex-wrap items-center gap-2">
                  {term.termName}
                  {term.isCurrent && <Badge tone="brand">Current</Badge>}
                </span>
              }
              description={
                <span className="tabular">
                  Term GPA <span className="font-medium text-ink">{gpa(term.termGpa)}</span> · Cumulative{' '}
                  <span className="font-medium text-ink">{gpa(term.cumulativeGpa)}</span> · Credits {term.creditsEarned} /{' '}
                  {term.creditsAttempted}
                </span>
              }
            />
            <TableScroll>
              <table className="w-full min-w-[520px] text-left text-sm">
                <thead className="border-b border-line text-xs uppercase tracking-wide text-muted">
                  <tr>
                    <th scope="col" className="px-4 py-3 font-medium sm:px-5">Course</th>
                    <th scope="col" className="px-4 py-3 font-medium">Credits</th>
                    <th scope="col" className="px-4 py-3 font-medium">Grade</th>
                    <th scope="col" className="px-4 py-3 font-medium">Points</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-line">
                  {term.courses.map((c) => (
                    <tr key={`${c.courseCode}-${c.sectionCode}`}>
                      <th scope="row" className="px-4 py-3 font-normal sm:px-5">
                        <span className="font-semibold text-ink">{c.courseCode}</span> <span className="text-ink">{c.courseName}</span>
                      </th>
                      <td className="tabular px-4 py-3">{c.credits}</td>
                      <td className="px-4 py-3">
                        <Grade course={c} />
                      </td>
                      <td className="tabular px-4 py-3">{c.gradePoints !== null ? c.gradePoints.toFixed(1) : '—'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </TableScroll>
          </section>
        </Card>
      ))}
    </div>
  )
}
