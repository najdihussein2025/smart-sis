import { GraduationCap, MessageSquareText, Pencil, Trash2 } from 'lucide-react'
import { cn } from '@/shared/lib/cn'
import { formatDateTime } from '@/shared/lib/format'
import type { ConversationSummary } from '../types'

interface Props {
  conversations: ConversationSummary[]
  selectedId: number | null
  onSelect: (id: number) => void
  onRename: (c: ConversationSummary) => void
  onDelete: (c: ConversationSummary) => void
}

export function ConversationList({ conversations, selectedId, onSelect, onRename, onDelete }: Props) {
  return (
    <ul className="space-y-1" aria-label="Your conversations">
      {conversations.map((c) => {
        const Icon = c.mode === 'Tutor' ? GraduationCap : MessageSquareText
        const active = c.id === selectedId
        return (
          <li key={c.id} className={cn('group flex items-center gap-1 rounded-lg', active ? 'bg-brand-50' : 'hover:bg-canvas')}>
            <button
              type="button"
              onClick={() => onSelect(c.id)}
              aria-current={active ? 'true' : undefined}
              className="flex min-w-0 flex-1 items-start gap-2.5 rounded-lg px-3 py-2.5 text-left focus-visible:outline-2 focus-visible:outline-accent"
            >
              <Icon className="mt-0.5 size-4 shrink-0 text-muted" aria-hidden />
              <span className="min-w-0">
                <span className={cn('block truncate text-sm', active ? 'font-medium text-ink' : 'text-ink')}>{c.title}</span>
                <span className="block text-xs text-muted">
                  {c.mode === 'Tutor' ? `Tutor${c.courseCode ? ` · ${c.courseCode}` : ''} · ` : ''}
                  {formatDateTime(c.updatedAt)}
                </span>
              </span>
            </button>
            <button
              type="button"
              onClick={() => onRename(c)}
              aria-label={`Rename ${c.title}`}
              className="rounded-md p-1.5 text-muted hover:bg-surface hover:text-ink"
            >
              <Pencil className="size-3.5" aria-hidden />
            </button>
            <button
              type="button"
              onClick={() => onDelete(c)}
              aria-label={`Delete ${c.title}`}
              className="mr-1 rounded-md p-1.5 text-muted hover:bg-surface hover:text-danger"
            >
              <Trash2 className="size-3.5" aria-hidden />
            </button>
          </li>
        )
      })}
    </ul>
  )
}
