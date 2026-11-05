import type { ReactNode } from 'react'

interface Props {
  eyebrow?: string
  title: string
  children?: ReactNode
  /** Buttons for the page ("New course"). Right of the title on wide screens, under it (with a gap) on narrow ones. */
  actions?: ReactNode
}

export function PageHeader({ eyebrow, title, children, actions }: Props) {
  return (
    <header className="mb-8 flex flex-wrap items-end justify-between gap-x-6 gap-y-4">
      <div className="min-w-0">
        {eyebrow && <p className="text-xs font-semibold uppercase tracking-[0.14em] text-accent">{eyebrow}</p>}
        <h1 className="mt-1 font-display text-3xl font-semibold tracking-tight text-ink sm:text-[34px]">{title}</h1>
        {children && <div className="mt-2 text-muted">{children}</div>}
      </div>
      {actions && <div className="flex shrink-0 gap-2">{actions}</div>}
    </header>
  )
}
