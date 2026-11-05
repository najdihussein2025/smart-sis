import { api } from '@/shared/api/client'
import type { SaveTermRequest, Term } from './types'

const base = '/api/admin/terms'

export const termsApi = {
  list: () => api.get<Term[]>(base).then((r) => r.data),
  create: (body: SaveTermRequest) => api.post<Term>(base, body).then((r) => r.data),
  update: (id: number, body: SaveTermRequest) => api.put<Term>(`${base}/${id}`, body).then((r) => r.data),
}
