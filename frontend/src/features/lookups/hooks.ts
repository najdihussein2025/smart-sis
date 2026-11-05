import { useQuery } from '@tanstack/react-query'
import { lookupsApi } from './api'

export const lookupKeys = { all: ['lookups'] as const }

/** Dropdown lists for the admin forms. Small and rarely changed, so cached for a few minutes. */
export const useLookups = () => useQuery({ queryKey: lookupKeys.all, queryFn: lookupsApi.get, staleTime: 5 * 60_000 })
