import { useEffect, useId, useRef, useState } from 'react'
import { useNavigate } from 'react-router'
import { Bell, CheckCheck } from 'lucide-react'
import { errorMessage } from '@/shared/api/problem'
import { cn } from '@/shared/lib/cn'
import { formatDateTime } from '@/shared/lib/format'
import { Button } from '@/shared/ui/Button'
import { Spinner } from '@/shared/ui/Spinner'
import { useLatestNotifications, useMarkAllNotificationsRead, useMarkNotificationRead, useUnreadCount } from '../hooks'
import type { AppNotification } from '../types'

/**
 * The bell in the top bar (PB-12): unread badge (refreshed every minute), and a dropdown with the latest notifications.
 * Opening one marks it read and goes to its link. Escape or a click outside closes the dropdown.
 */
export function NotificationBell() {
  const [open, setOpen] = useState(false)
  const panelId = useId()
  const root = useRef<HTMLDivElement>(null)
  const button = useRef<HTMLButtonElement>(null)
  const navigate = useNavigate()
  const unread = useUnreadCount()
  const latest = useLatestNotifications(open)
  const markRead = useMarkNotificationRead()
  const markAll = useMarkAllNotificationsRead()

  const count = unread.data?.count ?? 0
  const badge = count > 99 ? '99+' : String(count)

  useEffect(() => {
    if (!open) return
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        setOpen(false)
        button.current?.focus()
      }
    }
    const onPointer = (e: MouseEvent) => {
      if (root.current && !root.current.contains(e.target as Node)) setOpen(false)
    }
    document.addEventListener('keydown', onKey)
    document.addEventListener('mousedown', onPointer)
    return () => {
      document.removeEventListener('keydown', onKey)
      document.removeEventListener('mousedown', onPointer)
    }
  }, [open])

  const openNotification = (n: AppNotification) => {
    if (!n.readAt) markRead.mutate(n.id)
    if (n.link) {
      setOpen(false)
      navigate(n.link)
    }
  }

  return (
    <div ref={root} className="relative">
      <button
        ref={button}
        type="button"
        className="relative grid size-11 cursor-pointer place-items-center rounded-lg text-muted hover:bg-brand-50 hover:text-ink sm:size-10"
        aria-label={count > 0 ? `Notifications, ${count} unread` : 'Notifications'}
        aria-expanded={open}
        aria-controls={open ? panelId : undefined}
        onClick={() => setOpen((o) => !o)}
      >
        <Bell className="size-5" aria-hidden />
        {count > 0 && (
          <span
            aria-hidden
            className="absolute -right-0.5 -top-0.5 grid h-[18px] min-w-[18px] place-items-center rounded-full bg-danger px-1 text-xs font-semibold leading-none text-white"
          >
            {badge}
          </span>
        )}
      </button>

      {open && (
        <div
          id={panelId}
          role="region"
          aria-label="Notifications"
          className="fixed inset-x-4 top-16 z-50 overflow-hidden rounded-[var(--radius-card)] border border-line bg-surface shadow-xl sm:absolute sm:inset-x-auto sm:top-12 sm:right-0 sm:w-96"
        >
          <div className="flex items-center justify-between gap-3 border-b border-line px-4 py-3">
            <h2 className="font-display text-base font-semibold text-ink">Notifications</h2>
            <Button
              variant="ghost"
              size="sm"
              disabled={count === 0}
              loading={markAll.isPending}
              onClick={() => markAll.mutate()}
            >
              <CheckCheck className="size-4" aria-hidden /> Mark all as read
            </Button>
          </div>

          <div className="max-h-[min(28rem,70dvh)] overflow-y-auto">
            {latest.isPending ? (
              <div className="grid place-items-center py-10 text-muted">
                <Spinner />
              </div>
            ) : latest.isError ? (
              <div className="space-y-3 p-4 text-sm">
                <p role="alert" className="text-danger">
                  {errorMessage(latest.error, 'Could not load your notifications.')}
                </p>
                <Button variant="secondary" size="sm" onClick={() => latest.refetch()}>
                  Try again
                </Button>
              </div>
            ) : latest.data.items.length === 0 ? (
              <p className="px-4 py-10 text-center text-sm text-muted">
                You're all caught up. New announcements and alerts will show up here.
              </p>
            ) : (
              <ul className="divide-y divide-line" aria-label="Latest notifications">
                {latest.data.items.map((n) => (
                  <li key={n.id}>
                    <button
                      type="button"
                      onClick={() => openNotification(n)}
                      className={cn('flex w-full gap-3 px-4 py-3 text-left hover:bg-canvas', !n.readAt && 'bg-brand-50/60')}
                    >
                      <span
                        aria-hidden
                        className={cn('mt-1.5 size-2 shrink-0 rounded-full', n.readAt ? 'bg-transparent' : 'bg-accent')}
                      />
                      <span className="min-w-0 flex-1">
                        <span className={cn('block truncate text-sm text-ink', !n.readAt && 'font-semibold')}>
                          {!n.readAt && <span className="sr-only">Unread: </span>}
                          {n.title}
                        </span>
                        <span className="mt-0.5 line-clamp-2 block text-[13px] text-muted">{n.body}</span>
                        <span className="mt-1 block text-xs text-muted">{formatDateTime(n.createdAt)}</span>
                      </span>
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>
      )}
    </div>
  )
}
