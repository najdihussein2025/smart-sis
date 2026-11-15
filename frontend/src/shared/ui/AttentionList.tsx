import type { LucideIcon } from 'lucide-react'
import { ChevronRight, CheckCircle2 } from 'lucide-react'
import { Link } from 'react-router'
import { cn } from '@/shared/lib/cn'
import { Card } from './Card'
import { CardHeader } from './CardHeader'

export interface AttentionItem {
  id: string
  icon: LucideIcon
  title: string
  detail?: string
  /** danger = blocks something now, warning = soon, info = worth a look. Spoken as a word too, never colour only. */
  tone: 'danger' | 'warning' | 'info'
  to: string
  /** Link text for screen readers when the title alone isn't a good link name. */
  actionLabel?: string
}

const tones = {
  danger: { tile: 'bg-danger-soft text-danger', word: 'Action needed' },
  warning: { tile: 'bg-warning-soft text-warning', word: 'Soon' },
  info: { tile: 'bg-info-soft text-info', word: 'Note' },
} as const

/** "Needs attention" card on a dashboard: each row is one link to the page that fixes it. */
export function AttentionList({ title = 'Needs attention', items, emptyText }: {
  title?: string
  items: AttentionItem[]
  /** Shown with a green check when nothing needs doing. */
  emptyText: string
}) {
  return (
    <Card>
      <CardHeader title={title} />
      {items.length === 0 ? (
        <p className="flex items-center gap-2.5 px-4 py-5 text-sm text-success sm:px-5">
          <CheckCircle2 className="size-5 shrink-0" aria-hidden />
          {emptyText}
        </p>
      ) : (
        <ul className="divide-y divide-line">
          {items.map((item) => {
            const t = tones[item.tone]
            const Icon = item.icon
            return (
              <li key={item.id}>
                <Link
                  to={item.to}
                  aria-label={item.actionLabel}
                  className="group flex min-h-16 items-center gap-3 px-4 py-3 transition-colors hover:bg-canvas sm:px-5"
                >
                  <span className={cn('grid size-9 shrink-0 place-items-center rounded-lg', t.tile)}>
                    <Icon className="size-[18px]" aria-hidden />
                  </span>
                  <span className="min-w-0 flex-1">
                    <span className="sr-only">{t.word}: </span>
                    <span className="block font-medium text-ink">{item.title}</span>
                    {item.detail && <span className="block text-sm text-muted">{item.detail}</span>}
                  </span>
                  <ChevronRight className="size-4 shrink-0 text-muted transition-transform group-hover:translate-x-0.5" aria-hidden />
                </Link>
              </li>
            )
          })}
        </ul>
      )}
    </Card>
  )
}
