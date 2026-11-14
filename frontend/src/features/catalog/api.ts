import { api } from '@/shared/api/client'
import type { Catalog, CatalogCourseDetail, CatalogMajor, CatalogQuery } from './types'

export const catalogApi = {
  list: (q: CatalogQuery) => api.get<Catalog>('/api/catalog/courses', { params: q }).then((r) => r.data),
  get: (id: number) => api.get<CatalogCourseDetail>(`/api/catalog/courses/${id}`).then((r) => r.data),
  majors: () => api.get<CatalogMajor[]>('/api/catalog/majors').then((r) => r.data),
}
