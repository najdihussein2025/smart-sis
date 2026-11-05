import { useEffect, useState } from 'react'
import { Layers, Pencil, Plus } from 'lucide-react'
import { useCourses } from '@/features/courses/hooks'
import { useTerms } from '@/features/terms/hooks'
import { useSavedMessage } from '@/shared/lib/useSavedMessage'
import { Alert } from '@/shared/ui/Alert'
import { Badge } from '@/shared/ui/Badge'
import { Button } from '@/shared/ui/Button'
import { Card } from '@/shared/ui/Card'
import { EmptyState } from '@/shared/ui/EmptyState'
import { PageHeader } from '@/shared/ui/PageHeader'
import { Pagination } from '@/shared/ui/Pagination'
import { QueryError } from '@/shared/ui/QueryError'
import { SelectField } from '@/shared/ui/SelectField'
import { TableScroll } from '@/shared/ui/TableScroll'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { SectionFormDialog } from '../components/SectionFormDialog'
import { hm, shortDays } from '../components/meetings'
import { useSections } from '../hooks'
import type { Section } from '../types'

const PAGE_SIZE = 20

type DialogState = { open: false } | { open: true; section?: Section }

/** PB-04 / T08: admin lists sections per term and course, and creates / edits them with their weekly meetings. */
export function SectionsPage() {
  const terms = useTerms()
  const courses = useCourses({ pageSize: 100 })
  // '' = all terms. Starts on the current term once the terms have loaded.
  const [termId, setTermId] = useState<string | null>(null)
  const [courseId, setCourseId] = useState('')
  const [page, setPage] = useState(1)
  const [dialog, setDialog] = useState<DialogState>({ open: false })
  const saved = useSavedMessage()

  useEffect(() => {
    if (termId === null && terms.data) setTermId(String(terms.data.find((t) => t.isCurrent)?.id ?? ''))
  }, [termId, terms.data])

  // Wait for the default term before listing, so the first list isn't "all terms" for a moment.
  const ready = termId !== null || terms.isError
  const sections = useSections(
    { termId: termId ? Number(termId) : undefined, courseId: courseId ? Number(courseId) : undefined, page, pageSize: PAGE_SIZE },
    ready,
  )

  const openNew = () => setDialog({ open: true })
  const close = () => setDialog({ open: false })
  const filtered = !!termId || !!courseId

  return (
    <>
      <PageHeader
        eyebrow="Administration"
        title="Sections"
        actions={
          <Button onClick={openNew}>
            <Plus className="size-4" aria-hidden /> New section
          </Button>
        }
      >
        Who teaches each course, how many seats it has, and when and where it meets.
      </PageHeader>

      {saved.message && (
        <div className="mb-4">
          <Alert tone="success">{saved.message}</Alert>
        </div>
      )}

      <Card>
        <div className="grid gap-3 border-b border-line p-4 sm:grid-cols-2">
          <SelectField
            label="Term"
            value={termId ?? ''}
            onChange={(e) => {
              setTermId(e.target.value)
              setPage(1)
            }}
          >
            <option value="">All terms</option>
            {terms.data?.map((t) => (
              <option key={t.id} value={t.id}>
                {t.name}
                {t.isCurrent ? ' (current)' : ''}
              </option>
            ))}
          </SelectField>
          <SelectField
            label="Course"
            value={courseId}
            onChange={(e) => {
              setCourseId(e.target.value)
              setPage(1)
            }}
          >
            <option value="">All courses</option>
            {courses.data?.items.map((c) => (
              <option key={c.id} value={c.id}>
                {c.code} · {c.name}
              </option>
            ))}
          </SelectField>
        </div>

        {!ready || sections.isPending ? (
          <TableSkeleton label="Loading sections" />
        ) : sections.isError ? (
          <QueryError error={sections.error} onRetry={() => sections.refetch()} />
        ) : sections.data.items.length === 0 ? (
          <EmptyState icon={Layers} title={filtered ? 'No sections here yet' : 'No sections yet'}>
            {filtered
              ? 'No section matches this term and course. Choose other filters, or create one.'
              : 'Create the first section: course, term, capacity and weekly meetings.'}
            <div className="mt-4">
              <Button onClick={openNew}>
                <Plus className="size-4" aria-hidden /> New section
              </Button>
            </div>
          </EmptyState>
        ) : (
          <>
            <TableScroll>
              <table className="w-full text-left text-sm max-md:block md:min-w-[720px]">
                <thead className="border-b border-line text-xs uppercase tracking-wide text-muted max-md:hidden">
                  <tr>
                    <th scope="col" className="px-4 py-3 font-medium">Section</th>
                    <th scope="col" className="px-4 py-3 font-medium">Instructor</th>
                    <th scope="col" className="px-4 py-3 font-medium">Seats</th>
                    <th scope="col" className="px-4 py-3 font-medium">Meetings</th>
                    <th scope="col" className="px-4 py-3"><span className="sr-only">Actions</span></th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-line max-md:block">
                  {sections.data.items.map((s) => (
                    <tr key={s.id} className="align-top max-md:grid max-md:grid-cols-[minmax(0,1fr)_auto] max-md:gap-x-3 max-md:gap-y-1.5 max-md:px-4 max-md:py-4">
                      <th scope="row" className="px-4 py-3.5 font-normal max-md:p-0">
                        <span className="block font-medium text-ink">
                          {s.courseCode} · {s.sectionCode}
                        </span>
                        <span className="block text-muted">{s.courseName}</span>
                        {!termId && <span className="block text-xs text-muted">{s.termName}</span>}
                      </th>
                      <td className="px-4 py-3.5 text-muted max-md:col-span-2 max-md:p-0">
                        <span className="mr-1.5 font-medium text-ink md:hidden">Instructor</span>
                        {s.instructorName ?? <Badge tone="warning">Not assigned</Badge>}
                      </td>
                      <td className="whitespace-nowrap px-4 py-3.5 max-md:col-span-2 max-md:p-0">
                        <span className="mr-1.5 font-medium text-ink md:hidden">Seats</span>
                        <Seats section={s} />
                      </td>
                      <td className="px-4 py-3.5 text-muted max-md:col-span-2 max-md:p-0">
                        <ul className="space-y-0.5" aria-label={`Meetings of ${s.courseCode} ${s.sectionCode}`}>
                          {s.meetings.map((m) => (
                            <li key={m.id} className="whitespace-nowrap">
                              <span className="inline-block w-9 font-medium text-ink">{shortDays[m.dayOfWeek]}</span>
                              {hm(m.startTime)}–{hm(m.endTime)} · {m.roomName}
                              {m.type === 'Lab' && <span className="ml-1.5"><Badge>Lab</Badge></span>}
                            </li>
                          ))}
                        </ul>
                      </td>
                      <td className="px-4 py-3.5 text-right max-md:col-start-2 max-md:row-start-1 max-md:p-0">
                        <Button
                          variant="ghost"
                          className="h-8 px-2.5"
                          onClick={() => setDialog({ open: true, section: s })}
                          aria-label={`Edit ${s.courseCode} ${s.sectionCode}`}
                        >
                          <Pencil className="size-4" aria-hidden /> Edit
                        </Button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </TableScroll>
            <Pagination page={sections.data.page} totalPages={sections.data.totalPages} totalCount={sections.data.totalCount} noun="section" onPage={setPage} />
          </>
        )}
      </Card>

      <SectionFormDialog
        open={dialog.open}
        section={dialog.open ? dialog.section : undefined}
        defaultTermId={termId ? Number(termId) : undefined}
        onClose={close}
        onSaved={(s) => {
          saved.show(`${s.courseCode} ${s.sectionCode} saved.`)
          close()
        }}
      />
    </>
  )
}

/** "12 / 30 seats", with a Full badge (text, not only colour) when no seat is left. */
function Seats({ section }: { section: Section }) {
  const full = section.seatsTaken >= section.capacity
  return (
    <span className="inline-flex items-center gap-2">
      <span className="tabular-nums text-ink">
        {section.seatsTaken} / {section.capacity}
      </span>
      {full && <Badge tone="danger">Full</Badge>}
    </span>
  )
}
