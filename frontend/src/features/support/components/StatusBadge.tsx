import { Badge } from '@/shared/ui/Badge'
import type { SupportStatus } from '../types'

export const statusLabels: Record<SupportStatus, string> = {
  Open: 'Open',
  InProgress: 'In progress',
  Resolved: 'Resolved',
  Closed: 'Closed',
}

const tones = { Open: 'warning', InProgress: 'info', Resolved: 'success', Closed: 'neutral' } as const

/** Status pill with its name, so colour is never the only signal. */
export function StatusBadge({ status }: { status: SupportStatus }) {
  return (
    <Badge tone={tones[status]} dot>
      {statusLabels[status]}
    </Badge>
  )
}
