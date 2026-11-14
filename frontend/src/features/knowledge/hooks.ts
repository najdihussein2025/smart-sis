import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { knowledgeApi } from './api'

export const knowledgeKeys = { all: ['knowledge'] as const }

export const useKnowledgeDocuments = () => useQuery({ queryKey: knowledgeKeys.all, queryFn: knowledgeApi.list })

function useKnowledgeMutation<A, R>(fn: (arg: A) => Promise<R>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: fn,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: knowledgeKeys.all }),
  })
}

export const useUploadDocument = () => useKnowledgeMutation(knowledgeApi.upload)
export const useReingestDocument = () => useKnowledgeMutation(knowledgeApi.reingest)
export const useDeleteDocument = () => useKnowledgeMutation(knowledgeApi.remove)
