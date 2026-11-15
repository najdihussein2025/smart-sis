import { useId, useRef, type ReactNode } from 'react'
import { X } from 'lucide-react'
import { cn } from '@/shared/lib/cn'
import { useFocusTrap } from '@/shared/lib/useFocusTrap'

interface DialogProps {
  open: boolean
  title: string
  description?: string
  onClose: () => void
  children: ReactNode
  /** Footer buttons, right-aligned. */
  actions?: ReactNode
  /** "md" (default) for short forms, "lg" for forms with fields side by side. */
  size?: 'md' | 'lg'
}

/**
 * Modal dialog: Escape and the backdrop close it, focus moves inside on open, Tab stays inside, and focus returns to the
 * opener on close. On small screens it fills the width (16 px margin) and its body scrolls inside, never the page.
 */
export function Dialog({ open, title, description, onClose, children, actions, size = 'md' }: DialogProps) {
  const titleId = useId()
  const panel = useRef<HTMLDivElement>(null)

  useFocusTrap(panel, open, {
    onEscape: onClose,
    // The first field (or action) rather than the close button, so typing can start at once.
    initialFocus: (root) => root.querySelector<HTMLElement>('input:not([type="hidden"]), select, textarea, button:not([data-close])'),
  })

  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 grid place-items-center p-4 sm:p-6">
      <div className="absolute inset-0 bg-ink/40" aria-hidden onClick={onClose} />
      <div
        ref={panel}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        className={cn(
          'relative flex max-h-[calc(100dvh-2rem)] w-full flex-col rounded-[var(--radius-card)] bg-surface shadow-xl',
          size === 'lg' ? 'max-w-3xl' : 'max-w-xl',
        )}
      >
        <div className="flex items-start justify-between gap-4 border-b border-line px-4 py-4 sm:px-6">
          <div className="min-w-0">
            <h2 id={titleId} className="font-display text-xl font-semibold text-ink">
              {title}
            </h2>
            {description && <p className="mt-0.5 text-sm text-muted">{description}</p>}
          </div>
          <button data-close type="button" onClick={onClose} aria-label="Close" className="-mr-2 -mt-1 grid size-11 shrink-0 cursor-pointer place-items-center rounded-lg text-muted hover:bg-brand-50 hover:text-ink sm:size-9">
            <X className="size-5" aria-hidden />
          </button>
        </div>
        <div className="min-h-0 overflow-y-auto px-4 py-5 sm:px-6">{children}</div>
        {actions && <div className="flex flex-wrap justify-end gap-2 border-t border-line px-4 py-4 sm:px-6">{actions}</div>}
      </div>
    </div>
  )
}
