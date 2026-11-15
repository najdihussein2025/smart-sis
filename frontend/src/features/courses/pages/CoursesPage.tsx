import { useState } from 'react'
import { BookOpen, Pencil, Plus, Search } from 'lucide-react'
import { useLookups } from '@/features/lookups/hooks'
import { useDebouncedValue } from '@/shared/lib/useDebouncedValue'
import { useSavedMessage } from '@/shared/lib/useSavedMessage'
import { Alert } from '@/shared/ui/Alert'
import { Button } from '@/shared/ui/Button'
import { Card } from '@/shared/ui/Card'
import { EmptyState } from '@/shared/ui/EmptyState'
import { PageHeader } from '@/shared/ui/PageHeader'
import { Pagination } from '@/shared/ui/Pagination'
import { QueryError } from '@/shared/ui/QueryError'
import { SearchField } from '@/shared/ui/SearchField'
import { SelectField } from '@/shared/ui/SelectField'
import { SortHeader, useSort } from '@/shared/ui/SortHeader'
import { TableScroll } from '@/shared/ui/TableScroll'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { CourseFormDialog } from '../components/CourseFormDialog'
import { useCourses } from '../hooks'
import type { CourseListItem } from '../types'

const PAGE_SIZE = 20

type DialogState = { open: false } | { open: true; courseId?: number }
type SortKey = 'code' | 'credits'

/** PB-03 / T08: admin searches, creates and edits courses with their prerequisites and major / elective mapping. */
export function CoursesPage() {
  const [search, setSearch] = useState('')
  const [departmentId, setDepartmentId] = useState('')
  const [page, setPage] = useState(1)
  const term = useDebouncedValue(search.trim())
  const courses = useCourses({ search: term, departmentId: departmentId ? Number(departmentId) : undefined, page, pageSize: PAGE_SIZE })
  const { sorted, sort, toggle } = useSort<CourseListItem, SortKey>(courses.data?.items, (c, key) =>
    key === 'code' ? c.code : c.credits,
  )
  const lookups = useLookups()
  const [dialog, setDialog] = useState<DialogState>({ open: false })
  const saved = useSavedMessage()
  const filtered = !!term || !!departmentId

  const openNew = () => setDialog({ open: true })
  const close = () => setDialog({ open: false })
  const clearFilters = () => {
    setSearch('')
    setDepartmentId('')
    setPage(1)
  }

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Academics"
        title="Courses"
        icon={BookOpen}
        actions={
          <Button onClick={openNew}>
            <Plus className="size-4" aria-hidden /> New course
          </Button>
        }
      >
        Codes, credits, prerequisites and the majors each course counts for.
      </PageHeader>

      {saved.message && <Alert tone="success">{saved.message}</Alert>}

      <Card>
        <div className="grid gap-3 border-b border-line p-4 sm:grid-cols-[minmax(0,1fr)_16rem] sm:items-end sm:px-5">
          <SearchField
            label="Search"
            placeholder="Code or name, e.g. CSC 2"
            value={search}
            onChange={(v) => {
              setSearch(v)
              setPage(1)
            }}
          />
          <SelectField
            label="Department"
            value={departmentId}
            onChange={(e) => {
              setDepartmentId(e.target.value)
              setPage(1)
            }}
          >
            <option value="">All departments</option>
            {lookups.data?.departments.map((d) => (
              <option key={d.id} value={d.id}>
                {d.code} · {d.name}
              </option>
            ))}
          </SelectField>
        </div>

        {courses.isPending ? (
          <TableSkeleton label="Loading courses" />
        ) : courses.isError ? (
          <QueryError error={courses.error} onRetry={() => courses.refetch()} />
        ) : courses.data.items.length === 0 ? (
          filtered ? (
            <EmptyState
              icon={Search}
              title="No courses match"
              action={
                <Button variant="ghost" onClick={clearFilters}>
                  Clear filters
                </Button>
              }
            >
              Try another code or name, or choose “All departments”.
            </EmptyState>
          ) : (
            <EmptyState
              icon={BookOpen}
              title="No courses yet"
              action={
                <Button onClick={openNew}>
                  <Plus className="size-4" aria-hidden /> New course
                </Button>
              }
            >
              Create the first course. Sections and the student catalog are built from courses.
            </EmptyState>
          )
        ) : (
          <>
            <TableScroll>
              <table className="w-full text-left text-sm max-md:block">
                <thead className="border-b border-line bg-canvas/60 text-xs uppercase tracking-wide text-muted max-md:hidden">
                  <tr>
                    <SortHeader label="Code" sortKey="code" sort={sort} onSort={toggle} />
                    <th scope="col" className="px-5 py-3 font-medium">Name</th>
                    <th scope="col" className="px-5 py-3 font-medium">Department</th>
                    <SortHeader label="Credits" sortKey="credits" sort={sort} onSort={toggle} align="right" />
                    <th scope="col" className="px-5 py-3"><span className="sr-only">Actions</span></th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-line max-md:block">
                  {sorted.map((c) => (
                    <tr
                      key={c.id}
                      className="transition-colors hover:bg-canvas/50 max-md:grid max-md:grid-cols-[minmax(0,1fr)_auto] max-md:items-center max-md:gap-x-3 max-md:gap-y-1 max-md:px-4 max-md:py-4"
                    >
                      <th scope="row" className="whitespace-nowrap px-5 py-3.5 font-medium text-ink max-md:p-0">{c.code}</th>
                      <td className="px-5 py-3.5 text-ink max-md:col-span-2 max-md:row-start-2 max-md:p-0">{c.name}</td>
                      <td className="px-5 py-3.5 text-muted max-md:row-start-3 max-md:p-0" title={c.departmentName}>
                        <span className="sr-only md:hidden">Department </span>
                        <span>{c.departmentCode}</span>
                      </td>
                      <td className="tabular px-5 py-3.5 text-right text-muted max-md:col-start-2 max-md:row-start-3 max-md:p-0">
                        <span>{c.credits}</span>
                        <span className="md:hidden"> credits</span>
                      </td>
                      <td className="px-5 py-3.5 text-right max-md:col-start-2 max-md:row-start-1 max-md:-mr-2 max-md:p-0">
                        <Button variant="ghost" size="sm" onClick={() => setDialog({ open: true, courseId: c.id })} aria-label={`Edit ${c.code}`}>
                          <Pencil className="size-4" aria-hidden /> Edit
                        </Button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </TableScroll>
            <Pagination page={courses.data.page} totalPages={courses.data.totalPages} totalCount={courses.data.totalCount} noun="course" onPage={setPage} />
          </>
        )}
      </Card>

      <CourseFormDialog
        open={dialog.open}
        courseId={dialog.open ? dialog.courseId : undefined}
        onClose={close}
        onSaved={(c) => {
          saved.show(`${c.code} saved.`)
          close()
        }}
      />
    </div>
  )
}
