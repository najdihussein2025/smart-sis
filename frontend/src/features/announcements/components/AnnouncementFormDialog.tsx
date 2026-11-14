import { useEffect, useMemo, useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { useQuery } from '@tanstack/react-query'
import { zodResolver } from '@hookform/resolvers/zod'
import { lookupsApi } from '@/features/lookups/api'
import { lookupKeys } from '@/features/lookups/hooks'
import { applyFieldErrors, errorMessage } from '@/shared/api/problem'
import { Alert } from '@/shared/ui/Alert'
import { Button } from '@/shared/ui/Button'
import { Checkbox } from '@/shared/ui/Checkbox'
import { DateTimeField } from '@/shared/ui/DateTimeField'
import { Dialog } from '@/shared/ui/Dialog'
import { QueryError } from '@/shared/ui/QueryError'
import { SelectField } from '@/shared/ui/SelectField'
import { TextAreaField } from '@/shared/ui/TextAreaField'
import { TextField } from '@/shared/ui/TextField'
import { useAnnouncementSections, useSaveAnnouncement } from '../hooks'
import type { Announcement } from '../types'
import {
  adminScopes, announcementFields, announcementSchema, emptyAnnouncement, nowLocal, toAdminRequest, toFormValues,
  toSectionRequest, type AnnouncementFormValues, type PosterRole,
} from './announcementForm'

interface Props {
  open: boolean
  role: PosterRole
  /** The announcement to edit; undefined to post a new one. */
  announcement?: Announcement
  onClose: () => void
  onSaved: (announcement: Announcement) => void
}

/** Post or edit an announcement. Admin: audience (everyone, students, instructors, a major). Instructor: one of their sections. */
export function AnnouncementFormDialog({ open, role, announcement, onClose, onSaved }: Props) {
  const save = useSaveAnnouncement()
  const isAdmin = role === 'Admin'
  const sections = useAnnouncementSections(open && !isAdmin)
  const [topError, setTopError] = useState<string | null>(null)
  const schema = useMemo(() => announcementSchema(role), [role])
  const { register, control, handleSubmit, formState, reset, setError, watch } = useForm<AnnouncementFormValues>({
    resolver: zodResolver(schema),
    defaultValues: emptyAnnouncement(role),
  })

  // Fresh form each time the dialog opens.
  useEffect(() => {
    if (!open) return
    reset(announcement ? toFormValues(announcement) : emptyAnnouncement(role))
    setTopError(null)
    save.reset()
    // Only when the dialog opens or the announcement changes; `reset` and `save` are stable enough.
  }, [open, announcement])

  // The server message goes away as soon as the user changes something.
  useEffect(() => {
    const sub = watch(() => setTopError(null))
    return () => sub.unsubscribe()
  }, [watch])

  const v = watch()
  const e = formState.errors
  // Majors come from the admin lookups, loaded once the admin picks "one major"; an instructor never calls that endpoint.
  const lookups = useQuery({
    queryKey: lookupKeys.all,
    queryFn: lookupsApi.get,
    staleTime: 5 * 60_000,
    enabled: open && isAdmin && v.scope === 'Major',
  })
  // An admin editing an instructor's section post keeps its section.
  const keepsSection = isAdmin && announcement?.scope === 'Section'

  const onSubmit = handleSubmit((values) => {
    const options = {
      onSuccess: onSaved,
      onError: (error: unknown) => {
        if (!applyFieldErrors(error, announcementFields, setError)) setTopError(errorMessage(error))
      },
    }
    if (isAdmin) save.mutate({ role: 'Admin', id: announcement?.id, body: toAdminRequest(values) }, options)
    else save.mutate({ role: 'Instructor', id: announcement?.id, body: toSectionRequest(values) }, options)
  })

  const now = nowLocal()
  const lookupError = isAdmin ? lookups.isError : sections.isError

  return (
    <Dialog
      open={open}
      onClose={onClose}
      size="lg"
      title={announcement ? 'Edit announcement' : 'New announcement'}
      description={
        isAdmin
          ? 'Everyone in the audience gets a notification when it is published.'
          : 'Students enrolled in the section get a notification when it is published.'
      }
      actions={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="announcement-form" loading={save.isPending}>
            {announcement ? 'Save changes' : v.publishLater ? 'Schedule' : 'Publish'}
          </Button>
        </>
      }
    >
      <form id="announcement-form" onSubmit={onSubmit} noValidate className="space-y-5">
        {topError && <Alert>{topError}</Alert>}
        {lookupError && (
          <QueryError
            error={isAdmin ? lookups.error : sections.error}
            onRetry={() => void (isAdmin ? lookups.refetch() : sections.refetch())}
          />
        )}

        {isAdmin ? (
          keepsSection ? (
            <SelectField label="Audience" disabled hint="Posted by an instructor to one section; the audience stays the same." value="Section">
              <option value="Section">{announcement?.sectionLabel ?? 'One section'}</option>
            </SelectField>
          ) : (
            <div className="grid gap-4 sm:grid-cols-2">
              <SelectField label="Audience" error={e.scope?.message} {...register('scope')}>
                {adminScopes.map((s) => (
                  <option key={s.value} value={s.value}>
                    {s.label}
                  </option>
                ))}
              </SelectField>
              {v.scope === 'Major' && (
                <SelectField label="Major" error={e.majorId?.message} disabled={!lookups.data} {...register('majorId')}>
                  <option value="">{lookups.isPending ? 'Loading…' : 'Choose a major'}</option>
                  {lookups.data?.majors.map((m) => (
                    <option key={m.id} value={m.id}>
                      {m.name}
                    </option>
                  ))}
                </SelectField>
              )}
            </div>
          )
        ) : (
          <SelectField
            label="Section"
            error={e.sectionId?.message}
            disabled={!sections.data}
            hint={sections.data?.length === 0 ? 'You have no sections this term.' : undefined}
            {...register('sectionId')}
          >
            <option value="">{sections.isPending ? 'Loading…' : 'Choose a section'}</option>
            {sections.data?.map((s) => (
              <option key={s.id} value={s.id}>
                {s.label} · {s.courseName} ({s.enrolledCount} enrolled)
              </option>
            ))}
            {/* A post on a section from an earlier term stays selectable when editing it. */}
            {announcement?.sectionId && !sections.data?.some((s) => s.id === announcement.sectionId) && (
              <option value={announcement.sectionId}>{announcement.sectionLabel}</option>
            )}
          </SelectField>
        )}

        <TextField label="Title" maxLength={200} error={e.title?.message} {...register('title')} />
        <TextAreaField label="Message" rows={6} maxLength={4000} error={e.body?.message} {...register('body')} />

        <div className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-3">
            <Checkbox label="Publish later" hint="Otherwise it is published as soon as you save." {...register('publishLater')} />
            {v.publishLater && (
              <Controller
                control={control}
                name="publishAt"
                render={({ field }) => (
                  <DateTimeField
                    label="Publish at"
                    value={field.value}
                    onChange={field.onChange}
                    onBlur={field.onBlur}
                    min={now}
                    minExclusive
                    error={e.publishAt?.message}
                  />
                )}
              />
            )}
          </div>
          <div className="space-y-3">
            <Checkbox label="Expires" hint="Otherwise it stays in the feed." {...register('expires')} />
            {v.expires && (
              <Controller
                control={control}
                name="expiresAt"
                render={({ field }) => (
                  <DateTimeField
                    label="Expires at"
                    value={field.value}
                    onChange={field.onChange}
                    onBlur={field.onBlur}
                    min={v.publishLater && v.publishAt > now ? v.publishAt : now}
                    minExclusive
                    error={e.expiresAt?.message}
                  />
                )}
              />
            )}
          </div>
        </div>
      </form>
    </Dialog>
  )
}
