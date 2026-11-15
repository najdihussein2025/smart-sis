import { useId, type ReactNode } from 'react'
import type { UseQueryResult } from '@tanstack/react-query'
import { AlertCircle, type LucideIcon } from 'lucide-react'
import { cn } from '@/shared/lib/cn'
import type { AttentionItem } from '@/shared/ui/AttentionList'
import { AttentionList } from '@/shared/ui/AttentionList'
import { Card } from '@/shared/ui/Card'
import { CardHeader } from '@/shared/ui/CardHeader'
import { QueryError } from '@/shared/ui/QueryError'
import { StatCardGrid, StatCardSkeleton } from '@/shared/ui/StatCard'

export const plural = (n: number, word: string, many = `${word}s`) => `${n} ${n === 1 ? word : many}`

/** Link styled as a button. On the white cards. 44 px tall on touch screens, compact from `sm`. */
export const linkButton = {
  secondary:
    'inline-flex h-11 shrink-0 items-center justify-center gap-1.5 rounded-lg border border-line-strong/60 bg-surface px-3 text-sm font-medium text-ink shadow-sm transition-colors hover:border-line-strong hover:bg-canvas sm:h-9',
  ghost:
    'inline-flex h-11 shrink-0 items-center justify-center gap-1.5 rounded-lg px-3 text-sm font-medium text-muted transition-colors hover:bg-brand-50 hover:text-ink sm:h-9',
  /** On the navy hero card. */
  onBrand:
    'inline-flex h-11 shrink-0 items-center justify-center gap-1.5 rounded-lg bg-accent px-3.5 text-sm font-semibold text-brand transition hover:brightness-105 sm:h-9',
  onBrandGhost:
    'inline-flex h-11 shrink-0 items-center justify-center gap-1.5 rounded-lg px-3 text-sm font-medium text-white/85 ring-1 ring-white/25 transition-colors hover:bg-white/10 hover:text-white sm:h-9',
} as const

/**
 * Dashboard layout. Phones and tablets: the "next up" card, then what needs attention, then the numbers. Wide screens:
 * the card and the numbers on the left, the attention list in a column on the right.
 */
export function DashboardColumns({ main, attention, stats }: { main: ReactNode; attention: ReactNode; stats: ReactNode }) {
  return (
    <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_340px] xl:grid-rows-[auto_1fr] xl:items-start">
      <div className="min-w-0 xl:col-start-1 xl:row-start-1">{main}</div>
      <div className="min-w-0 xl:col-start-2 xl:row-span-2 xl:row-start-1">{attention}</div>
      <section aria-labelledby="dashboard-stats" className="min-w-0 xl:col-start-1 xl:row-start-2">
        <h2 id="dashboard-stats" className="mb-3 text-xs font-semibold tracking-[0.14em] text-muted uppercase">
          At a glance
        </h2>
        {stats}
      </section>
    </div>
  )
}

/** Loading → skeleton cards, error → message with retry, then the cards. */
export function StatGrid<T>({ query, cards, columns, children }: {
  query: UseQueryResult<T>
  cards: number
  columns: string
  children: (data: T) => ReactNode
}) {
  if (query.isPending) {
    return (
      <StatCardGrid columns={columns} role="status" aria-busy aria-label="Loading your numbers">
        {Array.from({ length: cards }, (_, i) => <StatCardSkeleton key={i} />)}
      </StatCardGrid>
    )
  }
  if (query.isError) {
    return (
      <Card>
        <QueryError error={query.error} onRetry={() => void query.refetch()} />
      </Card>
    )
  }
  return <StatCardGrid columns={columns}>{children(query.data)}</StatCardGrid>
}

/** A row saying a check couldn't run, so a failed request never reads as "all clear". */
export function couldNotCheck(id: string, what: string, to: string): AttentionItem {
  return { id: `failed-${id}`, icon: AlertCircle, tone: 'info', title: `Couldn't check ${what}`, detail: 'Open the page to see it', to }
}

/** The "Needs attention" card, with skeleton rows until every source has answered (no false "all clear"). */
export function AttentionPanel({ loading, items, emptyText }: { loading: boolean; items: AttentionItem[]; emptyText: string }) {
  if (loading) {
    return (
      <Card role="status" aria-busy="true" aria-label="Checking what needs attention">
        <CardHeader title="Needs attention" />
        <ul className="divide-y divide-line">
          {[0, 1, 2].map((i) => (
            <li key={i} className="flex min-h-16 items-center gap-3 px-4 py-3 sm:px-5">
              <span className="size-9 shrink-0 animate-pulse rounded-lg bg-canvas" />
              <span className="flex-1 space-y-2">
                <span className="block h-3.5 w-3/5 animate-pulse rounded bg-canvas" />
                <span className="block h-3 w-2/5 animate-pulse rounded bg-canvas" />
              </span>
            </li>
          ))}
        </ul>
      </Card>
    )
  }
  return <AttentionList items={items} emptyText={emptyText} />
}

/** The navy "next up" card at the top of the student dashboard. */
export function HeroCard({ eyebrow, title, icon: Icon, children, actions }: {
  eyebrow: string
  title: string
  icon: LucideIcon
  children?: ReactNode
  actions?: ReactNode
}) {
  const id = useId()
  return (
    <section
      aria-labelledby={id}
      className="relative overflow-hidden rounded-[var(--radius-card)] bg-brand p-5 text-white shadow-[var(--shadow-card)] sm:p-6"
    >
      <span aria-hidden className="absolute inset-y-0 left-0 w-1 bg-accent" />
      <div className="flex items-start gap-4">
        <span className="hidden size-11 shrink-0 place-items-center rounded-xl bg-white/10 text-accent sm:grid">
          <Icon className="size-5" aria-hidden />
        </span>
        <div className="min-w-0 flex-1">
          <p className="text-xs font-semibold tracking-[0.14em] text-accent uppercase">{eyebrow}</p>
          <h2 id={id} className="mt-1.5 font-display text-2xl leading-tight font-semibold tracking-tight break-words sm:text-[28px]">
            {title}
          </h2>
          {children && <div className="mt-3 text-sm text-white/80">{children}</div>}
          {actions && <div className="mt-5 flex flex-wrap gap-2">{actions}</div>}
        </div>
      </div>
    </section>
  )
}

export function HeroSkeleton({ label }: { label: string }) {
  return (
    <div role="status" aria-busy="true" aria-label={label} className="rounded-[var(--radius-card)] bg-brand p-5 sm:p-6">
      <div className="h-3 w-24 animate-pulse rounded bg-white/15" />
      <div className="mt-3 h-7 w-3/5 animate-pulse rounded bg-white/15" />
      <div className="mt-4 h-4 w-2/5 animate-pulse rounded bg-white/10" />
    </div>
  )
}

/** One fact on a card ("08:00–09:15", "B-101") with its icon. */
export function Fact({ icon: Icon, children, className }: { icon: LucideIcon; children: ReactNode; className?: string }) {
  return (
    <span className={cn('inline-flex items-center gap-1.5', className)}>
      <Icon className="size-4 shrink-0 opacity-70" aria-hidden />
      {children}
    </span>
  )
}
