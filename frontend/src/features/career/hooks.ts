import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useAddToCart } from '@/features/cart/hooks'
import { careerAdminApi, careerApi } from './api'
import type { SaveCareerPathRequest, SaveQuestionRequest, SaveTraitRequest, SubmitAssessmentRequest } from './types'

export const careerKeys = {
  all: ['career'] as const,
  assessments: ['career', 'assessments'] as const,
  quiz: ['career', 'quiz'] as const,
  recommendations: ['career', 'recommendations'] as const,
  careers: ['career', 'careers'] as const,
  roadmap: (id: number) => ['career', 'roadmap', id] as const,
}

export const useMyAssessments = () => useQuery({ queryKey: careerKeys.assessments, queryFn: careerApi.assessments })

/** Loaded only when the student starts the quiz; never cached between attempts. */
export const useQuiz = (enabled: boolean) =>
  useQuery({ queryKey: careerKeys.quiz, queryFn: careerApi.quiz, enabled, gcTime: 0, staleTime: Infinity })

export function useSubmitAssessment() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: SubmitAssessmentRequest) => careerApi.submit(body),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: careerKeys.all }),
  })
}

export const useRecommendations = (enabled: boolean) =>
  useQuery({ queryKey: careerKeys.recommendations, queryFn: careerApi.recommendations, enabled })

export const useCareerList = () => useQuery({ queryKey: careerKeys.careers, queryFn: careerApi.careers })

export const useRoadmap = (careerId: number | undefined) =>
  useQuery({ queryKey: careerKeys.roadmap(careerId ?? 0), queryFn: () => careerApi.roadmap(careerId!), enabled: careerId !== undefined })

/** The cart's own "Add to cart", plus a refresh of the career views so the button turns into "In Cart". */
export function useCareerAddToCart() {
  const queryClient = useQueryClient()
  const add = useAddToCart()
  return {
    ...add,
    mutate: (sectionId: number) =>
      add.mutate(sectionId, { onSuccess: () => void queryClient.invalidateQueries({ queryKey: careerKeys.all }) }),
  }
}

/** Admin */

export const careerAdminKeys = {
  all: ['career-admin'] as const,
  traits: ['career-admin', 'traits'] as const,
  questions: ['career-admin', 'questions'] as const,
  careers: ['career-admin', 'careers'] as const,
  courseTags: ['career-admin', 'course-tags'] as const,
}

export const useTraits = () => useQuery({ queryKey: careerAdminKeys.traits, queryFn: careerAdminApi.traits })
export const useQuestionBank = () => useQuery({ queryKey: careerAdminKeys.questions, queryFn: careerAdminApi.questions })
export const useAdminCareers = () => useQuery({ queryKey: careerAdminKeys.careers, queryFn: careerAdminApi.careers })
export const useCourseTags = () => useQuery({ queryKey: careerAdminKeys.courseTags, queryFn: careerAdminApi.courseTags })

/** Every admin save refreshes all admin career lists (trait names show in questions, careers and tags). */
function useAdminMutation<A, R>(fn: (args: A) => Promise<R>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: fn,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: careerAdminKeys.all }),
  })
}

export const useSaveTrait = () =>
  useAdminMutation(({ id, body }: { id?: number; body: SaveTraitRequest }) =>
    id === undefined ? careerAdminApi.createTrait(body) : careerAdminApi.updateTrait(id, body))

export const useSaveQuestion = () =>
  useAdminMutation(({ id, body }: { id?: number; body: SaveQuestionRequest }) =>
    id === undefined ? careerAdminApi.createQuestion(body) : careerAdminApi.updateQuestion(id, body))

export const useDeleteQuestion = () => useAdminMutation((id: number) => careerAdminApi.deleteQuestion(id))

export const useSaveCareer = () =>
  useAdminMutation(({ id, body }: { id?: number; body: SaveCareerPathRequest }) =>
    id === undefined ? careerAdminApi.createCareer(body) : careerAdminApi.updateCareer(id, body))

export const useSaveCourseTags = () =>
  useAdminMutation(({ courseId, traitIds }: { courseId: number; traitIds: number[] }) =>
    careerAdminApi.saveCourseTags(courseId, traitIds))
