import { Badge } from '@/shared/ui/Badge'
import type { ChargeStatus } from '../types'

const tones = { Pending: 'warning', Partial: 'info', Paid: 'success' } as const
const labels: Record<ChargeStatus, string> = { Pending: 'Pending', Partial: 'Partially paid', Paid: 'Paid' }

/** Pending / Partial / Paid, always with text (colour is never the only signal). */
export function ChargeStatusBadge({ status }: { status: ChargeStatus }) {
  return <Badge tone={tones[status]} dot>{labels[status]}</Badge>
}

export function OverdueBadge() {
  return <Badge tone="danger" dot>Overdue</Badge>
}
