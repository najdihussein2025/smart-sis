import { useState } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router'
import { LogOut, Menu, X, UserRound, Bell, type LucideIcon } from 'lucide-react'
import { useAuth } from '@/shared/auth/AuthProvider'
import { initials } from '@/shared/lib/format'
import { cn } from '@/shared/lib/cn'

export interface NavItem {
  to: string
  label: string
  icon: LucideIcon
  end?: boolean
}

/** Shared frame for the three role layouts: sidebar on desktop, top bar + drawer on mobile. */
export function RoleShell({ areaLabel, nav }: { areaLabel: string; nav: NavItem[] }) {
  const { user, signOut } = useAuth()
  const navigate = useNavigate()
  const [open, setOpen] = useState(false)
  const base = nav[0]?.to ?? '/'

  const logout = () => {
    signOut()
    navigate('/login', { replace: true })
  }

  const links = (
    <nav className="space-y-0.5" aria-label={`${areaLabel} navigation`}>
      {nav.map(({ to, label, icon: Icon, end }) => (
        <NavLink
          key={to}
          to={to}
          end={end}
          onClick={() => setOpen(false)}
          className={({ isActive }) =>
            cn(
              'flex items-center gap-3 rounded-lg px-3 py-2 text-sm transition-colors',
              isActive ? 'bg-white/10 text-white font-medium' : 'text-white/65 hover:bg-white/5 hover:text-white',
            )
          }
        >
          <Icon className="size-[18px]" aria-hidden />
          {label}
        </NavLink>
      ))}
    </nav>
  )

  const footer = (
    <div className="space-y-0.5 border-t border-white/10 pt-4">
      <NavLink
        to={`${base}/profile`}
        onClick={() => setOpen(false)}
        className="flex items-center gap-3 rounded-lg px-3 py-2 text-sm text-white/65 hover:bg-white/5 hover:text-white"
      >
        <UserRound className="size-[18px]" aria-hidden /> Profile
      </NavLink>
      <button
        onClick={logout}
        className="flex w-full items-center gap-3 rounded-lg px-3 py-2 text-sm text-white/65 hover:bg-white/5 hover:text-white"
      >
        <LogOut className="size-[18px]" aria-hidden /> Sign out
      </button>
    </div>
  )

  const sidebar = (
    <div className="flex h-full flex-col gap-6 bg-brand p-4 text-white">
      <div className="flex items-center gap-2.5 px-2 pt-2">
        <img src="/favicon.svg" alt="" className="size-8" />
        <div className="leading-tight">
          <p className="font-display text-[15px] font-semibold">Smart SIS</p>
          <p className="text-[11px] uppercase tracking-[0.14em] text-accent">{areaLabel}</p>
        </div>
      </div>
      <div className="flex-1 overflow-y-auto">{links}</div>
      {footer}
    </div>
  )

  return (
    <div className="min-h-dvh lg:grid lg:grid-cols-[248px_1fr]">
      <aside className="sticky top-0 hidden h-dvh lg:block">{sidebar}</aside>

      {open && (
        <div className="fixed inset-0 z-40 lg:hidden">
          <button aria-label="Close menu" className="absolute inset-0 bg-ink/40" onClick={() => setOpen(false)} />
          <div className="absolute inset-y-0 left-0 w-[272px]">{sidebar}</div>
        </div>
      )}

      <div className="flex min-w-0 flex-col">
        <header className="sticky top-0 z-30 flex h-16 items-center justify-between border-b border-line bg-canvas/85 px-4 backdrop-blur sm:px-8">
          <button className="rounded-lg p-2 text-ink lg:hidden" aria-label="Open menu" onClick={() => setOpen(true)}>
            {open ? <X className="size-5" /> : <Menu className="size-5" />}
          </button>
          <div className="hidden lg:block" />
          <div className="flex items-center gap-3">
            {/* TODO(phase-6): notifications dropdown with unread count */}
            <button className="rounded-lg p-2 text-muted hover:bg-brand-50 hover:text-ink" aria-label="Notifications">
              <Bell className="size-5" />
            </button>
            {user && (
              <div className="flex items-center gap-2.5">
                <span className="grid size-9 place-items-center rounded-full bg-brand text-[13px] font-medium text-white">
                  {initials(user.fullName)}
                </span>
                <div className="hidden leading-tight sm:block">
                  <p className="text-sm font-medium">{user.fullName}</p>
                  <p className="text-xs text-muted">ID {user.universityId}</p>
                </div>
              </div>
            )}
          </div>
        </header>
        <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-8 sm:px-8 sm:py-10">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
