import type { ReactNode } from 'react'
import { AlertCircle, CheckCircle2 } from 'lucide-react'
import { cn } from '@/shared/lib/cn'

export function Alert({ tone = 'danger', children }: { tone?: 'danger' | 'success'; children: ReactNode }) {
  const Icon = tone === 'danger' ? AlertCircle : CheckCircle2
  return (
    <div
      role={tone === 'danger' ? 'alert' : 'status'}
      className={cn(
        'flex items-start gap-2.5 rounded-lg px-3.5 py-3 text-sm',
        tone === 'danger' ? 'bg-danger-soft text-danger' : 'bg-success-soft text-success',
      )}
    >
      <Icon className="mt-px size-4 shrink-0" aria-hidden />
      <div>{children}</div>
    </div>
  )
}
