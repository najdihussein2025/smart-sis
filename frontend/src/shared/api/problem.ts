import { isAxiosError } from 'axios'

/** ASP.NET Core ProblemDetails, as returned by ExceptionMiddleware. */
export interface ProblemDetails {
  status?: number
  title?: string
  detail?: string
  code?: string
  errors?: Record<string, string[]>
}

export function getProblem(error: unknown): ProblemDetails | null {
  if (isAxiosError(error) && error.response?.data && typeof error.response.data === 'object') {
    return error.response.data as ProblemDetails
  }
  return null
}

/** Titles the framework (not our ExceptionMiddleware) writes. They are for developers, so the user sees a sentence instead. */
const technicalTitle = /^(one or more validation errors occurred|request failed|network error|bad request|not found|forbidden|unauthorized|internal server error)\.?/i

/** What to say for a status when the API sent no title we can show. */
function statusMessage(status: number): string | null {
  if (status === 400) return 'Some of the information is not valid. Check it and try again.'
  if (status === 401) return 'Your session has ended. Please sign in again.'
  if (status === 403) return "You don't have access to this."
  if (status === 404) return "We couldn't find what you were looking for. It may have been removed."
  if (status === 408) return 'The server took too long to answer. Please try again.'
  if (status === 413) return 'The file is too large.'
  if (status === 429) return 'Too many requests. Wait a moment and try again.'
  return null
}

/**
 * A sentence to show the user, never a stack trace or "Request failed with status code 500".
 * - No answer at all → connection message (or a timeout message).
 * - 5xx → a generic "on our side" message (500 details are only for developers).
 * - 4xx with a ProblemDetails title from ExceptionMiddleware (409 business rules, 404, 403…) → that title: they are written for people.
 * - Otherwise a message for the status, else `fallback`.
 */
export function errorMessage(error: unknown, fallback = 'Something went wrong. Please try again.'): string {
  if (!isAxiosError(error)) return fallback
  if (!error.response) {
    return error.code === 'ECONNABORTED' || error.code === 'ETIMEDOUT'
      ? 'The server took too long to answer. Please try again.'
      : 'Cannot reach the server. Check your connection and try again.'
  }
  const { status } = error.response
  if (status >= 500) return 'Something went wrong on our side. Please try again in a moment.'
  const title = getProblem(error)?.title?.trim()
  if (title && !technicalTitle.test(title)) return title
  return statusMessage(status) ?? fallback
}

/**
 * Puts the API's 400 field errors (`errors: { "name": [...] }`) under the matching form fields.
 * List keys use the form's dotted paths: "meetings[1].endTime" matches the field "meetings.1.endTime"; when no
 * field matches exactly, the closest parent does ("prerequisiteIds[0]" → "prerequisiteIds").
 * Returns true when at least one field matched, so the caller can skip the top-of-form alert.
 */
export function applyFieldErrors<Field extends string>(
  error: unknown,
  fields: readonly Field[],
  setError: (field: Field, error: { type: string; message: string }) => void,
): boolean {
  const errors = getProblem(error)?.errors
  if (!errors) return false
  let matched = false
  for (const [rawKey, messages] of Object.entries(errors)) {
    const key = rawKey.replace(/\[(\d+)\]/g, '.$1')
    const field =
      fields.find((f) => f === key) ??
      [...fields].sort((a, b) => b.length - a.length).find((f) => key.startsWith(`${f}.`))
    if (field && messages[0]) {
      setError(field, { type: 'server', message: messages[0] })
      matched = true
    }
  }
  return matched
}
