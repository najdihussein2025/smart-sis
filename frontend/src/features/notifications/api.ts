import { api } from '@/shared/api/client'
import type { PagedResult } from '@/shared/api/types'
import type { AppNotification, UnreadCount } from './types'

const base = '/api/notifications'

export const notificationsApi = {
  list: (q: { page?: number; pageSize?: number; unreadOnly?: boolean }) =>
    api.get<PagedResult<AppNotification>>(base, { params: q }).then((r) => r.data),
  unreadCount: () => api.get<UnreadCount>(`${base}/unread-count`).then((r) => r.data),
  markRead: (id: number) => api.post(`${base}/${id}/read`).then(() => undefined),
  markAllRead: () => api.post(`${base}/read-all`).then(() => undefined),
}
