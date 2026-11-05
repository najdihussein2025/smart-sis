// TODO: replace with generated types from `npm run gen:api` (CourseListItemDto, CourseDetailDto, SaveCourseRequest).

export type CourseType = 'Major' | 'Elective'

export interface CourseListItem {
  id: number
  code: string
  name: string
  credits: number
  departmentCode: string
  departmentName: string
}

export interface CourseRef {
  id: number
  code: string
  name: string
}

export interface CourseMajor {
  majorId: number
  majorName: string
  type: CourseType
}

export interface CourseDetail {
  id: number
  code: string
  name: string
  credits: number
  description: string | null
  departmentId: number
  departmentName: string
  prerequisites: CourseRef[]
  majors: CourseMajor[]
}

export interface SaveCourseRequest {
  code: string
  name: string
  credits: number
  description: string | null
  departmentId: number
  prerequisiteIds: number[]
  majors: { majorId: number; type: CourseType }[]
}

export interface CourseSearch {
  search?: string
  departmentId?: number
  page?: number
  pageSize?: number
}
