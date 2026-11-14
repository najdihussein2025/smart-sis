// TODO: replace with generated types from `npm run gen:api` (StudentDashboardDto, InstructorDashboardDto, AdminDashboardDto).

/** GET /api/dashboard/student. Money is in USD; dates are DateOnly strings ("2026-09-01"). */
export interface StudentDashboard {
  termName: string | null
  /** Null until the student has a finalized course. */
  cumulativeGpa: number | null
  termGpa: number | null
  creditsEarned: number
  enrolledCredits: number
  maxCredits: number
  classesToday: number
  outstandingBalance: number
  overdueBalance: number
  nextDueDate: string | null
  unreadNotifications: number
  /** Enrolled courses this term with a Warning or Critical attendance standing. */
  attendanceWarnings: number
  attendanceCritical: number
}

/** GET /api/dashboard/instructor. */
export interface InstructorDashboard {
  termName: string | null
  today: string
  activeSections: number
  classesToday: number
  missingScores: number
  sectionsWithMissingScores: number
  /** Meetings from Monday up to today with no attendance session yet. */
  attendanceToTake: number
}

/** GET /api/dashboard/admin. */
export interface AdminDashboard {
  termName: string | null
  activeStudents: number
  activeInstructors: number
  sectionsThisTerm: number
  openSupportRequests: number
  newSupportRequests: number
  unpaidCharges: number
  outstandingTotal: number
}
