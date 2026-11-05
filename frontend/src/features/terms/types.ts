// TODO: replace with generated types from `npm run gen:api` (TermDto, SaveTermRequest).

/** Window open right now, computed by the API from UTC times. */
export type TermWindow = 'Closed' | 'Enrollment' | 'AddDrop'

export interface Term {
  id: number
  name: string
  /** DateOnly, "yyyy-mm-dd". */
  startDate: string
  endDate: string
  /** UTC ISO strings. */
  enrollStart: string
  enrollEnd: string
  addDropStart: string
  addDropEnd: string
  isCurrent: boolean
  currentWindow: TermWindow
}

export type SaveTermRequest = Omit<Term, 'id' | 'currentWindow'>
