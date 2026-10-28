import axios, { AxiosError } from 'axios'
import { sessionStore } from '@/shared/auth/session'

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '',
  headers: { 'Content-Type': 'application/json' },
})

api.interceptors.request.use((config) => {
  const session = sessionStore.get()
  if (session) config.headers.Authorization = `Bearer ${session.accessToken}`
  return config
})

/** Listeners are told when the API rejects our token, so the app can sign out. */
const unauthorizedListeners = new Set<() => void>()
export function onUnauthorized(listener: () => void) {
  unauthorizedListeners.add(listener)
  return () => {
    unauthorizedListeners.delete(listener)
  }
}

api.interceptors.response.use(
  (r) => r,
  (error: AxiosError) => {
    const isLogin = error.config?.url?.includes('/api/auth/login')
    if (error.response?.status === 401 && !isLogin) {
      sessionStore.clear()
      unauthorizedListeners.forEach((l) => l())
    }
    return Promise.reject(error)
  },
)
