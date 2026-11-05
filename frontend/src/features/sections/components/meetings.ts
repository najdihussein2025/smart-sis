/** Day names for SectionMeetings.DayOfWeek (1 = Monday … 6 = Saturday). */
export const days = ['', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'] as const
export const shortDays = ['', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'] as const

const pad = (n: number) => String(n).padStart(2, '0')

/** Class times in 5-minute steps, 07:00 … 22:00 (24-hour). */
export const classTimes: string[] = Array.from({ length: (22 - 7) * 12 + 1 }, (_, i) => {
  const minutes = 7 * 60 + i * 5
  return `${pad(Math.floor(minutes / 60))}:${pad(minutes % 60)}`
})

/** "08:00:00" → "08:00". */
export const hm = (time: string) => time.slice(0, 5)
