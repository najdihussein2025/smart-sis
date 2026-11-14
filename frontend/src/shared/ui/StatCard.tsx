import type { LucideIcon } from 'lucide-react'
import { ChevronRight } from 'lucide-react'
import { Link } from 'react-router'
import { cn } from '@/shared/lib/cn'
import { Card } from './Card'

type Tone = 'default' | 'warning' | 'danger'

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
  warning: 'bg-warning-soft text-warning',
  danger: 'bg-danger-soft text-danger',
}

const hintTones: Record<Tone, string> = {
  default: 'text-muted',
  warning: 'text-warning',
  danger: 'text-danger',
}

/** A number on a dashboard or summary row. With `to`, the card is one link named by its label, value and hint. */
export function StatCard({ label, value, hint, icon: Icon, to, tone = 'default' }: Props) {
  const body = (
    <>
      <div className="flex items-start justify-between gap-3">
        <p className="text-sm text-muted">{label}</p>
        <span className={cn('grid size-9 shrink-0 place-items-center rounded-lg', iconTones[tone])}>
          <Icon className="size-[18px]" aria-hidden />
        </span>
      </div>
      <p className="mt-3 font-display text-3xl font-semibold tracking-tight break-words text-ink">{value}</p>
      {hint && <p className={cn('mt-1 text-[13px]', hintTones[tone])}>{hint}</p>}
    </>
  )

  if (!to) return <Card className="p-5">{body}</Card>

  return (
    <Card className="group relative p-5 pr-9 transition-shadow focus-within:ring-2 focus-within:ring-accent hover:shadow-md">
      {body}
      <Link
        to={to}
        className="absolute inset-0 rounded-[var(--radius-card)] focus:outline-none"
        aria-label={`${label}: ${value}${hint ? `. ${hint}` : ''}`}
      >
        <ChevronRight
          className="absolute right-3 bottom-4 size-4 text-muted transition-transform group-hover:translate-x-0.5"
          aria-hidden
        />
      </Link>
    </Card>
  )
}

/** Placeholder card while dashboard numbers load. */
export function StatCardSkeleton() {
  return (
    <Card className="p-5">
      <div className="h-4 w-28 animate-pulse rounded bg-canvas" />
      <div className="mt-4 h-8 w-20 animate-pulse rounded bg-canvas" />
      <div className="mt-2 h-3 w-32 animate-pulse rounded bg-canvas" />
    </Card>
  )
}
