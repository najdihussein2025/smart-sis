import type { ScheduleItem } from '@/features/schedule/types'

const pad = (n: number) => String(n).padStart(2, '0')

/** "HH:mm:ss" of a local time, comparable with the schedule's wall-clock strings. */
export function clockOf(now: Date): string {
  return `${pad(now.getHours())}:${pad(now.getMinutes())}:${pad(now.getSeconds())}`
}

/**
 * The API's DayOfWeek (1 = Monday … 6 = Saturday) for a date, or 0 on Sunday (nobody has classes then). Unlike
 * `todayDayOfWeek` in shared/lib this never maps Sunday onto Monday, so a Sunday dashboard doesn't show Monday's classes.
 */
export function apiDayOf(date: Date): number {
  return date.getDay()
}

/** The same for a DateOnly from the API ("2026-11-11"), read in UTC so it never shifts a day. */
export function apiDayOfIso(iso: string): number {
  return new Date(`${iso}T00:00:00Z`).getUTCDay()
}

const byStart = (a: ScheduleItem, b: ScheduleItem) => a.dayOfWeek - b.dayOfWeek || a.startTime.localeCompare(b.startTime)

export interface NextClass {
  item: ScheduleItem
  /** `now` = happening right now, `today` = later today, `later` = a later day (this week or, after Saturday, next week). */
  when: 'now' | 'today' | 'later'
}

export interface DayPlan {
  /** Every meeting on `day`, by start time. */
  today: ScheduleItem[]
  /** Meetings on `day` that haven't ended yet at `clock`. */
  remainingToday: ScheduleItem[]
  /** The class to show as "Next up", or null when the timetable is empty. */
  next: NextClass | null
}

/**
 * What's next on a weekly timetable. `day` is the API's DayOfWeek (0 = Sunday), `clock` a "HH:mm:ss" wall-clock time.
 * The timetable repeats every week, so after the last class of the week the next one is the first class of the week.
 */
export function planDay(items: ScheduleItem[], day: number, clock: string): DayPlan {
  const sorted = [...items].sort(byStart)
  const today = sorted.filter((i) => i.dayOfWeek === day)
  const remainingToday = today.filter((i) => i.endTime > clock)

  const current = remainingToday[0]
  if (current) return { today, remainingToday, next: { item: current, when: current.startTime <= clock ? 'now' : 'today' } }

  const later = sorted.find((i) => i.dayOfWeek > day) ?? sorted[0]
  return { today, remainingToday, next: later ? { item: later, when: 'later' } : null }
}
