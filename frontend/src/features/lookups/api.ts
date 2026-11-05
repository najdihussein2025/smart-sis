import { api } from '@/shared/api/client'
import type { Lookups } from './types'

export const lookupsApi = {
  get: () => api.get<Lookups>('/api/admin/lookups').then((r) => r.data),
}
