import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { coursesApi } from './api'
import type { CourseSearch, SaveCourseRequest } from './types'

export const courseKeys = {
  all: ['courses'] as const,
  list: (q: CourseSearch) => ['courses', 'list', q] as const,
  detail: (id: number) => ['courses', 'detail', id] as const,
}

/** Paged search. Keeps the previous page on screen while the next one loads. */
export const useCourses = (q: CourseSearch, enabled = true) =>
  useQuery({ queryKey: courseKeys.list(q), queryFn: () => coursesApi.search(q), placeholderData: keepPreviousData, enabled })

export const useCourse = (id: number | undefined) =>
  useQuery({ queryKey: courseKeys.detail(id ?? 0), queryFn: () => coursesApi.get(id!), enabled: id !== undefined })

/** Create when `id` is undefined, otherwise update. Refreshes lists and details (prerequisite names show elsewhere). */
export function useSaveCourse() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, body }: { id?: number; body: SaveCourseRequest }) =>
      id === undefined ? coursesApi.create(body) : coursesApi.update(id, body),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: courseKeys.all }),
  })
}
