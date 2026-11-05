import { api } from '@/shared/api/client'
import type { PagedResult } from '@/shared/api/types'
import type { CourseDetail, CourseListItem, CourseSearch, SaveCourseRequest } from './types'

const base = '/api/admin/courses'

export const coursesApi = {
  search: (q: CourseSearch) =>
    api
      .get<PagedResult<CourseListItem>>(base, { params: { ...q, search: q.search?.trim() || undefined } })
      .then((r) => r.data),
  get: (id: number) => api.get<CourseDetail>(`${base}/${id}`).then((r) => r.data),
  create: (body: SaveCourseRequest) => api.post<CourseDetail>(base, body).then((r) => r.data),
  update: (id: number, body: SaveCourseRequest) => api.put<CourseDetail>(`${base}/${id}`, body).then((r) => r.data),
}
