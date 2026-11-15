import type { ReactNode } from 'react'
import { cn } from '@/shared/lib/cn'

type Tone = 'neutral' | 'brand' | 'success' | 'warning' | 'danger' | 'info' | 'outline'

const tones: Record<Tone, string> = {
  neutral: 'bg-canvas text-muted ring-1 ring-inset ring-line',
  brand: 'bg-brand text-white',
  success: 'bg-success-soft text-success ring-1 ring-inset ring-success/15',
  warning: 'bg-warning-soft text-warning ring-1 ring-inset ring-warning/15',
  danger: 'bg-danger-soft text-danger ring-1 ring-inset ring-danger/15',
  info: 'bg-info-soft text-info ring-1 ring-inset ring-info/15',
  /** Labels that are not a status (a role, a type): no fill, so they don't compete with status colours. */
  outline: 'text-ink ring-1 ring-inset ring-line-strong/50',
}

const dots: Partial<Record<Tone, string>> = {
  success: 'bg-success',
  warning: 'bg-warning',
  danger: 'bg-danger',
  info: 'bg-info',
}

/** Small status pill. Always carries text, so colour is never the only signal. `dot` adds a leading status dot. */
export function Badge({ tone = 'neutral', dot, children }: { tone?: Tone; dot?: boolean; children: ReactNode }) {
  return (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium whitespace-nowrap',
        tones[tone],
      )}
    >
      {dot && dots[tone] && <span aria-hidden className={cn('size-1.5 rounded-full', dots[tone])} />}
      {children}
    </span>
  )
}
