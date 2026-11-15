import type { ButtonHTMLAttributes } from 'react'
import { cn } from '@/shared/lib/cn'
import { Spinner } from './Spinner'

type Variant = 'primary' | 'secondary' | 'ghost' | 'danger' | 'danger-ghost'
type Size = 'md' | 'sm' | 'icon'

const variants: Record<Variant, string> = {
  primary: 'bg-brand text-white shadow-sm hover:bg-brand-600 active:bg-brand-700',
  secondary: 'bg-surface text-ink border border-line-strong/60 shadow-sm hover:bg-canvas hover:border-line-strong',
  ghost: 'text-muted hover:text-ink hover:bg-brand-50',
  danger: 'bg-danger text-white shadow-sm hover:brightness-110',
  /** Row actions that remove something (Delete, Drop, Deactivate): red text, no fill until hover. */
  'danger-ghost': 'text-danger hover:bg-danger-soft',
}

/**
 * Touch targets (WCAG 2.5.5 / skill rule touch-target-size): 44 px tall on phones and tablets, the compact desktop
 * height from `sm` up, where a mouse is the usual pointer.
 */
const sizes: Record<Size, string> = {
  md: 'h-11 px-4 sm:h-10',
  sm: 'h-11 px-3 sm:h-9',
  icon: 'size-11 sm:size-9',
}

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant
  size?: Size
  loading?: boolean
}

/** The classes of a Button, for a router `<Link>` that should look like one (see LinkButton). */
export function buttonClasses(variant: Variant = 'primary', size: Size = 'md', className?: string) {
  return cn(
    'inline-flex shrink-0 cursor-pointer items-center justify-center gap-2 rounded-lg text-sm font-medium transition-colors',
    sizes[size],
    variants[variant],
    className,
  )
}

export type { Variant as ButtonVariant, Size as ButtonSize }

export function Button({ variant = 'primary', size = 'md', loading, disabled, className, children, ...rest }: ButtonProps) {
  return (
    <button
      {...rest}
      disabled={disabled || loading}
      className={cn(
        'inline-flex shrink-0 cursor-pointer items-center justify-center gap-2 rounded-lg text-sm font-medium transition-colors',
        // Every variant looks disabled when it is, not only the primary one.
        'disabled:cursor-not-allowed disabled:opacity-50 disabled:shadow-none',
        sizes[size],
        variants[variant],
        className,
      )}
    >
      {loading && <Spinner className="size-4" />}
      {children}
    </button>
  )
}
