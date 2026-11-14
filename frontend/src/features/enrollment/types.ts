import type { Meeting } from '@/shared/lib/schedule'
import type { TermWindow } from '@/shared/ui/WindowBadge'

// TODO: replace with generated types from `npm run gen:api` (MyEnrollmentsDto, EnrollResultDto).

export interface EnrolledSection {
  enrollmentId: number
  sectionId: number
  courseId: number
  courseCode: string
  courseName: string
  credits: number
  sectionCode: string
  instructorName: string | null
  meetings: Meeting[]
  enrolledAt: string
}

export interface MyEnrollments {
  termId: number | null
  termName: string | null
  window: TermWindow
  enrolledCredits: number
  maxCredits: number
  items: EnrolledSection[]
}

export interface EnrollOutcome {
  sectionId: number
  label: string | null
  success: boolean
  /** Why it failed, written for the student. */
  message: string | null
  /** window_closed, credit_cap, prerequisite_missing, same_course, schedule_conflict, section_full, … */
  code: string | null
}

export interface EnrollResult {
  results: EnrollOutcome[]
  enrolledCount: number
}
