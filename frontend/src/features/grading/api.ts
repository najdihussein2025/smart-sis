import { api } from '@/shared/api/client'
import type {
  ComponentRoster, CreateGradeComponentRequest, FinalizeResult, GradeComponent, GradeComponentList, GradingSection, MyGrades,
  SaveScoresRequest, TeachingSection, Transcript, UpdateGradeComponentRequest,
} from './types'

const admin = '/api/admin/grade-components'
const instructor = '/api/instructor/grading/sections'

export const gradingApi = {
  // Admin
  components: (courseId: number, termId: number) =>
    api.get<GradeComponentList>(admin, { params: { courseId, termId } }).then((r) => r.data),
  createComponent: (body: CreateGradeComponentRequest) => api.post<GradeComponent>(admin, body).then((r) => r.data),
  updateComponent: (id: number, body: UpdateGradeComponentRequest) => api.put<GradeComponent>(`${admin}/${id}`, body).then((r) => r.data),
  deleteComponent: (id: number) => api.delete(`${admin}/${id}`).then(() => undefined),

  // Instructor
  teachingSections: () => api.get<TeachingSection[]>(instructor).then((r) => r.data),
  section: (sectionId: number) => api.get<GradingSection>(`${instructor}/${sectionId}`).then((r) => r.data),
  roster: (sectionId: number, componentId: number) =>
    api.get<ComponentRoster>(`${instructor}/${sectionId}/components/${componentId}`).then((r) => r.data),
  saveScores: (sectionId: number, componentId: number, body: SaveScoresRequest) =>
    api.put<ComponentRoster>(`${instructor}/${sectionId}/components/${componentId}`, body).then((r) => r.data),
  finalize: (sectionId: number) => api.post<FinalizeResult>(`${instructor}/${sectionId}/finalize`).then((r) => r.data),

  // Student
  myGrades: () => api.get<MyGrades>('/api/grades/my').then((r) => r.data),
  transcript: () => api.get<Transcript>('/api/grades/transcript').then((r) => r.data),
}
