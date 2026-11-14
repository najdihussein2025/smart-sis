import { z } from 'zod'
import { fromLocalInput, toLocalInput } from '@/shared/lib/format'
import type { Announcement, AnnouncementScope, SaveAnnouncementRequest, SaveSectionAnnouncementRequest } from '../types'

export type PosterRole = 'Admin' | 'Instructor'

/** Audiences an admin can pick (rule C1). An instructor always posts to one of their sections. */
export const adminScopes: { value: AnnouncementScope; label: string }[] = [
  { value: 'All', label: 'Everyone' },
  { value: 'Students', label: 'All students' },
  { value: 'Instructors', label: 'All instructors' },
  { value: 'Major', label: 'Students of one major' },
]

/** "yyyy-mm-ddTHH:mm" in the viewer's time zone, the DateTimeField format. */
export const nowLocal = () => toLocalInput(new Date().toISOString())

/** Mirrors the API's validators (UX only; the API checks again). */
export function announcementSchema(role: PosterRole) {
  return z
    .object({
      title: z.string().trim().min(1, 'Enter a title.').max(200, 'Use at most 200 characters.'),
      body: z.string().trim().min(1, 'Write the announcement.').max(4000, 'Use at most 4000 characters.'),
      scope: z.enum(['All', 'Students', 'Instructors', 'Major', 'Section']),
      majorId: z.string(),
      sectionId: z.string(),
      publishLater: z.boolean(),
      publishAt: z.string(),
      expires: z.boolean(),
      expiresAt: z.string(),
    })
    .superRefine((v, ctx) => {
      const issue = (path: keyof AnnouncementFormValues, message: string) => ctx.addIssue({ code: 'custom', path: [path], message })
      if (role === 'Admin' && v.scope === 'Major' && !v.majorId) issue('majorId', 'Choose a major.')
      if ((role === 'Instructor' || v.scope === 'Section') && !v.sectionId) issue('sectionId', 'Choose a section.')
      const now = nowLocal()
      // Same-format strings ("yyyy-mm-ddTHH:mm") compare correctly as text.
      if (v.publishLater) {
        if (!v.publishAt) issue('publishAt', 'Choose when to publish it.')
        else if (v.publishAt <= now) issue('publishAt', 'Choose a time in the future, or untick "Publish later".')
      }
      if (v.expires) {
        if (!v.expiresAt) issue('expiresAt', 'Choose when it expires.')
        else if (v.expiresAt <= now) issue('expiresAt', 'The expiry must be in the future.')
        else if (v.publishLater && v.publishAt && v.expiresAt <= v.publishAt) issue('expiresAt', 'The expiry must be after the publish time.')
      }
    })
}

export type AnnouncementFormValues = z.infer<ReturnType<typeof announcementSchema>>

/** API field names that can carry a 400 error. */
export const announcementFields = [
  'title', 'body', 'scope', 'majorId', 'sectionId', 'publishAt', 'expiresAt',
] as const satisfies readonly (keyof AnnouncementFormValues)[]

export const emptyAnnouncement = (role: PosterRole): AnnouncementFormValues => ({
  title: '', body: '', scope: role === 'Admin' ? 'All' : 'Section', majorId: '', sectionId: '',
  publishLater: false, publishAt: '', expires: false, expiresAt: '',
})

export function toFormValues(a: Announcement): AnnouncementFormValues {
  return {
    title: a.title,
    body: a.body,
    scope: a.scope,
    majorId: a.majorId === null ? '' : String(a.majorId),
    sectionId: a.sectionId === null ? '' : String(a.sectionId),
    publishLater: a.status === 'Scheduled',
    publishAt: a.status === 'Scheduled' ? toLocalInput(a.publishAt) : '',
    expires: a.expiresAt !== null,
    expiresAt: a.expiresAt ? toLocalInput(a.expiresAt) : '',
  }
}

const times = (v: AnnouncementFormValues) => ({
  // Empty publish time = now (an already published post keeps its date).
  publishAt: v.publishLater && v.publishAt ? fromLocalInput(v.publishAt) : null,
  expiresAt: v.expires && v.expiresAt ? fromLocalInput(v.expiresAt) : null,
})

export function toAdminRequest(v: AnnouncementFormValues): SaveAnnouncementRequest {
  return {
    title: v.title.trim(),
    body: v.body.trim(),
    scope: v.scope,
    majorId: v.scope === 'Major' && v.majorId ? Number(v.majorId) : null,
    sectionId: v.scope === 'Section' && v.sectionId ? Number(v.sectionId) : null,
    ...times(v),
  }
}

export function toSectionRequest(v: AnnouncementFormValues): SaveSectionAnnouncementRequest {
  return { title: v.title.trim(), body: v.body.trim(), sectionId: Number(v.sectionId), ...times(v) }
}

/** Who an announcement is for, in words ("All students", "Computer Science students", "CSC 210 A"). */
export function audienceLabel(a: Pick<Announcement, 'scope' | 'majorName' | 'sectionLabel'>): string {
  switch (a.scope) {
    case 'All':
      return 'Everyone'
    case 'Students':
      return 'All students'
    case 'Instructors':
      return 'All instructors'
    case 'Major':
      return `${a.majorName ?? 'One major'} students`
    case 'Section':
      return a.sectionLabel ?? 'One section'
  }
}
