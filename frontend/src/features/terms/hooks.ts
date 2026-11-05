import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { termsApi } from './api'
import type { SaveTermRequest } from './types'

export const termKeys = { all: ['terms'] as const }

export const useTerms = () => useQuery({ queryKey: termKeys.all, queryFn: termsApi.list })

/** Create when `id` is undefined, otherwise update. Refreshes every term list (making one current unsets another). */
export function useSaveTerm() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, body }: { id?: number; body: SaveTermRequest }) =>
      id === undefined ? termsApi.create(body) : termsApi.update(id, body),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: termKeys.all }),
  })
}
