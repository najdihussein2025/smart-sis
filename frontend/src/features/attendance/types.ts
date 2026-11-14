import type { MeetingType } from '@/shared/lib/schedule'

// TODO: replace with generated types from `npm run gen:api` (Attendance DTOs).

export type AttendanceStatus = 'Present' | 'Absent'
export type AbsenceStanding = 'Perfect' | 'Warning' | 'Critical'
export type AbsenceAlertLevel = 'Warning' | 'Limit' | 'AutoDropped'

export interface AttendanceMeeting {
  id: number
  dayOfWeek: number
  startTime: string
  endTime: string
  roomName: string
  type: MeetingType
}

export interface TeachingSection {
  sectionId: number
  courseCode: string
  courseName: string
  sectionCode: string
  credits: number
  absenceLimit: number
  termStart: string
  termEnd: string
  enrolledCount: number
  meetings: AttendanceMeeting[]
}

export interface TeachingSections {
  termId: number | null
  termName: string | null
  /** The server's date (yyyy-mm-dd): attendance can't be taken after it. */
  today: string
  sections: TeachingSection[]
}

export interface AttendanceSheetRow {
  enrollmentId: number
  universityId: number
  fullName: string
  status: AttendanceStatus | null
  absences: number
}

export interface AttendanceSheet {
  sectionId: number
  courseCode: string
  sectionCode: string
  meetingId: number
  dayOfWeek: number
  startTime: string
  type: MeetingType
  sessionDate: string
  /** Set once attendance was submitted for this meeting and date. */
  sessionId: number | null
  submittedAt: string | null
  absenceLimit: number
  students: AttendanceSheetRow[]
}

export interface AttendanceMark {
  enrollmentId: number
  status: AttendanceStatus
}

export interface SubmitAttendance {
  meetingId: number
  sessionDate: string
  records: AttendanceMark[]
}

export interface AbsenceAlert {
  enrollmentId: number
  fullName: string
  level: AbsenceAlertLevel
  absences: number
  limit: number
}

export interface SubmitAttendanceResult {
  sessionId: number
  present: number
  absent: number
  alerts: AbsenceAlert[]
}

export interface SessionSummary {
  sessionId: number
  meetingId: number
  sessionDate: string
  dayOfWeek: number
  startTime: string
  type: MeetingType
  present: number
  absent: number
  submittedAt: string
}

export interface AbsenceDate {
  date: string
  dayOfWeek: number
  startTime: string
  type: MeetingType
}

export interface CourseAttendance {
  sectionId: number
  courseCode: string
  courseName: string
  sectionCode: string
  credits: number
  absences: number
  limit: number
  remaining: number
  standing: AbsenceStanding
  enrollmentStatus: 'Enrolled' | 'DroppedAbsence'
  sessionsRecorded: number
  absenceDates: AbsenceDate[]
}

export interface MyAttendance {
  termId: number | null
  termName: string | null
  courses: CourseAttendance[]
}
