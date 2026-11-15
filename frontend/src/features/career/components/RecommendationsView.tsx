import { BookOpen, Compass, Map as MapIcon, Sparkles, Trophy } from 'lucide-react'
import { errorMessage } from '@/shared/api/problem'
import { Alert } from '@/shared/ui/Alert'
import { Badge } from '@/shared/ui/Badge'
import { Button } from '@/shared/ui/Button'
import { Card } from '@/shared/ui/Card'
import { CardHeader } from '@/shared/ui/CardHeader'
import { EmptyState } from '@/shared/ui/EmptyState'
import { QueryError } from '@/shared/ui/QueryError'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { useCareerAddToCart, useRecommendations } from '../hooks'
import { PercentBar, SectionCartButtons } from './SectionCartButtons'

interface Props {
  onViewRoadmap: (careerId: number) => void
}

/** PB-17 / T67: top 3 careers with match bars, the explanation and suggested courses with "Add to cart". */
export function RecommendationsView({ onViewRoadmap }: Props) {
  const rec = useRecommendations(true)
  const add = useCareerAddToCart()

  if (rec.isPending)
    return (
      <Card>
        <TableSkeleton label="Loading recommendations" />
      </Card>
    )
  if (rec.isError)
    return (
      <Card>
        <QueryError error={rec.error} onRetry={() => rec.refetch()} />
      </Card>
    )

  const r = rec.data
  if (r.careers.length === 0)
    return (
      <Card>
        <EmptyState icon={Compass} title="No careers to match yet">
          The administration hasn't set up any career paths. Check again later.
        </EmptyState>
      </Card>
    )

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader
          title="Your top careers"
          icon={Trophy}
          description={`From your assessment in ${r.termName}${r.usesGrades ? ' and your grades in related courses' : ''}.`}
        />
        <ol className="divide-y divide-line" aria-label="Recommended careers">
          {r.careers.map((c, i) => (
            <li key={c.careerId} className="space-y-2 px-4 py-4 sm:px-5">
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div className="min-w-0">
                  <h3 className="font-semibold text-ink">
                    <span className="tabular text-muted">{i + 1}.</span> {c.name}
                  </h3>
                  {c.description && <p className="text-sm text-muted">{c.description}</p>}
                </div>
                <Button variant="secondary" size="sm" onClick={() => onViewRoadmap(c.careerId)} aria-label={`View the ${c.name} roadmap`}>
                  <MapIcon className="size-4" aria-hidden /> Roadmap
                </Button>
              </div>
              <PercentBar value={c.matchPct} label={`${c.name} match`} />
              <p className="tabular text-xs text-muted">
                Assessment {c.quizPct}%{c.gradePct !== null ? ` · Grades ${c.gradePct}%` : ''}
              </p>
              {c.topTraits.length > 0 && (
                <div className="flex flex-wrap gap-1.5">
                  {c.topTraits.map((t) => (
                    <Badge key={t} tone="outline">
                      {t}
                    </Badge>
                  ))}
                </div>
              )}
            </li>
          ))}
        </ol>
        <div className="border-t border-line bg-canvas/60 px-4 py-4 sm:px-5">
          <h3 className="flex items-center gap-1.5 text-sm font-semibold text-ink">
            <Sparkles className="size-4 text-accent-ink" aria-hidden /> Why these careers
          </h3>
          <p className="mt-1.5 text-sm leading-relaxed text-ink">{r.explanation}</p>
          <p className="mt-2 text-xs text-muted">
            {r.explanationSource === 'Ai' ? 'Written by the AI assistant from your scores.' : 'Standard explanation (the AI assistant is unavailable).'}
          </p>
        </div>
      </Card>

      <Card>
        <CardHeader title="Suggested courses" icon={BookOpen} />
        {add.isError && (
          <div className="px-4 pt-4 sm:px-5">
            <Alert>{errorMessage(add.error)}</Alert>
          </div>
        )}
        {r.suggestedCourses.length === 0 ? (
          <EmptyState icon={Compass} title="Nothing to suggest right now">
            You have passed or are taking every course on these careers' roadmaps.
          </EmptyState>
        ) : (
          <ul className="divide-y divide-line" aria-label="Suggested courses">
            {r.suggestedCourses.map((c) => (
              <li key={c.courseId} className="space-y-2 px-4 py-4 sm:px-5">
                <p className="text-ink">
                  <span className="font-semibold">{c.code}</span> <span>{c.name}</span>{' '}
                  <span className="tabular text-sm whitespace-nowrap text-muted">· {c.credits} credits</span>
                </p>
                <p className="text-xs text-muted">For {c.careers.join(', ')}</p>
                <SectionCartButtons
                  courseCode={c.code}
                  sections={c.sections}
                  addingId={add.isPending ? add.variables : undefined}
                  onAdd={add.mutate}
                />
              </li>
            ))}
          </ul>
        )}
      </Card>
    </div>
  )
}
