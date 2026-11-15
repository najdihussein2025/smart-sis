import type { ReactNode } from 'react'
import { AlertCircle, AlertTriangle, CheckCircle2, Info } from 'lucide-react'
import { cn } from '@/shared/lib/cn'

type Tone = 'danger' | 'success' | 'warning' | 'info'

const styles: Record<Tone, { box: string; icon: typeof Info }> = {
  danger: { box: 'bg-danger-soft text-danger border-danger/20', icon: AlertCircle },
  success: { box: 'bg-success-soft text-success border-success/20', icon: CheckCircle2 },
  warning: { box: 'bg-warning-soft text-warning border-warning/20', icon: AlertTriangle },
  info: { box: 'bg-info-soft text-info border-info/20', icon: Info },
}

/**
 * Inline message. "danger" is for errors (announced at once, role=alert); "warning" and "info" are for setup notices
 * that are not errors ("Add 20 questions to open the quiz"); "success" for saved confirmations.
 */
export function Alert({ tone = 'danger', title, children, action }: {
  tone?: Tone
  /** Bold first line. */
  title?: string
  children: ReactNode
  /** A button or link on the right ("Add questions"). */
  action?: ReactNode
}) {
  const { box, icon: Icon } = styles[tone]
  return (
    <div
      role={tone === 'danger' ? 'alert' : 'status'}
      className={cn('flex flex-wrap items-start gap-x-2.5 gap-y-2 rounded-xl border px-3.5 py-3 text-sm', box)}
    >
      <Icon className="mt-px size-4 shrink-0" aria-hidden />
      <div className="min-w-0 flex-1">
        {title && <p className="font-semibold">{title}</p>}
        <div>{children}</div>
      </div>
      {action && <div className="ml-auto shrink-0">{action}</div>}
    </div>
  )
}
