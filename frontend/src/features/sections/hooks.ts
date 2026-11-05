import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { sectionsApi } from './api'
import type { SaveSectionRequest, SectionSearch } from './types'

export const sectionKeys = {
  all: ['sections'] as const,
  list: (q: SectionSearch) => ['sections', 'list', q] as const,
}

/** Paged list. Keeps the previous page on screen while the next one loads. */
export const useSections = (q: SectionSearch, enabled = true) =>
  useQuery({ queryKey: sectionKeys.list(q), queryFn: () => sectionsApi.search(q), placeholderData: keepPreviousData, enabled })

/** Create when `id` is undefined, otherwise update. */
export function useSaveSection() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, body }: { id?: number; body: SaveSectionRequest }) =>
      id === undefined ? sectionsApi.create(body) : sectionsApi.update(id, body),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: sectionKeys.all }),
  })
}
