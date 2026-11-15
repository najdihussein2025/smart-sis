import { useMemo, useState } from 'react'
import { ArrowDown, ArrowUp, ArrowUpDown } from 'lucide-react'
import { cn } from '@/shared/lib/cn'

export type SortDir = 'asc' | 'desc'
export interface SortState<K extends string> {
  key: K
  dir: SortDir
}

/**
 * Client-side sort for the rows already on screen. `get` returns the value to compare per key (string, number, date
 * string or null; nulls sort last).
 */
export function useSort<T, K extends string>(
  rows: T[] | undefined,
  get: (row: T, key: K) => string | number | null | undefined,
  initial: SortState<K> | null = null,
) {
  const [sort, setSort] = useState<SortState<K> | null>(initial)
  const sorted = useMemo(() => {
    if (!rows || !sort) return rows ?? []
    const factor = sort.dir === 'asc' ? 1 : -1
    return [...rows].sort((a, b) => {
      const x = get(a, sort.key)
      const y = get(b, sort.key)
      if (x == null && y == null) return 0
      if (x == null) return 1
      if (y == null) return -1
      if (typeof x === 'number' && typeof y === 'number') return (x - y) * factor
      return String(x).localeCompare(String(y), undefined, { numeric: true, sensitivity: 'base' }) * factor
    })
    // `get` is a pure accessor; rows and sort are what change.
  }, [rows, sort])
  const toggle = (key: K) =>
    setSort((s) => (s?.key === key ? { key, dir: s.dir === 'asc' ? 'desc' : 'asc' } : { key, dir: 'asc' }))
  return { sorted, sort, toggle }
}

/**
 * A sortable column header: a button inside the `<th>`, with `aria-sort` on the cell so screen readers announce the
 * order. Put `align="right"` on number columns.
 */
export function SortHeader<K extends string>({ label, sortKey, sort, onSort, align = 'left', className }: {
  label: string
  sortKey: K
  sort: SortState<K> | null
  onSort: (key: K) => void
  align?: 'left' | 'right'
  className?: string
}) {
  const active = sort?.key === sortKey
  const Icon = !active ? ArrowUpDown : sort.dir === 'asc' ? ArrowUp : ArrowDown
  return (
    <th
      scope="col"
      aria-sort={active ? (sort.dir === 'asc' ? 'ascending' : 'descending') : 'none'}
      className={cn('px-5 py-3 font-medium', align === 'right' && 'text-right', className)}
    >
      <button
        type="button"
        onClick={() => onSort(sortKey)}
        className={cn(
          'group -mx-1.5 inline-flex cursor-pointer items-center gap-1 rounded px-1.5 py-0.5 hover:text-ink',
          align === 'right' && 'flex-row-reverse',
          active && 'text-ink',
        )}
      >
        {label}
        <Icon className={cn('size-3.5', active ? 'opacity-100' : 'opacity-40 group-hover:opacity-80')} aria-hidden />
      </button>
    </th>
  )
}
