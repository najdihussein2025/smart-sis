import { useEffect, useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Save } from 'lucide-react'
import { applyFieldErrors, errorMessage } from '@/shared/api/problem'
import { cn } from '@/shared/lib/cn'
import { Alert } from '@/shared/ui/Alert'
import { Badge } from '@/shared/ui/Badge'
import { Button } from '@/shared/ui/Button'
import { TableScroll } from '@/shared/ui/TableScroll'
import { useSaveScores } from '../hooks'
import type { ComponentRoster } from '../types'
import { num, scoreSheetSchema, type ScoreSheetValues } from './gradingForms'

interface Props {
  roster: ComponentRoster
  onSaved: (message: string) => void
}

const toValues = (roster: ComponentRoster): ScoreSheetValues => ({
  scores: roster.students.map((s) => ({ enrollmentId: s.enrollmentId, score: s.score === null ? '' : num(s.score) })),
})

/** One component's scores, one row per student. Empty = not graded; clearing a saved score removes it. */
export function ScoreSheet({ roster, onSaved }: Props) {
  const save = useSaveScores()
  const [topError, setTopError] = useState<string | null>(null)
  const { component } = roster
  const schema = useMemo(() => scoreSheetSchema(component.maxGrade), [component.maxGrade])
  const { register, handleSubmit, formState, reset, setError, watch } = useForm<ScoreSheetValues>({
    resolver: zodResolver(schema),
    defaultValues: toValues(roster),
  })

  // A new component, or fresh scores from the server, replaces what is in the form.
  useEffect(() => {
    reset(toValues(roster))
    setTopError(null)
  }, [roster, reset])

  useEffect(() => {
    const sub = watch(() => setTopError(null))
    return () => sub.unsubscribe()
  }, [watch])

  const locked = roster.isFinalized
  const typed = watch('scores')
  const notGraded = typed.filter((s) => s.score.trim() === '').length

  const onSubmit = handleSubmit((v) =>
    save.mutate(
      {
        sectionId: roster.sectionId,
        componentId: component.id,
        body: { scores: v.scores.map((s) => ({ enrollmentId: s.enrollmentId, score: s.score.trim() === '' ? null : Number(s.score) })) },
      },
      {
        onSuccess: () => onSaved(`Scores saved for ${component.name}.`),
        onError: (error) => {
          const fields = roster.students.map((_, i) => `scores.${i}.score` as const)
          if (!applyFieldErrors(error, fields, setError)) setTopError(errorMessage(error))
        },
      },
    ),
  )

  const e = formState.errors.scores

  return (
    <form onSubmit={onSubmit} noValidate aria-label={`Scores for ${component.name}`}>
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-line px-4 py-3">
        <p className="text-sm text-muted">
          Out of <span className="font-medium text-ink">{num(component.maxGrade)}</span> · weighs{' '}
          <span className="font-medium text-ink">{num(component.weight)}%</span>
        </p>
        {locked ? (
          <Badge tone="success">Finalized</Badge>
        ) : notGraded > 0 ? (
          <Badge tone="warning">{notGraded} not graded</Badge>
        ) : (
          <Badge tone="success">Everyone graded</Badge>
        )}
      </div>

      {topError && (
        <div className="p-4 pb-0">
          <Alert>{topError}</Alert>
        </div>
      )}

      <TableScroll>
        <table className="w-full min-w-[480px] text-left text-sm">
          <thead className="border-b border-line text-xs uppercase tracking-wide text-muted">
            <tr>
              <th scope="col" className="px-4 py-3 font-medium">Student</th>
              <th scope="col" className="px-4 py-3 font-medium">ID</th>
              <th scope="col" className="w-48 px-4 py-3 font-medium">Score</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-line">
            {roster.students.map((s, i) => {
              const error = e?.[i]?.score?.message
              const id = `score-${s.enrollmentId}`
              return (
                <tr key={s.enrollmentId} className="align-top">
                  <th scope="row" className="px-4 py-3 font-medium text-ink">
                    {s.fullName}
                  </th>
                  <td className="px-4 py-3 tabular-nums text-muted">{s.universityId}</td>
                  <td className="px-4 py-2">
                    <div className="flex items-center gap-2">
                      <input
                        id={id}
                        inputMode="decimal"
                        aria-label={`Score for ${s.fullName}`}
                        aria-invalid={!!error}
                        aria-describedby={error ? `${id}-error` : undefined}
                        disabled={locked}
                        placeholder="Not graded"
                        className={cn(
                          'h-10 w-28 rounded-lg border bg-surface px-3 text-[15px] tabular-nums text-ink placeholder:text-muted',
                          'focus:outline-none focus:ring-2 focus:ring-accent/40 disabled:bg-canvas disabled:text-muted',
                          error ? 'border-danger' : 'border-line-strong focus:border-brand',
                        )}
                        {...register(`scores.${i}.score`)}
                      />
                      <span className="text-muted">/ {num(component.maxGrade)}</span>
                    </div>
                    {error && (
                      <p id={`${id}-error`} className="mt-1 text-[13px] text-danger">
                        {error}
                      </p>
                    )}
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </TableScroll>

      {!locked && (
        <div className="flex justify-end border-t border-line px-4 py-3">
          <Button type="submit" loading={save.isPending}>
            <Save className="size-4" aria-hidden /> Save scores
          </Button>
        </div>
      )}
    </form>
  )
}
