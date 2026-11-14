import { api } from '@/shared/api/client'
import type { PagedResult } from '@/shared/api/types'
import type {
  Announcement, AnnouncementListQuery, AnnouncementSection, SaveAnnouncementRequest, SaveSectionAnnouncementRequest,
} from './types'

const admin = '/api/admin/announcements'
const instructor = '/api/instructor/announcements'

export const announcementsApi = {
  /** My feed (any role). */
  feed: (q: AnnouncementListQuery) => api.get<PagedResult<Announcement>>('/api/announcements', { params: q }).then((r) => r.data),

  adminList: (q: AnnouncementListQuery) => api.get<PagedResult<Announcement>>(admin, { params: q }).then((r) => r.data),
  adminCreate: (body: SaveAnnouncementRequest) => api.post<Announcement>(admin, body).then((r) => r.data),
  adminUpdate: (id: number, body: SaveAnnouncementRequest) => api.put<Announcement>(`${admin}/${id}`, body).then((r) => r.data),
  adminDelete: (id: number) => api.delete(`${admin}/${id}`).then(() => undefined),

  instructorList: (q: AnnouncementListQuery) => api.get<PagedResult<Announcement>>(instructor, { params: q }).then((r) => r.data),
  instructorSections: () => api.get<AnnouncementSection[]>(`${instructor}/sections`).then((r) => r.data),
  instructorCreate: (body: SaveSectionAnnouncementRequest) => api.post<Announcement>(instructor, body).then((r) => r.data),
  instructorUpdate: (id: number, body: SaveSectionAnnouncementRequest) =>
    api.put<Announcement>(`${instructor}/${id}`, body).then((r) => r.data),
  instructorDelete: (id: number) => api.delete(`${instructor}/${id}`).then(() => undefined),
}
