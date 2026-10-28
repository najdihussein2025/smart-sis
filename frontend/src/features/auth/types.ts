import type { Session } from '@/shared/auth/session'

// TODO: replace with generated types from `npm run gen:api` once the API is running.
export interface LoginRequest {
  universityId: number
  password: string
}

export type LoginResponse = Session

export interface ResetPasswordRequest {
  token: string
  newPassword: string
}
