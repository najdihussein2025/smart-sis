/** Day names for SectionMeetings.DayOfWeek (1 = Monday … 6 = Saturday). */
export const days = ['', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'] as const
export const shortDays = ['', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'] as const

/** "08:00:00" → "08:00". */
export const hm = (time: string) => time.slice(0, 5)

export type MeetingType = 'Lecture' | 'Lab'

/** A weekly meeting as the API sends it. Times are wall-clock "HH:mm:ss". */
export interface Meeting {
  id: number
  dayOfWeek: number
  startTime: string
  endTime: string
  roomId: number
  roomName: string
  type: MeetingType
}

/** Today's DayOfWeek in the API's numbering (1 = Monday … 6 = Saturday), or 1 on Sunday. */
export function todayDayOfWeek(now = new Date()): number {
  const d = now.getDay()
  return d === 0 ? 1 : d
}
