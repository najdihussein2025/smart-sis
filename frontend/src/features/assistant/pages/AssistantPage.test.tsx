import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { server } from '@/test/server'
import { profileFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'
import type { MyEnrollments } from '@/features/enrollment/types'
import type { ChatReply, Conversation, ConversationSummary, SendMessageRequest } from '../types'

// PB-15 / T63 and PB-19 / T70: the student's assistant and tutoring. Simulated data only (SR-23).

const summary = (id: number, title: string, extra: Partial<ConversationSummary> = {}): ConversationSummary => ({
  id, title, mode: 'Assistant', courseId: null, courseCode: null,
  createdAt: '2026-11-10T09:00:00Z', updatedAt: '2026-11-10T09:00:00Z', ...extra,
})

const mine: MyEnrollments = {
  termId: 1, termName: 'Fall 2026', window: 'Closed', enrolledCredits: 6, maxCredits: 18,
  items: [
    { enrollmentId: 1, sectionId: 21, courseId: 2, courseCode: 'CSC 210', courseName: 'Data Structures', credits: 3, sectionCode: 'A', instructorName: 'Demo Instructor', meetings: [], enrolledAt: '2026-09-01T10:00:00Z' },
    { enrollmentId: 2, sectionId: 41, courseId: 4, courseCode: 'MAT 201', courseName: 'Linear Algebra', credits: 3, sectionCode: 'A', instructorName: 'Demo Instructor', meetings: [], enrolledAt: '2026-09-01T10:00:00Z' },
  ],
}

/** A small in-memory chat backend: answers "Answer: <question>" and remembers conversations. */
function mockChat(initial: Conversation[] = []) {
  const conversations = new Map(initial.map((c) => [c.conversation.id, c]))
  const sent: SendMessageRequest[] = []
  let nextId = 100
  server.use(
    http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))),
    http.get('/api/enrollment/my', () => HttpResponse.json(mine)),
    http.get('/api/chat/conversations', () => HttpResponse.json([...conversations.values()].map((c) => c.conversation).reverse())),
    http.get('/api/chat/conversations/:id', ({ params }) => {
      const c = conversations.get(Number(params.id))
      return c ? HttpResponse.json(c) : HttpResponse.json({ title: 'This conversation was not found.' }, { status: 404 })
    }),
    http.post('/api/chat/messages', async ({ request }) => {
      const body = (await request.json()) as SendMessageRequest
      sent.push(body)
      const existing = body.conversationId ? conversations.get(body.conversationId) : undefined
      const conv: Conversation = existing ?? {
        conversation: summary(nextId++, body.message, body.mode === 'Tutor' ? { mode: 'Tutor', courseId: body.courseId ?? null, courseCode: 'CSC 210' } : {}),
        messages: [],
      }
      const user = { id: nextId++, role: 'User' as const, content: body.message, createdAt: '2026-11-14T10:00:00Z' }
      const assistant = { id: nextId++, role: 'Assistant' as const, content: `Answer: ${body.message}`, createdAt: '2026-11-14T10:00:01Z' }
      conv.messages.push(user, assistant)
      conversations.set(conv.conversation.id, conv)
      const reply: ChatReply = { conversation: conv.conversation, userMessage: user, assistantMessage: assistant }
      return HttpResponse.json(reply)
    }),
    http.put('/api/chat/conversations/:id', async ({ params, request }) => {
      const c = conversations.get(Number(params.id))!
      c.conversation = { ...c.conversation, title: ((await request.json()) as { title: string }).title }
      return HttpResponse.json(c.conversation)
    }),
    http.delete('/api/chat/conversations/:id', ({ params }) => {
      conversations.delete(Number(params.id))
      return new HttpResponse(null, { status: 204 })
    }),
  )
  return { sent }
}

const examPrep: Conversation = {
  conversation: summary(1, 'Exam prep'),
  messages: [
    { id: 11, role: 'User', content: 'When is add/drop?', createdAt: '2026-11-10T09:00:00Z' },
    { id: 12, role: 'Assistant', content: 'Add/drop closes on 15 Sep 2026.', createdAt: '2026-11-10T09:00:02Z' },
  ],
}

describe('AssistantPage', () => {
  it('lists conversations and opens one', async () => {
    mockChat([examPrep])
    const { user } = renderAt('/student/assistant', { as: 'Student' })

    const list = await screen.findByRole('list', { name: 'Your conversations' })
    await user.click(within(list).getByRole('button', { name: /^Exam prep/ }))

    expect(await screen.findByText('Add/drop closes on 15 Sep 2026.')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Exam prep' })).toBeInTheDocument()
  })

  it('starts a new conversation, then continues the same one', async () => {
    const { sent } = mockChat()
    const { user } = renderAt('/student/assistant', { as: 'Student' })
    expect(await screen.findByText(/No conversations yet/)).toBeInTheDocument()

    await user.type(screen.getByLabelText('Message'), 'What is my GPA?')
    await user.click(screen.getByRole('button', { name: 'Send' }))

    expect(await screen.findByText('Answer: What is my GPA?')).toBeInTheDocument()
    expect(sent[0]).toEqual({ message: 'What is my GPA?' })

    await user.type(screen.getByLabelText('Message'), 'And last term?{Enter}')
    expect(await screen.findByText('Answer: And last term?')).toBeInTheDocument()
    expect(sent[1]).toEqual({ message: 'And last term?', conversationId: 100 })
    expect(screen.getByText('Answer: What is my GPA?')).toBeInTheDocument()
  })

  it('sends a suggested question', async () => {
    const { sent } = mockChat()
    const { user } = renderAt('/student/assistant', { as: 'Student' })
    await user.click(await screen.findByRole('button', { name: 'How many absences am I allowed?' }))
    expect(await screen.findByText('Answer: How many absences am I allowed?')).toBeInTheDocument()
    expect(sent).toHaveLength(1)
  })

  it('does not send an empty message', async () => {
    const { sent } = mockChat()
    const { user } = renderAt('/student/assistant', { as: 'Student' })
    await user.click(await screen.findByRole('button', { name: 'Send' }))
    expect(screen.getByText('Type a message.')).toBeInTheDocument()
    expect(sent).toHaveLength(0)
  })

  it('shows the friendly message when the assistant is unavailable', async () => {
    mockChat()
    server.use(
      http.post('/api/chat/messages', () =>
        HttpResponse.json(
          { title: "The assistant isn't available right now. Please try again in a few minutes.", code: 'assistant_unavailable' },
          { status: 409 },
        ),
      ),
    )
    const { user } = renderAt('/student/assistant', { as: 'Student' })
    await user.type(await screen.findByLabelText('Message'), 'Hello?{Enter}')
    expect(await screen.findByRole('alert')).toHaveTextContent("The assistant isn't available right now.")
  })

  it('shows an error with retry when conversations cannot load', async () => {
    mockChat()
    server.use(http.get('/api/chat/conversations', () => HttpResponse.json({ title: 'Could not load.' }, { status: 500 })))
    renderAt('/student/assistant', { as: 'Student' })
    expect(await screen.findByText('Something went wrong on our side. Please try again in a moment.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })

  it('starts tutoring for an enrolled course', async () => {
    const { sent } = mockChat()
    const { user } = renderAt('/student/assistant', { as: 'Student' })

    await user.click(await screen.findByRole('button', { name: 'Tutoring' }))
    const dialog = await screen.findByRole('dialog', { name: 'Tutoring' })
    await user.click(within(dialog).getByRole('button', { name: 'Start tutoring' }))
    expect(within(dialog).getByText('Choose a course.')).toBeInTheDocument()

    await user.selectOptions(within(dialog).getByLabelText('Course'), 'CSC 210 · Data Structures')
    await user.click(within(dialog).getByRole('button', { name: 'Start tutoring' }))

    expect(screen.getByText('Tutor · CSC 210')).toBeInTheDocument()
    await user.type(screen.getByLabelText('Message'), 'Explain recursion{Enter}')
    expect(await screen.findByText('Answer: Explain recursion')).toBeInTheDocument()
    expect(sent[0]).toEqual({ message: 'Explain recursion', mode: 'Tutor', courseId: 2 })
  })

  it('renames and deletes a conversation', async () => {
    mockChat([examPrep])
    const { user } = renderAt('/student/assistant', { as: 'Student' })

    await user.click(await screen.findByRole('button', { name: 'Rename Exam prep' }))
    const rename = await screen.findByRole('dialog', { name: 'Rename conversation' })
    const title = within(rename).getByLabelText('Title')
    await user.clear(title)
    await user.click(within(rename).getByRole('button', { name: 'Save' }))
    expect(within(rename).getByText('Enter a title.')).toBeInTheDocument()
    await user.type(title, 'Finals')
    await user.click(within(rename).getByRole('button', { name: 'Save' }))
    expect(await screen.findByRole('button', { name: /^Finals/ })).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Delete Finals' }))
    const confirm = await screen.findByRole('dialog', { name: 'Delete conversation?' })
    await user.click(within(confirm).getByRole('button', { name: 'Delete' }))
    await waitFor(() => expect(screen.queryByRole('button', { name: /^Finals/ })).not.toBeInTheDocument())
    expect(screen.getByText(/No conversations yet/)).toBeInTheDocument()
  })
  it('does not send when Enter only confirms an IME composition', async () => {
    const { sent } = mockChat()
    const { user } = renderAt('/student/assistant', { as: 'Student' })
    const box = await screen.findByLabelText('Message')
    await user.type(box, 'مرحبا')

    fireEvent.keyDown(box, { key: 'Enter', isComposing: true })
    fireEvent.keyDown(box, { key: 'Enter', keyCode: 229 })

    expect(sent).toHaveLength(0)
    expect(box).toHaveValue('مرحبا')

    fireEvent.keyDown(box, { key: 'Enter' })
    expect(await screen.findByText('Answer: مرحبا')).toBeInTheDocument()
    expect(sent).toHaveLength(1)
  })

  it('hides "New chat" while the current chat is an empty draft', async () => {
    // A fresh copy: the rename test edits the shared fixture in place.
    mockChat([{ conversation: summary(1, 'Exam prep'), messages: [...examPrep.messages] }])
    const { user } = renderAt('/student/assistant', { as: 'Student' })
    const list = await screen.findByRole('list', { name: 'Your conversations' })
    expect(screen.queryByRole('button', { name: 'New chat' })).not.toBeInTheDocument()

    await user.click(within(list).getByRole('button', { name: /^Exam prep/ }))
    await user.click(await screen.findByRole('button', { name: 'New chat' }))

    expect(screen.getByRole('heading', { name: 'New conversation' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'New chat' })).not.toBeInTheDocument()
  })
})
