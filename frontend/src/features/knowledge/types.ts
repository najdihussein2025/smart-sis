// TODO: replace with generated types from `npm run gen:api` (KnowledgeDocumentDto).

export type KnowledgeStatus = 'Processing' | 'Ready' | 'Failed'

export interface KnowledgeDocument {
  id: number
  title: string
  status: KnowledgeStatus
  chunks: number
  canReingest: boolean
  uploadedByName: string | null
  createdAt: string
  updatedAt: string
}
