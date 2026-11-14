import { useEffect } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { applyFieldErrors, errorMessage, getProblem } from '@/shared/api/problem'
import { Alert } from '@/shared/ui/Alert'
import { Button } from '@/shared/ui/Button'
import { DateField } from '@/shared/ui/DateField'
import { Dialog } from '@/shared/ui/Dialog'
import { SelectField } from '@/shared/ui/SelectField'
import { TextField } from '@/shared/ui/TextField'
import { useCreateBatch } from '../hooks'
import type { ChargeBatch, FinanceOptions } from '../types'
import { batchFields, batchSchema, emptyBatch, toBatchRequest, type BatchFormValues } from './financeForms'

interface Props {
  open: boolean
  options: FinanceOptions | undefined
  onClose: () => void
  onSaved: (batch: ChargeBatch) => void
}

/** Rule F1: one action charges every active student of the major. */
export function BatchFormDialog({ open, options, onClose, onSaved }: Props) {
  const save = useCreateBatch()
  const { register, control, handleSubmit, formState, reset, setError, watch } = useForm<BatchFormValues>({
    resolver: zodResolver(batchSchema),
    defaultValues: emptyBatch,
  })

  useEffect(() => {
    if (!open) return
    const current = options?.terms.find((t) => t.isCurrent)
    reset({ ...emptyBatch, termId: current ? String(current.id) : '' })
    save.reset()
    // Only when the dialog opens.
  }, [open])

  const onSubmit = handleSubmit((values) =>
    save.mutate(toBatchRequest(values), {
      onSuccess: onSaved,
      onError: (error) => applyFieldErrors(error, batchFields, setError),
    }),
  )

  const problem = getProblem(save.error)
  const showTopError = save.isError && !(problem?.errors && batchFields.some((f) => problem.errors?.[f]))
  const e = formState.errors
  const major = options?.majors.find((m) => String(m.id) === watch('majorId'))

  return (
    <Dialog
      open={open}
      onClose={onClose}
      size="lg"
      title="Charge a major"
      description="Creates one charge for every active student of the major. Each student is notified by email."
      actions={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="batch-form" loading={save.isPending}>
            Create charges
          </Button>
        </>
      }
    >
      <form id="batch-form" onSubmit={onSubmit} noValidate className="space-y-5" onChange={() => save.isError && save.reset()}>
        {showTopError && <Alert>{errorMessage(save.error)}</Alert>}

        <div className="grid gap-4 sm:grid-cols-2">
          <SelectField
            label="Major"
            error={e.majorId?.message}
            hint={major ? `${major.activeStudents} active ${major.activeStudents === 1 ? 'student' : 'students'} will be charged.` : undefined}
            {...register('majorId')}
          >
            <option value="">Choose a major</option>
            {options?.majors.map((m) => (
              <option key={m.id} value={m.id}>
                {m.name}
              </option>
            ))}
          </SelectField>
          <SelectField label="Term" error={e.termId?.message} {...register('termId')}>
            <option value="">Choose a term</option>
            {options?.terms.map((t) => (
              <option key={t.id} value={t.id}>
                {t.name}
                {t.isCurrent ? ' (current)' : ''}
              </option>
            ))}
          </SelectField>
          <SelectField label="Payment type" error={e.paymentTypeId?.message} {...register('paymentTypeId')}>
            <option value="">Choose a type</option>
            {options?.paymentTypes.map((p) => (
              <option key={p.id} value={p.id}>
                {p.name}
              </option>
            ))}
          </SelectField>
          <TextField label="Title" placeholder="e.g. Fall 2026 tuition" error={e.title?.message} {...register('title')} />
          <TextField label="Amount per student" inputMode="decimal" placeholder="0.00" error={e.amount?.message} {...register('amount')} />
          <Controller
            control={control}
            name="dueDate"
            render={({ field }) => (
              <DateField label="Due date" value={field.value} onChange={field.onChange} onBlur={field.onBlur} error={e.dueDate?.message} />
            )}
          />
        </div>
      </form>
    </Dialog>
  )
}
