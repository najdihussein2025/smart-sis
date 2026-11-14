// TODO: replace with generated types from `npm run gen:api` (Grading DTOs).

/** Enrollment status as sent by the API. */
export type EnrollmentStatus = 'Enrolled' | 'Dropped' | 'DroppedAbsence' | 'Completed'

// ---- Admin: grade components (PB-08) ----

export interface GradeComponent {
  id: number
  courseId: number
  termId: number
  name: string
  maxGrade: number
  /** Percent of the final grade. */
  weight: number
  /** Scores already entered; a component with scores can't be deleted. */
  scoreCount: number
}

export interface GradeComponentList {
  courseId: number
  termId: number
  totalWeight: number
  /** True once a section of the course was finalized this term: components are locked. */
  isFinalized: boolean
  items: GradeComponent[]
}

export interface CreateGradeComponentRequest {
  courseId: number
  termId: number
  name: string
  maxGrade: number
  weight: number
}

export type UpdateGradeComponentRequest = Pick<CreateGradeComponentRequest, 'name' | 'maxGrade' | 'weight'>

// ---- Instructor: grade entry (PB-08) ----

export interface TeachingSection {
  sectionId: number
  courseCode: string
  courseName: string
  sectionCode: string
  termName: string
  studentCount: number
  componentCount: number
  totalWeight: number
  notGraded: number
  isFinalized: boolean
}

export interface GradingComponent {
  id: number
  name: string
  maxGrade: number
  weight: number
  graded: number
  notGraded: number
}

export interface GradingSection {
  sectionId: number
  courseCode: string
  courseName: string
  sectionCode: string
  termName: string
  studentCount: number
  totalWeight: number
  notGraded: number
  isFinalized: boolean
  components: GradingComponent[]
}

export interface RosterStudent {
  enrollmentId: number
  universityId: number
  fullName: string
  status: EnrollmentStatus
  /** Null when not graded yet. */
  score: number | null
}

export interface ComponentRoster {
  sectionId: number
  courseCode: string
  sectionCode: string
  isFinalized: boolean
  component: GradingComponent
  students: RosterStudent[]
}

export interface SaveScoresRequest {
  /** Null clears a score. */
  scores: { enrollmentId: number; score: number | null }[]
}

export interface FinalizedStudent {
  enrollmentId: number
  universityId: number
  fullName: string
  finalPct: number
  letterGrade: string
  gradePoints: number
}

export interface FinalizeResult {
  sectionId: number
  students: FinalizedStudent[]
}

// ---- Student: grades and transcript (PB-09) ----

export interface ComponentGrade {
  name: string
  weight: number
  maxGrade: number
  /** Null = "Not graded". */
  score: number | null
  pct: number | null
}

export interface CourseGrades {
  sectionId: number
  courseCode: string
  courseName: string
  sectionCode: string
  credits: number
  status: EnrollmentStatus
  /** Σ weight graded so far (0–100), for the progress bar. */
  gradedWeight: number
  /** Points earned so far out of 100. */
  earned: number
  /** Average on what is graded so far; null when nothing is graded. */
  runningPct: number | null
  finalPct: number | null
  letterGrade: string | null
  gradePoints: number | null
  components: ComponentGrade[]
}

export interface MyGrades {
  termId: number | null
  termName: string | null
  termGpa: number | null
  cumulativeGpa: number | null
  creditsEarned: number
  courses: CourseGrades[]
}

export interface TranscriptCourse {
  courseCode: string
  courseName: string
  sectionCode: string
  credits: number
  status: EnrollmentStatus
  finalPct: number | null
  letterGrade: string | null
  gradePoints: number | null
  /** False for an attempt replaced by a later retake. */
  countsInGpa: boolean
}

export interface TranscriptTerm {
  termId: number
  termName: string
  isCurrent: boolean
  termGpa: number | null
  cumulativeGpa: number | null
  creditsAttempted: number
  creditsEarned: number
  courses: TranscriptCourse[]
}

export interface Transcript {
  cumulativeGpa: number | null
  creditsAttempted: number
  creditsEarned: number
  terms: TranscriptTerm[]
}
