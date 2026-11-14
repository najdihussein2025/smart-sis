import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { catalogApi } from './api'
import type { CatalogQuery } from './types'

export const catalogKeys = {
  all: ['catalog'] as const,
  list: (q: CatalogQuery) => ['catalog', 'list', q] as const,
  course: (id: number) => ['catalog', 'course', id] as const,
  majors: ['catalog', 'majors'] as const,
}

export const useCatalog = (q: CatalogQuery) =>
  useQuery({ queryKey: catalogKeys.list(q), queryFn: () => catalogApi.list(q), placeholderData: keepPreviousData })

export const useCatalogCourse = (id: number) =>
  useQuery({ queryKey: catalogKeys.course(id), queryFn: () => catalogApi.get(id), enabled: Number.isFinite(id) })

export const useCatalogMajors = () =>
  useQuery({ queryKey: catalogKeys.majors, queryFn: catalogApi.majors, staleTime: 10 * 60_000 })
