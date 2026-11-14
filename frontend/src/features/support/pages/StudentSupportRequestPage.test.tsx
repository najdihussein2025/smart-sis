import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, waitFor, within } from '@testing-library/react'
import { server } from '@/test/server'
import { profileFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'
import type { SupportRequestDetail } from '../types'

// PB-13 / T51: one request with its thread and the reply box. Simulated data (SR-23).

const request = (extra: Partial<SupportRequestDetail> = {}): SupportRequestDetail => ({
  id: 7, categoryId: 1, categoryName: 'Registration', subject: 'Cannot add CSC 310',
  description: 'The catalog says CSC 310 needs CSC 210.', status: 'InProgress',
  createdAt: '2026-11-01T10:00:00Z', resolvedAt: null,
  messages: [
    { id: 1, authorId: 1, authorName: 'Demo Admin', authorRole: 'Admin', body: 'We are checking.', createdAt: '2026-11-01T11:00:00Z' },
  ],
  ...extra,
})

function mockRequest(initial: SupportRequestDetail) {
  const replies: string[] = []
  let current = initial
  server.use(
    http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))),
    http.get('/api/support/requests/7', () => HttpResponse.json(current)),
    http.post('/api/support/requests/7/messages', async ({ request: req }) => {
      const { body } = (await req.json()) as { body: string }
      replies.push(body)
      current = {
        ...current,
        status: current.status === 'Resolved' ? 'Open' : current.status,
        messages: [...current.messages, { id: 2, authorId: 3, authorName: 'Demo Student', authorRole: 'Student', body, createdAt: '2026-11-02T09:00:00Z' }],
      }
      return HttpResponse.json(current)
    }),
  )
  return { replies }
}

describe('StudentSupportRequestPage', () => {
  it('shows the request, its status and the thread, and sends a reply', async () => {
    const { replies } = mockRequest(request())
    const { user } = renderAt('/student/support/7', { as: 'Student' })

    expect(await screen.findByRole('heading', { name: 'Cannot add CSC 310' })).toBeInTheDocument()
    expect(screen.getByText('In progress')).toBeInTheDocument()
    const thread = screen.getByRole('list', { name: 'Conversation' })
    expect(within(thread).getByText('The catalog says CSC 310 needs CSC 210.')).toBeInTheDocument()
    expect(within(thread).getByText('Demo Admin · Support')).toBeInTheDocument()

    await user.type(screen.getByLabelText('Your reply'), ' Thanks! ')
    await user.click(screen.getByRole('button', { name: 'Send reply' }))

    expect(await within(thread).findByText('Thanks!')).toBeInTheDocument()
    expect(replies).toEqual(['Thanks!'])
    await waitFor(() => expect(screen.getByLabelText('Your reply')).toHaveValue(''))
  })

  it('asks for a reply before sending', async () => {
    const { replies } = mockRequest(request())
    const { user } = renderAt('/student/support/7', { as: 'Student' })
    await user.click(await screen.findByRole('button', { name: 'Send reply' }))
    expect(await screen.findByText('Write a reply.')).toBeInTheDocument()
    expect(replies).toHaveLength(0)
  })

  it('shows the API’s 409 above the reply box and clears it on typing', async () => {
    mockRequest(request())
    server.use(
      http.post('/api/support/requests/7/messages', () =>
        HttpResponse.json(
          { status: 409, title: 'This request is closed, so it can’t take more replies.', code: 'request_closed' },
          { status: 409 },
        ),
      ),
    )
    const { user } = renderAt('/student/support/7', { as: 'Student' })
    await user.type(await screen.findByLabelText('Your reply'), 'Hello')
    await user.click(screen.getByRole('button', { name: 'Send reply' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('This request is closed')
    await user.type(screen.getByLabelText('Your reply'), '!')
    await waitFor(() => expect(screen.queryByRole('alert')).not.toBeInTheDocument())
  })

  it('has no reply box once the request is closed', async () => {
    mockRequest(request({ status: 'Closed', resolvedAt: '2026-11-03T10:00:00Z' }))
    renderAt('/student/support/7', { as: 'Student' })
    expect(await screen.findByText(/This request is closed/)).toBeInTheDocument()
    expect(screen.queryByLabelText('Your reply')).not.toBeInTheDocument()
  })

  it('tells the student a reply reopens a resolved request', async () => {
    mockRequest(request({ status: 'Resolved' }))
    renderAt('/student/support/7', { as: 'Student' })
    expect(await screen.findByText('Replying reopens this request.')).toBeInTheDocument()
  })

  it('shows not found from the API with a retry', async () => {
    server.use(
      http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))),
      http.get('/api/support/requests/7', () => HttpResponse.json({ status: 404, title: 'Support request 7 was not found.' }, { status: 404 })),
    )
    renderAt('/student/support/7', { as: 'Student' })
    expect(await screen.findByText('Support request 7 was not found.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })
})
