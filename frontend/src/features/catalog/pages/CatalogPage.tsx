import { useState } from 'react'
import { Link } from 'react-router'
import { BookOpen, ChevronRight, Search } from 'lucide-react'
import { useDebouncedValue } from '@/shared/lib/useDebouncedValue'
import { Badge } from '@/shared/ui/Badge'
import { Card } from '@/shared/ui/Card'
import { EmptyState } from '@/shared/ui/EmptyState'
import { PageHeader } from '@/shared/ui/PageHeader'
import { QueryError } from '@/shared/ui/QueryError'
import { SelectField } from '@/shared/ui/SelectField'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { Tabs } from '@/shared/ui/Tabs'
import { WindowBadge } from '@/shared/ui/WindowBadge'
import { TextField } from '@/shared/ui/TextField'
import { useCatalog, useCatalogMajors } from '../hooks'
import type { CatalogCourse, CourseType } from '../types'

type Tab = 'All' | CourseType

/** PB-04 / T15: the student browses this term's courses by Major / Elective tab and major, then opens a course. */
export function CatalogPage() {
  const [tab, setTab] = useState<Tab>('All')
  const [majorId, setMajorId] = useState('')
  const [search, setSearch] = useState('')
  const term = useDebouncedValue(search.trim())
  const majors = useCatalogMajors()
  const catalog = useCatalog({
    type: tab === 'All' ? undefined : tab,
    majorId: majorId ? Number(majorId) : undefined,
    search: term || undefined,
  })

  // Until the student picks a major, the API filters by their own one and says which.
  const shownMajor = majorId || (catalog.data?.majorId ? String(catalog.data.majorId) : '')

  return (
    <>
      <PageHeader eyebrow={catalog.data?.termName ?? 'Course catalog'} title="Course catalog">
        <span className="inline-flex flex-wrap items-center gap-2">
          Courses offered this term. Open one to see its sections and add it to your cart.
          {catalog.data && <WindowBadge window={catalog.data.window} />}
        </span>
      </PageHeader>

      <Card>
        <div className="grid gap-3 border-b border-line p-4 md:grid-cols-[auto_1fr_1fr] md:items-end">
          <Tabs
            label="Course type"
            value={tab}
            onChange={setTab}
            items={[
              { value: 'All', label: 'All' },
              { value: 'Major', label: 'Major' },
              { value: 'Elective', label: 'Elective' },
            ]}
          />
          <SelectField label="Major" value={shownMajor} onChange={(e) => setMajorId(e.target.value)}>
            {majors.data?.map((m) => (
              <option key={m.id} value={m.id}>
                {m.name}
              </option>
            ))}
          </SelectField>
          <div className="relative">
            <Search className="pointer-events-none absolute left-3 top-[38px] size-[18px] text-muted" aria-hidden />
            <TextField label="Search" type="search" placeholder="Code or name" className="pl-10" value={search} onChange={(e) => setSearch(e.target.value)} />
          </div>
        </div>

        {catalog.isPending ? (
          <TableSkeleton label="Loading courses" />
        ) : catalog.isError ? (
          <QueryError error={catalog.error} onRetry={() => catalog.refetch()} />
        ) : catalog.data.termId === null ? (
          <EmptyState icon={BookOpen} title="No current term">
            The catalog opens once the university sets the current term. Check back soon.
          </EmptyState>
        ) : catalog.data.courses.length === 0 ? (
          <EmptyState icon={BookOpen} title="No courses here">
            No course matches this tab, major and search. Try another tab or clear the search.
          </EmptyState>
        ) : (
          <ul className="divide-y divide-line" aria-label="Courses">
            {catalog.data.courses.map((c) => (
              <CourseRow key={c.id} course={c} />
            ))}
          </ul>
        )}
      </Card>
    </>
  )
}

function CourseRow({ course: c }: { course: CatalogCourse }) {
  return (
    <li>
      <Link
        to={`/student/courses/${c.id}`}
        className="flex items-center gap-4 px-4 py-4 transition-colors hover:bg-canvas focus-visible:bg-canvas"
        aria-label={`${c.code} ${c.name}`}
      >
        <div className="min-w-0 flex-1">
          <p className="flex flex-wrap items-center gap-2">
            <span className="font-medium text-ink">{c.code}</span>
            <span className="text-ink">{c.name}</span>
            {c.type && <Badge tone={c.type === 'Major' ? 'brand' : 'neutral'}>{c.type}</Badge>}
            {c.status === 'InCart' && <Badge tone="warning">In cart</Badge>}
            {c.status === 'Enrolled' && <Badge tone="success">Enrolled</Badge>}
          </p>
          <p className="mt-0.5 text-sm text-muted">
            {c.credits} credits · {c.departmentName} · {c.sectionCount} {c.sectionCount === 1 ? 'section' : 'sections'} ·{' '}
            {c.seatsLeft === 0 ? 'Full' : `${c.seatsLeft} seats left`}
          </p>
        </div>
        <ChevronRight className="size-5 shrink-0 text-muted" aria-hidden />
      </Link>
    </li>
  )
}
