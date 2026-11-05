import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { applyFieldErrors, errorMessage, getProblem } from '@/shared/api/problem'
import { Alert } from '@/shared/ui/Alert'
import { Button } from '@/shared/ui/Button'
import { Checkbox } from '@/shared/ui/Checkbox'
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

export function TermFormDialog({ open, term, onClose, onSaved }: Props) {
  const save = useSaveTerm()
  const { register, handleSubmit, formState, reset, setError } = useForm<TermFormValues>({
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
          <TextField label="First day" type="date" error={e.startDate?.message} {...register('startDate')} />
          <TextField label="Last day" type="date" error={e.endDate?.message} {...register('endDate')} />
        </fieldset>

        <fieldset className="grid gap-4 sm:grid-cols-2">
          <legend className="mb-2 text-sm font-semibold text-ink">Enrollment window</legend>
          <TextField label="Enrollment opens" type="datetime-local" error={e.enrollStart?.message} {...register('enrollStart')} />
          <TextField label="Enrollment closes" type="datetime-local" error={e.enrollEnd?.message} {...register('enrollEnd')} />
        </fieldset>

        <fieldset className="grid gap-4 sm:grid-cols-2">
          <legend className="mb-2 text-sm font-semibold text-ink">Add/drop window</legend>
          <TextField label="Add/drop opens" type="datetime-local" error={e.addDropStart?.message} {...register('addDropStart')} />
          <TextField label="Add/drop closes" type="datetime-local" error={e.addDropEnd?.message} {...register('addDropEnd')} />
        </fieldset>

        <Checkbox
          label="Current term"
          hint="Only one term is current. Ticking this unsets the term that is current now."
          {...register('isCurrent')}
        />
      </form>
    </Dialog>
  )
}
