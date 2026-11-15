import { forwardRef } from 'react'
import { Pencil, Trash2 } from 'lucide-react'
import { cn } from '@/shared/lib/cn'
import { formatDateTime } from '@/shared/lib/format'
import { Badge } from '@/shared/ui/Badge'
import { Button } from '@/shared/ui/Button'
import type { Announcement } from '../types'
import { audienceLabel } from './announcementForm'

interface Props {
  announcement: Announcement
  /** Opened from a notification: drawn with a ring so the reader finds it. */
  highlighted?: boolean
  /** Edit / Delete buttons, shown only when the announcement can be managed. */
  onEdit?: (a: Announcement) => void
  onDelete?: (a: Announcement) => void
}

/** One announcement: title, who posted it, for whom, when, and the text. */
export const AnnouncementItem = forwardRef<HTMLElement, Props>(function AnnouncementItem(
  { announcement: a, highlighted, onEdit, onDelete },
  ref,
) {
  const manage = a.canManage && (onEdit || onDelete)
  return (
    <article
      ref={ref}
      id={`announcement-${a.id}`}
      aria-labelledby={`announcement-${a.id}-title`}
      tabIndex={-1}
      className={cn('px-5 py-5 outline-none sm:px-6', highlighted && 'rounded-lg bg-accent-soft/40 ring-2 ring-accent')}
    >
      <div className="flex flex-wrap items-start justify-between gap-x-4 gap-y-2">
        <div className="min-w-0">
          <h2 id={`announcement-${a.id}-title`} className="font-display text-lg font-semibold text-ink">
            {a.title}
          </h2>
          <p className="mt-0.5 text-sm text-muted">
            {a.authorName} · <time dateTime={a.publishAt}>{formatDateTime(a.publishAt)}</time>
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-1.5">
          <Badge tone="outline">{audienceLabel(a)}</Badge>
          {a.status === 'Scheduled' && <Badge tone="warning" dot>Scheduled</Badge>}
          {a.status === 'Expired' && <Badge tone="danger" dot>Expired</Badge>}
        </div>
      </div>

      <p className="mt-3 whitespace-pre-line break-words text-[15px] leading-relaxed text-ink">{a.body}</p>

      <div className="mt-3 flex flex-wrap items-center justify-between gap-2">
        <p className="text-xs text-muted">
          {a.status === 'Scheduled' && <>Publishes {formatDateTime(a.publishAt)}. </>}
          {a.expiresAt && (
            <>
              {a.status === 'Expired' ? 'Expired' : 'Until'} {formatDateTime(a.expiresAt)}.
            </>
          )}
        </p>
        {manage && (
          <div className="flex gap-1">
            {onEdit && (
              <Button variant="ghost" size="sm" onClick={() => onEdit(a)} aria-label={`Edit "${a.title}"`}>
                <Pencil className="size-4" aria-hidden /> Edit
              </Button>
            )}
            {onDelete && (
              <Button variant="danger-ghost" size="sm" onClick={() => onDelete(a)} aria-label={`Delete "${a.title}"`}>
                <Trash2 className="size-4" aria-hidden /> Delete
              </Button>
            )}
          </div>
        )}
      </div>
    </article>
  )
})
