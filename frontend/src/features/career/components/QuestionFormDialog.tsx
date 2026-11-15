import { useEffect, useState } from 'react'
import { useFieldArray, useForm, type Control, type UseFormRegister, type FieldErrors } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Plus, Trash2 } from 'lucide-react'
import { z } from 'zod'
import { applyFieldErrors, errorMessage } from '@/shared/api/problem'
import { Alert } from '@/shared/ui/Alert'
import { Button } from '@/shared/ui/Button'
import { Checkbox } from '@/shared/ui/Checkbox'
import { Dialog } from '@/shared/ui/Dialog'
import { SelectField } from '@/shared/ui/SelectField'
import { TextAreaField } from '@/shared/ui/TextAreaField'
import { TextField } from '@/shared/ui/TextField'
import { useSaveQuestion } from '../hooks'
import type { Question, SaveQuestionRequest, Trait } from '../types'

const pointSchema = z.object({
  traitId: z.string().min(1, 'Choose a trait.'),
  points: z.string().refine((p) => Number(p) >= 1 && Number(p) <= 5, 'Points must be between 1 and 5.'),
})

const optionSchema = z.object({
  id: z.number().nullable(),
  text: z.string().trim().min(1, 'Enter the option text.').max(300, 'Use at most 300 characters.'),
  points: z
    .array(pointSchema)
    .refine((ps) => new Set(ps.map((p) => p.traitId)).size === ps.length, 'Each trait can be listed once per option.'),
})

/** Mirrors SaveQuestionRequestValidator (UX only; the API checks again). */
export const questionSchema = z.object({
  text: z.string().trim().min(5, 'Enter a question of at least 5 characters.').max(500, 'Use at most 500 characters.'),
  isActive: z.boolean(),
  options: z
    .array(optionSchema)
    .min(2, 'Add 2 to 6 options.')
    .max(6, 'Add 2 to 6 options.')
    .refine((os) => os.some((o) => o.points.length > 0), 'At least one option must add points to a trait.'),
})

type FormValues = z.infer<typeof questionSchema>

const newOption = (): FormValues['options'][number] => ({ id: null, text: '', points: [] })
const empty = (): FormValues => ({ text: '', isActive: true, options: [newOption(), newOption()] })

function toForm(q: Question): FormValues {
  return {
    text: q.text,
    isActive: q.isActive,
    options: q.options.map((o) => ({
      id: o.id,
      text: o.text,
      points: o.points.map((p) => ({ traitId: String(p.traitId), points: String(p.points) })),
    })),
  }
}

function toRequest(v: FormValues): SaveQuestionRequest {
  return {
    text: v.text.trim(),
    isActive: v.isActive,
    options: v.options.map((o) => ({
      id: o.id,
      text: o.text.trim(),
      points: o.points.map((p) => ({ traitId: Number(p.traitId), points: Number(p.points) })),
    })),
  }
}

interface Props {
  open: boolean
  question?: Question
  traits: Trait[]
  onClose: () => void
  onSaved: (q: Question) => void
}

/** PB-16 / T29: create or edit a question, its options and the trait points each option adds. */
export function QuestionFormDialog({ open, question, traits, onClose, onSaved }: Props) {
  const save = useSaveQuestion()
  const [topError, setTopError] = useState<string | null>(null)
  const { register, control, handleSubmit, formState, reset, setError, watch } = useForm<FormValues>({
    resolver: zodResolver(questionSchema),
    defaultValues: empty(),
  })
  const options = useFieldArray({ control, name: 'options' })

  useEffect(() => {
    if (!open) return
    reset(question ? toForm(question) : empty())
    setTopError(null)
    save.reset()
  }, [open, question])

  useEffect(() => {
    const sub = watch(() => setTopError(null))
    return () => sub.unsubscribe()
  }, [watch])

  const e = formState.errors
  const onSubmit = handleSubmit((v) =>
    save.mutate(
      { id: question?.id, body: toRequest(v) },
      {
        onSuccess: onSaved,
        onError: (error) => {
          const fields = ['text', 'options', ...v.options.flatMap((_, i) => [`options.${i}.text`, `options.${i}.points`])] as const
          if (!applyFieldErrors(error, fields as unknown as (keyof FormValues)[], setError)) setTopError(errorMessage(error))
        },
      },
    ),
  )

  return (
    <Dialog
      open={open}
      onClose={onClose}
      size="lg"
      title={question ? 'Edit question' : 'New question'}
      description="Each option adds 1–5 points to one or more traits. Students never see the points."
      actions={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="question-form" loading={save.isPending}>
            {question ? 'Save changes' : 'Create question'}
          </Button>
        </>
      }
    >
      <form id="question-form" onSubmit={onSubmit} noValidate className="space-y-5">
        {topError && <Alert>{topError}</Alert>}
        {question && question.answerCount > 0 && (
          <p className="rounded-lg bg-warning-soft px-3.5 py-2.5 text-sm text-warning">
            Answered {question.answerCount} times: options students chose can't be removed. Untick “Active” to retire it.
          </p>
        )}
        <TextAreaField label="Question" rows={2} error={e.text?.message} {...register('text')} />
        <Checkbox label="Active" hint="Only active questions are asked in the quiz." {...register('isActive')} />

        <fieldset className="space-y-3">
          <legend className="block text-sm font-medium text-ink">Options</legend>
          {options.fields.map((field, i) => (
            <fieldset key={field.id} aria-label={`Answer option ${i + 1}`} className="space-y-3 rounded-lg border border-line p-3">
              <div className="flex items-end gap-2">
                <div className="flex-1">
                  <TextField label={`Option ${i + 1}`} error={e.options?.[i]?.text?.message} {...register(`options.${i}.text`)} />
                </div>
                <Button
                  type="button"
                  variant="danger-ghost"
                  size="icon"
                  onClick={() => options.remove(i)}
                  disabled={options.fields.length <= 2}
                  aria-label={`Remove option ${i + 1}`}
                >
                  <Trash2 className="size-4" aria-hidden />
                </Button>
              </div>
              <OptionPoints index={i} control={control} register={register} errors={e} traits={traits} />
            </fieldset>
          ))}
          {(e.options?.message || e.options?.root?.message) && <Alert>{e.options?.message ?? e.options?.root?.message}</Alert>}
          <Button type="button" variant="secondary" onClick={() => options.append(newOption())} disabled={options.fields.length >= 6}>
            <Plus className="size-4" aria-hidden /> Add option
          </Button>
        </fieldset>
      </form>
    </Dialog>
  )
}

function OptionPoints({
  index, control, register, errors, traits,
}: {
  index: number
  control: Control<FormValues>
  register: UseFormRegister<FormValues>
  errors: FieldErrors<FormValues>
  traits: Trait[]
}) {
  const points = useFieldArray({ control, name: `options.${index}.points` })
  const oe = errors.options?.[index]
  return (
    <div className="space-y-2 pl-1">
      {points.fields.map((p, j) => (
        <div key={p.id} className="grid grid-cols-[minmax(0,1fr)_6rem_auto] items-end gap-2">
          <SelectField label="Trait" error={oe?.points?.[j]?.traitId?.message} {...register(`options.${index}.points.${j}.traitId`)}>
            <option value="">Choose a trait</option>
            {traits.map((t) => (
              <option key={t.id} value={t.id}>
                {t.name}
              </option>
            ))}
          </SelectField>
          <SelectField label="Points" error={oe?.points?.[j]?.points?.message} {...register(`options.${index}.points.${j}.points`)}>
            {[1, 2, 3, 4, 5].map((n) => (
              <option key={n} value={n}>
                +{n}
              </option>
            ))}
          </SelectField>
          <Button
            type="button"
            variant="danger-ghost"
            size="icon"
            onClick={() => points.remove(j)}
            aria-label={`Remove trait ${j + 1} from option ${index + 1}`}
          >
            <Trash2 className="size-4" aria-hidden />
          </Button>
        </div>
      ))}
      {oe?.points?.message && <p className="text-[13px] text-danger">{oe.points.message}</p>}
      <Button
        type="button"
        variant="ghost"
        size="sm"
        onClick={() => points.append({ traitId: '', points: '1' })}
        disabled={traits.length === 0}
      >
        <Plus className="size-4" aria-hidden /> Add trait points
      </Button>
    </div>
  )
}
