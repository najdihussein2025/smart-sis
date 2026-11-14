import { useRef, type KeyboardEvent, type ReactNode } from 'react'
import { cn } from '@/shared/lib/cn'

export interface TabItem<T extends string> {
  value: T
  label: ReactNode
  /** Accessible name when the label is short ("Mon" → "Monday"). */
  ariaLabel?: string
}

interface Props<T extends string> {
  label: string
  items: TabItem<T>[]
  value: T
  onChange: (value: T) => void
}

/**
 * Segmented tabs (catalog Major / Elective, schedule day tabs). Arrow keys move between tabs, as screen-reader users
 * expect from role="tablist". Scrolls sideways on narrow screens instead of wrapping.
 */
export function Tabs<T extends string>({ label, items, value, onChange }: Props<T>) {
  const refs = useRef<(HTMLButtonElement | null)[]>([])

  const onKeyDown = (e: KeyboardEvent, index: number) => {
    const step = e.key === 'ArrowRight' ? 1 : e.key === 'ArrowLeft' ? -1 : 0
    if (!step) return
    e.preventDefault()
    const next = (index + step + items.length) % items.length
    onChange(items[next]!.value)
    refs.current[next]?.focus()
  }

  return (
    <div role="tablist" aria-label={label} className="flex max-w-full gap-1 overflow-x-auto rounded-xl border border-line bg-canvas p-1">
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
            aria-label={item.ariaLabel}
            tabIndex={selected ? 0 : -1}
            onClick={() => onChange(item.value)}
            onKeyDown={(e) => onKeyDown(e, i)}
            className={cn(
              'shrink-0 rounded-lg px-3.5 py-1.5 text-sm font-medium transition-colors',
              selected ? 'bg-surface text-ink shadow-sm' : 'text-muted hover:text-ink',
            )}
          >
            {item.label}
          </button>
        )
      })}
    </div>
  )
}
