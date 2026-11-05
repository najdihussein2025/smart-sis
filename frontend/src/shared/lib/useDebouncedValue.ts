import { useEffect, useState } from 'react'

/** The value, updated only after it has stopped changing for `ms` (search boxes, so each key press isn't a request). */
export function useDebouncedValue<T>(value: T, ms = 300): T {
  const [debounced, setDebounced] = useState(value)
  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), ms)
    return () => clearTimeout(timer)
  }, [value, ms])
  return debounced
}
