import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { gradingApi } from './api'
import type { CreateGradeComponentRequest, SaveScoresRequest } from './types'

export const gradingKeys = {
  all: ['grading'] as const,
  components: (courseId: number, termId: number) => ['grading', 'components', courseId, termId] as const,
  teaching: ['grading', 'teaching'] as const,
  section: (sectionId: number) => ['grading', 'section', sectionId] as const,
  roster: (sectionId: number, componentId: number) => ['grading', 'roster', sectionId, componentId] as const,
  my: ['grading', 'my'] as const,
  transcript: ['grading', 'transcript'] as const,
}

// ---- Admin ----

export const useGradeComponents = (courseId: number | undefined, termId: number | undefined) =>
  useQuery({
    queryKey: gradingKeys.components(courseId ?? 0, termId ?? 0),
    queryFn: () => gradingApi.components(courseId!, termId!),
    enabled: !!courseId && !!termId,
  })

/** Create when `id` is undefined, otherwise update. */
export function useSaveGradeComponent() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, body }: { id?: number; body: CreateGradeComponentRequest }) =>
      id === undefined
        ? gradingApi.createComponent(body)
        : gradingApi.updateComponent(id, { name: body.name, maxGrade: body.maxGrade, weight: body.weight }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: gradingKeys.all }),
  })
}

export function useDeleteGradeComponent() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => gradingApi.deleteComponent(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: gradingKeys.all }),
  })
}

// ---- Instructor ----

export const useTeachingSections = () => useQuery({ queryKey: gradingKeys.teaching, queryFn: gradingApi.teachingSections })

export const useGradingSection = (sectionId: number | undefined) =>
  useQuery({ queryKey: gradingKeys.section(sectionId ?? 0), queryFn: () => gradingApi.section(sectionId!), enabled: !!sectionId })

export const useRoster = (sectionId: number | undefined, componentId: number | undefined) =>
  useQuery({
    queryKey: gradingKeys.roster(sectionId ?? 0, componentId ?? 0),
    queryFn: () => gradingApi.roster(sectionId!, componentId!),
    enabled: !!sectionId && !!componentId,
  })

/** Saves one component's scores and refreshes the counts ("X not graded") everywhere. */
export function useSaveScores() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ sectionId, componentId, body }: { sectionId: number; componentId: number; body: SaveScoresRequest }) =>
      gradingApi.saveScores(sectionId, componentId, body),
    onSuccess: (roster) => {
      queryClient.setQueryData(gradingKeys.roster(roster.sectionId, roster.component.id), roster)
      return queryClient.invalidateQueries({ queryKey: gradingKeys.all })
    },
  })
}

export function useFinalizeSection() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (sectionId: number) => gradingApi.finalize(sectionId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: gradingKeys.all }),
  })
}

// ---- Student ----

export const useMyGrades = () => useQuery({ queryKey: gradingKeys.my, queryFn: gradingApi.myGrades })

export const useTranscript = (enabled = true) => useQuery({ queryKey: gradingKeys.transcript, queryFn: gradingApi.transcript, enabled })
