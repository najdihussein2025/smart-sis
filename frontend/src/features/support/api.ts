import { api } from '@/shared/api/client'
import type { PagedResult } from '@/shared/api/types'
import type {
  AdminSupportCategory, AdminSupportRequest, AdminSupportRequestDetail, AdminSupportSearch, CreateSupportRequest,
  MySupportSearch, SupportAdmin, SupportCategory, SupportRequestDetail, SupportRequestSummary, SupportStatus,
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

const admin = '/api/admin/support'

/** Admin inbox (PB-14) and support categories. */
export const adminSupportApi = {
  search: (q: AdminSupportSearch) => api.get<PagedResult<AdminSupportRequest>>(admin, { params: q }).then((r) => r.data),
  get: (id: number) => api.get<AdminSupportRequestDetail>(`${admin}/${id}`).then((r) => r.data),
  admins: () => api.get<SupportAdmin[]>(`${admin}/admins`).then((r) => r.data),
  setStatus: (id: number, status: SupportStatus) =>
    api.put<AdminSupportRequestDetail>(`${admin}/${id}/status`, { status }).then((r) => r.data),
  assign: (id: number, adminId: number | null) =>
    api.put<AdminSupportRequestDetail>(`${admin}/${id}/assignee`, { adminId }).then((r) => r.data),
  reply: (id: number, body: string) => api.post<AdminSupportRequestDetail>(`${admin}/${id}/messages`, { body }).then((r) => r.data),
  categories: () => api.get<AdminSupportCategory[]>(`${admin}/categories`).then((r) => r.data),
  createCategory: (name: string) => api.post<AdminSupportCategory>(`${admin}/categories`, { name }).then((r) => r.data),
  renameCategory: (id: number, name: string) =>
    api.put<AdminSupportCategory>(`${admin}/categories/${id}`, { name }).then((r) => r.data),
  deleteCategory: (id: number) => api.delete(`${admin}/categories/${id}`).then(() => undefined),
}
