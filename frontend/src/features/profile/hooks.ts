import { useQuery } from '@tanstack/react-query'
import { profileApi } from './api'

export const profileKeys = { me: ['profile', 'me'] as const }

export const useProfile = () => useQuery({ queryKey: profileKeys.me, queryFn: profileApi.get, staleTime: 5 * 60_000 })
