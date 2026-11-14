// TODO: replace with generated types from `npm run gen:api` (Career DTOs).

/** Student */

export interface TraitScore {
  traitId: number
  traitName: string
  points: number
  maxPoints: number
  score: number
}

export interface Assessment {
  id: number
  termId: number
  termName: string
  submittedAt: string
  traits: TraitScore[]
}

export interface MyAssessments {
  currentTermId: number | null
  currentTermName: string | null
  canTake: boolean
  reason: string | null
  attempts: Assessment[]
}

export interface QuizOption {
  id: number
  text: string
}

export interface QuizQuestion {
  id: number
  text: string
  options: QuizOption[]
}

export interface Quiz {
  termName: string
  questions: QuizQuestion[]
}

export interface SubmitAssessmentRequest {
  answers: { questionId: number; optionId: number }[]
}

export type SectionStatus = 'Available' | 'InCart' | 'Enrolled'

export interface CareerSection {
  sectionId: number
  sectionCode: string
  seatsLeft: number
  status: SectionStatus
}

export interface CareerMatch {
  careerId: number
  name: string
  description: string | null
  matchPct: number
  quizPct: number
  gradePct: number | null
  topTraits: string[]
}

export interface SuggestedCourse {
  courseId: number
  code: string
  name: string
  credits: number
  careers: string[]
  sections: CareerSection[]
}

export interface Recommendations {
  assessmentId: number
  termName: string
  submittedAt: string
  usesGrades: boolean
  careers: CareerMatch[]
  explanation: string
  explanationSource: 'Ai' | 'Fallback'
  suggestedCourses: SuggestedCourse[]
}

export interface CareerSummary {
  id: number
  name: string
  description: string | null
}

export type RoadmapStepStatus = 'Done' | 'InProgress' | 'Next' | 'Later' | 'Milestone'

export interface RoadmapCourse {
  id: number
  code: string
  name: string
  credits: number
  sections: CareerSection[]
}

export interface RoadmapStep {
  stepOrder: number
  title: string
  description: string | null
  status: RoadmapStepStatus
  course: RoadmapCourse | null
}

export interface Roadmap {
  careerId: number
  name: string
  description: string | null
  doneCount: number
  courseStepCount: number
  steps: RoadmapStep[]
}

/** Admin */

export interface Trait {
  id: number
  name: string
  description: string | null
}

export interface SaveTraitRequest {
  name: string
  description: string | null
}

export interface TraitPoints {
  traitId: number
  traitName: string
  points: number
}

export interface QuestionOption {
  id: number
  text: string
  points: TraitPoints[]
}

export interface Question {
  id: number
  text: string
  isActive: boolean
  answerCount: number
  options: QuestionOption[]
}

export interface QuestionBank {
  activeCount: number
  minQuestions: number
  maxQuestions: number
  questions: Question[]
}

export interface SaveQuestionRequest {
  text: string
  isActive: boolean
  options: { id: number | null; text: string; points: { traitId: number; points: number }[] }[]
}

export interface TraitWeight {
  traitId: number
  traitName: string
  weight: number
}

export interface AdminRoadmapStep {
  id: number
  stepOrder: number
  title: string
  description: string | null
  courseId: number | null
  courseCode: string | null
  courseName: string | null
}

export interface CareerPath {
  id: number
  name: string
  description: string | null
  isActive: boolean
  traits: TraitWeight[]
  steps: AdminRoadmapStep[]
}

export interface SaveCareerPathRequest {
  name: string
  description: string | null
  isActive: boolean
  traits: { traitId: number; weight: number }[]
  steps: { title: string; description: string | null; courseId: number | null }[]
}

export interface CourseTags {
  courseId: number
  code: string
  name: string
  traitIds: number[]
}
