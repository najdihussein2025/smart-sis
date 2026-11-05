import { AxiosError, AxiosHeaders } from 'axios'
import { describe, expect, it } from 'vitest'
import { applyFieldErrors } from './problem'

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
