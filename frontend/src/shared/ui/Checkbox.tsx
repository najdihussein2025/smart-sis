import { forwardRef, useId, type InputHTMLAttributes } from 'react'

interface CheckboxProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> {
  label: string
  hint?: string
}

export const Checkbox = forwardRef<HTMLInputElement, CheckboxProps>(function Checkbox({ label, hint, id, ...rest }, ref) {
  const autoId = useId()
  const inputId = id ?? autoId
  return (
    <div className="flex items-start gap-3">
      <input
        ref={ref}
        id={inputId}
        type="checkbox"
        aria-describedby={hint ? `${inputId}-hint` : undefined}
        className="mt-0.5 size-4 rounded border-line accent-brand"
        {...rest}
      />
      <div>
        <label htmlFor={inputId} className="text-sm font-medium text-ink">
          {label}
        </label>
        {hint && (
          <p id={`${inputId}-hint`} className="text-[13px] text-muted">
            {hint}
          </p>
        )}
      </div>
    </div>
  )
})
