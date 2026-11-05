// TODO: replace with generated types from `npm run gen:api` (UserListItemDto, UserDetailDto, CreateUserRequest, UpdateUserRequest).
import type { Role } from '@/shared/auth/session'

export type UserStatus = 'Active' | 'Inactive' | 'All'

export interface UserListItem {
  id: number
  universityId: number
  fullName: string
  email: string
  role: Role
  isActive: boolean
  majorName: string | null
  departmentName: string | null
}

export interface UserDetail {
  id: number
  universityId: number
  fullName: string
  email: string
  role: Role
  phone: string | null
  /** DateOnly "yyyy-mm-dd". */
  dateOfBirth: string | null
  photoUrl: string | null
  isActive: boolean
  deletedAt: string | null
  student: {
    majorId: number
    majorName: string
    campusId: number
    campusName: string
    enrollmentDate: string
    maxCreditsOverride: number | null
    effectiveMaxCredits: number
  } | null
  instructor: { departmentId: number; departmentName: string } | null
}

export interface StudentInfo {
  majorId: number
  campusId: number
  enrollmentDate: string
  maxCreditsOverride: number | null
}

export interface UpdateUserRequest {
  email: string
  fullName: string
  phone: string | null
  dateOfBirth: string | null
  student: StudentInfo | null
  instructor: { departmentId: number } | null
}

export interface CreateUserRequest extends UpdateUserRequest {
  universityId: number
  role: 'Student' | 'Instructor'
  initialPassword: string
}

export interface UserSearch {
  search?: string
  role?: Role
  status?: UserStatus
  page?: number
  pageSize?: number
}
