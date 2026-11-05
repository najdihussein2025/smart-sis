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
import { SelectField } from '@/shared/ui/SelectField'
import { TableScroll } from '@/shared/ui/TableScroll'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { TextField } from '@/shared/ui/TextField'
import { CourseFormDialog } from '../components/CourseFormDialog'
import { useCourses } from '../hooks'

const PAGE_SIZE = 20

type DialogState = { open: false } | { open: true; courseId?: number }

/** PB-03 / T08: admin searches, creates and edits courses with their prerequisites and major / elective mapping. */
export function CoursesPage() {
  const [search, setSearch] = useState('')
  const [departmentId, setDepartmentId] = useState('')
  const [page, setPage] = useState(1)
  const term = useDebouncedValue(search.trim())
  const courses = useCourses({ search: term, departmentId: departmentId ? Number(departmentId) : undefined, page, pageSize: PAGE_SIZE })
  const lookups = useLookups()
  const [dialog, setDialog] = useState<DialogState>({ open: false })
  const saved = useSavedMessage()
  const filtered = !!term || !!departmentId

  const openNew = () => setDialog({ open: true })
  const close = () => setDialog({ open: false })

  return (
    <>
      <PageHeader
        eyebrow="Administration"
        title="Courses"
        actions={
          <Button onClick={openNew}>
            <Plus className="size-4" aria-hidden /> New course
          </Button>
        }
      >
        Codes, credits, prerequisites and the majors each course counts for.
      </PageHeader>

      {saved.message && (
        <div className="mb-4">
          <Alert tone="success">{saved.message}</Alert>
        </div>
      )}

      <Card>
        <div className="grid gap-3 border-b border-line p-4 sm:grid-cols-[minmax(0,1fr)_16rem]">
          <div className="relative">
            <Search className="pointer-events-none absolute left-3 top-[38px] size-[18px] text-muted" aria-hidden />
            <TextField
              label="Search"
              type="search"
              placeholder="Code or name, e.g. CSC 2"
              className="pl-10"
              value={search}
              onChange={(e) => {
                setSearch(e.target.value)
                setPage(1)
              }}
            />
          </div>
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
            <EmptyState icon={Search} title="No courses match">
              Try another code or name, or choose “All departments”.
            </EmptyState>
          ) : (
            <EmptyState icon={BookOpen} title="No courses yet">
              Create the first course. Sections and the student catalog are built from courses.
              <div className="mt-4">
                <Button onClick={openNew}>
                  <Plus className="size-4" aria-hidden /> New course
                </Button>
              </div>
            </EmptyState>
          )
        ) : (
          <>
            <TableScroll>
              <table className="w-full text-left text-sm">
                <thead className="border-b border-line text-xs uppercase tracking-wide text-muted">
                  <tr>
                    <th scope="col" className="px-5 py-3 font-medium">Code</th>
                    <th scope="col" className="px-5 py-3 font-medium">Name</th>
                    <th scope="col" className="px-5 py-3 font-medium max-sm:hidden">Department</th>
                    <th scope="col" className="px-5 py-3 font-medium text-right max-sm:hidden">Credits</th>
                    <th scope="col" className="px-5 py-3"><span className="sr-only">Actions</span></th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-line">
                  {courses.data.items.map((c) => (
                    <tr key={c.id}>
                      <th scope="row" className="whitespace-nowrap px-5 py-3.5 font-medium text-ink max-sm:align-top max-sm:pl-4 max-sm:pr-2">{c.code}</th>
                      <td className="px-5 py-3.5 text-ink">
                        {c.name}
                        {/* On phones the department and credits columns are hidden; show them under the name. */}
                        <span className="mt-0.5 block text-xs text-muted sm:hidden">
                          {c.departmentCode} · {c.credits} credits
                        </span>
                      </td>
                      <td className="px-5 py-3.5 text-muted max-sm:hidden" title={c.departmentName}>{c.departmentCode}</td>
                      <td className="px-5 py-3.5 text-right tabular-nums text-muted max-sm:hidden">{c.credits}</td>
                      <td className="px-5 py-3.5 text-right">
                        <Button variant="ghost" className="h-8 px-2.5" onClick={() => setDialog({ open: true, courseId: c.id })} aria-label={`Edit ${c.code}`}>
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
    </>
  )
}
