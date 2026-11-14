import { useEffect, useRef } from 'react'
import { Bot, UserRound } from 'lucide-react'
import { cn } from '@/shared/lib/cn'
import { Spinner } from '@/shared/ui/Spinner'

export interface DisplayMessage {
  key: string
  role: 'User' | 'Assistant'
  content: string
}

/** The conversation, oldest first. While waiting, the question shows straight away with a "thinking" line under it. */
export function MessageList({ messages, thinking }: { messages: DisplayMessage[]; thinking: boolean }) {
  const end = useRef<HTMLDivElement>(null)
  useEffect(() => {
    end.current?.scrollIntoView?.({ block: 'end' })
  }, [messages.length, thinking])

  return (
    <div className="space-y-4" role="log" aria-label="Messages" aria-live="polite">
      {messages.map((m) => (
        <div key={m.key} className={cn('flex gap-3', m.role === 'User' && 'flex-row-reverse')}>
          <span
            className={cn(
              'grid size-8 shrink-0 place-items-center rounded-full',
              m.role === 'User' ? 'bg-brand text-white' : 'bg-accent-soft text-accent',
            )}
          >
            {m.role === 'User' ? <UserRound className="size-4" aria-hidden /> : <Bot className="size-4" aria-hidden />}
          </span>
          <div
            className={cn(
              'max-w-[min(42rem,85%)] whitespace-pre-wrap break-words rounded-2xl px-4 py-2.5 text-[15px] leading-relaxed',
              m.role === 'User' ? 'bg-brand text-white' : 'border border-line bg-surface text-ink',
            )}
          >
            <span className="sr-only">{m.role === 'User' ? 'You: ' : 'Assistant: '}</span>
            {m.content}
          </div>
        </div>
      ))}
      {thinking && (
        <div className="flex items-center gap-3 text-sm text-muted">
          <span className="grid size-8 place-items-center rounded-full bg-accent-soft text-accent">
            <Bot className="size-4" aria-hidden />
          </span>
          <Spinner className="size-4" />
          Thinking…
        </div>
      )}
      <div ref={end} />
    </div>
  )
}
