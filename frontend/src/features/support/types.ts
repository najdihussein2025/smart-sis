// TODO: replace with generated types from `npm run gen:api` (Support DTOs).

export type SupportStatus = 'Open' | 'InProgress' | 'Resolved' | 'Closed'

export const supportStatuses: SupportStatus[] = ['Open', 'InProgress', 'Resolved', 'Closed']

export interface SupportCategory {
  id: number
  name: string
}

export interface AdminSupportCategory extends SupportCategory {
  /** Requests filed under it. A category in use can't be deleted. */
  requestCount: number
}

export interface SupportMessage {
  id: number
  authorId: number
  authorName: string
  authorRole: 'Student' | 'Instructor' | 'Admin'
  body: string
  createdAt: string
}

export interface SupportRequestSummary {
  id: number
  categoryId: number
  categoryName: string
  subject: string
  status: SupportStatus
  createdAt: string
  /** Newest reply, or submission time. */
  lastActivityAt: string
  messageCount: number
}

export interface SupportRequestDetail {
  id: number
  categoryId: number
  categoryName: string
  subject: string
  description: string
  status: SupportStatus
  createdAt: string
  resolvedAt: string | null
  messages: SupportMessage[]
}

export interface AdminSupportRequest {
  id: number
  subject: string
  categoryId: number
  categoryName: string
  status: SupportStatus
  studentId: number
  studentUniversityId: number
  studentName: string
  assignedAdminId: number | null
  assignedAdminName: string | null
  createdAt: string
  lastActivityAt: string
  messageCount: number
}

export interface AdminSupportRequestDetail {
  id: number
  subject: string
  description: string
  categoryId: number
  categoryName: string
  status: SupportStatus
  studentId: number
  studentUniversityId: number
  studentName: string
  studentEmail: string
  assignedAdminId: number | null
  assignedAdminName: string | null
  createdAt: string
  resolvedAt: string | null
  messages: SupportMessage[]
}

export interface SupportAdmin {
  id: number
  fullName: string
  universityId: number
}

export interface CreateSupportRequest {
  categoryId: number
  subject: string
  description: string
}

export interface MySupportSearch {
  status?: SupportStatus
  page?: number
  pageSize?: number
}

export type SupportAssignment = 'Any' | 'Me' | 'Unassigned'

export interface AdminSupportSearch {
  status?: SupportStatus
  categoryId?: number
  assigned?: SupportAssignment
  search?: string
  page?: number
  pageSize?: number
}
