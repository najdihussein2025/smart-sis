import { useEffect, useId, useRef, useState, type CSSProperties } from 'react'
import { DayPicker } from 'react-day-picker'
import 'react-day-picker/style.css'
import { CalendarDays } from 'lucide-react'
import { cn } from '@/shared/lib/cn'
import { dateToIso, dmyTemplateRest, dmyToIso, isoToDate, isoToDmy, maskDmy } from '@/shared/lib/dates'

export interface DateFieldProps {
  label: string
  /** ISO date "yyyy-mm-dd", or "" when empty. */
  value: string
  onChange: (iso: string) => void
  onBlur?: () => void
  /** Earliest allowed day (ISO). Earlier days are greyed out in the calendar and refused when typed. */
  min?: string
  /** Latest allowed day (ISO). */
  max?: string
  error?: string
  /** Red border without a message under the field (the parent shows the message). */
  invalid?: boolean
  hint?: string
  /** Hide the visible label (when a group heading already names the field). The input keeps it as its accessible name. */
  srOnlyLabel?: boolean
}

/**
 * Date input that always reads DD/MM/YYYY, whatever the browser's language (a native date input follows the
 * computer's locale, which is MM/DD/YYYY on many machines). Type the date or pick it from the calendar.
 */
export function DateField({ label, value, onChange, onBlur, min, max, error, invalid, hint, srOnlyLabel }: DateFieldProps) {
  const id = useId()
  const [text, setText] = useState(isoToDmy(value))
  const [refused, setRefused] = useState<string | null>(null)
  const [open, setOpen] = useState(false)
  const [position, setPosition] = useState<CSSProperties>({})
  const box = useRef<HTMLDivElement>(null)
  const field = useRef<HTMLDivElement>(null)

  // Follow outside changes (form reset, a value chosen in the calendar).
  useEffect(() => setText(isoToDmy(value)), [value])

  // The calendar floats above everything (fixed position), so a scrolling dialog never cuts it off.
  // Its place is worked out before it opens: rendered unplaced for even one frame it would sit in the dialog's flow
  // and push the content around.
  const toggle = () => {
    if (open) return setOpen(false)
    const place = field.current && calendarPlace(field.current)
    if (!place) return
    setPosition(place)
    setOpen(true)
  }

  useEffect(() => {
    if (!open) return
    const onDown = (e: MouseEvent) => {
      if (box.current && !box.current.contains(e.target as Node)) setOpen(false)
    }
    // When the page or dialog scrolls (a phone's momentum scroll can still be running when the calendar opens),
    // the calendar follows its field, and closes only once the field has left the screen.
    const follow = (e: Event) => {
      if (e.target instanceof Node && box.current?.contains(e.target)) return // scrolling inside the calendar itself
      const place = field.current && calendarPlace(field.current)
      if (place) setPosition(place)
      else setOpen(false)
    }
    document.addEventListener('mousedown', onDown)
    window.addEventListener('scroll', follow, true)
    window.addEventListener('resize', follow)
    return () => {
      document.removeEventListener('mousedown', onDown)
      window.removeEventListener('scroll', follow, true)
      window.removeEventListener('resize', follow)
    }
  }, [open])

  const outOfRange = (iso: string) =>
    min && iso < min ? `Choose ${isoToDmy(min)} or later.` : max && iso > max ? `Choose ${isoToDmy(max)} or earlier.` : null

  const accept = (iso: string) => {
    setRefused(null)
    setText(isoToDmy(iso))
    onChange(iso)
  }

  // Typed text is checked when the field loses focus. A date outside the allowed range is refused and the
  // previous value comes back, so an earlier-than-allowed date can never be entered.
  const commitTyped = () => {
    onBlur?.()
    if (text.trim() === '') {
      if (value) accept('')
      return
    }
    const iso = dmyToIso(text)
    if (!iso) {
      // Complete but not a real day (31/04, 29/02/2027), or not finished yet.
      setRefused(text.length === 10 ? `${text} doesn't exist.` : 'Finish the date as DD/MM/YYYY, e.g. 20/01/2028.')
      setText(isoToDmy(value))
      return
    }
    const problem = outOfRange(iso)
    if (problem) {
      setRefused(problem)
      setText(isoToDmy(value))
      return
    }
    accept(iso)
  }

  const message = refused ?? error
  const isInvalid = !!message || !!invalid
  const describedBy = message ? `${id}-error` : hint ? `${id}-hint` : undefined
  const selected = value ? isoToDate(value) : undefined
  const disabled = [...(min ? [{ before: isoToDate(min) }] : []), ...(max ? [{ after: isoToDate(max) }] : [])]

  return (
    <div
      className="space-y-1.5"
      ref={box}
      onKeyDown={(e) => {
        // Esc closes only the calendar, not the form dialog around it.
        if (open && e.key === 'Escape') {
          e.stopPropagation()
          setOpen(false)
        }
      }}
    >
      <label htmlFor={id} className={cn('block text-sm font-medium text-ink', srOnlyLabel && 'sr-only')}>
        {label}
      </label>
      <div className="relative" ref={field}>
        {/* The DD/MM/YYYY template stays visible and fills in as the user types ("20/01/YYYY"). It sits behind the
            transparent input: the typed part is invisible here (the input shows it), the rest is grey. */}
        <div
          aria-hidden
          data-testid="date-template"
          className="pointer-events-none absolute inset-0 flex items-center whitespace-pre rounded-lg border border-transparent bg-surface pl-3.5 text-[15px]"
        >
          <span className="text-transparent">{text}</span>
          <span className="text-muted/60">{dmyTemplateRest(text)}</span>
        </div>
        <input
          id={id}
          type="text"
          inputMode="numeric"
          autoComplete="off"
          value={text}
          onChange={(e) => {
            setRefused(null)
            setText(maskDmy(e.target.value))
          }}
          onBlur={commitTyped}
          onKeyDown={(e) => {
            if (e.key === 'Enter') {
              e.preventDefault()
              commitTyped()
            }
          }}
          aria-invalid={isInvalid}
          aria-describedby={describedBy}
          className={cn(
            'relative block w-full h-11 rounded-lg border bg-transparent pl-3.5 pr-11 text-[15px] text-ink',
            'transition-colors focus:outline-none focus:ring-2 focus:ring-accent/40',
            isInvalid ? 'border-danger focus:border-danger' : 'border-line focus:border-brand',
          )}
        />
        <button
          type="button"
          onClick={toggle}
          aria-label={`Open calendar for ${label}`}
          aria-expanded={open}
          className="absolute inset-y-0 right-0 grid w-11 place-items-center rounded-r-lg text-muted hover:text-ink"
        >
          <CalendarDays className="size-[18px]" aria-hidden />
        </button>

        {open && (
          <div
            role="dialog"
            aria-label={`Choose ${label}`}
            style={position}
            className="date-popover z-[60] rounded-[var(--radius-card)] border border-line bg-surface p-2 shadow-xl"
          >
            <DayPicker
              mode="single"
              weekStartsOn={1}
              // Month and year dropdowns, so a date years away (a birth date) is two clicks, not hundreds.
              captionLayout="dropdown"
              startMonth={min ? isoToDate(min) : new Date(1900, 0)}
              endMonth={max ? isoToDate(max) : new Date(2099, 11)}
              selected={selected}
              defaultMonth={selected ?? (min ? isoToDate(min) : max ? isoToDate(max) : undefined)}
              disabled={disabled}
              onSelect={(d) => {
                if (!d) return
                accept(dateToIso(d))
                setOpen(false)
              }}
            />
          </div>
        )}
      </div>
      {message ? (
        <p id={`${id}-error`} className="text-[13px] text-danger">
          {message}
        </p>
      ) : hint ? (
        <p id={`${id}-hint`} className="text-[13px] text-muted">
          {hint}
        </p>
      ) : null}
    </div>
  )
}

const CALENDAR_HEIGHT = 360
const CALENDAR_WIDTH = 320

/**
 * Where the calendar goes for a field: below it if it fits, else above it if it fits, else as low as the screen
 * allows (never off the top edge). Null when the field itself is off screen.
 */
function calendarPlace(fieldEl: HTMLElement): CSSProperties | null {
  const r = fieldEl.getBoundingClientRect()
  const vh = window.innerHeight
  if (r.bottom < 0 || r.top > vh) return null
  const below = r.bottom + 4
  const above = r.top - CALENDAR_HEIGHT - 4
  const top = below + CALENDAR_HEIGHT <= vh - 8 ? below : above >= 8 ? above : Math.max(8, vh - CALENDAR_HEIGHT - 8)
  return { position: 'fixed', top, left: Math.max(8, Math.min(r.left, window.innerWidth - CALENDAR_WIDTH - 8)) }
}
