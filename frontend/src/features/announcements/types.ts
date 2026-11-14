// TODO: replace with generated types from `npm run gen:api` (AnnouncementDto, SaveAnnouncementRequest…).

/** Rule C1. Admin: All / Students / Instructors / Major. Instructor: Section (own sections only). */
export type AnnouncementScope = 'All' | 'Students' | 'Instructors' | 'Major' | 'Section'

/** Computed by the API from PublishAt / ExpiresAt at request time. */
export type AnnouncementStatus = 'Scheduled' | 'Live' | 'Expired'

export interface Announcement {
  id: number
  title: string
  /** Plain text; line breaks are kept. */
  body: string
  scope: AnnouncementScope
  majorId: number | null
  majorName: string | null
  sectionId: number | null
  /** e.g. "CSC 210 A". */
  sectionLabel: string | null
  authorName: string
  authorRole: 'Student' | 'Instructor' | 'Admin'
  /** UTC ISO. */
  publishAt: string
  /** UTC ISO, or null when it never expires. */
  expiresAt: string | null
  status: AnnouncementStatus
  /** The signed-in user may edit and delete it (admin: all; instructor: own). */
  canManage: boolean
}

/** Admin post / edit. `sectionId` is only sent back when editing an instructor's section post. */
export interface SaveAnnouncementRequest {
  title: string
  body: string
  scope: AnnouncementScope
  majorId: number | null
  sectionId: number | null
  /** UTC ISO; null = publish now. */
  publishAt: string | null
  /** UTC ISO; null = never expires. */
  expiresAt: string | null
}

/** Instructor post / edit, for one section they teach. */
export interface SaveSectionAnnouncementRequest {
  title: string
  body: string
  sectionId: number
  publishAt: string | null
  expiresAt: string | null
}

/** A section the instructor teaches this term. */
export interface AnnouncementSection {
  id: number
  label: string
  courseName: string
  termName: string
  enrolledCount: number
}

export interface AnnouncementListQuery {
  page?: number
  pageSize?: number
  status?: AnnouncementStatus
}
