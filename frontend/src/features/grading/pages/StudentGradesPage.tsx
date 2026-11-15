import { useState } from 'react'
import { Award, GraduationCap, TrendingUp } from 'lucide-react'
import { Card } from '@/shared/ui/Card'
import { EmptyState } from '@/shared/ui/EmptyState'
import { PageHeader } from '@/shared/ui/PageHeader'
import { QueryError } from '@/shared/ui/QueryError'
import { StatCard, StatCardGrid } from '@/shared/ui/StatCard'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { Tabs } from '@/shared/ui/Tabs'
import { CourseGradesCard } from '../components/CourseGradesCard'
import { gpa } from '../components/gpa'
import { TranscriptView } from '../components/TranscriptView'
import { useMyGrades } from '../hooks'

type View = 'term' | 'transcript'

/** PB-09 / T37: this term's grades per course (components, progress, GPA) and the transcript. */
export function StudentGradesPage() {
  const [view, setView] = useState<View>('term')
  // Shares the cache with the "This term" view; only names the term in the eyebrow.
  const termName = useMyGrades().data?.termName

  return (
    <>
      <PageHeader eyebrow={(view === 'term' && termName) || 'Academics'} title="Grades" icon={GraduationCap}>
        Your scores per component as instructors enter them, your GPA, and your transcript.
      </PageHeader>

      <div className="mb-6">
        <Tabs<View>
          label="Grades view"
          value={view}
          onChange={setView}
          items={[
            { value: 'term', label: 'This term' },
            { value: 'transcript', label: 'Transcript' },
          ]}
        />
      </div>

      {view === 'term' ? <ThisTerm /> : <TranscriptView />}
    </>
  )
}

function ThisTerm() {
  const grades = useMyGrades()

  if (grades.isPending)
    return (
      <Card>
        <TableSkeleton label="Loading grades" />
      </Card>
    )
  if (grades.isError)
    return (
      <Card>
        <QueryError error={grades.error} onRetry={() => grades.refetch()} />
      </Card>
    )

  const g = grades.data
  return (
    <div className="space-y-6">
      <StatCardGrid columns="sm:grid-cols-3" aria-label="Grade summary">
        <StatCard
          label="Term GPA"
          value={gpa(g.termGpa)}
          hint={g.termGpa === null ? 'Shown once a course is finalized.' : undefined}
          icon={TrendingUp}
        />
        <StatCard label="Cumulative GPA" value={gpa(g.cumulativeGpa)} hint="Latest attempt of each course, 4.0 scale." icon={Award} />
        <StatCard label="Credits earned" value={String(g.creditsEarned)} hint="Passed with D or better." icon={GraduationCap} />
      </StatCardGrid>

      {g.courses.length === 0 ? (
        <Card>
          <EmptyState icon={GraduationCap} title={g.termId === null ? 'No current term' : 'No courses this term'}>
            {g.termId === null
              ? 'There is no term in progress right now. Your past grades are on the Transcript tab.'
              : 'Enroll in courses from the course catalog; their grades show up here as instructors enter them.'}
          </EmptyState>
        </Card>
      ) : (
        g.courses.map((c) => <CourseGradesCard key={c.sectionId} course={c} />)
      )}
    </div>
  )
}
