import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { errorMessage, getProblem } from '@/shared/api/problem'
import { Alert } from '@/shared/ui/Alert'
import { Button } from '@/shared/ui/Button'
import { Checkbox } from '@/shared/ui/Checkbox'
import { Dialog } from '@/shared/ui/Dialog'
import { TextField } from '@/shared/ui/TextField'
import { useSaveCourseTags, useSaveTrait } from '../hooks'
import type { CourseTags, Trait } from '../types'

const traitSchema = z.object({
  name: z.string().trim().min(2, 'Enter a name of at least 2 characters.').max(60, 'Use at most 60 characters.'),
  description: z.string().max(300, 'Use at most 300 characters.'),
})
type TraitForm = z.infer<typeof traitSchema>

/** Create or rename a trait. */
export function TraitFormDialog({
  open, trait, onClose, onSaved,
}: { open: boolean; trait?: Trait; onClose: () => void; onSaved: (t: Trait) => void }) {
  const save = useSaveTrait()
  const [topError, setTopError] = useState<string | null>(null)
  const { register, handleSubmit, formState, reset, setError } = useForm<TraitForm>({
    resolver: zodResolver(traitSchema),
    defaultValues: { name: '', description: '' },
  })

  useEffect(() => {
    if (!open) return
    reset({ name: trait?.name ?? '', description: trait?.description ?? '' })
    setTopError(null)
    save.reset()
  }, [open, trait])

  const onSubmit = handleSubmit((v) =>
    save.mutate(
      { id: trait?.id, body: { name: v.name.trim(), description: v.description.trim() || null } },
      {
        onSuccess: onSaved,
        onError: (error) => {
          const p = getProblem(error)
          if (p?.code === 'duplicate_trait') setError('name', { type: 'server', message: p.title ?? 'Name already used.' }, { shouldFocus: true })
          else if (p?.errors?.name?.[0]) setError('name', { type: 'server', message: p.errors.name[0] })
          else setTopError(errorMessage(error))
        },
      },
    ),
  )

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={trait ? `Edit ${trait.name}` : 'New trait'}
      actions={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="trait-form" loading={save.isPending}>
            {trait ? 'Save changes' : 'Create trait'}
          </Button>
        </>
      }
    >
      <form id="trait-form" onSubmit={onSubmit} noValidate className="space-y-4" onChange={() => setTopError(null)}>
        {topError && <Alert>{topError}</Alert>}
        <TextField label="Name" error={formState.errors.name?.message} {...register('name')} />
        <TextField label="Description" error={formState.errors.description?.message} {...register('description')} />
      </form>
    </Dialog>
  )
}

/** Tag a course with traits: grades in it count towards those traits (~30% of a match). */
export function CourseTagsDialog({
  course, traits, onClose, onSaved,
}: { course: CourseTags | null; traits: Trait[]; onClose: () => void; onSaved: (c: CourseTags) => void }) {
  const save = useSaveCourseTags()
  const [selected, setSelected] = useState<number[]>([])

  useEffect(() => {
    if (!course) return
    setSelected(course.traitIds)
    save.reset()
  }, [course])

  const toggle = (id: number) => {
    save.reset()
    setSelected((s) => (s.includes(id) ? s.filter((x) => x !== id) : [...s, id]))
  }

  return (
    <Dialog
      open={course !== null}
      onClose={onClose}
      title={course ? `Traits of ${course.code}` : 'Traits'}
      description="Grades in this course count towards the ticked traits."
      actions={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button
            type="button"
            loading={save.isPending}
            onClick={() => course && save.mutate({ courseId: course.courseId, traitIds: selected }, { onSuccess: onSaved })}
          >
            Save tags
          </Button>
        </>
      }
    >
      <div className="space-y-3">
        {save.isError && <Alert>{errorMessage(save.error)}</Alert>}
        {traits.map((t) => (
          <Checkbox key={t.id} label={t.name} checked={selected.includes(t.id)} onChange={() => toggle(t.id)} />
        ))}
      </div>
    </Dialog>
  )
}
