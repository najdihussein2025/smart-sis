import { Badge } from '@/shared/ui/Badge'
import { Card } from '@/shared/ui/Card'
import { CardHeader } from '@/shared/ui/CardHeader'
import { TableScroll } from '@/shared/ui/TableScroll'
import type { CourseGrades } from '../types'
import { num } from './gradingForms'

/** The course's status in words (colour is never the only signal). */
function StatusBadge({ course }: { course: CourseGrades }) {
  if (course.status === 'Completed')
    return (
      <Badge tone="success" dot>
        Final: {course.letterGrade}
        {course.finalPct !== null ? ` (${num(course.finalPct)}%)` : ''}
      </Badge>
    )
  if (course.status === 'DroppedAbsence')
    return (
      <Badge tone="danger" dot>
        Dropped for absences · F
      </Badge>
    )
  return (
    <Badge tone="info" dot>
      In progress
    </Badge>
  )
}

/** How much of the course is graded, and how many of its 100 points are earned: a two-tone bar with a legend. */
function Progress({ course, label }: { course: CourseGrades; label: string }) {
  const graded = Math.min(course.gradedWeight, 100)
  const earned = Math.min(course.earned, 100)
  return (
    <div className="space-y-2.5 border-b border-line p-4 sm:px-5">
      <div className="tabular flex flex-wrap items-baseline justify-between gap-2 text-sm">
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
        aria-valuetext={`${num(course.gradedWeight)}% graded, ${num(course.earned)} of 100 points earned`}
        className="relative h-2.5 overflow-hidden rounded-full bg-canvas ring-1 ring-inset ring-line"
      >
        {/* Graded weight, with the points earned inside it. */}
        <div className="absolute inset-y-0 left-0 rounded-full bg-accent/50" style={{ width: `${graded}%` }} />
        <div className="absolute inset-y-0 left-0 rounded-full bg-brand" style={{ width: `${earned}%` }} />
      </div>
      <ul className="tabular flex flex-wrap gap-x-4 gap-y-1 text-xs text-muted" aria-label="Legend">
        <li className="inline-flex items-center gap-1.5">
          <span aria-hidden className="size-2 rounded-full bg-brand" /> Earned: {num(course.earned)} of 100 points
        </li>
        <li className="inline-flex items-center gap-1.5">
          <span aria-hidden className="size-2 rounded-full bg-accent/50 ring-1 ring-accent" /> Graded: {num(course.gradedWeight)}%
        </li>
      </ul>
    </div>
  )
}

/** One course this term: components with weight, score / max and %, "Not graded", and a progress bar. */
export function CourseGradesCard({ course }: { course: CourseGrades }) {
  const label = `${course.courseCode} ${course.sectionCode}`
  return (
    <Card>
      <article aria-label={`${course.courseCode} grades`}>
        <CardHeader
          title={
            <>
              <span className="font-semibold">{course.courseCode}</span> <span className="font-normal">{course.courseName}</span>
            </>
          }
          description={
            <span className="tabular">
              Section {course.sectionCode} · {course.credits} credits
            </span>
          }
          actions={<StatusBadge course={course} />}
        />

        {course.components.length === 0 ? (
          <p className="p-4 text-sm text-muted sm:px-5">The grade components for this course aren&apos;t set up yet.</p>
        ) : (
          <>
            <Progress course={course} label={label} />
            <TableScroll>
              <table className="w-full min-w-[480px] text-left text-sm">
                <thead className="border-b border-line text-xs tracking-wide text-muted uppercase">
                  <tr>
                    <th scope="col" className="px-4 py-3 font-medium sm:px-5">Component</th>
                    <th scope="col" className="px-4 py-3 text-right font-medium">Weight</th>
                    <th scope="col" className="px-4 py-3 text-right font-medium">Score</th>
                    <th scope="col" className="px-4 py-3 text-right font-medium sm:px-5">%</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-line">
                  {course.components.map((c) => (
                    <tr key={c.name}>
                      <th scope="row" className="px-4 py-3 font-medium text-ink sm:px-5">{c.name}</th>
                      <td className="tabular px-4 py-3 text-right">{num(c.weight)}%</td>
                      <td className="tabular px-4 py-3 text-right">
                        {c.score !== null ? `${num(c.score)} / ${num(c.maxGrade)}` : <Badge tone="warning">Not graded</Badge>}
                      </td>
                      <td className="tabular px-4 py-3 text-right sm:px-5">{c.pct !== null ? `${num(c.pct)}%` : '—'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </TableScroll>
          </>
        )}
      </article>
    </Card>
  )
}
