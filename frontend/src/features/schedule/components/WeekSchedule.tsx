import { useState } from 'react'
import { CalendarOff, Clock, MapPin, User } from 'lucide-react'
import { days, hm, shortDays, todayDayOfWeek } from '@/shared/lib/schedule'
import { Badge } from '@/shared/ui/Badge'
import { EmptyState } from '@/shared/ui/EmptyState'
import { Tabs, type TabItem } from '@/shared/ui/Tabs'
import type { ScheduleItem } from '../types'

type Day = '1' | '2' | '3' | '4' | '5' | '6'
const dayValues: Day[] = ['1', '2', '3', '4', '5', '6']

interface Props {
  items: ScheduleItem[]
  /** The student view shows who teaches each class; the teaching schedule does not need it. */
  showInstructor?: boolean
}

/**
 * Mon–Sat day tabs over a week of meetings, opening on today (Sunday opens Monday). Each class shows its time,
 * course, section, room, Lecture/Lab and (student view) instructor. Used by the student and instructor pages.
 */
export function WeekSchedule({ items, showInstructor = false }: Props) {
  const [day, setDay] = useState<Day>(String(todayDayOfWeek()) as Day)

  const tabs: TabItem<Day>[] = dayValues.map((d) => {
    const n = items.filter((i) => i.dayOfWeek === Number(d)).length
    return {
      value: d,
      ariaLabel: days[Number(d)],
      label: (
        <span className="inline-flex items-center gap-1.5">
          {shortDays[Number(d)]}
          {n > 0 && (
            <span className="rounded-full bg-accent-soft px-1.5 text-xs text-accent-ink" aria-hidden>
              {n}
            </span>
          )}
        </span>
      ),
    }
  })

  const dayName = days[Number(day)]
  const classes = items
    .filter((i) => i.dayOfWeek === Number(day))
    .sort((a, b) => a.startTime.localeCompare(b.startTime))

  return (
    <div>
      <div className="border-b border-line p-3 sm:p-4">
        <Tabs label="Day of the week" items={tabs} value={day} onChange={setDay} />
      </div>

      <div role="tabpanel" aria-label={dayName}>
        {classes.length === 0 ? (
          <EmptyState icon={CalendarOff} title={`No classes on ${dayName}`}>
            Pick another day to see its classes.
          </EmptyState>
        ) : (
          <ul className="divide-y divide-line" aria-label={`Classes on ${dayName}`}>
            {classes.map((c) => (
              <li key={c.meetingId} className="flex gap-4 px-4 py-4 sm:px-5">
                <div className="w-14 shrink-0 text-sm sm:w-20">
                  <p className="font-semibold text-ink">{hm(c.startTime)}</p>
                  <p className="text-muted">{hm(c.endTime)}</p>
                </div>
                <div className="min-w-0 flex-1">
                  <div className="flex flex-wrap items-center gap-2">
                    <p className="font-semibold text-ink">
                      {c.courseCode} {c.sectionCode}
                    </p>
                    <Badge tone={c.type === 'Lab' ? 'warning' : 'neutral'}>{c.type}</Badge>
                  </div>
                  <p className="mt-0.5 text-sm text-ink">{c.courseName}</p>
                  <ul className="mt-2 flex flex-wrap gap-x-4 gap-y-1 text-sm text-muted">
                    <li className="inline-flex items-center gap-1.5">
                      <Clock className="size-3.5" aria-hidden />
                      {hm(c.startTime)}–{hm(c.endTime)}
                    </li>
                    <li className="inline-flex items-center gap-1.5">
                      <MapPin className="size-3.5" aria-hidden />
                      <span className="sr-only">Room </span>
                      {c.roomName}
                    </li>
                    {showInstructor && (
                      <li className="inline-flex items-center gap-1.5">
                        <User className="size-3.5" aria-hidden />
                        <span className="sr-only">Instructor </span>
                        {c.instructorName ?? 'Instructor to be announced'}
                      </li>
                    )}
                  </ul>
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  )
}
