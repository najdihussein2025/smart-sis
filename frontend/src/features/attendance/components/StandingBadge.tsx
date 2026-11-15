import { Badge } from '@/shared/ui/Badge'
import type { AbsenceStanding } from '../types'

const tone = { Perfect: 'success', Warning: 'warning', Critical: 'danger' } as const

/** Rule A5: green "Perfect", orange "Warning", red "Critical". The word is always shown, so colour is never the only signal. */
export function StandingBadge({ standing, dot = true }: { standing: AbsenceStanding; dot?: boolean }) {
  return (
    <Badge tone={tone[standing]} dot={dot}>
      {standing}
    </Badge>
  )
}
