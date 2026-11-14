import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { adminSupportApi, supportApi } from './api'
import type { AdminSupportRequestDetail, AdminSupportSearch, CreateSupportRequest, MySupportSearch, SupportStatus } from './types'

export const supportKeys = {
  all: ['support'] as const,
  categories: ['support', 'categories'] as const,
  mine: (q: MySupportSearch) => ['support', 'mine', q] as const,
  request: (id: number) => ['support', 'request', id] as const,
  admin: ['support', 'admin'] as const,
  adminList: (q: AdminSupportSearch) => ['support', 'admin', 'list', q] as const,
  adminRequest: (id: number) => ['support', 'admin', 'request', id] as const,
  adminCategories: ['support', 'admin', 'categories'] as const,
  admins: ['support', 'admin', 'admins'] as const,
}

export const useSupportCategories = (enabled = true) =>
  useQuery({ queryKey: supportKeys.categories, queryFn: supportApi.categories, enabled })

/** The student's own requests. Keeps the previous page on screen while the next one loads. */
export const useMySupportRequests = (q: MySupportSearch) =>
  useQuery({ queryKey: supportKeys.mine(q), queryFn: () => supportApi.list(q), placeholderData: keepPreviousData })

export const useSupportRequest = (id: number) =>
  useQuery({ queryKey: supportKeys.request(id), queryFn: () => supportApi.get(id), enabled: Number.isFinite(id) })

export function useCreateSupportRequest() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: CreateSupportRequest) => supportApi.create(body),
    onSuccess: (created) => {
      queryClient.setQueryData(supportKeys.request(created.id), created)
      return queryClient.invalidateQueries({ queryKey: ['support', 'mine'] })
    },
  })
}

export function useReplyToSupportRequest(id: number) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: string) => supportApi.reply(id, body),
    onSuccess: (detail) => {
      queryClient.setQueryData(supportKeys.request(id), detail)
      return queryClient.invalidateQueries({ queryKey: ['support', 'mine'] })
    },
  })
}

// Admin inbox (PB-14)

export const useAdminSupportRequests = (q: AdminSupportSearch) =>
  useQuery({ queryKey: supportKeys.adminList(q), queryFn: () => adminSupportApi.search(q), placeholderData: keepPreviousData })

export const useAdminSupportRequest = (id: number) =>
  useQuery({ queryKey: supportKeys.adminRequest(id), queryFn: () => adminSupportApi.get(id), enabled: Number.isFinite(id) })

export const useSupportAdmins = () => useQuery({ queryKey: supportKeys.admins, queryFn: adminSupportApi.admins })

/** Status change, assignment or reply on one request: the answer is the updated request, the inbox list is refreshed. */
function useAdminRequestMutation<T>(id: number, fn: (value: T) => Promise<AdminSupportRequestDetail>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: fn,
    onSuccess: (detail) => {
      queryClient.setQueryData(supportKeys.adminRequest(id), detail)
      return queryClient.invalidateQueries({ queryKey: ['support', 'admin', 'list'] })
    },
  })
}

export const useSetSupportStatus = (id: number) =>
  useAdminRequestMutation(id, (status: SupportStatus) => adminSupportApi.setStatus(id, status))

export const useAssignSupportRequest = (id: number) =>
  useAdminRequestMutation(id, (adminId: number | null) => adminSupportApi.assign(id, adminId))

export const useAdminReply = (id: number) => useAdminRequestMutation(id, (body: string) => adminSupportApi.reply(id, body))

export const useAdminSupportCategories = () =>
  useQuery({ queryKey: supportKeys.adminCategories, queryFn: adminSupportApi.categories })

/** Category changes also refresh the students' category list and the inbox (category names). */
function useCategoryMutation<T, R>(fn: (value: T) => Promise<R>) {
  const queryClient = useQueryClient()
  return useMutation({ mutationFn: fn, onSuccess: () => queryClient.invalidateQueries({ queryKey: supportKeys.all }) })
}

export const useCreateSupportCategory = () => useCategoryMutation((name: string) => adminSupportApi.createCategory(name))

export const useRenameSupportCategory = () =>
  useCategoryMutation(({ id, name }: { id: number; name: string }) => adminSupportApi.renameCategory(id, name))

export const useDeleteSupportCategory = () => useCategoryMutation((id: number) => adminSupportApi.deleteCategory(id))
