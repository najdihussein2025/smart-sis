// TODO: replace with generated types from `npm run gen:api` (ChatReplyDto, ConversationDto, ...).

export type ChatMode = 'Assistant' | 'Tutor'

export interface ConversationSummary {
  id: number
  title: string
  mode: ChatMode
  courseId: number | null
  courseCode: string | null
  createdAt: string
  updatedAt: string
}

export interface ChatMessage {
  id: number
  role: 'User' | 'Assistant'
  content: string
  createdAt: string
}

export interface Conversation {
  conversation: ConversationSummary
  messages: ChatMessage[]
}

export interface ChatReply {
  conversation: ConversationSummary
  userMessage: ChatMessage
  assistantMessage: ChatMessage
}

export interface SendMessageRequest {
  message: string
  conversationId?: number
  mode?: ChatMode
  courseId?: number
}
