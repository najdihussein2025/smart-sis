import { AxiosError, AxiosHeaders } from 'axios'
import { describe, expect, it } from 'vitest'
import { applyFieldErrors, errorMessage } from './problem'

function badRequest(errors: Record<string, string[]>) {
  const config = { headers: new AxiosHeaders() }
  return new AxiosError('400', 'ERR_BAD_REQUEST', config, null, {
    status: 400, statusText: 'Bad Request', headers: {}, config, data: { title: 'Some fields are not valid.', errors },
  })
}

describe('applyFieldErrors', () => {
  it('puts list errors on the matching row field, or on the list when only the list is a field', () => {
    const set: Record<string, string> = {}
    const matched = applyFieldErrors(
      badRequest({
        'meetings[1].endTime': ['The meeting must end after it starts.'],
        'prerequisiteIds[0]': ["Course 9 doesn't exist."],
        unknown: ['ignored'],
      }),
      ['meetings', 'meetings.1.endTime', 'prerequisiteIds'],
      (f, e) => (set[f] = e.message),
    )
    expect(matched).toBe(true)
    expect(set).toEqual({
      'meetings.1.endTime': 'The meeting must end after it starts.',
      prerequisiteIds: "Course 9 doesn't exist.",
    })
  })

  it('returns false when no field matches', () => {
    expect(applyFieldErrors(badRequest({ other: ['x'] }), ['name'], () => {})).toBe(false)
  })
})

function failed(status: number | null, data: unknown = null, code = 'ERR_BAD_RESPONSE') {
  const config = { headers: new AxiosHeaders() }
  return new AxiosError(`Request failed with status code ${status}`, code, config, null,
    status === null ? undefined : { status, statusText: '', headers: {}, config, data })
}

describe('errorMessage', () => {
  it('shows business-rule titles as written', () => {
    expect(errorMessage(failed(409, { title: 'Section is full' }))).toBe('Section is full')
  })

  it('never shows a 500 detail or the axios text', () => {
    const message = errorMessage(failed(500, { title: 'Something went wrong.', detail: 'System.Exception: boom\n at X.Y()' }))
    expect(message).toBe('Something went wrong on our side. Please try again in a moment.')
    expect(errorMessage(failed(502, '<html>Bad gateway</html>'))).not.toMatch(/status code|html/i)
  })

  it('explains a missing connection and a timeout', () => {
    expect(errorMessage(failed(null, null, 'ERR_NETWORK'))).toBe('Cannot reach the server. Check your connection and try again.')
    expect(errorMessage(failed(null, null, 'ECONNABORTED'))).toMatch(/too long/)
  })

  it('maps framework titles and empty bodies to a sentence for the status', () => {
    expect(errorMessage(failed(400, { title: 'One or more validation errors occurred.' }))).toMatch(/not valid/)
    expect(errorMessage(failed(403, ''))).toBe("You don't have access to this.")
    expect(errorMessage(failed(404, { title: 'Course not found.' }))).toBe('Course not found.')
  })

  it('uses the fallback for anything else', () => {
    expect(errorMessage(new Error('x'), 'Could not load this page.')).toBe('Could not load this page.')
  })
})
