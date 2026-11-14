import { api } from '@/shared/api/client'
import type { PagedResult } from '@/shared/api/types'
import type {
  CreateSupportRequest, MySupportSearch, SupportCategory, SupportRequestDetail, SupportRequestSummary,
} from './types'

const base = '/api/support'

/** Student side (PB-13): only the signed-in student's own requests. */
export const supportApi = {
  categories: () => api.get<SupportCategory[]>(`${base}/categories`).then((r) => r.data),
  list: (q: MySupportSearch) => api.get<PagedResult<SupportRequestSummary>>(`${base}/requests`, { params: q }).then((r) => r.data),
  get: (id: number) => api.get<SupportRequestDetail>(`${base}/requests/${id}`).then((r) => r.data),
  create: (body: CreateSupportRequest) => api.post<SupportRequestDetail>(`${base}/requests`, body).then((r) => r.data),
  reply: (id: number, body: string) =>
    api.post<SupportRequestDetail>(`${base}/requests/${id}/messages`, { body }).then((r) => r.data),
}
