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
 * Shapes what is typed into a date field as DD/MM/YYYY, refusing impossible digits as they are typed:
 * - only digits; the slashes are added; nothing after the 4-digit year;
 * - day 01–31: a first digit 4–9 becomes 04–09, after a 3 only 0 or 1, no 00;
 * - month 01–12: a first digit 2–9 becomes 02–09, after a 1 only 0–2, no 00;
 * - year 1900–2099;
 * - a typed separator after one digit pads it ("1/" → "01/").
 * Day-in-month (31/04, 29/02 in 2027) is checked when the field is left, since the month may come later.
 * The slash only appears once the next digit is typed, so Backspace never gets stuck on it.
 */
export function maskDmy(raw: string): string {
  const parts: string[] = ['']
  const sizes = [2, 2, 4]
  // Would `digit` be allowed as the next digit of part `i` (which currently holds `cur`)?
  const allowed = (i: number, cur: string, digit: string): boolean => {
    const d = Number(digit)
    if (i === 0) return cur === '' || (cur === '0' ? d >= 1 : cur === '3' ? d <= 1 : true)
    if (i === 1) return cur === '' || (cur === '0' ? d >= 1 : d <= 2)
    if (cur === '') return d === 1 || d === 2
    if (cur.length === 1) return cur === '1' ? d === 9 : d === 0
    return true
  }
  for (const ch of raw) {
    const i = parts.length - 1
    if (/\d/.test(ch)) {
      const full = parts[i]!.length === sizes[i]
      if (full && i === 2) break // the year is full
      const target = full ? i + 1 : i
      const cur = full ? '' : parts[i]!
      if (!allowed(target, cur, ch)) continue
      if (full) parts.push('')
      // A day starting with 4–9 or a month starting with 2–9 can only be one digit: pad it.
      const pad = cur === '' && ((target === 0 && ch > '3') || (target === 1 && ch > '1'))
      parts[target] = cur + (pad ? `0${ch}` : ch)
    } else if (/[/.\- ]/.test(ch) && i < 2 && parts[i]!.length > 0) {
      if (parts[i] === '0') continue // "0/" is not a day or month
      parts[i] = parts[i]!.padStart(2, '0')
      parts.push('')
    }
  }
  return parts.join('/')
}

/** The grey template still to fill after what was typed: "20/0" → "1/YYYY"-style rest of "DD/MM/YYYY". */
export function dmyTemplateRest(text: string): string {
  return 'DD/MM/YYYY'.slice(text.length)
}
