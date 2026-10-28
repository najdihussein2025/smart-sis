import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { onUnauthorized } from '@/shared/api/client'
import { sessionStore, type Session, type SessionUser } from './session'

interface AuthContextValue {
  user: SessionUser | null
  signIn: (session: Session) => void
  signOut: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const [session, setSession] = useState<Session | null>(() => sessionStore.get())

  const signIn = useCallback((s: Session) => {
    sessionStore.set(s)
    setSession(s)
  }, [])

  const signOut = useCallback(() => {
    sessionStore.clear()
    setSession(null)
    queryClient.clear()
  }, [queryClient])

  useEffect(() => onUnauthorized(signOut), [signOut])

  const value = useMemo(() => ({ user: session?.user ?? null, signIn, signOut }), [session, signIn, signOut])
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside <AuthProvider>')
  return ctx
}
