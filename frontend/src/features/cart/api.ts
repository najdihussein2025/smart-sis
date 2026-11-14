import { api } from '@/shared/api/client'
import type { Cart } from './types'

export const cartApi = {
  get: () => api.get<Cart>('/api/cart').then((r) => r.data),
  add: (sectionId: number) => api.post<Cart>('/api/cart', { sectionId }).then((r) => r.data),
  remove: (sectionId: number) => api.delete(`/api/cart/${sectionId}`).then(() => undefined),
}
