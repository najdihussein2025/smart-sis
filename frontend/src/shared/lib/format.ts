const dateFmt = new Intl.DateTimeFormat('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })
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
