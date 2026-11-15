import type { ReactNode } from 'react'
import type { LucideIcon } from 'lucide-react'
import { ArrowUpRight } from 'lucide-react'
import { Link } from 'react-router'
import { cn } from '@/shared/lib/cn'
import { Card } from './Card'

type Tone = 'default' | 'success' | 'warning' | 'danger'

interface Props {
  label: string
  value: string
  hint?: string
  icon: LucideIcon
  /** Makes the whole card a link to the page behind the number ("Classes today" → schedule). */
  to?: string
  /**
   * Draws attention to the number (overdue balance, critical absences). The hint must say why in words, so colour is
   * never the only signal.
   */
  tone?: Tone
}

const iconTones: Record<Tone, string> = {
  default: 'bg-brand-50 text-brand',
  success: 'bg-success-soft text-success',
  warning: 'bg-warning-soft text-warning',
  danger: 'bg-danger-soft text-danger',
}

const hintTones: Record<Tone, string> = {
  default: 'text-muted',
  success: 'text-success',
  warning: 'text-warning',
  danger: 'text-danger',
}

/** Coloured rule along the top edge, so a warning card stands out in a grid of neutral ones. */
const edgeTones: Record<Tone, string> = {
  default: 'bg-transparent',
  success: 'bg-success',
  warning: 'bg-warning',
  danger: 'bg-danger',
}

/**
 * A number on a dashboard or summary row. Compact on phones (two per row), roomier from `sm`. With `to`, the card is
 * one link named by its label, value and hint.
 */
export function StatCard({ label, value, hint, icon: Icon, to, tone = 'default' }: Props) {
  const body = (
    <>
      <span aria-hidden className={cn('absolute inset-x-0 top-0 h-1 rounded-t-[var(--radius-card)]', edgeTones[tone])} />
      <div className="flex items-start justify-between gap-2">
        <p className="text-[13px] leading-snug text-muted sm:text-sm">{label}</p>
        <span className={cn('grid size-8 shrink-0 place-items-center rounded-lg sm:size-9', iconTones[tone])}>
          <Icon className="size-4 sm:size-[18px]" aria-hidden />
        </span>
      </div>
      <p className="tabular mt-2 font-display text-2xl font-semibold tracking-tight break-words text-ink sm:mt-3 sm:text-3xl">
        {value}
      </p>
      {hint && <p className={cn('mt-1 text-xs leading-snug sm:text-[13px]', hintTones[tone])}>{hint}</p>}
    </>
  )

  if (!to) return <Card className="relative overflow-hidden p-4 sm:p-5">{body}</Card>

  return (
    <Card className="group relative overflow-hidden p-4 transition-shadow focus-within:ring-2 focus-within:ring-focus hover:shadow-[var(--shadow-card-hover)] sm:p-5">
      {body}
      <Link
        to={to}
        className="absolute inset-0 rounded-[var(--radius-card)] focus:outline-none"
        aria-label={`${label}: ${value}${hint ? `. ${hint}` : ''}`}
      >
        <ArrowUpRight
          className="absolute right-3 bottom-3 size-4 text-muted opacity-0 transition group-hover:opacity-100 sm:opacity-60"
          aria-hidden
        />
      </Link>
    </Card>
  )
}

/** Grid for stat cards: two per row on phones, `columns` (e.g. "lg:grid-cols-3") from there. */
export function StatCardGrid({ columns = 'lg:grid-cols-3', children, ...rest }: {
  columns?: string
  children: ReactNode
  role?: string
  'aria-busy'?: boolean
  'aria-label'?: string
}) {
  return (
    <div
      className={cn(
        // An odd last card fills the row on phones instead of leaving a hole next to it.
        'grid grid-cols-2 gap-3 sm:gap-4 max-sm:[&>*:last-child:nth-child(odd)]:col-span-2',
        columns,
      )}
      {...rest}
    >
      {children}
    </div>
  )
}

/** Placeholder card while dashboard numbers load. */
export function StatCardSkeleton() {
  return (
    <Card className="p-4 sm:p-5">
      <div className="h-4 w-24 animate-pulse rounded bg-canvas" />
      <div className="mt-4 h-8 w-16 animate-pulse rounded bg-canvas" />
      <div className="mt-2 h-3 w-28 animate-pulse rounded bg-canvas" />
    </Card>
  )
}
