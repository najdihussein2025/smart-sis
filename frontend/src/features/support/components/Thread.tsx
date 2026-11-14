import { cn } from '@/shared/lib/cn'
import { formatDateTime } from '@/shared/lib/format'
import type { SupportMessage } from '../types'

interface Props {
  /** The original request, shown first. */
  description: string
  createdAt: string
  /** Who submitted it, e.g. "You" or the student's name. */
  submittedBy: string
  messages: SupportMessage[]
  /** Label for the admins' replies, e.g. "Support" for the student. Defaults to the author's name. */
  staffLabel?: (m: SupportMessage) => string
  /** Label for the student's replies. */
  studentLabel?: (m: SupportMessage) => string
}

/** The request and its replies, oldest first. Staff replies sit on the other side and are labelled, not only coloured. */
export function Thread({ description, createdAt, submittedBy, messages, staffLabel, studentLabel }: Props) {
  return (
    <ol className="space-y-4" aria-label="Conversation">
      <Entry who={submittedBy} when={createdAt} body={description} staff={false} note="Request" />
      {messages.map((m) => {
        const staff = m.authorRole !== 'Student'
        const who = staff ? (staffLabel?.(m) ?? m.authorName) : (studentLabel?.(m) ?? m.authorName)
        return <Entry key={m.id} who={who} when={m.createdAt} body={m.body} staff={staff} />
      })}
    </ol>
  )
}

function Entry({ who, when, body, staff, note }: { who: string; when: string; body: string; staff: boolean; note?: string }) {
  return (
    <li className={cn('flex', staff ? 'justify-end' : 'justify-start')}>
      <article
        className={cn(
          'max-w-[min(100%,36rem)] rounded-xl border px-4 py-3 text-sm',
          staff ? 'border-brand/20 bg-brand-50' : 'border-line bg-surface',
        )}
      >
        <header className="mb-1 flex flex-wrap items-baseline gap-x-2 text-xs text-muted">
          <span className="font-medium text-ink">{who}</span>
          {note && <span>· {note}</span>}
          <time dateTime={when}>{formatDateTime(when)}</time>
        </header>
        <p className="whitespace-pre-wrap break-words text-ink">{body}</p>
      </article>
    </li>
  )
}
