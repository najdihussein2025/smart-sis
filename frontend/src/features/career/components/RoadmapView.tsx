import { Map as MapIcon } from 'lucide-react'
import { errorMessage } from '@/shared/api/problem'
import { Alert } from '@/shared/ui/Alert'
import { Badge } from '@/shared/ui/Badge'
import { Card } from '@/shared/ui/Card'
import { EmptyState } from '@/shared/ui/EmptyState'
import { QueryError } from '@/shared/ui/QueryError'
import { SelectField } from '@/shared/ui/SelectField'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { useCareerAddToCart, useCareerList, useRoadmap } from '../hooks'
import type { RoadmapStepStatus } from '../types'
import { SectionCartButtons } from './SectionCartButtons'

const statusBadge: Record<RoadmapStepStatus, { label: string; tone: 'success' | 'info' | 'warning' | 'neutral' | 'outline' }> = {
  Done: { label: 'Done', tone: 'success' },
  InProgress: { label: 'In progress', tone: 'info' },
  Next: { label: 'Next', tone: 'warning' },
  Later: { label: 'Later', tone: 'neutral' },
  Milestone: { label: 'Milestone', tone: 'outline' },
}

interface Props {
  careerId: number | undefined
  onCareerChange: (careerId: number) => void
}

/** PB-20 / T73: a career's roadmap with the student's progress and "Add to cart" for courses still to take. */
export function RoadmapView({ careerId, onCareerChange }: Props) {
  const careers = useCareerList()
  const roadmap = useRoadmap(careerId)
  const add = useCareerAddToCart()

  return (
    <Card id="roadmap">
      <div className="flex flex-wrap items-end justify-between gap-4 border-b border-line px-4 py-4 sm:px-5">
        <div className="flex min-w-0 items-center gap-3">
          <span className="grid size-9 shrink-0 place-items-center rounded-lg bg-brand-50 text-brand">
            <MapIcon className="size-[18px]" aria-hidden />
          </span>
          <div className="min-w-0">
            <h2 className="font-display text-lg leading-snug font-semibold text-ink">Career roadmap</h2>
            <p className="text-sm text-muted">Steps to get there, with the courses you have done and the ones to take next.</p>
          </div>
        </div>
        <div className="w-full sm:w-64">
          <SelectField
            label="Career"
            value={careerId ?? ''}
            disabled={!careers.data}
            onChange={(e) => onCareerChange(Number(e.target.value))}
          >
            {careerId === undefined && <option value="">Choose a career</option>}
            {careers.data?.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </SelectField>
        </div>
      </div>

      {add.isError && (
        <div className="px-4 pt-4 sm:px-5">
          <Alert>{errorMessage(add.error)}</Alert>
        </div>
      )}

      {careerId === undefined ? (
        <EmptyState icon={MapIcon} title="Choose a career">
          Pick a career above to see its roadmap.
        </EmptyState>
      ) : roadmap.isPending ? (
        <TableSkeleton label="Loading roadmap" />
      ) : roadmap.isError ? (
        <QueryError error={roadmap.error} onRetry={() => roadmap.refetch()} />
      ) : roadmap.data.steps.length === 0 ? (
        <EmptyState icon={MapIcon} title="No steps yet">
          This career has no roadmap steps yet.
        </EmptyState>
      ) : (
        <>
          {roadmap.data.courseStepCount > 0 && (
            <p className="tabular px-4 pt-4 text-sm text-muted sm:px-5">
              {roadmap.data.doneCount} of {roadmap.data.courseStepCount} courses done
            </p>
          )}
          <ol className="space-y-0 px-4 py-4 sm:px-5" aria-label={`${roadmap.data.name} roadmap`}>
            {roadmap.data.steps.map((s) => {
              const badge = statusBadge[s.status]
              const canAdd = s.course && (s.status === 'Next' || s.status === 'Later')
              return (
                <li key={s.stepOrder} className="relative border-l-2 border-line pb-5 pl-5 last:pb-0">
                  <span
                    className={`absolute -left-[7px] top-1.5 size-3 rounded-full ${s.status === 'Done' ? 'bg-success' : s.status === 'Next' ? 'bg-accent' : 'bg-line'}`}
                    aria-hidden
                  />
                  <div className="flex flex-wrap items-center gap-2">
                    <h3 className="font-semibold text-ink">
                      <span className="tabular text-muted">{s.stepOrder}.</span> {s.title}
                    </h3>
                    <Badge tone={badge.tone} dot>
                      {badge.label}
                    </Badge>
                  </div>
                  {s.course && (
                    <p className="mt-0.5 text-sm text-ink">
                      <span className="font-semibold">{s.course.code}</span> {s.course.name}{' '}
                      <span className="tabular whitespace-nowrap text-muted">· {s.course.credits} credits</span>
                    </p>
                  )}
                  {s.description && <p className="mt-0.5 text-sm text-muted">{s.description}</p>}
                  {canAdd && (
                    <div className="mt-2">
                      <SectionCartButtons
                        courseCode={s.course!.code}
                        sections={s.course!.sections}
                        addingId={add.isPending ? add.variables : undefined}
                        onAdd={add.mutate}
                      />
                    </div>
                  )}
                </li>
              )
            })}
          </ol>
        </>
      )}
    </Card>
  )
}
