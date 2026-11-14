import { useState } from 'react'
import { Bot, GraduationCap, MessageSquarePlus } from 'lucide-react'
import { errorMessage } from '@/shared/api/problem'
import { Alert } from '@/shared/ui/Alert'
import { Badge } from '@/shared/ui/Badge'
import { Button } from '@/shared/ui/Button'
import { Card } from '@/shared/ui/Card'
import { ConfirmDialog } from '@/shared/ui/ConfirmDialog'
import { EmptyState } from '@/shared/ui/EmptyState'
import { PageHeader } from '@/shared/ui/PageHeader'
import { QueryError } from '@/shared/ui/QueryError'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { Composer } from '../components/Composer'
import { ConversationList } from '../components/ConversationList'
import { MessageList, type DisplayMessage } from '../components/MessageList'
import { RenameDialog } from '../components/RenameDialog'
import { TutorDialog, type TutorCourse } from '../components/TutorDialog'
import { useConversation, useConversations, useDeleteConversation, useSendMessage } from '../hooks'
import type { ConversationSummary, SendMessageRequest } from '../types'

/** What a new (not yet saved) conversation will be. */
type Draft = { mode: 'Assistant' } | { mode: 'Tutor'; course: TutorCourse }

const SUGGESTIONS = ["What's on my schedule this week?", 'When does the add/drop period close?', 'How many absences am I allowed?']

/**
 * PB-15 / T63: the student's AI assistant with saved conversations. PB-19 / T70: tutoring for an enrolled course.
 * Personal answers come from the student's own data only; the API passes the student's sign-in to the assistant.
 */
export function AssistantPage() {
  const list = useConversations()
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const [draft, setDraft] = useState<Draft>({ mode: 'Assistant' })
  const [pending, setPending] = useState<string | null>(null)
  const [tutorOpen, setTutorOpen] = useState(false)
  const [renaming, setRenaming] = useState<ConversationSummary | null>(null)
  const [deleting, setDeleting] = useState<ConversationSummary | null>(null)

  const conversation = useConversation(selectedId)
  const send = useSendMessage()
  const remove = useDeleteConversation()

  const open = (id: number) => {
    setSelectedId(id)
    send.reset()
  }
  const startNew = (next: Draft) => {
    setSelectedId(null)
    setDraft(next)
    send.reset()
  }

  const sendMessage = (message: string) => {
    const body: SendMessageRequest =
      selectedId !== null
        ? { message, conversationId: selectedId }
        : draft.mode === 'Tutor'
          ? { message, mode: 'Tutor', courseId: draft.course.courseId }
          : { message }
    setPending(message)
    send.mutate(body, {
      onSuccess: (reply) => setSelectedId(reply.conversation.id),
      onSettled: () => setPending(null),
    })
  }

  const confirmDelete = () => {
    if (!deleting) return
    remove.mutate(deleting.id, {
      onSuccess: () => {
        if (deleting.id === selectedId) startNew({ mode: 'Assistant' })
        setDeleting(null)
      },
    })
  }

  const summary = selectedId !== null ? conversation.data?.conversation : undefined
  const tutorCode = summary ? (summary.mode === 'Tutor' ? summary.courseCode : null) : draft.mode === 'Tutor' ? draft.course.courseCode : null
  const isTutor = summary ? summary.mode === 'Tutor' : draft.mode === 'Tutor'

  const messages: DisplayMessage[] = (selectedId !== null ? (conversation.data?.messages ?? []) : []).map((m) => ({
    key: String(m.id),
    role: m.role,
    content: m.content,
  }))
  if (pending) messages.push({ key: 'pending', role: 'User', content: pending })

  return (
    <>
      <PageHeader eyebrow="Student" title="Assistant">
        Ask about your schedule, grades, deadlines and university policies, or get tutoring for one of your courses.
      </PageHeader>

      <div className="grid gap-4 lg:grid-cols-[18rem_minmax(0,1fr)]">
        <Card className="flex flex-col gap-3 p-3 lg:max-h-[calc(100dvh-14rem)]">
          <div className="grid grid-cols-2 gap-2 lg:grid-cols-1">
            <Button onClick={() => startNew({ mode: 'Assistant' })}>
              <MessageSquarePlus className="size-4" aria-hidden /> New chat
            </Button>
            <Button variant="secondary" onClick={() => setTutorOpen(true)}>
              <GraduationCap className="size-4" aria-hidden /> Tutoring
            </Button>
          </div>
          <div className="min-h-0 flex-1 overflow-y-auto max-lg:max-h-60">
            {list.isPending ? (
              <TableSkeleton label="Loading conversations" />
            ) : list.isError ? (
              <QueryError error={list.error} onRetry={() => list.refetch()} />
            ) : list.data.length === 0 ? (
              <p className="px-3 py-4 text-sm text-muted">No conversations yet. Ask your first question to start one.</p>
            ) : (
              <ConversationList
                conversations={list.data}
                selectedId={selectedId}
                onSelect={open}
                onRename={setRenaming}
                onDelete={(c) => {
                  remove.reset()
                  setDeleting(c)
                }}
              />
            )}
          </div>
        </Card>

        <Card className="flex min-h-[28rem] flex-col lg:max-h-[calc(100dvh-14rem)]">
          <div className="flex flex-wrap items-center gap-2 border-b border-line px-5 py-3.5">
            <h2 className="min-w-0 truncate font-display text-lg font-semibold text-ink">
              {summary?.title ?? (isTutor ? 'New tutoring session' : 'New conversation')}
            </h2>
            {isTutor && <Badge tone="warning">Tutor{tutorCode ? ` · ${tutorCode}` : ''}</Badge>}
          </div>

          <div className="min-h-0 flex-1 overflow-y-auto px-5 py-5">
            {selectedId !== null && conversation.isPending ? (
              <TableSkeleton label="Loading messages" />
            ) : selectedId !== null && conversation.isError ? (
              <QueryError error={conversation.error} onRetry={() => conversation.refetch()} />
            ) : messages.length === 0 ? (
              isTutor ? (
                <EmptyState icon={GraduationCap} title={`Tutoring for ${tutorCode ?? 'your course'}`}>
                  Ask about a concept you're studying. The tutor explains step by step and gives hints, but won't do graded work for you.
                </EmptyState>
              ) : (
                <EmptyState icon={Bot} title="How can I help?">
                  I answer from your own records and the university's policies. If I can't find something, I'll say so.
                  <div className="mt-4 flex flex-wrap justify-center gap-2">
                    {SUGGESTIONS.map((s) => (
                      <Button key={s} variant="secondary" className="h-auto py-2 text-left" disabled={send.isPending} onClick={() => sendMessage(s)}>
                        {s}
                      </Button>
                    ))}
                  </div>
                </EmptyState>
              )
            ) : (
              <MessageList messages={messages} thinking={send.isPending} />
            )}
          </div>

          <div className="space-y-3 border-t border-line px-5 py-4">
            {send.isError && <Alert>{errorMessage(send.error, 'The assistant could not answer. Please try again.')}</Alert>}
            <Composer
              disabled={send.isPending}
              placeholder={isTutor ? 'Ask the tutor to explain a concept…' : 'Ask about your courses, grades, deadlines or policies…'}
              onSend={sendMessage}
            />
          </div>
        </Card>
      </div>

      <TutorDialog
        open={tutorOpen}
        onClose={() => setTutorOpen(false)}
        onStart={(course) => {
          setTutorOpen(false)
          startNew({ mode: 'Tutor', course })
        }}
      />
      <RenameDialog conversation={renaming} onClose={() => setRenaming(null)} />
      <ConfirmDialog
        open={deleting !== null}
        title="Delete conversation?"
        confirmLabel="Delete"
        loading={remove.isPending}
        error={remove.isError ? errorMessage(remove.error) : null}
        onConfirm={confirmDelete}
        onClose={() => setDeleting(null)}
      >
        “{deleting?.title}” will be removed from your conversations.
      </ConfirmDialog>
    </>
  )
}
