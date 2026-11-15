import { useEffect, useLayoutEffect, useRef, useState, type FormEvent, type KeyboardEvent } from 'react'
import { SendHorizontal } from 'lucide-react'
import { Button } from '@/shared/ui/Button'

export const MAX_MESSAGE = 4000

interface Props {
  placeholder: string
  /** Shown instead of `placeholder` on phones, where the long one is cut off. */
  shortPlaceholder?: string
  disabled: boolean
  onSend: (message: string) => void
}

const PHONE = '(max-width: 639px)'

/** True below the `sm` breakpoint. Safe where matchMedia doesn't exist (tests, old browsers). */
function useIsPhone() {
  const query = () => typeof window !== 'undefined' && typeof window.matchMedia === 'function' && window.matchMedia(PHONE).matches
  const [phone, setPhone] = useState(query)
  useEffect(() => {
    if (typeof window.matchMedia !== 'function') return
    const mq = window.matchMedia(PHONE)
    const onChange = () => setPhone(mq.matches)
    mq.addEventListener?.('change', onChange)
    return () => mq.removeEventListener?.('change', onChange)
  }, [])
  return phone
}

/**
 * Enter sends, Shift+Enter adds a line. Blank messages are never sent. Enter that confirms an IME composition
 * (Arabic, Chinese, Japanese input) only ends the composition, it never sends. The box grows with the text up to
 * about ten lines, then scrolls.
 */
export function Composer({ placeholder, shortPlaceholder, disabled, onSend }: Props) {
  const [text, setText] = useState('')
  const [error, setError] = useState<string | null>(null)
  const box = useRef<HTMLTextAreaElement>(null)
  const phone = useIsPhone()

  // Auto-grow: reset to one row, then take the content height (the CSS max-height caps it at 10rem).
  useLayoutEffect(() => {
    const el = box.current
    if (!el) return
    el.style.height = 'auto'
    el.style.height = `${el.scrollHeight}px`
  }, [text])

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
    if (e.key !== 'Enter' || e.shiftKey) return
    // 229 is the keyCode browsers report for keys pressed while an IME is composing.
    if (e.nativeEvent.isComposing || e.keyCode === 229) return
    e.preventDefault()
    if (!disabled) submit()
  }

  return (
    <form onSubmit={submit} className="space-y-1.5" noValidate>
      <div className="flex items-end gap-2">
        <label htmlFor="assistant-message" className="sr-only">
          Message
        </label>
        <textarea
          ref={box}
          id="assistant-message"
          rows={1}
          value={text}
          placeholder={phone && shortPlaceholder ? shortPlaceholder : placeholder}
          aria-invalid={!!error}
          aria-describedby={error ? 'assistant-message-error' : 'assistant-message-hint'}
          onChange={(e) => {
            setText(e.target.value)
            setError(null)
          }}
          onKeyDown={onKeyDown}
          className="block max-h-40 min-h-11 w-full resize-none overflow-y-auto rounded-lg border border-line-strong bg-surface px-3.5 py-2.5 text-base text-ink placeholder:text-muted focus:border-brand focus:ring-2 focus:ring-accent/40 focus:outline-none sm:text-[15px]"
        />
        <Button type="submit" disabled={disabled} aria-label="Send">
          <SendHorizontal className="size-4" aria-hidden />
        </Button>
      </div>
      {error ? (
        <p id="assistant-message-error" className="text-xs text-danger">
          {error}
        </p>
      ) : (
        <p id="assistant-message-hint" className="text-xs text-muted max-sm:sr-only">
          Enter to send, Shift+Enter for a new line.
        </p>
      )}
    </form>
  )
}
