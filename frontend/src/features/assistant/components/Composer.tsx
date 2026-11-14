import { useState, type FormEvent, type KeyboardEvent } from 'react'
import { SendHorizontal } from 'lucide-react'
import { Button } from '@/shared/ui/Button'

export const MAX_MESSAGE = 4000

interface Props {
  placeholder: string
  disabled: boolean
  onSend: (message: string) => void
}

/** Enter sends, Shift+Enter adds a line. Blank messages are never sent. */
export function Composer({ placeholder, disabled, onSend }: Props) {
  const [text, setText] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = (e?: FormEvent) => {
    e?.preventDefault()
    const message = text.trim()
    if (!message) {
      setError('Type a message.')
      return
    }
    if (message.length > MAX_MESSAGE) {
      setError(`Keep the message under ${MAX_MESSAGE} characters.`)
      return
    }
    onSend(message)
    setText('')
  }

  const onKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey && !disabled) {
      e.preventDefault()
      submit()
    }
  }

  return (
    <form onSubmit={submit} className="space-y-1.5" noValidate>
      <div className="flex items-end gap-2">
        <label htmlFor="assistant-message" className="sr-only">
          Message
        </label>
        <textarea
          id="assistant-message"
          rows={2}
          value={text}
          placeholder={placeholder}
          aria-invalid={!!error}
          aria-describedby={error ? 'assistant-message-error' : undefined}
          onChange={(e) => {
            setText(e.target.value)
            setError(null)
          }}
          onKeyDown={onKeyDown}
          className="block max-h-40 min-h-11 w-full resize-y rounded-lg border border-line-strong bg-surface px-3.5 py-2.5 text-[15px] text-ink placeholder:text-muted focus:border-brand focus:outline-none focus:ring-2 focus:ring-accent/40"
        />
        <Button type="submit" disabled={disabled} aria-label="Send" className="h-11 shrink-0 px-3.5">
          <SendHorizontal className="size-4" aria-hidden />
        </Button>
      </div>
      {error && (
        <p id="assistant-message-error" className="text-[13px] text-danger">
          {error}
        </p>
      )}
    </form>
  )
}
