const dateFmt = new Intl.DateTimeFormat('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })
const dateTimeFmt = new Intl.DateTimeFormat('en-GB', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' })
const dateOnlyFmt = new Intl.DateTimeFormat('en-GB', { day: '2-digit', month: 'short', year: 'numeric', timeZone: 'UTC' })
const moneyFmt = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })

/** API dates are UTC ISO strings (or yyyy-mm-dd for DateOnly). */
export function formatDate(value: string | null | undefined): string {
  if (!value) return '—'
  return dateFmt.format(new Date(value))
}

export function formatMoney(value: number): string {
  return moneyFmt.format(value)
}

export function initials(fullName: string): string {
  return fullName
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((p) => p[0]!.toUpperCase())
    .join('')
}

/** A DateOnly from the API ("2026-09-01"). Formatted in UTC so it never shifts a day in other time zones. */
export function formatDateOnly(value: string | null | undefined): string {
  if (!value) return '—'
  return dateOnlyFmt.format(new Date(`${value}T00:00:00Z`))
}

/** A UTC instant from the API, shown in the viewer's local time ("01 Sept, 08:00"). */
export function formatDateTime(value: string | null | undefined): string {
  if (!value) return '—'
  return dateTimeFmt.format(new Date(value))
}

const pad = (n: number) => String(n).padStart(2, '0')

/** UTC ISO string → value for <input type="datetime-local"> in the viewer's time zone. */
export function toLocalInput(iso: string): string {
  const d = new Date(iso)
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

/** <input type="datetime-local"> value (viewer's time zone) → UTC ISO string ending in Z, as the API requires. */
export function fromLocalInput(local: string): string {
  return new Date(local).toISOString()
}

/** Today as "yyyy-mm-dd" in the viewer's time zone (compares with DateOnly strings). */
export function todayIso(now = new Date()): string {
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`
}
