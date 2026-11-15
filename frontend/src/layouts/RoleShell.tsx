import { useId, useRef, useState } from 'react'
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

/** A labelled block of sidebar links ("Academics", "Money"). The first group usually has no label (Dashboard). */
export interface NavGroup {
  label?: string
  items: NavItem[]
}

const linkClass = ({ isActive }: { isActive: boolean }) =>
  cn(
    'relative flex min-h-11 items-center gap-3 rounded-lg px-3 text-sm transition-colors lg:min-h-9',
    isActive
      ? 'bg-[var(--color-nav-active)] font-medium text-white before:absolute before:inset-y-2 before:-left-4 before:w-1 before:rounded-r-full before:bg-accent'
      : 'text-white/75 hover:bg-[var(--color-nav-hover)] hover:text-white',
  )

function Group({ group, onNavigate }: { group: NavGroup; onNavigate: () => void }) {
  const headingId = useId()
  return (
    <div>
      {group.label && (
        <p id={headingId} className="px-3 pt-4 pb-1 text-xs font-semibold tracking-[0.12em] text-white/50 uppercase">
          {group.label}
        </p>
      )}
      <ul className="space-y-0.5" aria-labelledby={group.label ? headingId : undefined}>
        {group.items.map(({ to, label, icon: Icon, end }) => (
          <li key={to}>
            <NavLink to={to} end={end} onClick={onNavigate} className={linkClass}>
              <Icon className="size-[18px] shrink-0" aria-hidden />
              {label}
            </NavLink>
          </li>
        ))}
      </ul>
    </div>
  )
}

/** Shared frame for the three role layouts: sidebar on desktop, top bar + drawer on mobile. */
export function RoleShell({ areaLabel, nav }: { areaLabel: string; nav: NavGroup[] }) {
  const { user, signOut } = useAuth()
  const navigate = useNavigate()
  const [open, setOpen] = useState(false)
  const drawer = useRef<HTMLDivElement>(null)
  const base = nav[0]?.items[0]?.to ?? '/'
  const close = () => setOpen(false)

  // Mobile menu: focus moves into it, Tab stays inside, Escape closes it and focus returns to the menu button.
  useFocusTrap(drawer, open, { onEscape: close })

  const logout = () => {
    signOut()
    navigate('/login', { replace: true })
  }

  const sidebar = (closeButton?: boolean) => (
    <div className="flex h-full flex-col bg-gradient-to-b from-brand to-brand-700 text-white">
      <div className="flex items-center gap-3 px-5 pt-5 pb-3">
        <img src="/favicon.svg" alt="" className="size-9" />
        <div className="leading-tight">
          <p className="font-display text-base font-semibold">Smart SIS</p>
          <p className="text-xs font-medium tracking-[0.14em] text-accent uppercase">{areaLabel}</p>
        </div>
        {closeButton && (
          <button
            type="button"
            aria-label="Close menu"
            onClick={close}
            className="-mr-2 ml-auto grid size-11 cursor-pointer place-items-center rounded-lg text-white/75 hover:bg-white/10 hover:text-white"
          >
            <X className="size-5" aria-hidden />
          </button>
        )}
      </div>

      <nav aria-label={`${areaLabel} navigation`} className="flex-1 overflow-y-auto px-4 pb-4 [scrollbar-width:thin]">
        {nav.map((group, i) => (
          <Group key={group.label ?? i} group={group} onNavigate={close} />
        ))}
      </nav>

      <div className="space-y-0.5 border-t border-white/10 px-4 py-3">
        <NavLink to={`${base}/profile`} onClick={close} className={linkClass}>
          <UserRound className="size-[18px]" aria-hidden /> Profile
        </NavLink>
        <button
          type="button"
          onClick={logout}
          className="flex min-h-11 w-full cursor-pointer items-center gap-3 rounded-lg px-3 text-sm text-white/75 transition-colors hover:bg-[var(--color-nav-hover)] hover:text-white lg:min-h-9"
        >
          <LogOut className="size-[18px]" aria-hidden /> Sign out
        </button>
      </div>
    </div>
  )

  return (
    <div className="min-h-dvh lg:grid lg:grid-cols-[264px_1fr]">
      <a
        href="#main"
        className="sr-only z-50 rounded-lg bg-surface px-4 py-2 text-sm font-medium text-ink shadow-lg focus:not-sr-only focus:fixed focus:top-3 focus:left-3"
      >
        Skip to main content
      </a>
      <aside className="sticky top-0 hidden h-dvh lg:block">{sidebar()}</aside>

      {open && (
        <div className="fixed inset-0 z-40 lg:hidden">
          <div aria-hidden className="absolute inset-0 bg-ink/50 backdrop-blur-[2px]" onClick={close} />
          <div
            ref={drawer}
            id="mobile-menu"
            role="dialog"
            aria-modal="true"
            aria-label="Menu"
            className="absolute inset-y-0 left-0 w-[min(288px,85vw)] shadow-2xl"
          >
            {sidebar(true)}
          </div>
        </div>
      )}

      <div className="flex min-w-0 flex-col">
        <header className="sticky top-0 z-30 flex h-16 items-center justify-between border-b border-line bg-canvas/85 px-4 backdrop-blur sm:px-8">
          <div className="flex items-center gap-1 lg:hidden">
            <button
              type="button"
              className="-ml-2 grid size-11 cursor-pointer place-items-center rounded-lg text-ink hover:bg-brand-50"
              aria-label="Open menu"
              aria-expanded={open}
              aria-controls="mobile-menu"
              onClick={() => setOpen(true)}
            >
              <Menu className="size-5" aria-hidden />
            </button>
            <span className="font-display text-base font-semibold text-ink">Smart SIS</span>
          </div>
          <div className="hidden lg:block" />
          <div className="flex items-center gap-2 sm:gap-3">
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
        <main id="main" tabIndex={-1} className="mx-auto w-full max-w-6xl min-w-0 flex-1 px-4 py-6 focus:outline-none sm:px-8 sm:py-10">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
