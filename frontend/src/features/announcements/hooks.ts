import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { Role } from '@/shared/auth/session'
import { announcementsApi } from './api'
import type { AnnouncementListQuery, SaveAnnouncementRequest, SaveSectionAnnouncementRequest } from './types'

export const announcementKeys = {
  all: ['announcements'] as const,
  feed: (q: AnnouncementListQuery) => ['announcements', 'feed', q] as const,
  manage: (role: Role, q: AnnouncementListQuery) => ['announcements', 'manage', role, q] as const,
  sections: ['announcements', 'sections'] as const,
}

export const useAnnouncementFeed = (q: AnnouncementListQuery) =>
  useQuery({ queryKey: announcementKeys.feed(q), queryFn: () => announcementsApi.feed(q), placeholderData: keepPreviousData })

/** Admin: every post. Instructor: own posts. Any status. */
export const useManagedAnnouncements = (role: Role, q: AnnouncementListQuery, enabled = true) =>
  useQuery({
    queryKey: announcementKeys.manage(role, q),
    queryFn: () => (role === 'Admin' ? announcementsApi.adminList(q) : announcementsApi.instructorList(q)),
    placeholderData: keepPreviousData,
    enabled: enabled && role !== 'Student',
  })

/** Instructor: sections taught this term, for the post form. */
export const useAnnouncementSections = (enabled = true) =>
  useQuery({ queryKey: announcementKeys.sections, queryFn: announcementsApi.instructorSections, enabled })

type SaveVars =
  | { role: 'Admin'; id?: number; body: SaveAnnouncementRequest }
  | { role: 'Instructor'; id?: number; body: SaveSectionAnnouncementRequest }

/** Create when `id` is undefined, otherwise update. Posting can notify the poster's own feed and the bell. */
export function useSaveAnnouncement() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (v: SaveVars) =>
      v.role === 'Admin'
        ? v.id === undefined
          ? announcementsApi.adminCreate(v.body)
          : announcementsApi.adminUpdate(v.id, v.body)
        : v.id === undefined
          ? announcementsApi.instructorCreate(v.body)
          : announcementsApi.instructorUpdate(v.id, v.body),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: announcementKeys.all }),
  })
}

export function useDeleteAnnouncement() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ role, id }: { role: Role; id: number }) =>
      role === 'Admin' ? announcementsApi.adminDelete(id) : announcementsApi.instructorDelete(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: announcementKeys.all }),
  })
}
