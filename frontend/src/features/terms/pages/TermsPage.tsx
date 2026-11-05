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
      <PageHeader
        eyebrow="Administration"
        title="Terms"
        actions={
          <Button onClick={openNew}>
            <Plus className="size-4" aria-hidden /> New term
          </Button>
        }
      >
        Each term has an enrollment window, then an add/drop window.
      </PageHeader>

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
            {/* A table from md up; on phones each term stacks into a block (same markup, no sideways scrolling). */}
            <table className="w-full text-left text-sm max-md:block md:min-w-[680px]">
              <thead className="border-b border-line text-xs uppercase tracking-wide text-muted max-md:hidden">
                <tr>
                  <th scope="col" className="px-4 py-3 font-medium">Term</th>
                  <th scope="col" className="px-4 py-3 font-medium">Dates</th>
                  <th scope="col" className="px-4 py-3 font-medium">Enrollment</th>
                  <th scope="col" className="px-4 py-3 font-medium">Add/drop</th>
                  <th scope="col" className="px-4 py-3 font-medium">Status</th>
                  <th scope="col" className="px-4 py-3"><span className="sr-only">Actions</span></th>
                </tr>
              </thead>
              <tbody className="divide-y divide-line max-md:block">
                {terms.data.map((t) => (
                  <tr key={t.id} className="max-md:grid max-md:grid-cols-[minmax(0,1fr)_auto] max-md:items-center max-md:gap-x-3 max-md:gap-y-1.5 max-md:px-4 max-md:py-4">
                    <th scope="row" className="whitespace-nowrap px-4 py-3.5 font-medium text-ink max-md:p-0 max-md:text-base">{t.name}</th>
                    <td className="px-4 py-3.5 text-muted max-md:col-span-2 max-md:row-start-3 max-md:p-0">
                      <MobileLabel>Dates</MobileLabel>
                      <Range from={formatDateOnly(t.startDate)} to={formatDateOnly(t.endDate)} />
                    </td>
                    <td className="px-4 py-3.5 text-muted max-md:col-span-2 max-md:p-0">
                      <MobileLabel>Enrollment</MobileLabel>
                      <Range from={formatDateTime(t.enrollStart)} to={formatDateTime(t.enrollEnd)} />
                    </td>
                    <td className="px-4 py-3.5 text-muted max-md:col-span-2 max-md:p-0">
                      <MobileLabel>Add/drop</MobileLabel>
                      <Range from={formatDateTime(t.addDropStart)} to={formatDateTime(t.addDropEnd)} />
                    </td>
                    <td className="px-4 py-3.5 max-md:col-span-2 max-md:row-start-2 max-md:p-0">
                      <TermStatus term={t} />
                    </td>
                    <td className="px-4 py-3.5 text-right max-md:col-start-2 max-md:row-start-1 max-md:p-0">
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

/** "22 Oct, 08:00 – 30 Oct, 20:00": breaks after the dash when the column is narrow, never inside a date. */
function Range({ from, to }: { from: string; to: string }) {
  return (
    <>
      <span className="whitespace-nowrap">{from} –</span> <span className="whitespace-nowrap">{to}</span>
    </>
  )
}

/** The column name, shown before the value only on phones (where the table header is hidden). */
function MobileLabel({ children }: { children: string }) {
  return <span className="mr-1.5 font-medium text-ink md:hidden">{children}</span>
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
