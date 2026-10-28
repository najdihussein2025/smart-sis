import type { ButtonHTMLAttributes } from 'react'
import { cn } from '@/shared/lib/cn'
import { Spinner } from './Spinner'

type Variant = 'primary' | 'secondary' | 'ghost' | 'danger'

const variants: Record<Variant, string> = {
  primary: 'bg-brand text-white hover:bg-brand-600 disabled:bg-brand/50',
  secondary: 'bg-surface text-ink border border-line hover:bg-canvas',
  ghost: 'text-muted hover:text-ink hover:bg-brand-50',
  danger: 'bg-danger text-white hover:brightness-110',
}

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant
  loading?: boolean
}

export function Button({ variant = 'primary', loading, disabled, className, children, ...rest }: ButtonProps) {
  return (
    <button
      {...rest}
      disabled={disabled || loading}
      className={cn(
        'inline-flex items-center justify-center gap-2 rounded-lg px-4 h-10 text-sm font-medium transition-colors',
        'disabled:cursor-not-allowed',
        variants[variant],
        className,
      )}
    >
      {loading && <Spinner className="size-4" />}
      {children}
    </button>
  )
}
