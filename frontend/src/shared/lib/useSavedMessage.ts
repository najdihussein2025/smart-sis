import { useEffect, useState } from 'react'

/** How long a "saved" message stays on screen. */
export const SAVED_MESSAGE_MS = 5000

/**
 * A success message that hides itself after SAVED_MESSAGE_MS. `show` again restarts the countdown, even with the
 * same text.
 */
export function useSavedMessage() {
  const [saved, setSaved] = useState<{ text: string } | null>(null)
  useEffect(() => {
    if (!saved) return
    const timer = setTimeout(() => setSaved(null), SAVED_MESSAGE_MS)
    return () => clearTimeout(timer)
  }, [saved])
  return { message: saved?.text ?? null, show: (text: string) => setSaved({ text }) }
}
