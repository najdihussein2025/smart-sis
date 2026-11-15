import { formatDate, formatMoney } from '@/shared/lib/format'
import type { Payment } from '../types'

/** The payments recorded against one charge, oldest first. */
export function PaymentHistory({ payments, label }: { payments: Payment[]; label: string }) {
  if (payments.length === 0) return <p className="text-sm text-muted">No payments yet.</p>
  return (
    <ul aria-label={label} className="space-y-1 text-sm">
      {payments.map((p) => (
        <li key={p.id} className="flex flex-wrap items-baseline gap-x-2">
          <span className="tabular font-medium text-ink">{formatMoney(p.amount)}</span>
          <span className="tabular text-muted">on {formatDate(p.paidAt)}</span>
          {p.note && <span className="text-muted">· {p.note}</span>}
        </li>
      ))}
    </ul>
  )
}
