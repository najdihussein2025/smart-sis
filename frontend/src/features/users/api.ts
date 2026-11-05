import { api } from '@/shared/api/client'
import type { PagedResult } from '@/shared/api/types'
import type { CreateUserRequest, UpdateUserRequest, UserDetail, UserListItem, UserSearch } from './types'

const base = '/api/admin/users'

export const usersApi = {
  search: (q: UserSearch) =>
    api.get<PagedResult<UserListItem>>(base, { params: { ...q, search: q.search?.trim() || undefined } }).then((r) => r.data),
  get: (id: number) => api.get<UserDetail>(`${base}/${id}`).then((r) => r.data),
  create: (body: CreateUserRequest) => api.post<UserDetail>(base, body).then((r) => r.data),
  update: (id: number, body: UpdateUserRequest) => api.put<UserDetail>(`${base}/${id}`, body).then((r) => r.data),
  deactivate: (id: number) => api.post<void>(`${base}/${id}/deactivate`).then(() => undefined),
}
