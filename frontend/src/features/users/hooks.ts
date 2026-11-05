import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { lookupKeys } from '@/features/lookups/hooks'
import { usersApi } from './api'
import type { CreateUserRequest, UpdateUserRequest, UserSearch } from './types'

export const userKeys = {
  all: ['users'] as const,
  list: (q: UserSearch) => ['users', 'list', q] as const,
  detail: (id: number) => ['users', 'detail', id] as const,
}

export const useUsers = (q: UserSearch) =>
  useQuery({ queryKey: userKeys.list(q), queryFn: () => usersApi.search(q), placeholderData: keepPreviousData })

export const useUser = (id: number | undefined) =>
  useQuery({ queryKey: userKeys.detail(id ?? 0), queryFn: () => usersApi.get(id!), enabled: id !== undefined })

/** Users and the instructor dropdown (lookups) both change when an account is saved or deactivated. */
function useRefreshUsers() {
  const queryClient = useQueryClient()
  return () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: userKeys.all }),
      queryClient.invalidateQueries({ queryKey: lookupKeys.all }),
    ])
}

export function useCreateUser() {
  const refresh = useRefreshUsers()
  return useMutation({ mutationFn: (body: CreateUserRequest) => usersApi.create(body), onSuccess: refresh })
}

export function useUpdateUser() {
  const refresh = useRefreshUsers()
  return useMutation({
    mutationFn: ({ id, body }: { id: number; body: UpdateUserRequest }) => usersApi.update(id, body),
    onSuccess: refresh,
  })
}

export function useDeactivateUser() {
  const refresh = useRefreshUsers()
  return useMutation({ mutationFn: (id: number) => usersApi.deactivate(id), onSuccess: refresh })
}
