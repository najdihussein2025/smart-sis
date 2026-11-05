import { describe, expect, it } from 'vitest'
import { dmyTemplateRest, maskDmy } from './dates'

describe('maskDmy', () => {
  it.each([
    ['20012028', '20/01/2028'],
    ['2', '2'],
    ['20', '20'],
    ['200', '20/0'],
    ['20/', '20/'],
    ['1/2/2028', '01/02/2028'],
    ['20.01.2028', '20/01/2028'],
    ['ab20cd01xx2028', '20/01/2028'],
    ['/', ''],
    ['', ''],
    ['31122099', '31/12/2099'],
    ['20/01/20289', '20/01/2028'], // nothing after the year
  ])('shapes %s as %s', (typed, shown) => expect(maskDmy(typed)).toBe(shown))

  it.each([
    ['32', '3'], // day can't pass 31
    ['39', '3'],
    ['00', '0'], // no day 00
    ['0/', '0'],
    ['5', '05'], // a day starting with 5 can only be 05
    ['2013', '20/1'], // month can't pass 12
    ['2019', '20/1'],
    ['2000', '20/0'], // no month 00
    ['203', '20/03'], // a month starting with 3 can only be 03
    ['20013', '20/01'], // year starts with 19 or 20
    ['200118', '20/01/1'],
    ['2001199', '20/01/199'],
    ['151515151', '15/11/1'], // the example from the bug report
  ])('refuses impossible digits: %s → %s', (typed, shown) => expect(maskDmy(typed)).toBe(shown))
})

describe('dmyTemplateRest', () => {
  it.each([
    ['', 'DD/MM/YYYY'],
    ['2', 'D/MM/YYYY'],
    ['20/0', '1/YYYY'.replace('1', 'M')],
    ['20/01/2028', ''],
  ])('%s leaves %s to fill', (typed, rest) => expect(dmyTemplateRest(typed)).toBe(rest))
})
