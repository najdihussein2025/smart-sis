import { useState } from 'react'
import { ClipboardList, Compass, Play } from 'lucide-react'
import { formatDate } from '@/shared/lib/format'
import { Alert } from '@/shared/ui/Alert'
import { Button } from '@/shared/ui/Button'
import { Card } from '@/shared/ui/Card'
import { EmptyState } from '@/shared/ui/EmptyState'
import { PageHeader } from '@/shared/ui/PageHeader'
import { QueryError } from '@/shared/ui/QueryError'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { QuizView } from '../components/QuizView'
import { RecommendationsView } from '../components/RecommendationsView'
import { RoadmapView } from '../components/RoadmapView'
import { PercentBar } from '../components/SectionCartButtons'
import { useMyAssessments, useRecommendations } from '../hooks'
import type { Assessment } from '../types'

/**
 * PB-16 / T29, PB-17 / T67, PB-20 / T73: take the career assessment (once per term), see the top 3 careers with an
 * explanation and suggested courses, and follow a career's roadmap with "Add to cart".
 */
export function CareerPage() {
  const mine = useMyAssessments()
  const [taking, setTaking] = useState(false)
  const [submitted, setSubmitted] = useState(false)
  const [roadmapCareer, setRoadmapCareer] = useState<number | undefined>()

  const header = (
    <PageHeader eyebrow="Career guidance" title="Career">
      Discover careers that fit your strengths and the courses that lead there.
    </PageHeader>
  )

  if (taking)
    return (
      <>
        {header}
        <QuizView
          onCancel={() => setTaking(false)}
          onSubmitted={() => {
            setTaking(false)
            setSubmitted(true)
          }}
        />
      </>
    )

  if (mine.isPending)
    return (
      <>
        {header}
        <Card>
          <TableSkeleton label="Loading your assessments" />
        </Card>
      </>
    )
  if (mine.isError)
    return (
      <>
        {header}
        <Card>
          <QueryError error={mine.error} onRetry={() => mine.refetch()} />
        </Card>
      </>
    )

  const latest = mine.data.attempts[0]

  return (
    <>
      {header}
      <div className="space-y-6">
        {submitted && <Alert tone="success">Assessment submitted. Here are your results.</Alert>}

        {mine.data.canTake ? (
          <Card className="flex flex-wrap items-center justify-between gap-4 p-5">
            <div className="max-w-xl">
              <h2 className="font-medium text-ink">{latest ? 'Retake the career assessment' : 'Take the career assessment'}</h2>
              <p className="mt-1 text-sm text-muted">
                Answer 20 to 30 short questions about what you enjoy. It takes about 5 minutes and you can take it once per term
                {mine.data.currentTermName ? ` (${mine.data.currentTermName})` : ''}.
              </p>
            </div>
            <Button onClick={() => setTaking(true)}>
              <Play className="size-4" aria-hidden /> {latest ? 'Retake assessment' : 'Start assessment'}
            </Button>
          </Card>
        ) : !latest ? (
          <Card>
            <EmptyState icon={ClipboardList} title="The assessment is closed">
              {mine.data.reason}
            </EmptyState>
          </Card>
        ) : (
          <p className="text-sm text-muted">{mine.data.reason}</p>
        )}

        {latest ? (
          <Results latest={latest} roadmapCareer={roadmapCareer} onRoadmapCareer={setRoadmapCareer} />
        ) : (
          mine.data.canTake && (
            <Card>
              <EmptyState icon={Compass} title="No results yet">
                Take the assessment to see your top careers, why they fit you and the courses that lead there.
              </EmptyState>
            </Card>
          )
        )}
      </div>
    </>
  )
}

function Results({
  latest, roadmapCareer, onRoadmapCareer,
}: { latest: Assessment; roadmapCareer: number | undefined; onRoadmapCareer: (id: number) => void }) {
  const rec = useRecommendations(true)
  // Until the student picks one, the roadmap shows the best match.
  const careerId = roadmapCareer ?? rec.data?.careers[0]?.careerId

  return (
    <>
      <RecommendationsView
        onViewRoadmap={(id) => {
          onRoadmapCareer(id)
          document.getElementById('roadmap')?.scrollIntoView?.({ behavior: 'smooth' })
        }}
      />
      <TraitScores assessment={latest} />
      <RoadmapView careerId={careerId} onCareerChange={onRoadmapCareer} />
    </>
  )
}

function TraitScores({ assessment }: { assessment: Assessment }) {
  return (
    <Card>
      <div className="border-b border-line px-5 py-4">
        <h2 className="font-medium text-ink">Your trait scores</h2>
        <p className="mt-0.5 text-sm text-muted">
          Assessment of {assessment.termName}, submitted {formatDate(assessment.submittedAt)}.
        </p>
      </div>
      {assessment.traits.length === 0 ? (
        <p className="px-5 py-4 text-sm text-muted">No trait scores were recorded.</p>
      ) : (
        <ul className="space-y-3 px-5 py-4" aria-label="Trait scores">
          {assessment.traits.map((t) => (
            <li key={t.traitId}>
              <p className="mb-1 text-sm text-ink">{t.traitName}</p>
              <PercentBar value={t.score} label={t.traitName} />
            </li>
          ))}
        </ul>
      )}
    </Card>
  )
}
