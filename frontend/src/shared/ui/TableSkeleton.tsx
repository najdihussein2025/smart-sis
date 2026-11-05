/** Loading placeholder for a list screen. */
export function TableSkeleton({ label, rows = 3 }: { label: string; rows?: number }) {
  return (
    <div className="space-y-3 p-5" aria-busy="true" aria-label={label}>
      {Array.from({ length: rows }, (_, i) => (
        <div key={i} className="h-10 animate-pulse rounded-lg bg-canvas" />
      ))}
    </div>
  )
}
