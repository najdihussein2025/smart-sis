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

/** A sentence to show the user. Business-rule (409) titles are already written for people. */
export function errorMessage(error: unknown, fallback = 'Something went wrong. Please try again.'): string {
  if (isAxiosError(error) && !error.response) return 'Cannot reach the server. Check your connection.'
  return getProblem(error)?.title ?? fallback
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
