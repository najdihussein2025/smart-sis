import { forwardRef, useId, type InputHTMLAttributes } from 'react'
import { Search, X } from 'lucide-react'
import { cn } from '@/shared/lib/cn'

interface Props extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type' | 'onChange' | 'value'> {
  label: string
  value: string
  onChange: (value: string) => void
  /** Hide the visible label (the toolbar makes the purpose obvious). It stays the accessible name. */
  srOnlyLabel?: boolean
}

/** The one search box: magnifier, type="search", a clear button once there is text. */
export const SearchField = forwardRef<HTMLInputElement, Props>(function SearchField(
  { label, value, onChange, srOnlyLabel, className, id, ...rest },
  ref,
) {
  const autoId = useId()
  const inputId = id ?? autoId
  return (
    <div className="space-y-1.5">
      <label htmlFor={inputId} className={cn('block text-sm font-medium text-ink', srOnlyLabel && 'sr-only')}>
        {label}
      </label>
      <div className="relative">
        <Search className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-muted" aria-hidden />
        <input
          ref={ref}
          id={inputId}
          type="search"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          className={cn(
            'block h-11 w-full rounded-lg border border-line-strong bg-surface pr-10 pl-10 text-base text-ink placeholder:text-muted sm:text-[15px]',
            'transition-colors focus:border-brand focus:ring-2 focus:ring-accent/40 focus:outline-none',
            '[&::-webkit-search-cancel-button]:appearance-none',
            className,
          )}
          {...rest}
        />
        {value && (
          <button
            type="button"
            onClick={() => onChange('')}
            aria-label={`Clear ${label.toLowerCase()}`}
            className="absolute top-1/2 right-1 grid size-9 -translate-y-1/2 cursor-pointer place-items-center rounded-md text-muted hover:bg-brand-50 hover:text-ink"
          >
            <X className="size-4" aria-hidden />
          </button>
        )}
      </div>
    </div>
  )
})
