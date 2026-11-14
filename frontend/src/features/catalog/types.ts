import type { Meeting } from '@/shared/lib/schedule'
import type { TermWindow } from '@/shared/ui/WindowBadge'

// TODO: replace with generated types from `npm run gen:api` (CatalogDto, CatalogCourseDetailDto).

export type { TermWindow }
export type CourseType = 'Major' | 'Elective'
/** The student's own status for a course or section. */
export type CatalogStatus = 'Available' | 'InCart' | 'Enrolled'

export interface CatalogCourse {
  id: number
  code: string
  name: string
  credits: number
  departmentName: string
  type: CourseType | null
  sectionCount: number
  seatsLeft: number
  status: CatalogStatus
}

export interface Catalog {
  termId: number | null
  termName: string | null
  window: TermWindow
  majorId: number | null
  courses: CatalogCourse[]
}

export interface CatalogSection {
  id: number
  sectionCode: string
  instructorName: string | null
  capacity: number
  seatsTaken: number
  seatsLeft: number
  meetings: Meeting[]
  status: CatalogStatus
}

export interface CatalogCourseDetail {
  id: number
  code: string
  name: string
  credits: number
  description: string | null
  departmentName: string
  prerequisites: { id: number; code: string; name: string; passed: boolean }[]
  sections: CatalogSection[]
}

export interface CatalogMajor {
  id: number
  name: string
}

export interface CatalogQuery {
  type?: CourseType
  majorId?: number
  search?: string
}
