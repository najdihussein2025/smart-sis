import type { MeetingType } from '@/shared/lib/schedule'

// TODO: replace with generated types from `npm run gen:api` (ScheduleDto).

/** One weekly meeting of a section the user is enrolled in or teaches. Times are wall-clock "HH:mm:ss". */
export interface ScheduleItem {
  sectionId: number
  meetingId: number
  courseCode: string
  courseName: string
  sectionCode: string
  /** 1 = Monday … 6 = Saturday. */
  dayOfWeek: number
  startTime: string
  endTime: string
  roomName: string
  /** Null on the teaching schedule (it's always the signed-in instructor). */
  instructorName: string | null
  type: MeetingType
}

/** termId/termName are null when there is no current term. Items are ordered by day, then start time. */
export interface Schedule {
  termId: number | null
  termName: string | null
  items: ScheduleItem[]
}
