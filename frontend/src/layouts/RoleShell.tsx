import { useRef, useState } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router'
import { LogOut, Menu, X, UserRound, type LucideIcon } from 'lucide-react'
import { NotificationBell } from '@/features/notifications/components/NotificationBell'
import { useAuth } from '@/shared/auth/AuthProvider'
import { initials } from '@/shared/lib/format'
import { cn } from '@/shared/lib/cn'
import { useFocusTrap } from '@/shared/lib/useFocusTrap'

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
  const drawer = useRef<HTMLDivElement>(null)
  const base = nav[0]?.to ?? '/'

  // Mobile menu: focus moves into it, Tab stays inside, Escape closes it and focus returns to the menu button.
  useFocusTrap(drawer, open, { onEscape: () => setOpen(false) })

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
              isActive ? 'bg-white/10 text-white font-medium' : 'text-white/75 hover:bg-white/5 hover:text-white',
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
        className={({ isActive }) =>
          cn(
            'flex items-center gap-3 rounded-lg px-3 py-2 text-sm transition-colors',
            isActive ? 'bg-white/10 text-white font-medium' : 'text-white/75 hover:bg-white/5 hover:text-white',
          )
        }
      >
        <UserRound className="size-[18px]" aria-hidden /> Profile
      </NavLink>
      <button
        type="button"
        onClick={logout}
        className="flex w-full items-center gap-3 rounded-lg px-3 py-2 text-sm text-white/75 hover:bg-white/5 hover:text-white"
      >
        <LogOut className="size-[18px]" aria-hidden /> Sign out
      </button>
    </div>
  )

  const sidebar = (closeButton?: boolean) => (
    <div className="flex h-full flex-col gap-6 bg-brand p-4 text-white">
      <div className="flex items-center gap-2.5 px-2 pt-2">
        <img src="/favicon.svg" alt="" className="size-8" />
        <div className="leading-tight">
          <p className="font-display text-[15px] font-semibold">Smart SIS</p>
          <p className="text-[11px] uppercase tracking-[0.14em] text-accent">{areaLabel}</p>
        </div>
        {closeButton && (
          <button
            type="button"
            aria-label="Close menu"
            onClick={() => setOpen(false)}
            className="ml-auto rounded-lg p-2 text-white/75 hover:bg-white/10 hover:text-white"
          >
            <X className="size-5" aria-hidden />
          </button>
        )}
      </div>
      <div className="flex-1 overflow-y-auto">{links}</div>
      {footer}
    </div>
  )

  return (
    <div className="min-h-dvh lg:grid lg:grid-cols-[248px_1fr]">
      <a
        href="#main"
        className="sr-only z-50 rounded-lg bg-surface px-4 py-2 text-sm font-medium text-ink shadow-lg focus:not-sr-only focus:fixed focus:top-3 focus:left-3"
      >
        Skip to main content
      </a>
      <aside className="sticky top-0 hidden h-dvh lg:block">{sidebar()}</aside>

      {open && (
        <div className="fixed inset-0 z-40 lg:hidden">
          <div aria-hidden className="absolute inset-0 bg-ink/40" onClick={() => setOpen(false)} />
          <div
            ref={drawer}
            id="mobile-menu"
            role="dialog"
            aria-modal="true"
            aria-label="Menu"
            className="absolute inset-y-0 left-0 w-[min(272px,85vw)]"
          >
            {sidebar(true)}
          </div>
        </div>
      )}

      <div className="flex min-w-0 flex-col">
        <header className="sticky top-0 z-30 flex h-16 items-center justify-between border-b border-line bg-canvas/85 px-4 backdrop-blur sm:px-8">
          <div className="flex items-center gap-2 lg:hidden">
            <button
              type="button"
              className="rounded-lg p-2 text-ink hover:bg-brand-50"
              aria-label="Open menu"
              aria-expanded={open}
              aria-controls="mobile-menu"
              onClick={() => setOpen(true)}
            >
              <Menu className="size-5" aria-hidden />
            </button>
            <span className="font-display text-[15px] font-semibold text-ink">Smart SIS</span>
          </div>
          <div className="hidden lg:block" />
          <div className="flex items-center gap-3">
            <NotificationBell />
            {user && (
              <div className="flex items-center gap-2.5">
                <span aria-hidden className="grid size-9 shrink-0 place-items-center rounded-full bg-brand text-[13px] font-medium text-white">
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
        <main id="main" tabIndex={-1} className="mx-auto w-full min-w-0 max-w-6xl flex-1 px-4 py-8 focus:outline-none sm:px-8 sm:py-10">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
