import type { ReactNode } from 'react'
import type { LucideIcon } from 'lucide-react'

export function EmptyState({ icon: Icon, title, children }: { icon: LucideIcon; title: string; children?: ReactNode }) {
  return (
    <div className="flex flex-col items-center px-6 py-14 text-center">
      <span className="grid size-12 place-items-center rounded-full bg-accent-soft text-accent">
        <Icon className="size-6" aria-hidden />
      </span>
      <h3 className="mt-4 font-display text-lg font-semibold text-ink">{title}</h3>
      {children && <div className="mt-1.5 max-w-sm text-sm text-muted">{children}</div>}
    </div>
  )
}
