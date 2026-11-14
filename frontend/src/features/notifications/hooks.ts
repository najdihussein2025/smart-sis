import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { notificationsApi } from './api'

/** How often the bell asks for new notifications. */
export const NOTIFICATIONS_REFRESH_MS = 60_000
/** Notifications shown in the dropdown. */
export const BELL_PAGE_SIZE = 10

export const notificationKeys = {
  all: ['notifications'] as const,
  unread: ['notifications', 'unread'] as const,
  list: ['notifications', 'list'] as const,
}

export const useUnreadCount = () =>
  useQuery({ queryKey: notificationKeys.unread, queryFn: notificationsApi.unreadCount, refetchInterval: NOTIFICATIONS_REFRESH_MS })

/** The latest notifications, loaded while the dropdown is open. */
export const useLatestNotifications = (enabled: boolean) =>
  useQuery({
    queryKey: notificationKeys.list,
    queryFn: () => notificationsApi.list({ page: 1, pageSize: BELL_PAGE_SIZE }),
    enabled,
    refetchInterval: enabled ? NOTIFICATIONS_REFRESH_MS : false,
  })

export function useMarkNotificationRead() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: notificationsApi.markRead,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: notificationKeys.all }),
  })
}

export function useMarkAllNotificationsRead() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: notificationsApi.markAllRead,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: notificationKeys.all }),
  })
}
