import { Check, ShoppingCart } from 'lucide-react'
import { Button } from '@/shared/ui/Button'
import type { CareerSection } from '../types'

interface Props {
  courseCode: string
  sections: CareerSection[]
  /** Section id being added right now, if any. */
  addingId?: number
  onAdd: (sectionId: number) => void
}

/** This term's sections of a course, each with "Add to cart" / "In Cart" / "Enrolled" / "Full". */
export function SectionCartButtons({ courseCode, sections, addingId, onAdd }: Props) {
  if (sections.length === 0) return <p className="text-sm text-muted">Not offered this term.</p>
  return (
    <ul className="flex flex-wrap gap-2" aria-label={`Sections of ${courseCode}`}>
      {sections.map((s) => {
        const label = `${courseCode} ${s.sectionCode}`
        return (
          <li key={s.sectionId}>
            {s.status === 'Enrolled' ? (
              <Button variant="secondary" className="h-9" disabled aria-label={`${label}: Enrolled`}>
                <Check className="size-4" aria-hidden /> {s.sectionCode} · Enrolled
              </Button>
            ) : s.status === 'InCart' ? (
              <Button variant="secondary" className="h-9" disabled aria-label={`${label}: In Cart`}>
                <ShoppingCart className="size-4" aria-hidden /> {s.sectionCode} · In Cart
              </Button>
            ) : s.seatsLeft === 0 ? (
              <Button variant="secondary" className="h-9" disabled aria-label={`${label}: Full`}>
                {s.sectionCode} · Full
              </Button>
            ) : (
              <Button
                className="h-9"
                loading={addingId === s.sectionId}
                onClick={() => onAdd(s.sectionId)}
                aria-label={`Add ${label} to cart`}
              >
                <ShoppingCart className="size-4" aria-hidden /> Add {s.sectionCode} to cart
              </Button>
            )}
          </li>
        )
      })}
    </ul>
  )
}

/** A horizontal bar with the percentage written next to it, so colour is never the only signal. */
export function PercentBar({ value, label }: { value: number; label: string }) {
  return (
    <div className="flex items-center gap-3">
      <div
        className="h-2.5 flex-1 overflow-hidden rounded-full bg-canvas"
        role="meter"
        aria-label={label}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={value}
      >
        <div className="h-full rounded-full bg-accent" style={{ width: `${Math.max(0, Math.min(100, value))}%` }} />
      </div>
      <span className="w-12 shrink-0 text-right text-sm font-medium tabular-nums text-ink">{value}%</span>
    </div>
  )
}
