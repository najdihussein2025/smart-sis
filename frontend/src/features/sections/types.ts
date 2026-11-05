// TODO: replace with generated types from `npm run gen:api` (SectionDto, SaveSectionRequest).

export type MeetingType = 'Lecture' | 'Lab'

/** A weekly meeting. Times are wall-clock "HH:mm:ss"; dayOfWeek 1 = Monday … 6 = Saturday. */
export interface SectionMeeting {
  id: number
  dayOfWeek: number
  startTime: string
  endTime: string
  roomId: number
  roomName: string
  type: MeetingType
}

export interface Section {
  id: number
  courseId: number
  courseCode: string
  courseName: string
  termId: number
  termName: string
  sectionCode: string
  instructorId: number | null
  instructorName: string | null
  capacity: number
  /** Counted from enrollments (Enrolled or Completed), never stored. */
  seatsTaken: number
  meetings: SectionMeeting[]
}

export interface SaveMeetingRequest {
  /** Existing meeting id to keep it (and its attendance history); null for a new meeting. */
  id: number | null
  dayOfWeek: number
  startTime: string
  endTime: string
  roomId: number
  type: MeetingType
}

export interface SaveSectionRequest {
  courseId: number
  termId: number
  sectionCode: string
  instructorId: number | null
  capacity: number
  meetings: SaveMeetingRequest[]
}

export interface SectionSearch {
  termId?: number
  courseId?: number
  page?: number
  pageSize?: number
}
