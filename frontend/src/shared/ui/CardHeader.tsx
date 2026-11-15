import type { ReactNode } from 'react'
import type { LucideIcon } from 'lucide-react'

/**
 * The top row of a Card: title (h2 by default), optional one-line description, actions on the right. One header style
 * for every card, so lists, forms and sheets look like the same product.
 */
export function CardHeader({ title, description, icon: Icon, actions, as: Heading = 'h2' }: {
  title: ReactNode
  description?: ReactNode
  icon?: LucideIcon
  actions?: ReactNode
  as?: 'h2' | 'h3'
}) {
  return (
    <div className="flex flex-wrap items-center justify-between gap-3 border-b border-line px-4 py-4 sm:px-5">
      <div className="flex min-w-0 items-center gap-3">
        {Icon && (
          <span className="grid size-9 shrink-0 place-items-center rounded-lg bg-brand-50 text-brand">
            <Icon className="size-[18px]" aria-hidden />
          </span>
        )}
        <div className="min-w-0">
          <Heading className="font-display text-lg leading-snug font-semibold text-ink">{title}</Heading>
          {description && <p className="text-sm text-muted">{description}</p>}
        </div>
      </div>
      {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
    </div>
  )
}
