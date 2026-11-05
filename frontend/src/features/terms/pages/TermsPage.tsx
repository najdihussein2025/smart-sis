import { useEffect, useState } from 'react'
import { CalendarRange, Pencil, Plus } from 'lucide-react'
import { formatDateOnly, formatDateTime, todayIso } from '@/shared/lib/format'
import { Alert } from '@/shared/ui/Alert'
import { Badge } from '@/shared/ui/Badge'
import { Button } from '@/shared/ui/Button'
import { Card } from '@/shared/ui/Card'
import { EmptyState } from '@/shared/ui/EmptyState'
import { PageHeader } from '@/shared/ui/PageHeader'
import { QueryError } from '@/shared/ui/QueryError'
import { TableScroll } from '@/shared/ui/TableScroll'
import { TermFormDialog } from '../components/TermFormDialog'
import { useTerms } from '../hooks'
import type { Term } from '../types'

type DialogState = { open: false } | { open: true; term?: Term }

/** How long the "saved" message stays on screen. */
export const SAVED_MESSAGE_MS = 5000

/** UC-16 / PB-02: admin creates and edits terms and their two registration windows. */
export function TermsPage() {
  const terms = useTerms()
  const [dialog, setDialog] = useState<DialogState>({ open: false })
  const [saved, setSaved] = useState<{ name: string } | null>(null)

  // The "saved" message hides itself after a few seconds (a new save restarts the countdown).
  useEffect(() => {
    if (!saved) return
    const timer = setTimeout(() => setSaved(null), SAVED_MESSAGE_MS)
    return () => clearTimeout(timer)
  }, [saved])

  const openNew = () => setDialog({ open: true })
  const close = () => setDialog({ open: false })

  return (
    <>
      <div className="flex flex-wrap items-start justify-between gap-4">
        <PageHeader eyebrow="Administration" title="Terms">
          Each term has an enrollment window, then an add/drop window.
        </PageHeader>
        <Button onClick={openNew}>
          <Plus className="size-4" aria-hidden /> New term
        </Button>
      </div>

      {saved && (
        <div className="mb-4">
          <Alert tone="success">{saved.name} saved.</Alert>
        </div>
      )}

      <Card>
        {terms.isPending ? (
          <TableSkeleton />
        ) : terms.isError ? (
          <QueryError error={terms.error} onRetry={() => terms.refetch()} />
        ) : terms.data.length === 0 ? (
          <EmptyState icon={CalendarRange} title="No terms yet">
            Create the first term with its enrollment and add/drop windows. Sections and enrollment need a term.
            <div className="mt-4">
              <Button onClick={openNew}>
                <Plus className="size-4" aria-hidden /> New term
              </Button>
            </div>
          </EmptyState>
        ) : (
          <TableScroll>
            <table className="w-full min-w-[760px] text-left text-sm">
              <thead className="border-b border-line text-xs uppercase tracking-wide text-muted">
                <tr>
                  <th scope="col" className="px-5 py-3 font-medium">Term</th>
                  <th scope="col" className="px-5 py-3 font-medium">Dates</th>
                  <th scope="col" className="px-5 py-3 font-medium">Enrollment</th>
                  <th scope="col" className="px-5 py-3 font-medium">Add/drop</th>
                  <th scope="col" className="px-5 py-3 font-medium">Status</th>
                  <th scope="col" className="px-5 py-3"><span className="sr-only">Actions</span></th>
                </tr>
              </thead>
              <tbody className="divide-y divide-line">
                {terms.data.map((t) => (
                  <tr key={t.id}>
                    <th scope="row" className="px-5 py-3.5 font-medium text-ink">{t.name}</th>
                    <td className="px-5 py-3.5 text-muted whitespace-nowrap">
                      {formatDateOnly(t.startDate)} – {formatDateOnly(t.endDate)}
                    </td>
                    <td className="px-5 py-3.5 text-muted whitespace-nowrap">
                      {formatDateTime(t.enrollStart)} – {formatDateTime(t.enrollEnd)}
                    </td>
                    <td className="px-5 py-3.5 text-muted whitespace-nowrap">
                      {formatDateTime(t.addDropStart)} – {formatDateTime(t.addDropEnd)}
                    </td>
                    <td className="px-5 py-3.5">
                      <TermStatus term={t} />
                    </td>
                    <td className="px-5 py-3.5 text-right">
                      <Button variant="ghost" className="h-8 px-2.5" onClick={() => setDialog({ open: true, term: t })} aria-label={`Edit ${t.name}`}>
                        <Pencil className="size-4" aria-hidden /> Edit
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </TableScroll>
        )}
      </Card>

      <TermFormDialog
        open={dialog.open}
        term={dialog.open ? dialog.term : undefined}
        onClose={close}
        onSaved={(t) => {
          setSaved({ name: t.name })
          close()
        }}
      />
    </>
  )
}

function TermStatus({ term }: { term: Term }) {
  const today = todayIso()
  const phase = term.endDate < today ? 'Past' : term.startDate > today ? 'Upcoming' : null
  return (
    <div className="flex flex-wrap gap-1.5">
      {term.isCurrent && <Badge tone="brand">Current</Badge>}
      {!term.isCurrent && phase && <Badge>{phase}</Badge>}
      {term.currentWindow === 'Enrollment' && <Badge tone="success">Enrollment open</Badge>}
      {term.currentWindow === 'AddDrop' && <Badge tone="success">Add/drop open</Badge>}
    </div>
  )
}

function TableSkeleton() {
  return (
    <div className="space-y-3 p-5" aria-busy="true" aria-label="Loading terms">
      {[0, 1, 2].map((i) => (
        <div key={i} className="h-10 animate-pulse rounded-lg bg-canvas" />
      ))}
    </div>
  )
}
