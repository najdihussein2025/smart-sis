import { forwardRef, useId, type InputHTMLAttributes } from 'react'
import { cn } from '@/shared/lib/cn'

interface TextFieldProps extends InputHTMLAttributes<HTMLInputElement> {
  label: string
  error?: string
  hint?: string
}

export const TextField = forwardRef<HTMLInputElement, TextFieldProps>(function TextField(
  { label, error, hint, className, id, ...rest },
  ref,
) {
  const autoId = useId()
  const inputId = id ?? autoId
  const describedBy = error ? `${inputId}-error` : hint ? `${inputId}-hint` : undefined

  return (
    <div className="space-y-1.5">
      <label htmlFor={inputId} className="block text-sm font-medium text-ink">
        {label}
      </label>
      <input
        ref={ref}
        id={inputId}
        aria-invalid={!!error}
        aria-describedby={describedBy}
        className={cn(
          'block w-full h-11 rounded-lg border bg-surface px-3.5 text-[15px] text-ink placeholder:text-muted/70',
          'transition-colors focus:outline-none focus:ring-2 focus:ring-accent/40',
          error ? 'border-danger focus:border-danger' : 'border-line focus:border-brand',
          className,
        )}
        {...rest}
      />
      {error ? (
        <p id={`${inputId}-error`} className="text-[13px] text-danger">
          {error}
        </p>
      ) : hint ? (
        <p id={`${inputId}-hint`} className="text-[13px] text-muted">
          {hint}
        </p>
      ) : null}
    </div>
  )
})
