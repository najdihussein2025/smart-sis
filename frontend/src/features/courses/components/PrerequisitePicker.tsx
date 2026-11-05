import { useId, useState } from 'react'
import { Plus, Search, X } from 'lucide-react'
import { cn } from '@/shared/lib/cn'
import { useDebouncedValue } from '@/shared/lib/useDebouncedValue'
import { useCourses } from '../hooks'
import type { CourseRef } from '../types'

interface Props {
  /** Chosen prerequisites, in the order they were added. */
  value: CourseRef[]
  onChange: (value: CourseRef[]) => void
  /** The course being edited: it can't be its own prerequisite, so it isn't offered. */
  excludeId?: number
  error?: string
}

/** Multi-select of courses: search by code or name, add from the results, remove with the × on each chip. */
export function PrerequisitePicker({ value, onChange, excludeId, error }: Props) {
  const id = useId()
  const [search, setSearch] = useState('')
  const term = useDebouncedValue(search.trim(), 250)
  const results = useCourses({ search: term, pageSize: 8 }, term.length > 0)
  const chosen = new Set(value.map((c) => c.id))
  const options = (results.data?.items ?? []).filter((c) => c.id !== excludeId && !chosen.has(c.id))

  return (
    <fieldset className="space-y-2" aria-describedby={error ? `${id}-error` : undefined}>
      <legend className="block text-sm font-medium text-ink">Prerequisites</legend>
      <p className="text-[13px] text-muted">Courses a student must pass (D or better) before enrolling.</p>

      {value.length > 0 ? (
        <ul className="flex flex-wrap gap-2" aria-label="Chosen prerequisites">
          {value.map((c) => (
            <li key={c.id} className="inline-flex items-center gap-1 rounded-full border border-line bg-brand-50 py-1 pl-3 pr-1 text-sm text-ink">
              <span className="font-medium">{c.code}</span>
              <span className="hidden text-muted sm:inline">{c.name}</span>
              <button
                type="button"
                onClick={() => onChange(value.filter((p) => p.id !== c.id))}
                aria-label={`Remove ${c.code}`}
                className="grid size-6 place-items-center rounded-full text-muted hover:bg-surface hover:text-ink"
              >
                <X className="size-3.5" aria-hidden />
              </button>
            </li>
          ))}
        </ul>
      ) : (
        <p className="text-sm text-muted">None.</p>
      )}

      <div className="relative">
        <label htmlFor={`${id}-search`} className="sr-only">
          Search courses to add as prerequisites
        </label>
        <Search className="pointer-events-none absolute left-3 top-3 size-[18px] text-muted" aria-hidden />
        <input
          id={`${id}-search`}
          type="search"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search by code or name to add…"
          autoComplete="off"
          aria-invalid={!!error}
          className={cn(
            'block w-full h-11 rounded-lg border bg-surface pl-10 pr-3 text-[15px] text-ink placeholder:text-muted/70',
            'focus:outline-none focus:ring-2 focus:ring-accent/40',
            error ? 'border-danger' : 'border-line focus:border-brand',
          )}
        />
      </div>

      {term && search.trim() && (
        <div className="rounded-lg border border-line">
          {results.isPending ? (
            <p className="px-3 py-2 text-sm text-muted">Searching…</p>
          ) : results.isError ? (
            <p className="px-3 py-2 text-sm text-danger">Could not search courses. Try again.</p>
          ) : options.length === 0 ? (
            <p className="px-3 py-2 text-sm text-muted">No other course matches “{term}”.</p>
          ) : (
            <ul aria-label="Matching courses" className="max-h-48 divide-y divide-line overflow-y-auto">
              {options.map((c) => (
                <li key={c.id}>
                  <button
                    type="button"
                    onClick={() => {
                      onChange([...value, { id: c.id, code: c.code, name: c.name }])
                      setSearch('')
                    }}
                    aria-label={`Add ${c.code} ${c.name}`}
                    className="flex w-full items-center gap-2 px-3 py-2 text-left text-sm hover:bg-brand-50"
                  >
                    <Plus className="size-4 shrink-0 text-muted" aria-hidden />
                    <span className="font-medium text-ink">{c.code}</span>
                    <span className="truncate text-muted">{c.name}</span>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}

      {error && (
        <p id={`${id}-error`} role="alert" className="text-[13px] text-danger">
          {error}
        </p>
      )}
    </fieldset>
  )
}
