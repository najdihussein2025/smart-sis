import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router'
import { useAuth } from '@/shared/auth/AuthProvider'
import { homeFor, type Role } from '@/shared/auth/session'

/** UI guard only. The API enforces roles; this just keeps people on their own area. */
export function RequireRole({ role, children }: { role: Role; children: ReactNode }) {
  const { user } = useAuth()
  const location = useLocation()

  if (!user) return <Navigate to="/login" replace state={{ from: location.pathname }} />
  if (user.role !== role) return <Navigate to={homeFor[user.role]} replace />
  return <>{children}</>
}

export function HomeRedirect() {
  const { user } = useAuth()
  return <Navigate to={user ? homeFor[user.role] : '/login'} replace />
}
