import type { AbsenceAlertLevel, AbsenceStanding, AttendanceSheetRow, AttendanceStatus } from './types'

/** Rule A5: 0 absences → Perfect; 1 … limit−2 → Warning; limit−1 and up → Critical. */
export function standingOf(absences: number, limit: number): AbsenceStanding {
  if (absences <= 0) return 'Perfect'
  return absences >= limit - 1 ? 'Critical' : 'Warning'
}

/**
 * Rules A2 / A3: the alert a student reaches with `absences` after this session: Warning at limit−1, Limit at
 * limit, auto-drop at limit+1 (or more). Null below limit−1.
 */
export function alertLevelAt(absences: number, limit: number): AbsenceAlertLevel | null {
  if (absences > limit) return 'AutoDropped'
  if (absences === limit) return 'Limit'
  if (absences === limit - 1) return 'Warning'
  return null
}

export interface ProjectedAlert {
  enrollmentId: number
  fullName: string
  level: AbsenceAlertLevel
  /** Absences once this session is submitted. */
  absences: number
}

/** Students this submission pushes to Warning, Limit or auto-drop: only those marked Absent now. */
export function projectedAlerts(
  students: AttendanceSheetRow[],
  marks: Record<number, AttendanceStatus>,
  limit: number,
): ProjectedAlert[] {
  return students.flatMap((s) => {
    if (marks[s.enrollmentId] !== 'Absent') return []
    const absences = s.absences + 1
    const level = alertLevelAt(absences, limit)
    return level ? [{ enrollmentId: s.enrollmentId, fullName: s.fullName, level, absences }] : []
  })
}
