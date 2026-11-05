import { api } from '@/shared/api/client'
import type { PagedResult } from '@/shared/api/types'
import type { SaveSectionRequest, Section, SectionSearch } from './types'

const base = '/api/admin/sections'

export const sectionsApi = {
  search: (q: SectionSearch) => api.get<PagedResult<Section>>(base, { params: q }).then((r) => r.data),
  create: (body: SaveSectionRequest) => api.post<Section>(base, body).then((r) => r.data),
  update: (id: number, body: SaveSectionRequest) => api.put<Section>(`${base}/${id}`, body).then((r) => r.data),
}
