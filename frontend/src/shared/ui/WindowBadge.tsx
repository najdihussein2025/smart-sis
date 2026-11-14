import { Badge } from './Badge'

export type TermWindow = 'Closed' | 'Enrollment' | 'AddDrop'

const text: Record<TermWindow, string> = {
  Enrollment: 'Enrollment open',
  AddDrop: 'Add/drop open',
  Closed: 'Registration closed',
}

/** Which registration window is open (rule E1), as a badge with text. */
export function WindowBadge({ window }: { window: TermWindow }) {
  return <Badge tone={window === 'Closed' ? 'neutral' : 'success'}>{text[window]}</Badge>
}
