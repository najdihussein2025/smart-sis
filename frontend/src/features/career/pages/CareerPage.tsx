import { useState } from 'react'
import { ArrowRight, ClipboardList, Compass, Lock, RotateCcw } from 'lucide-react'
import { formatDate } from '@/shared/lib/format'
import { Alert } from '@/shared/ui/Alert'
import { Button } from '@/shared/ui/Button'
import { Card } from '@/shared/ui/Card'
import { CardHeader } from '@/shared/ui/CardHeader'
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
    <PageHeader eyebrow="Campus" title="Career" icon={Compass}>
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
          latest ? (
            <Card className="flex flex-wrap items-center justify-between gap-4 p-4 sm:p-5">
              <div className="max-w-xl">
                <h2 className="font-display text-lg font-semibold text-ink">Retake the career assessment</h2>
                <p className="mt-1 text-sm text-muted">
                  You can take it once per term{mine.data.currentTermName ? ` (${mine.data.currentTermName})` : ''}. Your results
                  update once you submit.
                </p>
              </div>
              <Button variant="secondary" onClick={() => setTaking(true)}>
                <RotateCcw className="size-4" aria-hidden /> Retake assessment
              </Button>
            </Card>
          ) : (
            <Card className="overflow-hidden">
              <div className="flex flex-col items-start gap-5 bg-brand-50/60 p-5 sm:flex-row sm:items-center sm:p-6">
                <span className="grid size-14 shrink-0 place-items-center rounded-2xl bg-brand text-accent shadow-sm">
                  <ClipboardList className="size-6" aria-hidden />
                </span>
                <div className="min-w-0 flex-1">
                  <h2 className="font-display text-xl font-semibold text-ink">Take the career assessment</h2>
                  <p className="mt-1 max-w-prose text-sm text-muted">
                    Answer 20 to 30 short questions about what you enjoy. In about 5 minutes you&apos;ll see your top careers, why
                    they fit you and the courses that lead there. You can take it once per term
                    {mine.data.currentTermName ? ` (${mine.data.currentTermName})` : ''}.
                  </p>
                </div>
                <Button onClick={() => setTaking(true)} className="max-sm:w-full">
                  Start assessment <ArrowRight className="size-4" aria-hidden />
                </Button>
              </div>
            </Card>
          )
        ) : !latest ? (
          <Card>
            <EmptyState icon={Lock} title="The assessment is closed">
              {mine.data.reason}
            </EmptyState>
          </Card>
        ) : (
          <Alert tone="info">{mine.data.reason}</Alert>
        )}

        {latest && <Results latest={latest} roadmapCareer={roadmapCareer} onRoadmapCareer={setRoadmapCareer} />}
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
      <CardHeader
        title="Your trait scores"
        description={`Assessment of ${assessment.termName}, submitted ${formatDate(assessment.submittedAt)}.`}
      />
      {assessment.traits.length === 0 ? (
        <p className="px-4 py-4 text-sm text-muted sm:px-5">No trait scores were recorded.</p>
      ) : (
        <ul className="space-y-3 px-4 py-4 sm:px-5" aria-label="Trait scores">
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
