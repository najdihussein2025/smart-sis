import { useMemo, useState, type ReactNode } from 'react'
import { Building2, Search } from 'lucide-react'
import { Alert } from '@/shared/ui/Alert'
import { Button } from '@/shared/ui/Button'
import { Card } from '@/shared/ui/Card'
import { CardHeader } from '@/shared/ui/CardHeader'
import { EmptyState } from '@/shared/ui/EmptyState'
import { PageHeader } from '@/shared/ui/PageHeader'
import { QueryError } from '@/shared/ui/QueryError'
import { SearchField } from '@/shared/ui/SearchField'
import { Tabs } from '@/shared/ui/Tabs'
import { TableScroll } from '@/shared/ui/TableScroll'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { useLookups } from '../hooks'
import type { Lookups } from '../types'

type ListKey = 'campuses' | 'departments' | 'majors' | 'rooms' | 'instructors'

const LISTS: { value: ListKey; label: string; noun: string; description: string }[] = [
  { value: 'departments', label: 'Departments', noun: 'departments', description: 'Own courses and instructors.' },
  { value: 'majors', label: 'Majors', noun: 'majors', description: 'Each student studies one; the credit cap is the default per term.' },
  { value: 'campuses', label: 'Campuses', noun: 'campuses', description: 'Where students are registered and rooms are.' },
  { value: 'rooms', label: 'Rooms', noun: 'rooms', description: 'Rooms sections can meet in, with their seat capacity.' },
  { value: 'instructors', label: 'Instructors', noun: 'instructors', description: 'Active instructors who can be given sections.' },
]

/** A column: header text, the cell, and whether it holds a number (right-aligned, tabular). */
interface Column<T> {
  header: string
  cell: (row: T) => ReactNode
  numeric?: boolean
}

interface Row {
  id: number
  /** Lower-case text the search box matches against. */
  text: string
  /** Name shown as the card title on phones and the row header on desktop. */
  title: ReactNode
}

/** Read-only view of the reference lists the admin forms pick from (GET /api/admin/lookups). */
export function LookupsPage() {
  const lookups = useLookups()
  const [tab, setTab] = useState<ListKey>('departments')
  const [search, setSearch] = useState('')
  const term = search.trim().toLowerCase()

  return (
    <div className="space-y-6">
      <PageHeader eyebrow="Setup" title="Lookups" icon={Building2}>
        The campuses, departments, majors, rooms and instructors that the user, course and section forms pick from.
      </PageHeader>

      <Alert tone="info" title="Read-only for now">
        Editing these lists isn’t available in the app yet. Instructors are added and removed on the Users page.
      </Alert>

      {lookups.isPending ? (
        <Card>
          <TableSkeleton label="Loading lookups" rows={5} />
        </Card>
      ) : lookups.isError ? (
        <Card>
          <QueryError error={lookups.error} onRetry={() => lookups.refetch()} />
        </Card>
      ) : (
        <>
          <Tabs
            label="Lookup lists"
            value={tab}
            onChange={(v) => {
              setTab(v)
              setSearch('')
            }}
            items={LISTS.map((l) => ({ value: l.value, label: l.label, count: lookups.data[l.value].length }))}
          />
          <LookupList data={lookups.data} list={tab} search={search} term={term} onSearch={setSearch} />
        </>
      )}
    </div>
  )
}

function LookupList({ data, list, search, term, onSearch }: {
  data: Lookups
  list: ListKey
  search: string
  term: string
  onSearch: (v: string) => void
}) {
  const meta = LISTS.find((l) => l.value === list)!
  const { rows, columns } = useMemo(() => build(data, list), [data, list])
  const shown = term ? rows.filter((r) => r.text.includes(term)) : rows

  return (
    <Card>
      <CardHeader title={meta.label} description={meta.description} />
      <div className="border-b border-line p-4 sm:px-5">
        <div className="sm:max-w-sm">
          <SearchField label={`Search ${meta.noun}`} srOnlyLabel placeholder={`Search ${meta.noun}`} value={search} onChange={onSearch} />
        </div>
      </div>

      {rows.length === 0 ? (
        <EmptyState icon={Building2} title={`No ${meta.noun} yet`}>
          The list is empty. It’s filled from the database seed, so ask the team to add the first entries.
        </EmptyState>
      ) : shown.length === 0 ? (
        <EmptyState
          icon={Search}
          title={`No ${meta.noun} match`}
          action={
            <Button variant="ghost" onClick={() => onSearch('')}>
              Clear search
            </Button>
          }
        >
          Nothing matches “{search.trim()}”. Try another name or code.
        </EmptyState>
      ) : (
        <TableScroll>
          <table aria-label={meta.label} className="w-full text-left text-sm max-md:block">
            <thead className="border-b border-line bg-canvas/60 text-xs uppercase tracking-wide text-muted max-md:hidden">
              <tr>
                <th scope="col" className="px-5 py-3 font-medium">Name</th>
                {columns.map((c) => (
                  <th key={c.header} scope="col" className={`px-5 py-3 font-medium ${c.numeric ? 'text-right' : ''}`}>
                    {c.header}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-line max-md:block">
              {shown.map((r) => (
                <tr key={r.id} className="transition-colors hover:bg-canvas/50 max-md:grid max-md:gap-y-1 max-md:px-4 max-md:py-3.5">
                  <th scope="row" className="px-5 py-3 font-medium text-ink max-md:p-0">{r.title}</th>
                  {columns.map((c) => (
                    <td
                      key={c.header}
                      className={`px-5 py-3 text-muted max-md:p-0 ${c.numeric ? 'tabular text-right max-md:text-left' : ''}`}
                    >
                      <span className="mr-1.5 md:hidden">{c.header}:</span>
                      {c.cell(r)}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </TableScroll>
      )}
      {rows.length > 0 && (
        <p role="status" className="border-t border-line px-5 py-3 text-sm text-muted">
          {term ? `${shown.length} of ${rows.length} ${meta.noun}` : `${rows.length} ${meta.noun}`}
        </p>
      )}
    </Card>
  )
}

type Built = { rows: (Row & { cells: Record<string, ReactNode> })[]; columns: Column<Row & { cells: Record<string, ReactNode> }>[] }

/** Turns one lookup list into rows with their search text and extra columns. */
function build(data: Lookups, list: ListKey): Built {
  const col = (header: string, numeric = false): Built['columns'][number] => ({ header, numeric, cell: (r) => r.cells[header] })
  const departments = new Map(data.departments.map((d) => [d.id, d]))
  switch (list) {
    case 'campuses':
      return {
        columns: [col('Rooms', true)],
        rows: data.campuses.map((c) => ({
          id: c.id,
          title: c.name,
          text: c.name.toLowerCase(),
          cells: { Rooms: data.rooms.filter((r) => r.campusId === c.id).length },
        })),
      }
    case 'departments':
      return {
        columns: [col('Code'), col('Majors', true)],
        rows: data.departments.map((d) => ({
          id: d.id,
          title: d.name,
          text: `${d.code} ${d.name}`.toLowerCase(),
          cells: {
            Code: <span className="font-mono text-ink">{d.code}</span>,
            Majors: data.majors.filter((m) => m.departmentId === d.id).length,
          },
        })),
      }
    case 'majors':
      return {
        columns: [col('Department'), col('Credit cap', true)],
        rows: data.majors.map((m) => {
          const dept = departments.get(m.departmentId)
          return {
            id: m.id,
            title: m.name,
            text: `${m.name} ${dept?.code ?? ''} ${dept?.name ?? ''}`.toLowerCase(),
            cells: {
              Department: dept ? <><span className="font-mono text-ink">{dept.code}</span> · {dept.name}</> : '—',
              'Credit cap': `${m.defaultMaxCredits} cr`,
            },
          }
        }),
      }
    case 'rooms':
      return {
        columns: [col('Campus'), col('Capacity', true)],
        rows: data.rooms.map((r) => ({
          id: r.id,
          title: r.name,
          text: `${r.name} ${r.campusName}`.toLowerCase(),
          cells: { Campus: r.campusName, Capacity: `${r.capacity} seats` },
        })),
      }
    case 'instructors':
      return {
        columns: [col('University ID', true), col('Department')],
        rows: data.instructors.map((i) => {
          const dept = departments.get(i.departmentId)
          return {
            id: i.id,
            title: i.fullName,
            text: `${i.fullName} ${i.universityId} ${dept?.code ?? ''} ${dept?.name ?? ''}`.toLowerCase(),
            cells: { 'University ID': i.universityId, Department: dept ? `${dept.code} · ${dept.name}` : '—' },
          }
        }),
      }
  }
}
