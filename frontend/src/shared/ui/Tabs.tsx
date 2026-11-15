import { useEffect, useRef, type KeyboardEvent, type ReactNode } from 'react'
import { cn } from '@/shared/lib/cn'

export interface TabItem<T extends string> {
  value: T
  label: ReactNode
  /** Accessible name when the label is short ("Mon" → "Monday"). */
  ariaLabel?: string
  /** Small count after the label ("Requests 3"). Spoken as part of the tab name. */
  count?: number
}

interface Props<T extends string> {
  label: string
  items: TabItem<T>[]
  value: T
  onChange: (value: T) => void
  /** "fit" (default) sizes the strip to its tabs; "fill" shares the full width equally (day pickers on a phone). */
  layout?: 'fit' | 'fill'
}

/**
 * Segmented tabs (catalog Major / Elective, schedule day tabs). Arrow keys (and Home / End) move between tabs, as
 * screen-reader users expect from role="tablist". When the strip is wider than the screen it scrolls sideways with a
 * soft fade on the right edge, and the selected tab is scrolled into view.
 */
export function Tabs<T extends string>({ label, items, value, onChange, layout = 'fit' }: Props<T>) {
  const refs = useRef<(HTMLButtonElement | null)[]>([])
  const strip = useRef<HTMLDivElement>(null)
  const selectedIndex = items.findIndex((i) => i.value === value)

  useEffect(() => {
    const el = refs.current[selectedIndex]
    const box = strip.current
    if (!el || !box || box.scrollWidth <= box.clientWidth) return
    box.scrollTo({ left: el.offsetLeft - box.clientWidth / 2 + el.clientWidth / 2 })
  }, [selectedIndex])

  const onKeyDown = (e: KeyboardEvent, index: number) => {
    const last = items.length - 1
    const next =
      e.key === 'ArrowRight' ? (index + 1) % items.length
      : e.key === 'ArrowLeft' ? (index + last) % items.length
      : e.key === 'Home' ? 0
      : e.key === 'End' ? last
      : -1
    if (next < 0) return
    e.preventDefault()
    onChange(items[next]!.value)
    refs.current[next]?.focus()
  }

  return (
    <div
      ref={strip}
      role="tablist"
      aria-label={label}
      className={cn(
        'flex max-w-full gap-1 overflow-x-auto rounded-xl border border-line bg-canvas p-1 [scrollbar-width:none]',
        layout === 'fit' ? 'w-fit' : 'w-full',
      )}
    >
      {items.map((item, i) => {
        const selected = item.value === value
        return (
          <button
            key={item.value}
            ref={(el) => {
              refs.current[i] = el
            }}
            type="button"
            role="tab"
            aria-selected={selected}
            aria-label={item.ariaLabel ?? (typeof item.label === 'string' && item.count !== undefined ? `${item.label}, ${item.count}` : undefined)}
            tabIndex={selected ? 0 : -1}
            onClick={() => onChange(item.value)}
            onKeyDown={(e) => onKeyDown(e, i)}
            className={cn(
              'inline-flex min-h-10 shrink-0 cursor-pointer items-center justify-center gap-1.5 rounded-lg px-3.5 text-sm font-medium whitespace-nowrap transition-colors sm:min-h-9',
              layout === 'fill' && 'flex-1 px-2',
              selected ? 'bg-surface text-ink shadow-sm ring-1 ring-line' : 'text-muted hover:bg-surface/60 hover:text-ink',
            )}
          >
            {item.label}
            {item.count !== undefined && (
              <span
                aria-hidden
                className={cn(
                  'tabular rounded-full px-1.5 text-xs leading-5',
                  selected ? 'bg-brand text-white' : 'bg-line text-ink',
                )}
              >
                {item.count}
              </span>
            )}
          </button>
        )
      })}
    </div>
  )
}
