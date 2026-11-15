import { useEffect, useState } from 'react'
import { useFieldArray, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowDown, ArrowUp, Plus, Trash2 } from 'lucide-react'
import { z } from 'zod'
import { applyFieldErrors, errorMessage, getProblem } from '@/shared/api/problem'
import { Alert } from '@/shared/ui/Alert'
import { Button } from '@/shared/ui/Button'
import { Checkbox } from '@/shared/ui/Checkbox'
import { Dialog } from '@/shared/ui/Dialog'
import { SelectField } from '@/shared/ui/SelectField'
import { TextAreaField } from '@/shared/ui/TextAreaField'
import { TextField } from '@/shared/ui/TextField'
import { useSaveCareer } from '../hooks'
import type { CareerPath, CourseTags, SaveCareerPathRequest, Trait } from '../types'

/** Mirrors SaveCareerPathRequestValidator (UX only; the API checks again). */
const careerSchema = z.object({
  name: z.string().trim().min(2, 'Enter a name of at least 2 characters.').max(100, 'Use at most 100 characters.'),
  description: z.string().max(1000, 'Use at most 1000 characters.'),
  isActive: z.boolean(),
  traits: z
    .array(z.object({
      traitId: z.string().min(1, 'Choose a trait.'),
      weight: z.string().refine((w) => Number(w) >= 1 && Number(w) <= 10, 'Weight must be between 1 and 10.'),
    }))
    .min(1, 'Weigh at least one trait.')
    .refine((ts) => new Set(ts.map((t) => t.traitId)).size === ts.length, 'Each trait can be weighed once.'),
  steps: z
    .array(z.object({
      title: z.string().trim().min(2, 'Enter a title of at least 2 characters.').max(150, 'Use at most 150 characters.'),
      description: z.string().max(1000, 'Use at most 1000 characters.'),
      courseId: z.string(),
    }))
    .max(20, 'Use at most 20 steps.'),
})

type FormValues = z.infer<typeof careerSchema>

const empty = (): FormValues => ({ name: '', description: '', isActive: true, traits: [{ traitId: '', weight: '5' }], steps: [] })

function toForm(c: CareerPath): FormValues {
  return {
    name: c.name,
    description: c.description ?? '',
    isActive: c.isActive,
    traits: c.traits.map((t) => ({ traitId: String(t.traitId), weight: String(t.weight) })),
    steps: c.steps.map((s) => ({ title: s.title, description: s.description ?? '', courseId: s.courseId === null ? '' : String(s.courseId) })),
  }
}

function toRequest(v: FormValues): SaveCareerPathRequest {
  return {
    name: v.name.trim(),
    description: v.description.trim() || null,
    isActive: v.isActive,
    traits: v.traits.map((t) => ({ traitId: Number(t.traitId), weight: Number(t.weight) })),
    steps: v.steps.map((s) => ({ title: s.title.trim(), description: s.description.trim() || null, courseId: s.courseId ? Number(s.courseId) : null })),
  }
}

interface Props {
  open: boolean
  career?: CareerPath
  traits: Trait[]
  courses: CourseTags[]
  onClose: () => void
  onSaved: (c: CareerPath) => void
}

/** PB-17 / PB-20 (T72): a career with trait weights and ordered roadmap steps, each optionally linked to a course. */
export function CareerFormDialog({ open, career, traits, courses, onClose, onSaved }: Props) {
  const save = useSaveCareer()
  const [topError, setTopError] = useState<string | null>(null)
  const { register, control, handleSubmit, formState, reset, setError, watch } = useForm<FormValues>({
    resolver: zodResolver(careerSchema),
    defaultValues: empty(),
  })
  const weights = useFieldArray({ control, name: 'traits' })
  const steps = useFieldArray({ control, name: 'steps' })

  useEffect(() => {
    if (!open) return
    reset(career ? toForm(career) : empty())
    setTopError(null)
    save.reset()
  }, [open, career])

  useEffect(() => {
    const sub = watch(() => setTopError(null))
    return () => sub.unsubscribe()
  }, [watch])

  const e = formState.errors
  const onSubmit = handleSubmit((v) =>
    save.mutate(
      { id: career?.id, body: toRequest(v) },
      {
        onSuccess: onSaved,
        onError: (error) => {
          if (getProblem(error)?.code === 'duplicate_career')
            return setError('name', { type: 'server', message: getProblem(error)?.title ?? 'Name already used.' }, { shouldFocus: true })
          const fields = [
            'name', 'description', 'traits', 'steps',
            ...v.traits.flatMap((_, i) => [`traits.${i}.traitId`, `traits.${i}.weight`]),
            ...v.steps.flatMap((_, i) => [`steps.${i}.title`, `steps.${i}.courseId`]),
          ] as unknown as (keyof FormValues)[]
          if (!applyFieldErrors(error, fields, setError)) setTopError(errorMessage(error))
        },
      },
    ),
  )

  return (
    <Dialog
      open={open}
      onClose={onClose}
      size="lg"
      title={career ? `Edit ${career.name}` : 'New career'}
      description="Trait weights (1–10) decide the match; roadmap steps guide students, and linked courses can be added to the cart."
      actions={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="career-form" loading={save.isPending}>
            {career ? 'Save changes' : 'Create career'}
          </Button>
        </>
      }
    >
      <form id="career-form" onSubmit={onSubmit} noValidate className="space-y-5">
        {topError && <Alert>{topError}</Alert>}
        <TextField label="Name" error={e.name?.message} {...register('name')} />
        <TextAreaField label="Description" rows={2} error={e.description?.message} {...register('description')} />
        <Checkbox label="Active" hint="Inactive careers are hidden from students and never recommended." {...register('isActive')} />

        <fieldset className="space-y-2">
          <legend className="block text-sm font-medium text-ink">Trait weights</legend>
          {weights.fields.map((f, i) => (
            <div key={f.id} className="grid grid-cols-[minmax(0,1fr)_6rem_auto] items-end gap-2">
              <SelectField label={`Trait ${i + 1}`} error={e.traits?.[i]?.traitId?.message} {...register(`traits.${i}.traitId`)}>
                <option value="">Choose a trait</option>
                {traits.map((t) => (
                  <option key={t.id} value={t.id}>
                    {t.name}
                  </option>
                ))}
              </SelectField>
              <SelectField label="Weight" error={e.traits?.[i]?.weight?.message} {...register(`traits.${i}.weight`)}>
                {Array.from({ length: 10 }, (_, n) => n + 1).map((n) => (
                  <option key={n} value={n}>
                    {n}
                  </option>
                ))}
              </SelectField>
              <Button
                type="button"
                variant="danger-ghost"
                size="icon"
                onClick={() => weights.remove(i)}
                disabled={weights.fields.length <= 1}
                aria-label={`Remove trait ${i + 1}`}
              >
                <Trash2 className="size-4" aria-hidden />
              </Button>
            </div>
          ))}
          {(e.traits?.message || e.traits?.root?.message) && <Alert>{e.traits?.message ?? e.traits?.root?.message}</Alert>}
          <Button type="button" variant="secondary" onClick={() => weights.append({ traitId: '', weight: '5' })}>
            <Plus className="size-4" aria-hidden /> Add trait
          </Button>
        </fieldset>

        <fieldset className="space-y-3">
          <legend className="block text-sm font-medium text-ink">Roadmap steps</legend>
          {steps.fields.length === 0 && <p className="text-sm text-muted">No steps yet.</p>}
          {steps.fields.map((f, i) => (
            <fieldset key={f.id} aria-label={`Step ${i + 1}`} className="space-y-3 rounded-lg border border-line p-3">
              <div className="flex items-center justify-between">
                <h4 className="text-sm font-semibold text-ink">Step {i + 1}</h4>
                <div className="flex gap-1">
                  <Button type="button" variant="ghost" size="icon" onClick={() => steps.move(i, i - 1)} disabled={i === 0} aria-label={`Move step ${i + 1} up`}>
                    <ArrowUp className="size-4" aria-hidden />
                  </Button>
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    onClick={() => steps.move(i, i + 1)}
                    disabled={i === steps.fields.length - 1}
                    aria-label={`Move step ${i + 1} down`}
                  >
                    <ArrowDown className="size-4" aria-hidden />
                  </Button>
                  <Button type="button" variant="danger-ghost" size="icon" onClick={() => steps.remove(i)} aria-label={`Remove step ${i + 1}`}>
                    <Trash2 className="size-4" aria-hidden />
                  </Button>
                </div>
              </div>
              <div className="grid gap-3 sm:grid-cols-2">
                <TextField label="Title" error={e.steps?.[i]?.title?.message} {...register(`steps.${i}.title`)} />
                <SelectField label="Course" error={e.steps?.[i]?.courseId?.message} {...register(`steps.${i}.courseId`)}>
                  <option value="">No course (milestone)</option>
                  {courses.map((c) => (
                    <option key={c.courseId} value={c.courseId}>
                      {c.code} · {c.name}
                    </option>
                  ))}
                </SelectField>
              </div>
              <TextField label="Description" error={e.steps?.[i]?.description?.message} {...register(`steps.${i}.description`)} />
            </fieldset>
          ))}
          {e.steps?.message && <Alert>{e.steps.message}</Alert>}
          <Button
            type="button"
            variant="secondary"
            onClick={() => steps.append({ title: '', description: '', courseId: '' })}
            disabled={steps.fields.length >= 20}
          >
            <Plus className="size-4" aria-hidden /> Add step
          </Button>
        </fieldset>
      </form>
    </Dialog>
  )
}
