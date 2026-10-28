import { api } from '@/shared/api/client'
import type { UserProfile } from './types'

export const profileApi = {
  get: () => api.get<UserProfile>('/api/profile').then((r) => r.data),
}
