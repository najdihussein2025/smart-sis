import { describe, expect, it } from 'vitest'
import { maskDmy } from './dates'

describe('maskDmy', () => {
  it.each([
    ['20012028', '20/01/2028'],
    ['151515151', '15/15/1515'], // never more than DD/MM/YYYY
    ['2', '2'],
    ['20', '20'],
    ['200', '20/0'],
    ['20/', '20/'],
    ['1/2/2028', '01/02/2028'],
    ['20.01.2028', '20/01/2028'],
    ['ab20cd01xx2028', '20/01/2028'],
    ['/', ''],
    ['', ''],
  ])('%s → %s', (typed, shown) => expect(maskDmy(typed)).toBe(shown))
})
