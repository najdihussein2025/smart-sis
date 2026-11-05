import { useEffect, useRef, useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useLookups } from '@/features/lookups/hooks'
import { applyFieldErrors, errorMessage, getProblem } from '@/shared/api/problem'
import { Alert } from '@/shared/ui/Alert'
import { Button } from '@/shared/ui/Button'
import { Dialog } from '@/shared/ui/Dialog'
import { QueryError } from '@/shared/ui/QueryError'
import { SelectField } from '@/shared/ui/SelectField'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { TextAreaField } from '@/shared/ui/TextAreaField'
import { TextField } from '@/shared/ui/TextField'
import { useCourse, useSaveCourse } from '../hooks'
import type { CourseDetail, CourseType } from '../types'
import {
  courseApiFields, courseSchema, emptyCourse, toFormField, toFormValues, toRequest, withMajorType, type CourseFormValues,
} from './courseForm'
import { PrerequisitePicker } from './PrerequisitePicker'

interface Props {
  open: boolean
  /** Id of the course to edit; undefined to create one. */
  courseId?: number
  onClose: () => void
  onSaved: (course: CourseDetail) => void
}

/** 409 codes that belong to one field, shown under it instead of at the top of the form. */
const conflictField: Record<string, keyof CourseFormValues> = {
  duplicate_course_code: 'code',
  prerequisite_cycle: 'prerequisites',
  prerequisite_self: 'prerequisites',
}

export function CourseFormDialog({ open, courseId, onClose, onSaved }: Props) {
  const editing = courseId !== undefined
  const course = useCourse(open ? courseId : undefined)
  const lookups = useLookups()
  const save = useSaveCourse()
  const [topError, setTopError] = useState<string | null>(null)
  const { register, control, handleSubmit, formState, reset, setError, watch } = useForm<CourseFormValues>({
    resolver: zodResolver(courseSchema),
    defaultValues: emptyCourse,
  })

  // Fresh form each time the dialog opens; when editing, as soon as the course has loaded. Only once per opening,
  // so a background refetch can't wipe what the user is typing.
  const loaded = editing ? course.data : undefined
  const filled = useRef(false)
  useEffect(() => {
    if (!open) {
      filled.current = false
      return
    }
    if (filled.current || (editing && !loaded)) return
    filled.current = true
    reset(loaded ? toFormValues(loaded) : emptyCourse)
    setTopError(null)
    save.reset()
    // `reset` and `save` are stable enough not to re-run this.
  }, [open, editing, loaded])

  // The message at the top of the form goes away as soon as the user changes something.
  useEffect(() => {
    const sub = watch(() => setTopError(null))
    return () => sub.unsubscribe()
  }, [watch])

  const onSubmit = handleSubmit((values) =>
    save.mutate(
      { id: courseId, body: toRequest(values) },
      {
        onSuccess: onSaved,
        onError: (error) => {
          const problem = getProblem(error)
          const field = problem?.code ? conflictField[problem.code] : undefined
          if (field) return setError(field, { type: 'server', message: problem?.title ?? 'Not saved.' }, { shouldFocus: true })
          const matched = applyFieldErrors(error, courseApiFields, (f, e) => setError(toFormField(f), e))
          if (!matched) setTopError(errorMessage(error))
        },
      },
    ),
  )

  const e = formState.errors
  const ready = !editing || course.isSuccess

  return (
    <Dialog
      open={open}
      onClose={onClose}
      size="lg"
      title={editing ? (course.data ? `Edit ${course.data.code}` : 'Edit course') : 'New course'}
      description="Prerequisites and majors are replaced by what you save here."
      actions={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="course-form" loading={save.isPending} disabled={!ready}>
            {editing ? 'Save changes' : 'Create course'}
          </Button>
        </>
      }
    >
      {editing && course.isPending ? (
        <TableSkeleton label="Loading course" rows={4} />
      ) : editing && course.isError ? (
        <QueryError error={course.error} onRetry={() => course.refetch()} />
      ) : (
        <form id="course-form" onSubmit={onSubmit} noValidate className="space-y-5">
          {topError && <Alert>{topError}</Alert>}

          <div className="grid gap-4 sm:grid-cols-[10rem_minmax(0,1fr)]">
            <TextField label="Code" placeholder="e.g. CSC 101" autoCapitalize="characters" error={e.code?.message} {...register('code')} />
            <TextField label="Name" placeholder="e.g. Data Structures" error={e.name?.message} {...register('name')} />
          </div>

          <div className="grid gap-4 sm:grid-cols-[10rem_minmax(0,1fr)]">
            <TextField label="Credits" inputMode="numeric" error={e.credits?.message} {...register('credits')} />
            <SelectField label="Department" error={e.departmentId?.message} disabled={!lookups.data} {...register('departmentId')}>
              <option value="">{lookups.isPending ? 'Loading…' : 'Choose a department'}</option>
              {lookups.data?.departments.map((d) => (
                <option key={d.id} value={d.id}>
                  {d.code} · {d.name}
                </option>
              ))}
            </SelectField>
          </div>
          {lookups.isError && <QueryError error={lookups.error} onRetry={() => lookups.refetch()} />}

          <TextAreaField label="Description (optional)" error={e.description?.message} {...register('description')} />

          <Controller
            control={control}
            name="prerequisites"
            render={({ field }) => (
              <PrerequisitePicker value={field.value} onChange={field.onChange} excludeId={courseId} error={e.prerequisites?.message} />
            )}
          />

          <Controller
            control={control}
            name="majors"
            render={({ field }) => (
              <fieldset className="space-y-2">
                <legend className="block text-sm font-medium text-ink">Majors</legend>
                <p className="text-[13px] text-muted">How the course counts for each major. Leave “Not in this major” to skip it.</p>
                <div className="grid gap-3 sm:grid-cols-2">
                  {lookups.data?.majors.map((m) => (
                    <SelectField
                      key={m.id}
                      label={m.name}
                      value={field.value.find((x) => x.majorId === m.id)?.type ?? ''}
                      onChange={(ev) => field.onChange(withMajorType(field.value, m.id, ev.target.value as CourseType | ''))}
                    >
                      <option value="">Not in this major</option>
                      <option value="Major">Major (required)</option>
                      <option value="Elective">Elective</option>
                    </SelectField>
                  ))}
                </div>
                {e.majors?.message && <p className="text-[13px] text-danger">{e.majors.message}</p>}
              </fieldset>
            )}
          />
        </form>
      )}
    </Dialog>
  )
}
