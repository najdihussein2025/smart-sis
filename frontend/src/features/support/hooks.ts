import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { supportApi } from './api'
import type { CreateSupportRequest, MySupportSearch } from './types'

export const supportKeys = {
  all: ['support'] as const,
  categories: ['support', 'categories'] as const,
  mine: (q: MySupportSearch) => ['support', 'mine', q] as const,
  request: (id: number) => ['support', 'request', id] as const,
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
