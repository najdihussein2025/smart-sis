export { days, hm, shortDays } from '@/shared/lib/schedule'

const pad = (n: number) => String(n).padStart(2, '0')

/** Class times in 5-minute steps, 07:00 … 22:00 (24-hour). */
export const classTimes: string[] = Array.from({ length: (22 - 7) * 12 + 1 }, (_, i) => {
  const minutes = 7 * 60 + i * 5
  return `${pad(Math.floor(minutes / 60))}:${pad(minutes % 60)}`
})
