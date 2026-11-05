import { z } from 'zod'
import type { SaveSectionRequest, Section } from '../types'
import { days, hm } from './meetings'

const meetingSchema = z
  .object({
    /** Kept so an edited meeting keeps its id (and its attendance history). */
    id: z.number().nullable(),
    dayOfWeek: z.string().refine((d) => Number(d) >= 1 && Number(d) <= 6, 'Choose a day from Monday to Saturday.'),
    startTime: z.string().min(1, 'Choose a start time.'),
    endTime: z.string().min(1, 'Choose an end time.'),
    roomId: z.string().min(1, 'Choose a room.'),
    type: z.enum(['Lecture', 'Lab']),
  })
  .refine((m) => !m.startTime || !m.endTime || m.endTime > m.startTime, {
    path: ['endTime'],
    message: 'The meeting must end after it starts.',
  })

/** Mirrors the API's SaveSectionRequestValidator (UX only; the API checks again, including clashes). */
export const sectionSchema = z
  .object({
    courseId: z.string().min(1, 'Choose a course.'),
    termId: z.string().min(1, 'Choose a term.'),
    sectionCode: z
      .string()
      .trim()
      .min(1, 'Enter a section code.')
      .max(8, 'Use at most 8 characters.')
      .regex(/^[A-Za-z0-9]*$/, 'Use letters and digits only, e.g. "A" or "B2".'),
    instructorId: z.string(),
    capacity: z.string().trim().refine((c) => /^\d+$/.test(c) && Number(c) >= 1 && Number(c) <= 500, 'Capacity must be between 1 and 500.'),
    meetings: z.array(meetingSchema).min(1, 'Add at least one meeting.').max(10, 'Use at most 10 meetings.'),
  })
  .superRefine((v, ctx) => {
    // A section's own meetings can't overlap (same day, start < otherEnd && otherStart < end); back to back is fine.
    for (let i = 0; i < v.meetings.length; i++)
      for (let j = i + 1; j < v.meetings.length; j++) {
        const a = v.meetings[i]!
        const b = v.meetings[j]!
        if (a.dayOfWeek === b.dayOfWeek && a.startTime && b.startTime && a.startTime < b.endTime && b.startTime < a.endTime) {
          ctx.addIssue({
            code: 'custom',
            path: ['meetings'],
            message: `Meetings ${i + 1} and ${j + 1} of this section overlap on ${days[Number(a.dayOfWeek)]}.`,
          })
          return
        }
      }
  })

export type SectionFormValues = z.infer<typeof sectionSchema>
export type MeetingFormValues = SectionFormValues['meetings'][number]

export const newMeeting = (): MeetingFormValues => ({
  id: null, dayOfWeek: '1', startTime: '08:00', endTime: '09:15', roomId: '', type: 'Lecture',
})

export const emptySection = (termId = ''): SectionFormValues => ({
  courseId: '', termId, sectionCode: '', instructorId: '', capacity: '30', meetings: [newMeeting()],
})

/** API field names that can carry an error. Meeting rows use "meetings.N.field" (from "meetings[N].field"). */
export function sectionApiFields(meetingCount: number): string[] {
  const rows = Array.from({ length: meetingCount }, (_, i) =>
    ['dayOfWeek', 'startTime', 'endTime', 'roomId', 'type'].map((f) => `meetings.${i}.${f}`),
  ).flat()
  return ['courseId', 'termId', 'sectionCode', 'instructorId', 'capacity', 'meetings', ...rows]
}

export function toFormValues(s: Section): SectionFormValues {
  return {
    courseId: String(s.courseId),
    termId: String(s.termId),
    sectionCode: s.sectionCode,
    instructorId: s.instructorId === null ? '' : String(s.instructorId),
    capacity: String(s.capacity),
    meetings: s.meetings.map((m) => ({
      id: m.id,
      dayOfWeek: String(m.dayOfWeek),
      startTime: hm(m.startTime),
      endTime: hm(m.endTime),
      roomId: String(m.roomId),
      type: m.type,
    })),
  }
}

export function toRequest(v: SectionFormValues): SaveSectionRequest {
  return {
    courseId: Number(v.courseId),
    termId: Number(v.termId),
    sectionCode: v.sectionCode.trim(),
    instructorId: v.instructorId ? Number(v.instructorId) : null,
    capacity: Number(v.capacity),
    meetings: v.meetings.map((m) => ({
      id: m.id,
      dayOfWeek: Number(m.dayOfWeek),
      startTime: `${m.startTime}:00`,
      endTime: `${m.endTime}:00`,
      roomId: Number(m.roomId),
      type: m.type,
    })),
  }
}
