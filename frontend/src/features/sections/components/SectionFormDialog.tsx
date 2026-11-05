import { useEffect, useState } from 'react'
import { useFieldArray, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Plus, Trash2 } from 'lucide-react'
import { useCourses } from '@/features/courses/hooks'
import { useLookups } from '@/features/lookups/hooks'
import { useTerms } from '@/features/terms/hooks'
import { applyFieldErrors, errorMessage, getProblem } from '@/shared/api/problem'
import { Alert } from '@/shared/ui/Alert'
import { Button } from '@/shared/ui/Button'
import { Dialog } from '@/shared/ui/Dialog'
import { QueryError } from '@/shared/ui/QueryError'
import { SelectField } from '@/shared/ui/SelectField'
import { TextField } from '@/shared/ui/TextField'
import { useSaveSection } from '../hooks'
import type { Section } from '../types'
import { classTimes, days } from './meetings'
import {
  emptySection, newMeeting, sectionApiFields, sectionSchema, toFormValues, toRequest, type SectionFormValues,
} from './sectionForm'

interface Props {
  open: boolean
  /** The section to edit; undefined to create one. */
  section?: Section
  /** Term chosen in the list filter, preselected for a new section. */
  defaultTermId?: number
  onClose: () => void
  onSaved: (section: Section) => void
}

/** 409 codes about the meetings: shown in the Meetings box, next to the rows they are about. */
const meetingConflicts = new Set(['room_clash', 'instructor_clash', 'meeting_overlap', 'meeting_has_attendance'])
/** 409 codes that belong to one field. */
const fieldConflicts: Record<string, 'sectionCode' | 'capacity'> = {
  duplicate_section_code: 'sectionCode',
  capacity_below_taken: 'capacity',
}

export function SectionFormDialog({ open, section, defaultTermId, onClose, onSaved }: Props) {
  const save = useSaveSection()
  const lookups = useLookups()
  const terms = useTerms()
  const courses = useCourses({ pageSize: 100 }, open)
  const [topError, setTopError] = useState<string | null>(null)
  const [meetingError, setMeetingError] = useState<string | null>(null)
  const { register, control, handleSubmit, formState, reset, setError, watch, trigger } = useForm<SectionFormValues>({
    resolver: zodResolver(sectionSchema),
    defaultValues: emptySection(),
  })
  const meetings = useFieldArray({ control, name: 'meetings' })

  // Fresh form each time the dialog opens.
  useEffect(() => {
    if (!open) return
    reset(section ? toFormValues(section) : emptySection(defaultTermId ? String(defaultTermId) : ''))
    setTopError(null)
    setMeetingError(null)
    save.reset()
    // Only when the dialog opens or the section changes; `reset` and `save` are stable enough.
  }, [open, section])

  // Server messages go away as soon as the user changes something.
  useEffect(() => {
    const sub = watch(() => {
      setTopError(null)
      setMeetingError(null)
    })
    return () => sub.unsubscribe()
  }, [watch])

  const values = watch()
  const e = formState.errors

  const onSubmit = handleSubmit((v) =>
    save.mutate(
      { id: section?.id, body: toRequest(v) },
      {
        onSuccess: onSaved,
        onError: (error) => {
          const problem = getProblem(error)
          const code = problem?.code ?? ''
          if (meetingConflicts.has(code)) return setMeetingError(problem?.title ?? 'The meetings clash.')
          const field = fieldConflicts[code]
          if (field) return setError(field, { type: 'server', message: problem?.title ?? 'Not saved.' }, { shouldFocus: true })
          const fields = sectionApiFields(v.meetings.length) as (keyof SectionFormValues)[]
          if (!applyFieldErrors(error, fields, setError)) setTopError(errorMessage(error))
        },
      },
    ),
  )

  const seatsTaken = section?.seatsTaken ?? 0
  const lockedCourseTerm = seatsTaken > 0

  return (
    <Dialog
      open={open}
      onClose={onClose}
      size="lg"
      title={section ? `Edit ${section.courseCode} ${section.sectionCode}` : 'New section'}
      description="Each section meets weekly, Monday to Saturday. Times are 24-hour."
      actions={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="section-form" loading={save.isPending}>
            {section ? 'Save changes' : 'Create section'}
          </Button>
        </>
      }
    >
      <form id="section-form" onSubmit={onSubmit} noValidate className="space-y-5">
        {topError && <Alert>{topError}</Alert>}
        {(lookups.isError || terms.isError || courses.isError) && (
          <QueryError
            error={lookups.error ?? terms.error ?? courses.error}
            onRetry={() => {
              void lookups.refetch()
              void terms.refetch()
              void courses.refetch()
            }}
          />
        )}

        <div className="grid gap-4 sm:grid-cols-2">
          <SelectField
            label="Course"
            error={e.courseId?.message}
            disabled={!courses.data || lockedCourseTerm}
            hint={lockedCourseTerm ? 'Students hold seats, so the course and term stay as they are.' : undefined}
            {...register('courseId')}
          >
            <option value="">{courses.isPending ? 'Loading…' : 'Choose a course'}</option>
            {courses.data?.items.map((c) => (
              <option key={c.id} value={c.id}>
                {c.code} · {c.name}
              </option>
            ))}
          </SelectField>
          <SelectField label="Term" error={e.termId?.message} disabled={!terms.data || lockedCourseTerm} {...register('termId')}>
            <option value="">{terms.isPending ? 'Loading…' : 'Choose a term'}</option>
            {terms.data?.map((t) => (
              <option key={t.id} value={t.id}>
                {t.name}
                {t.isCurrent ? ' (current)' : ''}
              </option>
            ))}
          </SelectField>
        </div>

        <div className="grid gap-4 sm:grid-cols-[8rem_minmax(0,1fr)_8rem]">
          <TextField label="Section code" placeholder="e.g. A" autoCapitalize="characters" error={e.sectionCode?.message} {...register('sectionCode')} />
          <SelectField label="Instructor" error={e.instructorId?.message} disabled={!lookups.data} {...register('instructorId')}>
            <option value="">Not assigned yet</option>
            {lookups.data?.instructors.map((i) => (
              <option key={i.id} value={i.id}>
                {i.fullName} ({i.universityId})
              </option>
            ))}
          </SelectField>
          <TextField
            label="Capacity"
            inputMode="numeric"
            error={e.capacity?.message}
            hint={seatsTaken > 0 ? `${seatsTaken} seats taken` : undefined}
            {...register('capacity')}
          />
        </div>

        <fieldset className="space-y-3" aria-describedby="meetings-error">
          <legend className="block text-sm font-medium text-ink">Weekly meetings</legend>

          {meetings.fields.map((field, i) => {
            const me = e.meetings?.[i]
            const start = values.meetings?.[i]?.startTime ?? ''
            const end = values.meetings?.[i]?.endTime ?? ''
            const times = (current: string) => (current && !classTimes.includes(current) ? [...classTimes, current].sort() : classTimes)
            return (
              <fieldset key={field.id} aria-label={`Meeting ${i + 1}`} className="rounded-lg border border-line p-3">
                <div className="mb-2 flex items-center justify-between">
                  <h4 className="text-sm font-semibold text-ink">Meeting {i + 1}</h4>
                  <Button
                    type="button"
                    variant="ghost"
                    className="h-8 px-2"
                    onClick={() => meetings.remove(i)}
                    disabled={meetings.fields.length === 1}
                    aria-label={`Remove meeting ${i + 1}`}
                  >
                    <Trash2 className="size-4" aria-hidden /> Remove
                  </Button>
                </div>
                <div className="grid grid-cols-2 gap-3 sm:grid-cols-[minmax(0,1.15fr)_minmax(0,0.9fr)_minmax(0,0.9fr)_minmax(0,1.6fr)_minmax(0,1fr)]">
                  <SelectField label="Day" error={me?.dayOfWeek?.message} {...register(`meetings.${i}.dayOfWeek`)}>
                    {days.slice(1).map((d, n) => (
                      <option key={d} value={n + 1}>
                        {d}
                      </option>
                    ))}
                  </SelectField>
                  <SelectField
                    label="Starts"
                    error={me?.startTime?.message}
                    {...register(`meetings.${i}.startTime`, { onChange: () => void trigger(`meetings.${i}.endTime`) })}
                  >
                    {times(start).map((t) => (
                      <option key={t} value={t}>
                        {t}
                      </option>
                    ))}
                  </SelectField>
                  <SelectField label="Ends" error={me?.endTime?.message} {...register(`meetings.${i}.endTime`)}>
                    {/* Ending at or before the start is impossible, so those times can't be picked. */}
                    {times(end).map((t) => (
                      <option key={t} value={t} disabled={!!start && t <= start}>
                        {t}
                      </option>
                    ))}
                  </SelectField>
                  <SelectField label="Room" error={me?.roomId?.message} disabled={!lookups.data} {...register(`meetings.${i}.roomId`)}>
                    <option value="">Choose a room</option>
                    {lookups.data?.rooms.map((r) => (
                      <option key={r.id} value={r.id}>
                        {r.name} · {r.capacity} seats
                      </option>
                    ))}
                  </SelectField>
                  <SelectField label="Type" error={me?.type?.message} {...register(`meetings.${i}.type`)}>
                    <option value="Lecture">Lecture</option>
                    <option value="Lab">Lab</option>
                  </SelectField>
                </div>
              </fieldset>
            )
          })}

          {(meetingError || e.meetings?.message || e.meetings?.root?.message) && (
            <div id="meetings-error">
              <Alert>{meetingError ?? e.meetings?.message ?? e.meetings?.root?.message}</Alert>
            </div>
          )}

          <Button type="button" variant="secondary" onClick={() => meetings.append(newMeeting())} disabled={meetings.fields.length >= 10}>
            <Plus className="size-4" aria-hidden /> Add meeting
          </Button>
        </fieldset>
      </form>
    </Dialog>
  )
}
