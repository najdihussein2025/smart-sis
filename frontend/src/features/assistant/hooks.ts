import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { assistantApi } from './api'
import type { ChatReply, Conversation } from './types'

export const assistantKeys = {
  list: ['assistant', 'conversations'] as const,
  one: (id: number) => ['assistant', 'conversation', id] as const,
}

export const useConversations = () => useQuery({ queryKey: assistantKeys.list, queryFn: assistantApi.list })

export const useConversation = (id: number | null) =>
  useQuery({
    queryKey: assistantKeys.one(id ?? 0),
    queryFn: () => assistantApi.get(id!),
    enabled: id !== null,
  })

/** The reply carries both saved messages, so the open conversation is updated in place (no refetch flicker). */
export function useSendMessage() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: assistantApi.send,
    onSuccess: (reply: ChatReply) => {
      queryClient.setQueryData<Conversation>(assistantKeys.one(reply.conversation.id), (old) => ({
        conversation: reply.conversation,
        messages: [...(old?.messages ?? []), reply.userMessage, reply.assistantMessage],
      }))
      void queryClient.invalidateQueries({ queryKey: assistantKeys.list })
    },
  })
}

export function useRenameConversation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, title }: { id: number; title: string }) => assistantApi.rename(id, title),
    onSuccess: (summary) => {
      queryClient.setQueryData<Conversation>(assistantKeys.one(summary.id), (old) => (old ? { ...old, conversation: summary } : old))
      void queryClient.invalidateQueries({ queryKey: assistantKeys.list })
    },
  })
}

export function useDeleteConversation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: assistantApi.remove,
    onSuccess: (_, id) => {
      queryClient.removeQueries({ queryKey: assistantKeys.one(id) })
      void queryClient.invalidateQueries({ queryKey: assistantKeys.list })
    },
  })
}
