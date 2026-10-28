import type { HTMLAttributes } from 'react'
import { cn } from '@/shared/lib/cn'

export function Card({ className, ...rest }: HTMLAttributes<HTMLDivElement>) {
  return <div className={cn('rounded-[var(--radius-card)] bg-surface border border-line shadow-[var(--shadow-card)]', className)} {...rest} />
}
