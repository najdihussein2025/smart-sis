/** "3.50", or "—" before any grade is final. */
export const gpa = (value: number | null) => (value === null ? '—' : value.toFixed(2))
