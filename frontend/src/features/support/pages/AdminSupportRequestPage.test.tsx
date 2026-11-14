import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, waitFor, within } from '@testing-library/react'
import { server } from '@/test/server'
import { profileFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'
import type { AdminSupportRequestDetail, SupportStatus } from '../types'

// PB-14 / T54: one request in the admin inbox — status, assignee, thread and reply. Simulated data (SR-23).

const detail = (extra: Partial<AdminSupportRequestDetail> = {}): AdminSupportRequestDetail => ({
  id: 7, subject: 'Cannot add CSC 310', description: 'The catalog says CSC 310 needs CSC 210.', categoryId: 1,
  categoryName: 'Registration', status: 'Open', studentId: 3, studentUniversityId: 3001, studentName: 'Demo Student',
  studentEmail: 'student@smartsis.local', assignedAdminId: null, assignedAdminName: null,
  createdAt: '2026-11-01T10:00:00Z', resolvedAt: null,
  messages: [{ id: 1, authorId: 3, authorName: 'Demo Student', authorRole: 'Student', body: 'Any news?', createdAt: '2026-11-01T12:00:00Z' }],
  ...extra,
})

const admins = [
  { id: 1, fullName: 'Demo Admin', universityId: 1001 },
  { id: 8, fullName: 'Second Admin', universityId: 1002 },
]

function mockRequest(initial = detail()) {
  let current = initial
  const sent: { what: string; body: unknown }[] = []
  server.use(
    http.get('/api/profile', () => HttpResponse.json(profileFor('Admin'))),
    http.get('/api/admin/support/7', () => HttpResponse.json(current)),
    http.get('/api/admin/support/admins', () => HttpResponse.json(admins)),
    http.put('/api/admin/support/7/status', async ({ request }) => {
      const body = (await request.json()) as { status: SupportStatus }
      sent.push({ what: 'status', body })
      current = { ...current, status: body.status, resolvedAt: body.status === 'Resolved' ? '2026-11-03T10:00:00Z' : null }
      return HttpResponse.json(current)
    }),
    http.put('/api/admin/support/7/assignee', async ({ request }) => {
      const body = (await request.json()) as { adminId: number | null }
      sent.push({ what: 'assign', body })
      current = { ...current, assignedAdminId: body.adminId, assignedAdminName: admins.find((a) => a.id === body.adminId)?.fullName ?? null }
      return HttpResponse.json(current)
    }),
    http.post('/api/admin/support/7/messages', async ({ request }) => {
      const body = (await request.json()) as { body: string }
      sent.push({ what: 'reply', body })
      current = {
        ...current,
        status: current.status === 'Open' ? 'InProgress' : current.status,
        messages: [...current.messages, { id: 2, authorId: 1, authorName: 'Demo Admin', authorRole: 'Admin', body: body.body, createdAt: '2026-11-02T09:00:00Z' }],
      }
      return HttpResponse.json(current)
    }),
  )
  return { sent }
}

describe('AdminSupportRequestPage', () => {
  it('shows the student, status and thread', async () => {
    mockRequest()
    renderAt('/admin/support/7', { as: 'Admin' })

    expect(await screen.findByRole('heading', { name: 'Cannot add CSC 310' })).toBeInTheDocument()
    const student = screen.getByRole('region', { name: 'Student' })
    expect(within(student).getByText('Demo Student')).toBeInTheDocument()
    expect(within(student).getByRole('link', { name: 'student@smartsis.local' })).toBeInTheDocument()
    expect(screen.getByLabelText('Status')).toHaveValue('Open')
    const thread = screen.getByRole('list', { name: 'Conversation' })
    expect(within(thread).getByText('The catalog says CSC 310 needs CSC 210.')).toBeInTheDocument()
    expect(within(thread).getByText('Any news?')).toBeInTheDocument()
  })

  it('changes the status and confirms it', async () => {
    const { sent } = mockRequest()
    const { user } = renderAt('/admin/support/7', { as: 'Admin' })
    await user.selectOptions(await screen.findByLabelText('Status'), 'Resolved')

    expect(await screen.findByText('Status changed to Resolved.')).toBeInTheDocument()
    expect(sent).toEqual([{ what: 'status', body: { status: 'Resolved' } }])
    expect(screen.getByText(/resolved \d/)).toBeInTheDocument()
  })

  it('assigns the request to me and to another admin', async () => {
    const { sent } = mockRequest()
    const { user } = renderAt('/admin/support/7', { as: 'Admin' })
    await user.click(await screen.findByRole('button', { name: 'Assign to me' }))
    expect(await screen.findByText('Assigned to Demo Admin.')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Assign to me' })).not.toBeInTheDocument()

    await user.selectOptions(screen.getByLabelText('Assigned to'), 'Second Admin')
    expect(await screen.findByText('Assigned to Second Admin.')).toBeInTheDocument()
    expect(sent).toEqual([
      { what: 'assign', body: { adminId: 1 } },
      { what: 'assign', body: { adminId: 8 } },
    ])
  })

  it('shows a 400 from assigning', async () => {
    mockRequest()
    server.use(
      http.put('/api/admin/support/7/assignee', () =>
        HttpResponse.json({ status: 400, title: 'Choose an active admin.', errors: { adminId: ['Choose an active admin.'] } }, { status: 400 }),
      ),
    )
    const { user } = renderAt('/admin/support/7', { as: 'Admin' })
    await waitFor(async () => expect(await screen.findByRole('option', { name: 'Second Admin' })).toBeInTheDocument())
    await user.selectOptions(screen.getByLabelText('Assigned to'), 'Second Admin')
    expect(await screen.findByRole('alert')).toHaveTextContent('Choose an active admin.')
  })

  it('replies to the student', async () => {
    const { sent } = mockRequest()
    const { user } = renderAt('/admin/support/7', { as: 'Admin' })
    await user.type(await screen.findByLabelText('Reply to the student'), 'We are checking.')
    await user.click(screen.getByRole('button', { name: 'Send reply' }))

    const thread = screen.getByRole('list', { name: 'Conversation' })
    expect(await within(thread).findByText('We are checking.')).toBeInTheDocument()
    expect(within(thread).getByText('You')).toBeInTheDocument()
    expect(sent).toEqual([{ what: 'reply', body: { body: 'We are checking.' } }])
    await waitFor(() => expect(screen.getByLabelText('Status')).toHaveValue('InProgress'))
  })

  it('asks for a reply before sending', async () => {
    const { sent } = mockRequest()
    const { user } = renderAt('/admin/support/7', { as: 'Admin' })
    await user.click(await screen.findByRole('button', { name: 'Send reply' }))
    expect(await screen.findByText('Write a reply.')).toBeInTheDocument()
    expect(sent).toHaveLength(0)
  })

  it('shows a 409 from replying', async () => {
    mockRequest()
    server.use(
      http.post('/api/admin/support/7/messages', () =>
        HttpResponse.json({ status: 409, title: 'This request is closed, so it can’t take more replies.', code: 'request_closed' }, { status: 409 }),
      ),
    )
    const { user } = renderAt('/admin/support/7', { as: 'Admin' })
    await user.type(await screen.findByLabelText('Reply to the student'), 'Hello')
    await user.click(screen.getByRole('button', { name: 'Send reply' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('This request is closed')
  })

  it('has no reply box on a closed request', async () => {
    mockRequest(detail({ status: 'Closed', resolvedAt: '2026-11-03T10:00:00Z' }))
    renderAt('/admin/support/7', { as: 'Admin' })
    expect(await screen.findByText(/Change its status to reply again/)).toBeInTheDocument()
    expect(screen.queryByLabelText('Reply to the student')).not.toBeInTheDocument()
  })

  it('shows an error with a retry when the request fails to load', async () => {
    server.use(
      http.get('/api/profile', () => HttpResponse.json(profileFor('Admin'))),
      http.get('/api/admin/support/admins', () => HttpResponse.json(admins)),
      http.get('/api/admin/support/7', () => HttpResponse.json({ status: 404, title: 'Support request 7 was not found.' }, { status: 404 })),
    )
    renderAt('/admin/support/7', { as: 'Admin' })
    expect(await screen.findByText('Support request 7 was not found.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })
})
