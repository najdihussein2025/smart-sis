import { useCallback, useEffect, useRef, useState } from 'react'

/**
 * Guards typed-but-unsaved work (score sheets, attendance rosters).
 *
 * - While `dirty`, closing or reloading the tab asks the browser's "Leave site?" question.
 * - `guard(action)` runs `action` at once when nothing is unsaved; otherwise it holds the action and sets `pending`,
 *   so the page can show a ConfirmDialog ("Discard changes?"). `confirm()` runs the held action, `cancel()` drops it.
 *
 * Use it around anything that swaps the form out: changing section, tab, meeting or date.
 */
export function useUnsavedChanges(dirty: boolean) {
  const held = useRef<(() => void) | null>(null)
  const [pending, setPending] = useState(false)

  useEffect(() => {
    if (!dirty) return
    const onBeforeUnload = (e: BeforeUnloadEvent) => {
      e.preventDefault()
      // Older browsers need returnValue set to show the prompt.
      e.returnValue = ''
    }
    window.addEventListener('beforeunload', onBeforeUnload)
    return () => window.removeEventListener('beforeunload', onBeforeUnload)
  }, [dirty])

  const guard = useCallback(
    (action: () => void) => {
      if (!dirty) {
        action()
        return
      }
      held.current = action
      setPending(true)
    },
    [dirty],
  )

  const confirm = useCallback(() => {
    const action = held.current
    held.current = null
    setPending(false)
    action?.()
  }, [])

  const cancel = useCallback(() => {
    held.current = null
    setPending(false)
  }, [])

  return { guard, pending, confirm, cancel }
}
