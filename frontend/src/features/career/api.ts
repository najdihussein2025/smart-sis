import { api } from '@/shared/api/client'
import type {
  Assessment, CareerPath, CareerSummary, CourseTags, MyAssessments, Question, QuestionBank, Quiz, Recommendations, Roadmap,
  SaveCareerPathRequest, SaveQuestionRequest, SaveTraitRequest, SubmitAssessmentRequest, Trait,
} from './types'

const student = '/api/career'
const admin = '/api/admin/career'

export const careerApi = {
  assessments: () => api.get<MyAssessments>(`${student}/assessments`).then((r) => r.data),
  quiz: () => api.get<Quiz>(`${student}/quiz`).then((r) => r.data),
  submit: (body: SubmitAssessmentRequest) => api.post<Assessment>(`${student}/assessments`, body).then((r) => r.data),
  recommendations: () => api.get<Recommendations>(`${student}/recommendations`).then((r) => r.data),
  careers: () => api.get<CareerSummary[]>(`${student}/careers`).then((r) => r.data),
  roadmap: (careerId: number) => api.get<Roadmap>(`${student}/careers/${careerId}/roadmap`).then((r) => r.data),
}

export const careerAdminApi = {
  traits: () => api.get<Trait[]>(`${admin}/traits`).then((r) => r.data),
  createTrait: (body: SaveTraitRequest) => api.post<Trait>(`${admin}/traits`, body).then((r) => r.data),
  updateTrait: (id: number, body: SaveTraitRequest) => api.put<Trait>(`${admin}/traits/${id}`, body).then((r) => r.data),

  questions: () => api.get<QuestionBank>(`${admin}/questions`).then((r) => r.data),
  createQuestion: (body: SaveQuestionRequest) => api.post<Question>(`${admin}/questions`, body).then((r) => r.data),
  updateQuestion: (id: number, body: SaveQuestionRequest) => api.put<Question>(`${admin}/questions/${id}`, body).then((r) => r.data),
  deleteQuestion: (id: number) => api.delete(`${admin}/questions/${id}`).then(() => undefined),

  careers: () => api.get<CareerPath[]>(`${admin}/careers`).then((r) => r.data),
  createCareer: (body: SaveCareerPathRequest) => api.post<CareerPath>(`${admin}/careers`, body).then((r) => r.data),
  updateCareer: (id: number, body: SaveCareerPathRequest) => api.put<CareerPath>(`${admin}/careers/${id}`, body).then((r) => r.data),

  courseTags: () => api.get<CourseTags[]>(`${admin}/course-tags`).then((r) => r.data),
  saveCourseTags: (courseId: number, traitIds: number[]) =>
    api.put<CourseTags>(`${admin}/course-tags/${courseId}`, { traitIds }).then((r) => r.data),
}
