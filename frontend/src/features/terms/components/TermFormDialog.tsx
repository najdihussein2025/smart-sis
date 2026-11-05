import { useEffect } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { applyFieldErrors, errorMessage, getProblem } from '@/shared/api/problem'
import { addDays } from '@/shared/lib/dates'
import { Alert } from '@/shared/ui/Alert'
import { Button } from '@/shared/ui/Button'
import { Checkbox } from '@/shared/ui/Checkbox'
import { DateField } from '@/shared/ui/DateField'
import { DateTimeField } from '@/shared/ui/DateTimeField'
import { Dialog } from '@/shared/ui/Dialog'
import { TextField } from '@/shared/ui/TextField'
import { useSaveTerm } from '../hooks'
import type { Term } from '../types'
import { emptyTerm, termFields, termSchema, toFormValues, toRequest, type TermFormValues } from './termForm'

interface Props {
  open: boolean
  /** The term to edit; undefined to create one. */
  term?: Term
  onClose: () => void
  onSaved: (term: Term) => void
}

const dateFields = ['startDate', 'endDate', 'enrollStart', 'enrollEnd', 'addDropStart', 'addDropEnd'] as const

export function TermFormDialog({ open, term, onClose, onSaved }: Props) {
  const save = useSaveTerm()
  const { register, control, handleSubmit, formState, reset, setError, watch, getValues, trigger } = useForm<TermFormValues>({
    resolver: zodResolver(termSchema),
    defaultValues: emptyTerm,
  })

  // Fresh form every time the dialog opens (new term, or the latest values of the one being edited).
  useEffect(() => {
    if (!open) return
    reset(term ? toFormValues(term) : emptyTerm)
    save.reset()
    // Only when the dialog opens or the term changes; `reset` and `save` are stable enough not to re-run this.
  }, [open, term])

  // Each date limits the next one, so earlier dates can't be chosen. If an earlier field moves past a later one
  // that is already filled, the later one shows its error straight away.
  const v = watch()
  const recheckDates = () => {
    const filled = dateFields.filter((f) => getValues(f))
    if (filled.length) void trigger(filled)
  }

  const onSubmit = handleSubmit((values) =>
    save.mutate(
      { id: term?.id, body: toRequest(values) },
      {
        onSuccess: onSaved,
        onError: (error) => {
          if (getProblem(error)?.code === 'duplicate_term_name') {
            setError('name', { type: 'server', message: getProblem(error)?.title ?? 'This name is already used.' })
            return
          }
          applyFieldErrors(error, termFields, setError)
        },
      },
    ),
  )

  const problem = getProblem(save.error)
  const showTopError =
    save.isError && problem?.code !== 'duplicate_term_name' && !(problem?.errors && termFields.some((f) => problem.errors?.[f]))
  const e = formState.errors

  return (
    <Dialog
      open={open}
      onClose={onClose}
      size="lg"
      title={term ? `Edit ${term.name}` : 'New term'}
      description="Students enroll and swap during enrollment; add/drop opens after it closes. Times are in your time zone."
      actions={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="term-form" loading={save.isPending}>
            {term ? 'Save changes' : 'Create term'}
          </Button>
        </>
      }
    >
      <form id="term-form" onSubmit={onSubmit} noValidate className="space-y-5">
        {showTopError && <Alert>{errorMessage(save.error)}</Alert>}

        <TextField label="Name" placeholder="e.g. Fall 2026" error={e.name?.message} {...register('name')} />

        <fieldset className="grid gap-4 sm:grid-cols-2">
          <legend className="sr-only">Term dates</legend>
          <Controller
            control={control}
            name="startDate"
            render={({ field }) => (
              <DateField
                label="First day"
                value={field.value}
                onChange={(d) => {
                  field.onChange(d)
                  recheckDates()
                }}
                onBlur={field.onBlur}
                error={e.startDate?.message}
              />
            )}
          />
          <Controller
            control={control}
            name="endDate"
            render={({ field }) => (
              <DateField
                label="Last day"
                value={field.value}
                onChange={(d) => {
                  field.onChange(d)
                  recheckDates()
                }}
                onBlur={field.onBlur}
                min={v.startDate ? addDays(v.startDate, 1) : undefined}
                error={e.endDate?.message}
              />
            )}
          />
        </fieldset>

        <div className="space-y-2">
          <h3 className="text-sm font-semibold text-ink">Enrollment window</h3>
          <div className="grid gap-4 sm:grid-cols-2">
            <Controller
              control={control}
              name="enrollStart"
              render={({ field }) => (
                <DateTimeField
                  label="Enrollment opens"
                  value={field.value}
                  onChange={(d) => {
                    field.onChange(d)
                    recheckDates()
                  }}
                  onBlur={field.onBlur}
                  error={e.enrollStart?.message}
                />
              )}
            />
            <Controller
              control={control}
              name="enrollEnd"
              render={({ field }) => (
                <DateTimeField
                  label="Enrollment closes"
                  value={field.value}
                  onChange={(d) => {
                    field.onChange(d)
                    recheckDates()
                  }}
                  onBlur={field.onBlur}
                  min={v.enrollStart || undefined}
                  minExclusive
                  error={e.enrollEnd?.message}
                />
              )}
            />
          </div>
        </div>

        <div className="space-y-2">
          <h3 className="text-sm font-semibold text-ink">Add/drop window</h3>
          <div className="grid gap-4 sm:grid-cols-2">
            <Controller
              control={control}
              name="addDropStart"
              render={({ field }) => (
                <DateTimeField
                  label="Add/drop opens"
                  value={field.value}
                  onChange={(d) => {
                    field.onChange(d)
                    recheckDates()
                  }}
                  onBlur={field.onBlur}
                  min={v.enrollEnd || undefined}
                  error={e.addDropStart?.message}
                />
              )}
            />
            <Controller
              control={control}
              name="addDropEnd"
              render={({ field }) => (
                <DateTimeField
                  label="Add/drop closes"
                  value={field.value}
                  onChange={(d) => {
                    field.onChange(d)
                    recheckDates()
                  }}
                  onBlur={field.onBlur}
                  min={v.addDropStart || undefined}
                  minExclusive
                  maxDate={v.endDate || undefined}
                  error={e.addDropEnd?.message}
                />
              )}
            />
          </div>
        </div>

        <Checkbox
          label="Current term"
          hint="Only one term is current. Ticking this unsets the term that is current now."
          {...register('isCurrent')}
        />
      </form>
    </Dialog>
  )
}
