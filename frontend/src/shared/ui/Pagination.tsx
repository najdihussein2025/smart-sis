import { ChevronLeft, ChevronRight } from 'lucide-react'
import { Button } from './Button'

interface Props {
  page: number
  totalPages: number
  totalCount: number
  /** "course", "section"… shown as "12 courses". */
  noun: string
  onPage: (page: number) => void
}

/** "Page 2 of 5 · 87 courses" with previous / next buttons. */
export function Pagination({ page, totalPages, totalCount, noun, onPage }: Props) {
  return (
    <nav aria-label="Pages" className="flex flex-wrap items-center justify-between gap-3 border-t border-line px-5 py-3 text-sm text-muted">
      <span>
        {totalCount} {totalCount === 1 ? noun : `${noun}s`}
        {totalPages > 1 && ` · page ${page} of ${totalPages}`}
      </span>
      {totalPages > 1 && (
        <div className="flex gap-2">
          <Button variant="secondary" className="h-8 px-2.5" disabled={page <= 1} onClick={() => onPage(page - 1)}>
            <ChevronLeft className="size-4" aria-hidden /> Previous
          </Button>
          <Button variant="secondary" className="h-8 px-2.5" disabled={page >= totalPages} onClick={() => onPage(page + 1)}>
            Next <ChevronRight className="size-4" aria-hidden />
          </Button>
        </div>
      )}
    </nav>
  )
}
