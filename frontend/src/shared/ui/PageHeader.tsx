import { useEffect, type ReactNode } from 'react'

interface Props {
  eyebrow?: string
  title: string
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

export function PageHeader({ eyebrow, title, children, actions, documentTitle }: Props) {
  useDocumentTitle(documentTitle ?? title)
  return (
    <header className="mb-8 flex flex-wrap items-end justify-between gap-x-6 gap-y-4">
      <div className="min-w-0">
        {eyebrow && <p className="text-xs font-semibold uppercase tracking-[0.14em] text-accent-ink">{eyebrow}</p>}
        <h1 className="mt-1 font-display text-3xl font-semibold tracking-tight break-words text-ink sm:text-[34px]">{title}</h1>
        {children && <div className="mt-2 text-muted">{children}</div>}
      </div>
      {actions && <div className="flex min-w-0 flex-wrap gap-2">{actions}</div>}
    </header>
  )
}
