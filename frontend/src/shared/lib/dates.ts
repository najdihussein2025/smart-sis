/**
 * Day-month-year helpers for the date fields. Forms keep ISO strings ("2028-01-20", "2028-01-20T08:00");
 * people see and type DD/MM/YYYY and 24-hour times.
 */

const pad = (n: number) => String(n).padStart(2, '0')

/** "2028-01-20" → "20/01/2028"; "" for empty. */
export function isoToDmy(iso: string): string {
  if (!iso) return ''
  const [y, m, d] = iso.split('-')
  return `${d}/${m}/${y}`
}

/** "20/01/2028" (or "20-1-2028", "20.01.2028") → "2028-01-20"; null if it isn't a real date. */
export function dmyToIso(text: string): string | null {
  const m = /^(\d{1,2})[/.-](\d{1,2})[/.-](\d{4})$/.exec(text.trim())
  if (!m) return null
  const [day, month, year] = [Number(m[1]), Number(m[2]), Number(m[3])]
  const d = new Date(year, month - 1, day)
  if (d.getFullYear() !== year || d.getMonth() !== month - 1 || d.getDate() !== day) return null // e.g. 31/02
  return `${year}-${pad(month)}-${pad(day)}`
}

/** Local Date at midnight for an ISO date, for the calendar. */
export function isoToDate(iso: string): Date {
  const [y, m, d] = iso.split('-').map(Number)
  return new Date(y!, m! - 1, d!)
}

export function dateToIso(d: Date): string {
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
}

/** "2028-01-20" + 1 → "2028-01-21". */
export function addDays(iso: string, days: number): string {
  const d = isoToDate(iso)
  d.setDate(d.getDate() + days)
  return dateToIso(d)
}

/** "00:00", "00:15", … "23:45". */
export const quarterHours: string[] = Array.from({ length: 96 }, (_, i) => `${pad(Math.floor(i / 4))}:${pad((i % 4) * 15)}`)

/**
 * Shapes what is typed into a date field as DD/MM/YYYY: digits only, the slashes are added, at most 8 digits.
 * "20012028" → "20/01/2028". A typed separator after a single digit pads it ("1/" → "01/"). Letters are dropped.
 * The slash only appears once the next digit is typed, so Backspace never gets stuck on it.
 */
export function maskDmy(raw: string): string {
  const sizes = [2, 2, 4]
  const parts = ['']
  for (const ch of raw) {
    const i = parts.length - 1
    if (/\d/.test(ch)) {
      if (parts[i]!.length === sizes[i]) {
        if (i === 2) break // the year is full
        parts.push('')
      }
      parts[parts.length - 1] += ch
    } else if (/[/.\- ]/.test(ch) && i < 2 && parts[i]!.length > 0) {
      parts[i] = parts[i]!.padStart(2, '0')
      parts.push('')
    }
  }
  return parts.join('/')
}
