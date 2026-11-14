import { describe, expect, it } from 'vitest'
import { dateProblem, latestOn, weekdayOf } from './dates'

// T45: the browser-side date checks mirror the API's SessionDatePolicy. Wednesday 11/11/2026 is "today".
const term = { min: '2026-10-01', max: '2026-11-11' }

describe('attendance dates', () => {
  it('reads weekdays in the API numbering', () => {
    expect(weekdayOf('2026-11-09')).toBe(1) // Monday
    expect(weekdayOf('2026-11-14')).toBe(6) // Saturday
    expect(weekdayOf('2026-11-15')).toBe(0) // Sunday
  })

  it('finds the latest date of a weekday up to today', () => {
    expect(latestOn(3, term.min, term.max)).toBe('2026-11-11') // today is a Wednesday
    expect(latestOn(1, term.min, term.max)).toBe('2026-11-09')
    expect(latestOn(4, term.min, term.max)).toBe('2026-11-05')
  })

  it('finds nothing when that weekday has not come yet this term', () => {
    expect(latestOn(5, '2026-11-10', '2026-11-12')).toBeNull()
  })

  it('accepts a meeting day inside the term, up to today', () => {
    expect(dateProblem('2026-11-09', 1, term.min, term.max)).toBeNull()
    expect(dateProblem('2026-11-11', 3, term.min, term.max)).toBeNull()
  })

  it('refuses a future date, a date before the term and the wrong weekday', () => {
    expect(dateProblem('2026-11-16', 1, term.min, term.max)).toBe('Attendance can’t be taken for a future date.')
    expect(dateProblem('2026-09-28', 1, term.min, term.max)).toBe('Choose a date inside the term (from 01/10/2026).')
    expect(dateProblem('2026-11-10', 1, term.min, term.max)).toBe('This session is on Monday. Choose a Monday.')
  })
})
