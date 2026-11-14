import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { applyFieldErrors, errorMessage } from '@/shared/api/problem'
import { Alert } from '@/shared/ui/Alert'
import { Button } from '@/shared/ui/Button'
import { Dialog } from '@/shared/ui/Dialog'
import { QueryError } from '@/shared/ui/QueryError'
import { SelectField } from '@/shared/ui/SelectField'
import { TextAreaField } from '@/shared/ui/TextAreaField'
import { TextField } from '@/shared/ui/TextField'
import { useCreateSupportRequest, useSupportCategories } from '../hooks'
import type { SupportRequestDetail } from '../types'

export const newRequestSchema = z.object({
  categoryId: z.string().min(1, 'Choose a category.'),
  subject: z.string().trim().min(1, 'Enter a subject.').max(200, 'Keep the subject under 200 characters.'),
  description: z.string().trim().min(1, 'Describe the problem.').max(4000, 'Keep the description under 4000 characters.'),
})
type Values = z.infer<typeof newRequestSchema>
const fields = ['categoryId', 'subject', 'description'] as const
const empty: Values = { categoryId: '', subject: '', description: '' }

interface Props {
  open: boolean
  onClose: () => void
  onCreated: (request: SupportRequestDetail) => void
}

/** PB-13: submit a support request (category, subject, description). */
export function NewRequestDialog({ open, onClose, onCreated }: Props) {
  const categories = useSupportCategories(open)
  const create = useCreateSupportRequest()
  const [topError, setTopError] = useState<string | null>(null)
  const { register, handleSubmit, formState, reset, setError, watch } = useForm<Values>({
    resolver: zodResolver(newRequestSchema),
    defaultValues: empty,
  })

  useEffect(() => {
    if (!open) return
    reset(empty)
    setTopError(null)
    create.reset()
    // Only when the dialog opens.
  }, [open])

  useEffect(() => {
    const sub = watch(() => setTopError(null))
    return () => sub.unsubscribe()
  }, [watch])

  const onSubmit = handleSubmit((v) =>
    create.mutate(
      { categoryId: Number(v.categoryId), subject: v.subject.trim(), description: v.description.trim() },
      {
        onSuccess: onCreated,
        onError: (error) => {
          if (!applyFieldErrors(error, fields, setError)) setTopError(errorMessage(error))
        },
      },
    ),
  )

  const e = formState.errors
  return (
    <Dialog
      open={open}
      onClose={onClose}
      title="New support request"
      description="Tell us what's wrong. An administrator will answer here and by email."
      actions={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="support-request-form" loading={create.isPending}>
            Submit request
          </Button>
        </>
      }
    >
      <form id="support-request-form" onSubmit={onSubmit} noValidate className="space-y-4">
        {topError && <Alert>{topError}</Alert>}
        {categories.isError && <QueryError error={categories.error} onRetry={() => categories.refetch()} />}
        <SelectField label="Category" error={e.categoryId?.message} disabled={!categories.data} {...register('categoryId')}>
          <option value="">{categories.isPending ? 'Loading…' : 'Choose a category'}</option>
          {categories.data?.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </SelectField>
        <TextField label="Subject" maxLength={200} error={e.subject?.message} {...register('subject')} />
        <TextAreaField
          label="Description"
          rows={6}
          hint="Include course codes, dates or amounts that help us find the problem."
          error={e.description?.message}
          {...register('description')}
        />
      </form>
    </Dialog>
  )
}
