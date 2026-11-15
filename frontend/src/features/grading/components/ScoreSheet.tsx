import { useEffect, useMemo, useRef, useState, type KeyboardEvent } from 'react'
import { useForm, type FieldErrors } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Save } from 'lucide-react'
import { applyFieldErrors, errorMessage } from '@/shared/api/problem'
import { cn } from '@/shared/lib/cn'
import { useSavedMessage } from '@/shared/lib/useSavedMessage'
import { Alert } from '@/shared/ui/Alert'
import { Badge } from '@/shared/ui/Badge'
import { Button } from '@/shared/ui/Button'
import { TableScroll } from '@/shared/ui/TableScroll'
import { useSaveScores } from '../hooks'
import type { ComponentRoster } from '../types'
import { isDecimal2, num, scoreSheetSchema, type ScoreSheetValues } from './gradingForms'

interface Props {
  roster: ComponentRoster
  /** Called after a save, with the message shown next to the Save button. */
  onSaved?: (message: string) => void
  /** True while typed scores are not saved yet, so the page can guard switching component or section. */
  onDirtyChange?: (dirty: boolean) => void
}

const toValues = (roster: ComponentRoster): ScoreSheetValues => ({
  scores: roster.students.map((s) => ({ enrollmentId: s.enrollmentId, score: s.score === null ? '' : num(s.score) })),
})

const plural = (n: number, one: string, many: string) => `${n} ${n === 1 ? one : many}`

/** "2 scores are above the maximum (50)." or a general count when some are not numbers at all. */
function summaryText(values: ScoreSheetValues, errors: FieldErrors<ScoreSheetValues>, max: number): string | null {
  const wrong = values.scores.filter((_, i) => errors.scores?.[i]?.score)
  if (wrong.length === 0) return null
  const aboveMax = wrong.filter((s) => isDecimal2(s.score) && Number(s.score) > max).length
  return aboveMax === wrong.length
    ? `${plural(wrong.length, 'score is', 'scores are')} above the maximum (${num(max)}). Fix ${wrong.length === 1 ? 'it' : 'them'} to save.`
    : `${plural(wrong.length, 'score needs', 'scores need')} fixing before saving.`
}

/** One component's scores, one row per student. Empty = not graded; clearing a saved score removes it. */
export function ScoreSheet({ roster, onSaved, onDirtyChange }: Props) {
  const save = useSaveScores()
  const saved = useSavedMessage()
  const [topError, setTopError] = useState<string | null>(null)
  const [showSummary, setShowSummary] = useState(false)
  const summary = useRef<HTMLDivElement>(null)
  const form = useRef<HTMLFormElement>(null)
  const { component } = roster
  const schema = useMemo(() => scoreSheetSchema(component.maxGrade), [component.maxGrade])
  const { register, handleSubmit, formState, reset, setError, setFocus, watch } = useForm<ScoreSheetValues>({
    resolver: zodResolver(schema),
    defaultValues: toValues(roster),
    // A score above the max shows as soon as the field is left, then clears while it is corrected.
    mode: 'onBlur',
    reValidateMode: 'onChange',
  })
  const dirty = formState.isDirty
  const dirtyRef = useRef(dirty)
  dirtyRef.current = dirty

  // Fresh scores from the server replace the form, unless the instructor is in the middle of typing.
  useEffect(() => {
    if (dirtyRef.current) return
    reset(toValues(roster))
    setTopError(null)
  }, [roster, reset])

  useEffect(() => {
    const sub = watch(() => setTopError(null))
    return () => sub.unsubscribe()
  }, [watch])

  useEffect(() => onDirtyChange?.(dirty), [dirty, onDirtyChange])
  useEffect(() => () => onDirtyChange?.(false), [onDirtyChange])

  const locked = roster.isFinalized
  const typed = watch('scores')
  const notGraded = typed.filter((s) => s.score.trim() === '').length
  const summaryMessage = showSummary ? summaryText({ scores: typed }, formState.errors, component.maxGrade) : null

  const onSubmit = handleSubmit(
    (v) => {
      setShowSummary(false)
      save.mutate(
        {
          sectionId: roster.sectionId,
          componentId: component.id,
          body: { scores: v.scores.map((s) => ({ enrollmentId: s.enrollmentId, score: s.score.trim() === '' ? null : Number(s.score) })) },
        },
        {
          onSuccess: (fresh) => {
            reset(toValues(fresh))
            const text = `Scores saved for ${component.name}.`
            saved.show(text)
            onSaved?.(text)
          },
          onError: (error) => {
            const fields = roster.students.map((_, i) => `scores.${i}.score` as const)
            if (!applyFieldErrors(error, fields, setError)) setTopError(errorMessage(error))
          },
        },
      )
    },
    () => {
      setShowSummary(true)
      // After the summary renders, move focus to it so screen readers hear what is wrong.
      requestAnimationFrame(() => summary.current?.focus())
    },
  )

  // Enter moves down the column, like a spreadsheet, instead of submitting half-typed scores.
  const onKeyDown = (e: KeyboardEvent<HTMLInputElement>, i: number) => {
    if (e.key !== 'Enter') return
    e.preventDefault()
    if (i + 1 < roster.students.length) setFocus(`scores.${i + 1}.score`, { shouldSelect: true })
    else form.current?.querySelector<HTMLButtonElement>('button[type="submit"]')?.focus()
  }

  const e = formState.errors.scores

  return (
    <form ref={form} onSubmit={onSubmit} noValidate aria-label={`Scores for ${component.name}`}>
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-line px-4 py-3 sm:px-5">
        <p className="text-sm text-muted">
          Out of <span className="tabular font-medium text-ink">{num(component.maxGrade)}</span> · weighs{' '}
          <span className="tabular font-medium text-ink">{num(component.weight)}%</span>
        </p>
        {locked ? (
          <Badge tone="success" dot>
            Finalized
          </Badge>
        ) : notGraded > 0 ? (
          <Badge tone="warning" dot>
            {notGraded} not graded
          </Badge>
        ) : (
          <Badge tone="success" dot>
            Everyone graded
          </Badge>
        )}
      </div>

      {(topError || summaryMessage) && (
        <div ref={summary} tabIndex={-1} className="space-y-2 p-4 pb-0 focus:outline-none sm:px-5">
          {summaryMessage && <Alert title="Scores not saved">{summaryMessage}</Alert>}
          {topError && <Alert>{topError}</Alert>}
        </div>
      )}

      <TableScroll>
        <table className="w-full min-w-[480px] text-left text-sm">
          <thead className="border-b border-line text-xs uppercase tracking-wide text-muted">
            <tr>
              <th scope="col" className="px-4 py-3 font-medium sm:px-5">Student</th>
              <th scope="col" className="w-56 px-4 py-3 font-medium">Score</th>
              <th scope="col" className="px-4 py-3 font-medium">ID</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-line">
            {roster.students.map((s, i) => {
              const error = e?.[i]?.score?.message
              const id = `score-${s.enrollmentId}`
              const field = register(`scores.${i}.score`)
              return (
                <tr key={s.enrollmentId} className="align-top">
                  <th scope="row" className="px-4 py-3 font-medium text-ink sm:px-5">
                    {s.fullName}
                  </th>
                  <td className="px-4 py-2">
                    <div className="flex items-center gap-2">
                      <input
                        id={id}
                        inputMode="decimal"
                        enterKeyHint={i + 1 < roster.students.length ? 'next' : 'done'}
                        aria-label={`Score for ${s.fullName}`}
                        aria-invalid={!!error}
                        aria-describedby={error ? `${id}-error` : undefined}
                        disabled={locked}
                        placeholder="Not graded"
                        className={cn(
                          'h-11 w-28 scroll-mb-28 rounded-lg border bg-surface px-3 text-base tabular text-ink placeholder:text-muted sm:h-10 sm:text-[15px]',
                          'focus:outline-none focus:ring-2 focus:ring-accent/40 disabled:bg-canvas disabled:text-muted',
                          error ? 'border-danger' : 'border-line-strong focus:border-brand',
                        )}
                        {...field}
                        onKeyDown={(ev) => onKeyDown(ev, i)}
                      />
                      <span className="tabular text-muted">/ {num(component.maxGrade)}</span>
                    </div>
                    {error && (
                      <p id={`${id}-error`} className="mt-1 text-[13px] text-danger">
                        {error}
                      </p>
                    )}
                  </td>
                  <td className="tabular px-4 py-3 text-muted">{s.universityId}</td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </TableScroll>

      {!locked && (
        <div className="sticky bottom-0 z-10 flex flex-wrap items-center justify-between gap-3 rounded-b-[var(--radius-card)] border-t border-line bg-surface px-4 py-3 shadow-[0_-4px_12px_-8px_rgb(0_0_0/0.15)] sm:px-5">
          <div role="status" className="min-w-0 text-sm">
            {saved.message ? (
              <span className="font-medium text-success">{saved.message}</span>
            ) : (
              <span className="tabular text-muted">
                {roster.students.length - notGraded} of {roster.students.length} graded
                {dirty && <span className="font-medium text-warning"> · unsaved changes</span>}
              </span>
            )}
          </div>
          <Button type="submit" loading={save.isPending}>
            <Save className="size-4" aria-hidden /> Save scores
          </Button>
        </div>
      )}
    </form>
  )
}
