import { hm, shortDays, type Meeting } from '@/shared/lib/schedule'
import { Badge } from './Badge'

/** "Mon 08:00–09:15 · B-101", one line per weekly meeting, with a Lab badge. */
export function MeetingList({ meetings, label }: { meetings: Meeting[]; label: string }) {
  return (
    <ul className="space-y-0.5 text-sm text-muted" aria-label={label}>
      {meetings.map((m) => (
        <li key={m.id} className="whitespace-nowrap">
          <span className="inline-block w-9 font-medium text-ink">{shortDays[m.dayOfWeek]}</span>
          {hm(m.startTime)}–{hm(m.endTime)} · {m.roomName}
          {m.type === 'Lab' && (
            <span className="ml-1.5">
              <Badge>Lab</Badge>
            </span>
          )}
        </li>
      ))}
    </ul>
  )
}
