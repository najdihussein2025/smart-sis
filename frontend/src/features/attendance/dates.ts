import { addDays, isoToDate, isoToDmy } from '@/shared/lib/dates'
import { days } from '@/shared/lib/schedule'

/** Weekday of an ISO date in the API's numbering (0 = Sunday, 1 = Monday … 6 = Saturday). */
export const weekdayOf = (iso: string) => isoToDate(iso).getDay()

/** The latest date on or before `max` that falls on `dayOfWeek`, or null if that is before `min`. */
export function latestOn(dayOfWeek: number, min: string, max: string): string | null {
  let d = max
  while (weekdayOf(d) !== dayOfWeek) d = addDays(d, -1)
  return d >= min ? d : null
}

/** Why attendance can't be taken on `date` for a meeting on `dayOfWeek` (same rules as the API), or null. */
export function dateProblem(date: string, dayOfWeek: number, min: string, max: string): string | null {
  if (date > max) return 'Attendance can’t be taken for a future date.'
  if (date < min) return `Choose a date inside the term (from ${isoToDmy(min)}).`
  if (weekdayOf(date) !== dayOfWeek) return `This session is on ${days[dayOfWeek]}. Choose a ${days[dayOfWeek]}.`
  return null
}
