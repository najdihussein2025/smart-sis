import { describe, expect, it } from 'vitest'
import { alertLevelAt, projectedAlerts, standingOf } from './standing'

// Rules A2, A3, A5 with a 3-credit course (limit 9). Simulated data (SR-23).

describe('standingOf', () => {
  it.each([
    [0, 'Perfect'],
    [1, 'Warning'],
    [7, 'Warning'],
    [8, 'Critical'],
    [9, 'Critical'],
  ] as const)('%i absences → %s', (absences, standing) => expect(standingOf(absences, 9)).toBe(standing))
})

describe('alertLevelAt', () => {
  it.each([
    [7, null],
    [8, 'Warning'],
    [9, 'Limit'],
    [10, 'AutoDropped'],
  ] as const)('%i absences → %s', (absences, level) => expect(alertLevelAt(absences, 9)).toBe(level))
})

describe('projectedAlerts', () => {
  const row = (enrollmentId: number, absences: number) => ({
    enrollmentId, universityId: 3000 + enrollmentId, fullName: `Demo Student ${enrollmentId}`, status: null, absences,
  })

  it('only counts students marked absent in this session', () => {
    const students = [row(1, 7), row(2, 8), row(3, 9), row(4, 9)]
    const marks = { 1: 'Absent', 2: 'Absent', 3: 'Absent', 4: 'Present' } as const
    expect(projectedAlerts(students, marks, 9)).toEqual([
      { enrollmentId: 1, fullName: 'Demo Student 1', level: 'Warning', absences: 8 },
      { enrollmentId: 2, fullName: 'Demo Student 2', level: 'Limit', absences: 9 },
      { enrollmentId: 3, fullName: 'Demo Student 3', level: 'AutoDropped', absences: 10 },
    ])
  })
})
