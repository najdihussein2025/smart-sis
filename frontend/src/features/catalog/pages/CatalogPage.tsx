import { useState } from 'react'
import { Link } from 'react-router'
import { BookOpen, ChevronDown, ChevronRight, SlidersHorizontal } from 'lucide-react'
import { useDebouncedValue } from '@/shared/lib/useDebouncedValue'
import { Badge } from '@/shared/ui/Badge'
import { Button } from '@/shared/ui/Button'
import { Card } from '@/shared/ui/Card'
import { EmptyState } from '@/shared/ui/EmptyState'
import { PageHeader } from '@/shared/ui/PageHeader'
import { QueryError } from '@/shared/ui/QueryError'
import { SearchField } from '@/shared/ui/SearchField'
import { SelectField } from '@/shared/ui/SelectField'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { Tabs } from '@/shared/ui/Tabs'
import { WindowBadge } from '@/shared/ui/WindowBadge'
import { cn } from '@/shared/lib/cn'
import { useCatalog, useCatalogMajors } from '../hooks'
import type { CatalogCourse, CourseType } from '../types'

type Tab = 'All' | CourseType

/**
 * PB-04 / T15: the student browses this term's courses by Major / Elective tab and program, then opens a course.
 * "Major" on the tab is the course type (a core course of the program); the dropdown picks the program itself, so it
 * is labelled "Program" to keep the two apart.
 */
export function CatalogPage() {
  const [tab, setTab] = useState<Tab>('All')
  const [majorId, setMajorId] = useState('')
  const [search, setSearch] = useState('')
  const [filtersOpen, setFiltersOpen] = useState(false)
  const term = useDebouncedValue(search.trim())
  const majors = useCatalogMajors()
  const catalog = useCatalog({
    type: tab === 'All' ? undefined : tab,
    majorId: majorId ? Number(majorId) : undefined,
    search: term || undefined,
  })

  // Until the student picks a program, the API filters by their own one and says which.
  const shownMajor = majorId || (catalog.data?.majorId ? String(catalog.data.majorId) : '')
  const filtered = tab !== 'All' || search.trim() !== '' || majorId !== ''

  return (
    <>
      <PageHeader eyebrow={catalog.data?.termName ?? 'Academics'} title="Course catalog" icon={BookOpen}>
        <span className="inline-flex flex-wrap items-center gap-2">
          Courses offered this term. Open one to see its sections and add it to your cart.
          {catalog.data && <WindowBadge window={catalog.data.window} />}
        </span>
      </PageHeader>

      <Card>
        <div className="space-y-3 border-b border-line p-4 sm:p-5">
          <div className="grid gap-3 md:grid-cols-[minmax(0,1fr)_minmax(0,16rem)] md:items-end">
            <SearchField label="Search courses" placeholder="Code or name" value={search} onChange={setSearch} />
            <div className="hidden md:block">
              <ProgramSelect value={shownMajor} onChange={setMajorId} majors={majors.data} />
            </div>
          </div>
          <div className="flex flex-wrap items-center justify-between gap-3">
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
            <Button
              variant="secondary"
              size="sm"
              className="md:hidden"
              aria-expanded={filtersOpen}
              aria-controls="catalog-filters"
              onClick={() => setFiltersOpen((o) => !o)}
            >
              <SlidersHorizontal className="size-4" aria-hidden /> Filters
              <ChevronDown className={cn('size-4 transition-transform', filtersOpen && 'rotate-180')} aria-hidden />
            </Button>
          </div>
          {filtersOpen && (
            <div id="catalog-filters" className="md:hidden">
              <ProgramSelect value={shownMajor} onChange={setMajorId} majors={majors.data} />
            </div>
          )}
        </div>

        {catalog.isPending ? (
          <TableSkeleton label="Loading courses" rows={5} />
        ) : catalog.isError ? (
          <QueryError error={catalog.error} onRetry={() => catalog.refetch()} />
        ) : catalog.data.termId === null ? (
          <EmptyState icon={BookOpen} title="No current term">
            The catalog opens once the university sets the current term. Check back soon.
          </EmptyState>
        ) : catalog.data.courses.length === 0 ? (
          <EmptyState
            icon={BookOpen}
            title="No courses here"
            action={
              filtered && (
                <Button
                  variant="secondary"
                  onClick={() => {
                    setTab('All')
                    setSearch('')
                    setMajorId('')
                  }}
                >
                  Clear filters
                </Button>
              )
            }
          >
            No course matches this tab, program and search. Try another tab or clear the search.
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

function ProgramSelect({ value, onChange, majors }: { value: string; onChange: (v: string) => void; majors?: { id: number; name: string }[] }) {
  return (
    <SelectField label="Program" value={value} onChange={(e) => onChange(e.target.value)}>
      {majors?.map((m) => (
        <option key={m.id} value={m.id}>
          {m.name}
        </option>
      ))}
    </SelectField>
  )
}

function CourseRow({ course: c }: { course: CatalogCourse }) {
  const hasBadges = c.type !== null || c.status !== 'Available'
  return (
    <li>
      <Link
        to={`/student/courses/${c.id}`}
        className="flex items-center gap-4 px-4 py-4 transition-colors hover:bg-canvas focus-visible:bg-canvas sm:px-5"
        aria-label={`${c.code} ${c.name}`}
      >
        <div className="min-w-0 flex-1 space-y-1.5">
          <p className="text-ink">
            <span className="font-semibold">{c.code}</span> <span>{c.name}</span>
          </p>
          {hasBadges && (
            <p className="flex flex-wrap gap-1.5">
              {c.type && <Badge tone={c.type === 'Major' ? 'brand' : 'outline'}>{c.type}</Badge>}
              {c.status === 'InCart' && (
                <Badge tone="warning" dot>
                  In cart
                </Badge>
              )}
              {c.status === 'Enrolled' && (
                <Badge tone="success" dot>
                  Enrolled
                </Badge>
              )}
            </p>
          )}
          <p className="tabular flex flex-wrap gap-x-1.5 text-sm text-muted">
            <span className="whitespace-nowrap">{c.credits} credits</span>
            <span aria-hidden>·</span>
            <span>{c.departmentName}</span>
            <span aria-hidden>·</span>
            <span className="whitespace-nowrap">
              {c.sectionCount} {c.sectionCount === 1 ? 'section' : 'sections'}
            </span>
            <span aria-hidden>·</span>
            {c.seatsLeft === 0 ? (
              <span className="font-medium whitespace-nowrap text-danger">Full</span>
            ) : (
              <span className="whitespace-nowrap">{c.seatsLeft} seats left</span>
            )}
          </p>
        </div>
        <ChevronRight className="size-5 shrink-0 text-muted" aria-hidden />
      </Link>
    </li>
  )
}
