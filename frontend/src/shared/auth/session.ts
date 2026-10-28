export type Role = 'Student' | 'Instructor' | 'Admin'

export interface SessionUser {
  id: number
  universityId: number
  fullName: string
  role: Role
  photoUrl?: string | null
}

export interface Session {
  accessToken: string
  expiresAt: string
  user: SessionUser
}

const KEY = 'smartsis.session'

/** The token lives in localStorage so a refresh keeps you signed in. Cleared on logout and on any 401. */
export const sessionStore = {
  get(): Session | null {
    try {
      const raw = localStorage.getItem(KEY)
      if (!raw) return null
      const s = JSON.parse(raw) as Session
      if (new Date(s.expiresAt).getTime() <= Date.now()) {
        localStorage.removeItem(KEY)
        return null
      }
      return s
    } catch {
      return null
    }
  },
  set(session: Session) {
    try {
      localStorage.setItem(KEY, JSON.stringify(session))
    } catch {
      /* storage blocked: session lasts until reload */
    }
  },
  clear() {
    try {
      localStorage.removeItem(KEY)
    } catch {
      /* ignore */
    }
  },
}

export const homeFor: Record<Role, string> = {
  Student: '/student',
  Instructor: '/instructor',
  Admin: '/admin',
}
