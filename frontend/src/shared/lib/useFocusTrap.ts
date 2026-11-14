import { useEffect, useRef, type RefObject } from 'react'

const FOCUSABLE =
  'a[href], button:not([disabled]), input:not([disabled]):not([type="hidden"]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'

/** The elements Tab can reach inside `root`, in document order (hidden ones left out). */
export function focusableIn(root: HTMLElement): HTMLElement[] {
  return [...root.querySelectorAll<HTMLElement>(FOCUSABLE)].filter(
    (el) => !el.closest('[hidden], [inert]') && el.getAttribute('aria-hidden') !== 'true',
  )
}

interface Options {
  /** Called on Escape. */
  onEscape: () => void
  /** The element to focus first; defaults to the first focusable element in the container. */
  initialFocus?: (root: HTMLElement) => HTMLElement | null | undefined
}

/**
 * Modal focus handling for dialogs and the mobile menu (WCAG 2.1.2, 2.4.3):
 * moves focus inside when `active` turns on, keeps Tab / Shift+Tab inside, closes on Escape, and gives focus back to
 * the element that had it before (the button that opened it) when it turns off.
 */
export function useFocusTrap(container: RefObject<HTMLElement | null>, active: boolean, { onEscape, initialFocus }: Options) {
  // Kept in refs so a new onEscape function on every render doesn't re-run the effect (which would steal focus back).
  const escape = useRef(onEscape)
  const initial = useRef(initialFocus)
  escape.current = onEscape
  initial.current = initialFocus

  useEffect(() => {
    if (!active) return
    const root = container.current
    if (!root) return
    const previous = document.activeElement as HTMLElement | null
    const first = initial.current?.(root) ?? focusableIn(root)[0] ?? root
    first.focus()

    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        escape.current()
        return
      }
      if (e.key !== 'Tab') return
      const items = focusableIn(root)
      if (items.length === 0) {
        e.preventDefault()
        return
      }
      const firstItem = items[0]!
      const lastItem = items[items.length - 1]!
      const current = document.activeElement
      if (e.shiftKey && (current === firstItem || !root.contains(current))) {
        e.preventDefault()
        lastItem.focus()
      } else if (!e.shiftKey && (current === lastItem || !root.contains(current))) {
        e.preventDefault()
        firstItem.focus()
      }
    }
    document.addEventListener('keydown', onKey)
    return () => {
      document.removeEventListener('keydown', onKey)
      if (previous && document.contains(previous)) previous.focus()
    }
  }, [active, container])
}
