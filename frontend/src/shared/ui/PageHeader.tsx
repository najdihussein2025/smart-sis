import { useEffect, type ReactNode } from 'react'
import type { LucideIcon } from 'lucide-react'

interface Props {
  /**
   * Small label above the title. Convention: the term name ("Fall 2026") on term-scoped pages, otherwise the nav group
   * the page sits in ("Academics", "Money", "Help", "Administration"). Never the role name.
   */
  eyebrow?: string
  title: string
  /** Icon tile left of the title, the same icon as the page's sidebar item. */
  icon?: LucideIcon
  children?: ReactNode
  /** Buttons for the page ("New course"). Right of the title on wide screens, under it (with a gap) on narrow ones. */
  actions?: ReactNode
  /** Browser tab title when the heading isn't a good one ("Good morning, Demo" → "Dashboard"). Defaults to `title`. */
  documentTitle?: string
}

/** Sets the browser tab title ("Grades · Smart SIS"), so each route has its own title (WCAG 2.4.2). */
export function useDocumentTitle(title: string) {
  useEffect(() => {
    const previous = document.title
    document.title = title ? `${title} · Smart SIS` : 'Smart SIS'
    return () => {
      document.title = previous
    }
  }, [title])
}

export function PageHeader({ eyebrow, title, icon: Icon, children, actions, documentTitle }: Props) {
  useDocumentTitle(documentTitle ?? title)
  return (
    <header className="mb-6 flex flex-wrap items-end justify-between gap-x-6 gap-y-4 sm:mb-8">
      <div className="flex min-w-0 items-start gap-4">
        {Icon && (
          <span className="mt-1 hidden size-12 shrink-0 place-items-center rounded-2xl bg-brand text-accent shadow-sm sm:grid">
            <Icon className="size-6" aria-hidden />
          </span>
        )}
        <div className="min-w-0">
          {eyebrow && <p className="text-xs font-semibold uppercase tracking-[0.14em] text-accent-ink">{eyebrow}</p>}
          <h1 className="mt-1 font-display text-[28px] leading-tight font-semibold tracking-tight break-words text-ink sm:text-[34px]">
            {title}
          </h1>
          {children && <div className="mt-2 max-w-prose text-muted">{children}</div>}
        </div>
      </div>
      {actions && <div className="flex min-w-0 flex-wrap gap-2">{actions}</div>}
    </header>
  )
}
