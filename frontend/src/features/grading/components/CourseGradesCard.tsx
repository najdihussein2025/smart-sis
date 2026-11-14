import { Badge } from '@/shared/ui/Badge'
import { Card } from '@/shared/ui/Card'
import { TableScroll } from '@/shared/ui/TableScroll'
import type { CourseGrades } from '../types'
import { num } from './gradingForms'

/** The course's status in words (colour is never the only signal). */
function StatusBadge({ course }: { course: CourseGrades }) {
  if (course.status === 'Completed')
    return (
      <Badge tone="success">
        Final: {course.letterGrade}
        {course.finalPct !== null ? ` (${num(course.finalPct)}%)` : ''}
      </Badge>
    )
  if (course.status === 'DroppedAbsence') return <Badge tone="danger">Dropped for absences · F</Badge>
  return <Badge>In progress</Badge>
}

/** One course this term: components with weight, score / max and %, "Not graded", and a progress bar. */
export function CourseGradesCard({ course }: { course: CourseGrades }) {
  const label = `${course.courseCode} ${course.sectionCode}`
  return (
    <Card>
      <article aria-label={`${course.courseCode} grades`}>
        <header className="flex flex-wrap items-start justify-between gap-3 border-b border-line p-4">
          <div className="min-w-0">
            <h2 className="font-display text-lg font-semibold text-ink">
              {course.courseCode} · {course.courseName}
            </h2>
            <p className="text-sm text-muted">
              Section {course.sectionCode} · {course.credits} credits
            </p>
          </div>
          <StatusBadge course={course} />
        </header>

        <div className="space-y-2 border-b border-line p-4">
          <div className="flex flex-wrap items-baseline justify-between gap-2 text-sm">
            <span className="text-muted">
              Graded so far: <span className="font-medium text-ink">{num(course.gradedWeight)}%</span> of the course
            </span>
            <span className="text-muted">
              {course.runningPct !== null ? (
                <>
                  Average so far: <span className="font-medium text-ink">{num(course.runningPct)}%</span>
                </>
              ) : (
                'No grades yet'
              )}
            </span>
          </div>
          <div
            role="progressbar"
            aria-label={`Graded so far in ${label}`}
            aria-valuemin={0}
            aria-valuemax={100}
            aria-valuenow={course.gradedWeight}
            className="relative h-2 overflow-hidden rounded-full bg-canvas"
          >
            {/* Graded weight, with the points earned inside it. */}
            <div className="absolute inset-y-0 left-0 rounded-full bg-brand-50" style={{ width: `${Math.min(course.gradedWeight, 100)}%` }} />
            <div className="absolute inset-y-0 left-0 rounded-full bg-brand" style={{ width: `${Math.min(course.earned, 100)}%` }} />
          </div>
          <p className="text-[13px] text-muted">{num(course.earned)} of 100 points earned so far.</p>
        </div>

        {course.components.length === 0 ? (
          <p className="p-4 text-sm text-muted">The grade components for this course aren&apos;t set up yet.</p>
        ) : (
          <TableScroll>
            <table className="w-full min-w-[480px] text-left text-sm">
              <thead className="border-b border-line text-xs uppercase tracking-wide text-muted">
                <tr>
                  <th scope="col" className="px-4 py-3 font-medium">Component</th>
                  <th scope="col" className="px-4 py-3 font-medium">Weight</th>
                  <th scope="col" className="px-4 py-3 font-medium">Score</th>
                  <th scope="col" className="px-4 py-3 font-medium">%</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-line">
                {course.components.map((c) => (
                  <tr key={c.name}>
                    <th scope="row" className="px-4 py-3 font-medium text-ink">{c.name}</th>
                    <td className="px-4 py-3 tabular-nums">{num(c.weight)}%</td>
                    <td className="px-4 py-3 tabular-nums">
                      {c.score !== null ? `${num(c.score)} / ${num(c.maxGrade)}` : <Badge tone="warning">Not graded</Badge>}
                    </td>
                    <td className="px-4 py-3 tabular-nums">{c.pct !== null ? `${num(c.pct)}%` : '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </TableScroll>
        )}
      </article>
    </Card>
  )
}
