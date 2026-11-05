import type { ReactNode } from 'react'

/**
 * Wraps a wide table so it scrolls sideways inside the card on small screens.
 * `relative` matters: without it, absolutely positioned children (the `sr-only` "Actions" header) are placed
 * against the page instead of this box, escape the scroll area and make the whole page scroll sideways at 375 px.
 */
export function TableScroll({ children }: { children: ReactNode }) {
  return (
    <div className="relative overflow-x-auto">
      {children}
    </div>
  )
}
