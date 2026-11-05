import { z } from 'zod'
import { fromLocalInput, toLocalInput } from '@/shared/lib/format'
import type { SaveTermRequest, Term } from '../types'

const required = (message: string) => z.string().min(1, message)

/**
 * Mirrors the API's SaveTermRequestValidator so mistakes show before the request (UX only; the API checks again).
 * Window inputs are `datetime-local` values in the viewer's time zone; they're sent as UTC.
 */
export const termSchema = z
  .object({
    name: z.string().trim().min(1, 'Enter a term name.').max(64, 'Use at most 64 characters.'),
    startDate: required('Choose the first day of the term.'),
    endDate: required('Choose the last day of the term.'),
    enrollStart: required('Choose when enrollment opens.'),
    enrollEnd: required('Choose when enrollment closes.'),
    addDropStart: required('Choose when add/drop opens.'),
    addDropEnd: required('Choose when add/drop closes.'),
    isCurrent: z.boolean(),
  })
  .superRefine((v, ctx) => {
    const issue = (path: keyof TermFormValues, message: string) => ctx.addIssue({ code: 'custom', path: [path], message })
    // Same-format strings ("yyyy-mm-dd", "yyyy-mm-ddThh:mm") compare correctly as text.
    if (v.startDate && v.endDate && v.endDate <= v.startDate) issue('endDate', 'The term must end after it starts.')
    if (v.enrollStart && v.enrollEnd && v.enrollEnd <= v.enrollStart)
      issue('enrollEnd', 'The enrollment window must end after it starts.')
    if (v.addDropStart && v.addDropEnd && v.addDropEnd <= v.addDropStart)
      issue('addDropEnd', 'The add/drop window must end after it starts.')
    if (v.enrollEnd && v.addDropStart && v.addDropStart < v.enrollEnd)
      issue('addDropStart', "Add/drop can't open before the enrollment window closes.")
    // The API compares the UTC date of the close time with the end date.
    if (v.addDropEnd && v.endDate && fromLocalInput(v.addDropEnd).slice(0, 10) > v.endDate)
      issue('addDropEnd', 'Add/drop must close by the end of the term.')
  })

export type TermFormValues = z.infer<typeof termSchema>

export const termFields = [
  'name', 'startDate', 'endDate', 'enrollStart', 'enrollEnd', 'addDropStart', 'addDropEnd',
] as const satisfies readonly (keyof TermFormValues)[]

export const emptyTerm: TermFormValues = {
  name: '', startDate: '', endDate: '', enrollStart: '', enrollEnd: '', addDropStart: '', addDropEnd: '', isCurrent: false,
}

export function toFormValues(t: Term): TermFormValues {
  return {
    name: t.name,
    startDate: t.startDate,
    endDate: t.endDate,
    enrollStart: toLocalInput(t.enrollStart),
    enrollEnd: toLocalInput(t.enrollEnd),
    addDropStart: toLocalInput(t.addDropStart),
    addDropEnd: toLocalInput(t.addDropEnd),
    isCurrent: t.isCurrent,
  }
}

export function toRequest(v: TermFormValues): SaveTermRequest {
  return {
    name: v.name.trim(),
    startDate: v.startDate,
    endDate: v.endDate,
    enrollStart: fromLocalInput(v.enrollStart),
    enrollEnd: fromLocalInput(v.enrollEnd),
    addDropStart: fromLocalInput(v.addDropStart),
    addDropEnd: fromLocalInput(v.addDropEnd),
    isCurrent: v.isCurrent,
  }
}
