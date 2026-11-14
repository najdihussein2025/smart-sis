import { api } from '@/shared/api/client'
import type { ChatReply, Conversation, ConversationSummary, SendMessageRequest } from './types'

export const assistantApi = {
  list: () => api.get<ConversationSummary[]>('/api/chat/conversations').then((r) => r.data),
  get: (id: number) => api.get<Conversation>(`/api/chat/conversations/${id}`).then((r) => r.data),
  send: (body: SendMessageRequest) => api.post<ChatReply>('/api/chat/messages', body).then((r) => r.data),
  rename: (id: number, title: string) =>
    api.put<ConversationSummary>(`/api/chat/conversations/${id}`, { title }).then((r) => r.data),
  remove: (id: number) => api.delete(`/api/chat/conversations/${id}`).then(() => undefined),
}
