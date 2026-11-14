import { api } from '@/shared/api/client'
import type { KnowledgeDocument } from './types'

const base = '/api/admin/knowledge'

export const knowledgeApi = {
  list: () => api.get<KnowledgeDocument[]>(base).then((r) => r.data),
  upload: ({ title, file }: { title: string; file: File }) => {
    const form = new FormData()
    form.append('title', title)
    form.append('file', file)
    // The client defaults to JSON; multipart lets the browser add the boundary.
    return api.post<KnowledgeDocument>(base, form, { headers: { 'Content-Type': 'multipart/form-data' } }).then((r) => r.data)
  },
  reingest: (id: number) => api.post<KnowledgeDocument>(`${base}/${id}/reingest`).then((r) => r.data),
  remove: (id: number) => api.delete(`${base}/${id}`).then(() => undefined),
}
