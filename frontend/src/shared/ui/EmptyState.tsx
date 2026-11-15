import type { ReactNode } from 'react'
import type { LucideIcon } from 'lucide-react'

/**
 * Nothing to show yet. The sentence says what to do, and `action` holds the button that does it, so the empty state
 * never names a button the user then has to look for elsewhere.
 */
export function EmptyState({ icon: Icon, title, children, action }: {
  icon: LucideIcon
  title: string
  children?: ReactNode
  action?: ReactNode
}) {
  return (
    <div className="flex flex-col items-center px-6 py-12 text-center sm:py-14">
      <span className="grid size-14 place-items-center rounded-2xl bg-accent-soft text-accent-ink ring-8 ring-accent-soft/40">
        <Icon className="size-6" aria-hidden />
      </span>
      <h3 className="mt-5 font-display text-lg font-semibold text-ink">{title}</h3>
      {children && <div className="mt-1.5 max-w-sm text-sm text-muted">{children}</div>}
      {action && <div className="mt-5 flex flex-wrap justify-center gap-2">{action}</div>}
    </div>
  )
}
