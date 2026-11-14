import { describe, expect, it, vi } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, waitFor, within } from '@testing-library/react'
import { server } from '@/test/server'
import { profileFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'
import type { KnowledgeDocument } from '../types'

// PB-15 / T63: admin knowledge base. Simulated documents only (SR-23).

const doc = (id: number, title: string, extra: Partial<KnowledgeDocument> = {}): KnowledgeDocument => ({
  id, title, status: 'Ready', chunks: 4, canReingest: true, uploadedByName: 'Demo Admin',
  createdAt: '2026-11-01T10:00:00Z', updatedAt: '2026-11-01T10:00:00Z', ...extra,
})

function mockKnowledge(initial: KnowledgeDocument[]) {
  let docs = initial
  const uploads: { title: string; fileName: string }[] = []
  // jsdom's FormData doesn't survive into MSW's Request, so the sent fields are read from FormData.append calls.
  vi.restoreAllMocks()
  const append = vi.spyOn(FormData.prototype, 'append')
  server.use(
    http.get('/api/profile', () => HttpResponse.json(profileFor('Admin'))),
    http.get('/api/admin/knowledge', () => HttpResponse.json(docs)),
    http.post('/api/admin/knowledge', () => {
      const field = (name: string) => append.mock.calls.filter((c) => c[0] === name).at(-1)?.[1]
      const title = String(field('title'))
      uploads.push({ title, fileName: (field('file') as File).name })
      const created = doc(50, title, { chunks: 2 })
      docs = [created, ...docs]
      return HttpResponse.json(created, { status: 201 })
    }),
    http.post('/api/admin/knowledge/:id/reingest', ({ params }) => {
      docs = docs.map((d) => (d.id === Number(params.id) ? { ...d, status: 'Ready' as const } : d))
      return HttpResponse.json(docs.find((d) => d.id === Number(params.id)))
    }),
    http.delete('/api/admin/knowledge/:id', ({ params }) => {
      docs = docs.filter((d) => d.id !== Number(params.id))
      return new HttpResponse(null, { status: 204 })
    }),
  )
  return { uploads }
}

const textFile = (name: string, content = 'Make-up exams need a medical note.') => new File([content], name, { type: 'text/plain' })

describe('KnowledgePage', () => {
  it('lists documents with their status', async () => {
    mockKnowledge([doc(1, 'Academic policies'), doc(2, 'Library FAQ', { status: 'Failed', chunks: 0 })])
    renderAt('/admin/knowledge', { as: 'Admin' })

    const row = (await screen.findByRole('rowheader', { name: 'Library FAQ' })).closest('tr')!
    expect(within(row).getByText('Failed')).toBeInTheDocument()
    expect(screen.getByRole('rowheader', { name: 'Academic policies' })).toBeInTheDocument()
  })

  it('uploads a document', async () => {
    const { uploads } = mockKnowledge([])
    const { user } = renderAt('/admin/knowledge', { as: 'Admin' })
    expect(await screen.findByText('No documents yet')).toBeInTheDocument()

    await user.type(screen.getByLabelText('Title'), 'Exam rules')
    await user.upload(screen.getByLabelText(/^File/), textFile('exam-rules.md'))
    await user.click(screen.getByRole('button', { name: 'Upload' }))

    expect(await screen.findByText('Exam rules uploaded. The assistant can use it now.')).toBeInTheDocument()
    expect(uploads).toEqual([{ title: 'Exam rules', fileName: 'exam-rules.md' }])
    expect(await screen.findByRole('rowheader', { name: 'Exam rules' })).toBeInTheDocument()
  })

  it('asks for a title and a file', async () => {
    const { uploads } = mockKnowledge([])
    const { user } = renderAt('/admin/knowledge', { as: 'Admin' })
    await user.click(await screen.findByRole('button', { name: 'Upload' }))
    expect(screen.getByText('Enter a title.')).toBeInTheDocument()
    expect(screen.getByText('Choose a file.')).toBeInTheDocument()
    expect(uploads).toHaveLength(0)
  })

  it('rejects a file over 1 MB', async () => {
    mockKnowledge([])
    const { user } = renderAt('/admin/knowledge', { as: 'Admin' })
    await user.type(await screen.findByLabelText('Title'), 'Huge')
    await user.upload(screen.getByLabelText(/^File/), textFile('huge.txt', 'x'.repeat(1_000_001)))
    await user.click(screen.getByRole('button', { name: 'Upload' }))
    expect(await screen.findByText('The file must be 1 MB or smaller.')).toBeInTheDocument()
  })

  it('shows the API field error under the field', async () => {
    mockKnowledge([])
    server.use(
      http.post('/api/admin/knowledge', () =>
        HttpResponse.json({ title: 'Some fields are not valid.', errors: { file: ['Upload a .txt or .md file.'] } }, { status: 400 }),
      ),
    )
    const { user } = renderAt('/admin/knowledge', { as: 'Admin' })
    await user.type(await screen.findByLabelText('Title'), 'Rules')
    await user.upload(screen.getByLabelText(/^File/), textFile('rules.txt'))
    await user.click(screen.getByRole('button', { name: 'Upload' }))
    expect(await screen.findByText('Upload a .txt or .md file.')).toBeInTheDocument()
  })

  it('re-ingests a failed document and deletes one after confirming', async () => {
    mockKnowledge([doc(1, 'Academic policies'), doc(2, 'Library FAQ', { status: 'Failed', chunks: 0 })])
    const { user } = renderAt('/admin/knowledge', { as: 'Admin' })

    await user.click(await screen.findByRole('button', { name: 'Re-ingest Library FAQ' }))
    expect(await screen.findByText('Library FAQ is ready.')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Delete Academic policies' }))
    const confirm = await screen.findByRole('dialog', { name: 'Delete document?' })
    await user.click(within(confirm).getByRole('button', { name: 'Delete' }))
    await waitFor(() => expect(screen.queryByRole('rowheader', { name: 'Academic policies' })).not.toBeInTheDocument())
  })

  it('shows an error with retry', async () => {
    server.use(
      http.get('/api/profile', () => HttpResponse.json(profileFor('Admin'))),
      http.get('/api/admin/knowledge', () => HttpResponse.json({ title: 'Could not load documents.' }, { status: 500 })),
    )
    renderAt('/admin/knowledge', { as: 'Admin' })
    expect(await screen.findByText('Something went wrong on our side. Please try again in a moment.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })
})
