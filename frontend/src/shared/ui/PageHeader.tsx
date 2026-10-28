import type { ReactNode } from 'react'

export function PageHeader({ eyebrow, title, children }: { eyebrow?: string; title: string; children?: ReactNode }) {
  return (
    <header className="mb-8">
      {eyebrow && <p className="text-xs font-semibold uppercase tracking-[0.14em] text-accent">{eyebrow}</p>}
      <h1 className="mt-1 font-display text-3xl font-semibold tracking-tight text-ink sm:text-[34px]">{title}</h1>
      {children && <div className="mt-2 text-muted">{children}</div>}
    </header>
  )
}
