import { useId } from 'react'
import { cn } from '@/shared/lib/cn'
import { addDays, quarterHours } from '@/shared/lib/dates'
import { DateField } from './DateField'

export interface DateTimeFieldProps {
  label: string
  /** "yyyy-mm-ddTHH:mm" (viewer's time zone), or "" when empty. */
  value: string
  onChange: (value: string) => void
  onBlur?: () => void
  /** Earliest allowed moment ("yyyy-mm-ddTHH:mm"). Earlier days and times can't be chosen. */
  min?: string
  /** True when the value must be strictly after `min` (a window that must end after it starts). */
  minExclusive?: boolean
  /** Latest allowed day (ISO date). */
  maxDate?: string
  error?: string
}

/** A DD/MM/YYYY date plus a 24-hour time in 15-minute steps. Times before `min` on its day are disabled. */
export function DateTimeField({ label, value, onChange, onBlur, min, minExclusive, maxDate, error }: DateTimeFieldProps) {
  const groupId = useId()
  const [date = '', time = ''] = value ? value.split('T') : []
  const [minDay, minTime] = min ? min.split('T') : []

  // If nothing is left on the min day (e.g. it must be after 23:45), the first allowed day is the next one.
  const firstSlotOnMinDay = minDay ? quarterHours.find((t) => isAllowed(t, minTime!, minExclusive)) : undefined
  const minDate = minDay ? (firstSlotOnMinDay ? minDay : addDays(minDay, 1)) : undefined

  const options = time && !quarterHours.includes(time) ? [...quarterHours, time].sort() : quarterHours
  const timeDisabled = (t: string) => !!minDay && date === minDay && !isAllowed(t, minTime!, minExclusive)

  const setDate = (d: string) => {
    if (!d) return onChange('')
    // Keep the chosen time if it's still allowed on the new day, otherwise take the first allowed time.
    const keep = time && !(d === minDay && !isAllowed(time, minTime!, minExclusive))
    const t = keep ? time : d === minDay && firstSlotOnMinDay ? firstSlotOnMinDay : time || '08:00'
    onChange(`${d}T${t}`)
  }

  return (
    <fieldset className="space-y-1.5" aria-describedby={error ? `${groupId}-error` : undefined}>
      <legend className="block text-sm font-medium text-ink">{label}</legend>
      <div className="grid grid-cols-[minmax(0,1fr)_6.5rem] gap-2">
        <DateField
          label={`${label} date`}
          srOnlyLabel
          value={date}
          onChange={setDate}
          onBlur={onBlur}
          min={minDate}
          max={maxDate}
          invalid={!!error}
        />
        <div>
          <label htmlFor={`${groupId}-time`} className="sr-only">
            {`${label} time`}
          </label>
          <select
            id={`${groupId}-time`}
            value={time}
            disabled={!date}
            onChange={(e) => onChange(`${date}T${e.target.value}`)}
            onBlur={onBlur}
            aria-invalid={!!error}
            className={cn(
              'block w-full h-11 rounded-lg border bg-surface px-3 text-[15px] text-ink disabled:bg-canvas disabled:text-muted',
              'focus:outline-none focus:ring-2 focus:ring-accent/40',
              error ? 'border-danger' : 'border-line focus:border-brand',
            )}
          >
            {!time && <option value="">--:--</option>}
            {options.map((t) => (
              <option key={t} value={t} disabled={timeDisabled(t)}>
                {t}
              </option>
            ))}
          </select>
        </div>
      </div>
      {error && (
        <p id={`${groupId}-error`} className="text-[13px] text-danger">
          {error}
        </p>
      )}
    </fieldset>
  )
}

function isAllowed(t: string, minTime: string, exclusive?: boolean) {
  return exclusive ? t > minTime : t >= minTime
}
