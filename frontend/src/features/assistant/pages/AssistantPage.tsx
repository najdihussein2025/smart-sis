import { useState } from 'react'
import { Bot, ChevronDown, GraduationCap, History, MessageSquarePlus, MessageSquareText } from 'lucide-react'
import { errorMessage } from '@/shared/api/problem'
import { cn } from '@/shared/lib/cn'
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
  // Phones: the conversation comes first and past chats fold away under it.
  const [historyOpen, setHistoryOpen] = useState(false)

  const conversation = useConversation(selectedId)
  const send = useSendMessage()
  const remove = useDeleteConversation()

  const open = (id: number) => {
    setSelectedId(id)
    setHistoryOpen(false)
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
  // "New chat" means nothing while the current chat is already an empty, unsaved one.
  const isEmptyDraft = selectedId === null && draft.mode === 'Assistant' && !pending
  const count = list.data?.length

  return (
    <>
      <PageHeader eyebrow="Help" title="Assistant" icon={MessageSquareText}>
        Ask about your schedule, grades, deadlines and university policies, or get tutoring for one of your courses.
      </PageHeader>

      <div className="grid gap-6 lg:grid-cols-[18rem_minmax(0,1fr)] lg:gap-4">
        <Card className="order-2 flex flex-col gap-3 p-3 lg:order-1 lg:max-h-[calc(100dvh-14rem)]">
          <div className="flex flex-wrap gap-2 lg:flex-col">
            {!isEmptyDraft && (
              <Button variant="secondary" className="max-lg:flex-1" onClick={() => startNew({ mode: 'Assistant' })}>
                <MessageSquarePlus className="size-4" aria-hidden /> New chat
              </Button>
            )}
            <Button variant="secondary" className="max-lg:flex-1" onClick={() => setTutorOpen(true)}>
              <GraduationCap className="size-4" aria-hidden /> Tutoring
            </Button>
          </div>
          <button
            type="button"
            className="flex min-h-11 w-full cursor-pointer items-center gap-2 rounded-lg px-3 text-left text-sm font-medium text-ink hover:bg-canvas lg:hidden"
            aria-expanded={historyOpen}
            aria-controls="assistant-history"
            onClick={() => setHistoryOpen((o) => !o)}
          >
            <History className="size-4 text-muted" aria-hidden />
            <span className="flex-1">Past chats{count ? ` (${count})` : ''}</span>
            <ChevronDown className={cn('size-4 text-muted transition-transform', historyOpen && 'rotate-180')} aria-hidden />
          </button>
          <h2 className="hidden px-3 pt-1 text-xs font-semibold tracking-[0.14em] text-muted uppercase lg:block">Past chats</h2>
          <div
            id="assistant-history"
            className={cn('min-h-0 flex-1 overflow-y-auto max-lg:max-h-72', !historyOpen && 'max-lg:hidden')}
          >
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

        <Card className="order-1 flex min-h-[26rem] flex-col lg:order-2 lg:max-h-[calc(100dvh-14rem)]">
          <div className="flex flex-wrap items-center gap-2 border-b border-line px-4 py-3.5 sm:px-5">
            <h2 className="min-w-0 truncate font-display text-lg font-semibold text-ink">
              {summary?.title ?? (isTutor ? 'New tutoring session' : 'New conversation')}
            </h2>
            {isTutor && <Badge tone="warning">Tutor{tutorCode ? ` · ${tutorCode}` : ''}</Badge>}
          </div>

          <div className="min-h-0 flex-1 overflow-y-auto px-4 py-5 sm:px-5">
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
                <EmptyState
                  icon={Bot}
                  title="How can I help?"
                  action={SUGGESTIONS.map((s) => (
                    <button
                      key={s}
                      type="button"
                      disabled={send.isPending}
                      onClick={() => sendMessage(s)}
                      className="min-h-11 cursor-pointer rounded-full border border-line-strong/60 bg-surface px-4 py-2 text-left text-sm text-ink shadow-sm transition-colors hover:border-brand hover:bg-brand-50 disabled:cursor-not-allowed disabled:opacity-50 sm:min-h-9"
                    >
                      {s}
                    </button>
                  ))}
                >
                  I answer from your own records and the university's policies. If I can't find something, I'll say so.
                </EmptyState>
              )
            ) : (
              <MessageList messages={messages} thinking={send.isPending} />
            )}
          </div>

          <div className="space-y-3 border-t border-line px-4 py-4 sm:px-5">
            {send.isError && <Alert>{errorMessage(send.error, 'The assistant could not answer. Please try again.')}</Alert>}
            <Composer
              disabled={send.isPending}
              placeholder={isTutor ? 'Ask the tutor to explain a concept…' : 'Ask about your courses, grades, deadlines or policies…'}
              shortPlaceholder={isTutor ? 'Ask the tutor…' : 'Ask a question…'}
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
