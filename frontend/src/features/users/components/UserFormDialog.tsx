import { useEffect, useRef, useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useLookups } from '@/features/lookups/hooks'
import { applyFieldErrors, errorMessage, getProblem } from '@/shared/api/problem'
import { addDays } from '@/shared/lib/dates'
import { todayIso } from '@/shared/lib/format'
import { cn } from '@/shared/lib/cn'
import { Alert } from '@/shared/ui/Alert'
import { Button } from '@/shared/ui/Button'
import { DateField } from '@/shared/ui/DateField'
import { Dialog } from '@/shared/ui/Dialog'
import { QueryError } from '@/shared/ui/QueryError'
import { SelectField } from '@/shared/ui/SelectField'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { TextField } from '@/shared/ui/TextField'
import { useCreateUser, useUpdateUser, useUser } from '../hooks'
import type { UserDetail } from '../types'
import {
  emptyUser, toCreateRequest, toFormValues, toUpdateRequest, userApiFields, userSchema, type UserFormValues,
} from './userForm'

interface Props {
  open: boolean
  /** Id of the user to edit; undefined to create one. */
  userId?: number
  onClose: () => void
  onSaved: (user: UserDetail) => void
}

/** Duplicates (409) are shown under their field. */
const conflictField: Record<string, 'universityId' | 'email'> = {
  duplicate_university_id: 'universityId',
  duplicate_email: 'email',
}

export function UserFormDialog({ open, userId, onClose, onSaved }: Props) {
  const editing = userId !== undefined
  const user = useUser(open ? userId : undefined)
  const lookups = useLookups()
  const create = useCreateUser()
  const update = useUpdateUser()
  const save = editing ? update : create
  const [topError, setTopError] = useState<string | null>(null)
  const { register, control, handleSubmit, formState, reset, setError, watch } = useForm<UserFormValues>({
    resolver: zodResolver(userSchema(!editing)),
    defaultValues: emptyUser(),
  })

  // Fresh form once per opening; when editing, as soon as the account has loaded.
  const loaded = editing ? user.data : undefined
  const filled = useRef(false)
  useEffect(() => {
    if (!open) {
      filled.current = false
      return
    }
    if (filled.current || (editing && !loaded)) return
    filled.current = true
    reset(loaded ? toFormValues(loaded) : emptyUser())
    setTopError(null)
    create.reset()
    update.reset()
    // `reset`, `create` and `update` are stable enough not to re-run this.
  }, [open, editing, loaded])

  useEffect(() => {
    const sub = watch(() => setTopError(null))
    return () => sub.unsubscribe()
  }, [watch])

  const role = watch('role')
  const majorId = watch('student.majorId')
  const majorDefault = lookups.data?.majors.find((m) => String(m.id) === majorId)?.defaultMaxCredits

  const onError = (error: unknown) => {
    const problem = getProblem(error)
    const field = problem?.code ? conflictField[problem.code] : undefined
    if (field) return setError(field, { type: 'server', message: problem?.title ?? 'Already in use.' }, { shouldFocus: true })
    if (!applyFieldErrors(error, userApiFields, setError)) setTopError(errorMessage(error))
  }
  const onSubmit = handleSubmit((v) =>
    editing
      ? update.mutate({ id: userId, body: toUpdateRequest(v) }, { onSuccess: onSaved, onError })
      : create.mutate(toCreateRequest(v), { onSuccess: onSaved, onError }),
  )

  const e = formState.errors
  const ready = !editing || user.isSuccess
  const title = editing ? (user.data ? `Edit ${user.data.fullName}` : 'Edit user') : role === 'Student' ? 'New student' : 'New instructor'

  return (
    <Dialog
      open={open}
      onClose={onClose}
      size="lg"
      title={title}
      description={editing ? 'University ID and role don’t change.' : 'The user signs in with the University ID and this initial password.'}
      actions={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="user-form" loading={save.isPending} disabled={!ready}>
            {editing ? 'Save changes' : `Create ${role === 'Student' ? 'student' : 'instructor'}`}
          </Button>
        </>
      }
    >
      {editing && user.isPending ? (
        <TableSkeleton label="Loading user" rows={4} />
      ) : editing && user.isError ? (
        <QueryError error={user.error} onRetry={() => user.refetch()} />
      ) : (
        <form id="user-form" onSubmit={onSubmit} noValidate className="space-y-5">
          {topError && <Alert>{topError}</Alert>}
          {lookups.isError && <QueryError error={lookups.error} onRetry={() => lookups.refetch()} />}

          {editing ? (
            <p className="text-sm text-muted">
              {user.data?.role} · University ID <span className="font-medium text-ink">{user.data?.universityId}</span>
            </p>
          ) : (
            <Controller
              control={control}
              name="role"
              render={({ field }) => (
                <fieldset>
                  <legend className="mb-1.5 block text-sm font-medium text-ink">Role</legend>
                  <div className="inline-flex rounded-lg border border-line p-1" role="radiogroup" aria-label="Role">
                    {(['Student', 'Instructor'] as const).map((r) => (
                      <label
                        key={r}
                        className={cn(
                          'cursor-pointer rounded-md px-4 py-1.5 text-sm font-medium has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-accent/40',
                          field.value === r ? 'bg-brand text-white' : 'text-muted hover:text-ink',
                        )}
                      >
                        <input type="radio" className="sr-only" name={field.name} value={r} checked={field.value === r} onChange={() => field.onChange(r)} />
                        {r}
                      </label>
                    ))}
                  </div>
                </fieldset>
              )}
            />
          )}

          <div className="grid gap-4 sm:grid-cols-2">
            {!editing && (
              <TextField label="University ID" inputMode="numeric" placeholder="e.g. 3002" error={e.universityId?.message} {...register('universityId')} />
            )}
            <TextField label="Full name" autoComplete="off" error={e.fullName?.message} {...register('fullName')} />
            <TextField label="Email" type="email" autoComplete="off" error={e.email?.message} {...register('email')} />
            <TextField label="Phone (optional)" type="tel" error={e.phone?.message} {...register('phone')} />
            <Controller
              control={control}
              name="dateOfBirth"
              render={({ field }) => (
                <DateField
                  label="Date of birth (optional)"
                  value={field.value}
                  onChange={field.onChange}
                  onBlur={field.onBlur}
                  max={addDays(todayIso(), -1)}
                  error={e.dateOfBirth?.message}
                />
              )}
            />
            {!editing && (
              <TextField
                label="Initial password"
                type="text"
                autoComplete="off"
                hint="8+ characters with a letter and a number. Give it to the user; they can change it later."
                error={e.initialPassword?.message}
                {...register('initialPassword')}
              />
            )}
          </div>

          {role === 'Student' ? (
            <fieldset className="space-y-3">
              <legend className="block text-sm font-semibold text-ink">Student details</legend>
              <div className="grid gap-4 sm:grid-cols-2">
                <SelectField label="Major" error={e.student?.majorId?.message} disabled={!lookups.data} {...register('student.majorId')}>
                  <option value="">{lookups.isPending ? 'Loading…' : 'Choose a major'}</option>
                  {lookups.data?.majors.map((m) => (
                    <option key={m.id} value={m.id}>
                      {m.name}
                    </option>
                  ))}
                </SelectField>
                <SelectField label="Campus" error={e.student?.campusId?.message} disabled={!lookups.data} {...register('student.campusId')}>
                  <option value="">{lookups.isPending ? 'Loading…' : 'Choose a campus'}</option>
                  {lookups.data?.campuses.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.name}
                    </option>
                  ))}
                </SelectField>
                <Controller
                  control={control}
                  name="student.enrollmentDate"
                  render={({ field }) => (
                    <DateField
                      label="Enrollment date"
                      value={field.value}
                      onChange={field.onChange}
                      onBlur={field.onBlur}
                      error={e.student?.enrollmentDate?.message}
                    />
                  )}
                />
                <TextField
                  label="Credit cap per term (optional)"
                  inputMode="numeric"
                  placeholder={majorDefault ? String(majorDefault) : undefined}
                  hint={majorDefault ? `Leave empty to use the major’s ${majorDefault} credits.` : 'Leave empty to use the major’s default.'}
                  error={e.student?.maxCreditsOverride?.message}
                  {...register('student.maxCreditsOverride')}
                />
              </div>
            </fieldset>
          ) : (
            <fieldset className="space-y-3">
              <legend className="block text-sm font-semibold text-ink">Instructor details</legend>
              <div className="grid gap-4 sm:grid-cols-2">
                <SelectField label="Department" error={e.instructor?.departmentId?.message} disabled={!lookups.data} {...register('instructor.departmentId')}>
                  <option value="">{lookups.isPending ? 'Loading…' : 'Choose a department'}</option>
                  {lookups.data?.departments.map((d) => (
                    <option key={d.id} value={d.id}>
                      {d.code} · {d.name}
                    </option>
                  ))}
                </SelectField>
              </div>
            </fieldset>
          )}
        </form>
      )}
    </Dialog>
  )
}
