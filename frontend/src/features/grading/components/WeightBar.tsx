import { cn } from '@/shared/lib/cn'
import { num } from './gradingForms'

/** "Total weight 70% of 100% · 30% left", with a bar. Text carries the meaning; the bar only repeats it. */
export function WeightBar({ total }: { total: number }) {
  const complete = total === 100
  return (
    <div>
      <div className="flex flex-wrap items-baseline justify-between gap-2 text-sm">
        <span className="font-medium text-ink">Total weight {num(total)}% of 100%</span>
        <span className={cn(complete ? 'text-success' : 'text-warning')}>
          {complete ? 'Complete' : `${num(100 - total)}% left to assign`}
        </span>
      </div>
      <div
        role="progressbar"
        aria-label="Total weight"
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={total}
        className="mt-2 h-2 overflow-hidden rounded-full bg-canvas"
      >
        <div className={cn('h-full rounded-full', complete ? 'bg-success' : 'bg-accent')} style={{ width: `${Math.min(total, 100)}%` }} />
      </div>
    </div>
  )
}
