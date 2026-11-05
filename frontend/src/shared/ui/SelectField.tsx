import { forwardRef, useId, type SelectHTMLAttributes } from 'react'
import { cn } from '@/shared/lib/cn'

interface SelectFieldProps extends SelectHTMLAttributes<HTMLSelectElement> {
  label: string
  error?: string
  hint?: string
  /** Hide the visible label (a table header or group heading already names it). It stays the accessible name. */
  srOnlyLabel?: boolean
}

/** Native select with a label and the same error / hint layout as TextField. */
export const SelectField = forwardRef<HTMLSelectElement, SelectFieldProps>(function SelectField(
  { label, error, hint, srOnlyLabel, className, id, children, ...rest },
  ref,
) {
  const autoId = useId()
  const selectId = id ?? autoId
  const describedBy = error ? `${selectId}-error` : hint ? `${selectId}-hint` : undefined
  return (
    <div className="space-y-1.5">
      <label htmlFor={selectId} className={cn('block text-sm font-medium text-ink', srOnlyLabel && 'sr-only')}>
        {label}
      </label>
      <select
        ref={ref}
        id={selectId}
        aria-invalid={!!error}
        aria-describedby={describedBy}
        className={cn(
          'block w-full h-11 rounded-lg border bg-surface px-3 text-[15px] text-ink disabled:bg-canvas disabled:text-muted',
          'focus:outline-none focus:ring-2 focus:ring-accent/40',
          error ? 'border-danger focus:border-danger' : 'border-line focus:border-brand',
          className,
        )}
        {...rest}
      >
        {children}
      </select>
      {error ? (
        <p id={`${selectId}-error`} className="text-[13px] text-danger">
          {error}
        </p>
      ) : hint ? (
        <p id={`${selectId}-hint`} className="text-[13px] text-muted">
          {hint}
        </p>
      ) : null}
    </div>
  )
})
