import { api } from '@/shared/api/client'
import type { Schedule } from './types'

export const scheduleApi = {
  my: () => api.get<Schedule>('/api/schedule/my').then((r) => r.data),
  teaching: () => api.get<Schedule>('/api/schedule/teaching').then((r) => r.data),
}
