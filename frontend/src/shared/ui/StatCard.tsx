import type { LucideIcon } from 'lucide-react'
import { Card } from './Card'

export function StatCard({ label, value, hint, icon: Icon }: { label: string; value: string; hint?: string; icon: LucideIcon }) {
  return (
    <Card className="p-5">
      <div className="flex items-start justify-between">
        <p className="text-sm text-muted">{label}</p>
        <span className="grid size-9 place-items-center rounded-lg bg-brand-50 text-brand">
          <Icon className="size-[18px]" aria-hidden />
        </span>
      </div>
      <p className="mt-3 font-display text-3xl font-semibold tracking-tight text-ink">{value}</p>
      {hint && <p className="mt-1 text-[13px] text-muted">{hint}</p>}
    </Card>
  )
}
