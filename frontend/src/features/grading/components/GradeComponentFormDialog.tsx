import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { applyFieldErrors, errorMessage, getProblem } from '@/shared/api/problem'
import { Alert } from '@/shared/ui/Alert'
import { Button } from '@/shared/ui/Button'
import { Dialog } from '@/shared/ui/Dialog'
import { TextField } from '@/shared/ui/TextField'
import { useSaveGradeComponent } from '../hooks'
import type { GradeComponent } from '../types'
import { componentSchema, num, type ComponentFormValues } from './gradingForms'

interface Props {
  open: boolean
  courseId: number
  termId: number
  /** Course label for the title, e.g. "CSC 101". */
  courseCode: string
  /** Weight still free for this component (100 − the others). */
  weightLeft: number
  /** The component to edit; undefined to add one. */
  component?: GradeComponent
  onClose: () => void
  onSaved: (component: GradeComponent) => void
}

/** 409 codes that belong to one field. */
const fieldConflicts: Record<string, keyof ComponentFormValues> = {
  weight_over_100: 'weight',
  duplicate_component_name: 'name',
  scores_above_max: 'maxGrade',
}

const empty: ComponentFormValues = { name: '', maxGrade: '100', weight: '' }

export function GradeComponentFormDialog({ open, courseId, termId, courseCode, weightLeft, component, onClose, onSaved }: Props) {
  const save = useSaveGradeComponent()
  const [topError, setTopError] = useState<string | null>(null)
  const { register, handleSubmit, formState, reset, setError, watch } = useForm<ComponentFormValues>({
    resolver: zodResolver(componentSchema),
    defaultValues: empty,
  })

  useEffect(() => {
    if (!open) return
    reset(component ? { name: component.name, maxGrade: num(component.maxGrade), weight: num(component.weight) } : empty)
    setTopError(null)
    save.reset()
    // Only when the dialog opens or the component changes.
  }, [open, component])

  useEffect(() => {
    const sub = watch(() => setTopError(null))
    return () => sub.unsubscribe()
  }, [watch])

  const e = formState.errors
  const onSubmit = handleSubmit((v) =>
    save.mutate(
      { id: component?.id, body: { courseId, termId, name: v.name.trim(), maxGrade: Number(v.maxGrade), weight: Number(v.weight) } },
      {
        onSuccess: onSaved,
        onError: (error) => {
          const problem = getProblem(error)
          const field = fieldConflicts[problem?.code ?? '']
          if (field) return setError(field, { type: 'server', message: problem?.title ?? 'Not saved.' }, { shouldFocus: true })
          if (!applyFieldErrors(error, ['name', 'maxGrade', 'weight'] as const, setError)) setTopError(errorMessage(error))
        },
      },
    ),
  )

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={component ? `Edit ${component.name}` : `New component for ${courseCode}`}
      description="Students see each component with its weight. The weights of a course add up to at most 100%."
      actions={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="component-form" loading={save.isPending}>
            {component ? 'Save changes' : 'Add component'}
          </Button>
        </>
      }
    >
      <form id="component-form" onSubmit={onSubmit} noValidate className="space-y-4">
        {topError && <Alert>{topError}</Alert>}
        <TextField label="Name" placeholder="Midterm" error={e.name?.message} {...register('name')} />
        <div className="grid gap-4 sm:grid-cols-2">
          <TextField label="Max grade" inputMode="decimal" error={e.maxGrade?.message} hint="Highest possible score, e.g. 50." {...register('maxGrade')} />
          <TextField
            label="Weight (%)"
            inputMode="decimal"
            error={e.weight?.message}
            hint={`${num(weightLeft)}% is still free.`}
            {...register('weight')}
          />
        </div>
      </form>
    </Dialog>
  )
}
