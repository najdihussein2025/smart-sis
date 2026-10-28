import { api } from '@/shared/api/client'
import type { LoginRequest, LoginResponse, ResetPasswordRequest } from './types'

export const authApi = {
  login: (body: LoginRequest) => api.post<LoginResponse>('/api/auth/login', body).then((r) => r.data),
  forgotPassword: (universityId: number) => api.post('/api/auth/forgot-password', { universityId }),
  resetPassword: (body: ResetPasswordRequest) => api.post('/api/auth/reset-password', body),
}
