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
