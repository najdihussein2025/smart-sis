import { Badge } from '@/shared/ui/Badge'
import type { AbsenceStanding } from '../types'

const tone = { Perfect: 'success', Warning: 'warning', Critical: 'danger' } as const

/** Rule A5: green "Perfect", orange "Warning", red "Critical". The word is always shown, so colour is never the only signal. */
export function StandingBadge({ standing }: { standing: AbsenceStanding }) {
  return <Badge tone={tone[standing]}>{standing}</Badge>
}
