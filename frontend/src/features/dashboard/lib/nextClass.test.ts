import { describe, expect, it } from 'vitest'
import type { ScheduleItem } from '@/features/schedule/types'
import { apiDayOf, apiDayOfIso, planDay } from './nextClass'

const item = (meetingId: number, dayOfWeek: number, startTime: string, endTime: string): ScheduleItem => ({
  sectionId: 1, meetingId, courseCode: 'CSC 210', courseName: 'Data Structures', sectionCode: 'A',
  dayOfWeek, startTime, endTime, roomName: 'B-101', instructorName: null, type: 'Lecture',
})

const week = [item(3, 3, '13:00:00', '14:15:00'), item(1, 1, '08:00:00', '09:15:00'), item(2, 3, '08:00:00', '09:15:00')]

describe('planDay', () => {
  it('picks the class in progress', () => {
    const plan = planDay(week, 3, '08:30:00')
    expect(plan.next).toEqual({ item: week[2], when: 'now' })
    expect(plan.remainingToday.map((i) => i.meetingId)).toEqual([2, 3])
  })

  it('picks the next class later today', () => {
    expect(planDay(week, 3, '10:00:00').next).toEqual({ item: week[0], when: 'today' })
  })

  it('after the last class of the week, wraps to the first class of next week', () => {
    const plan = planDay(week, 3, '18:00:00')
    expect(plan.next).toEqual({ item: week[1], when: 'later' })
    expect(plan.remainingToday).toEqual([])
    expect(plan.today).toHaveLength(2)
  })

  it('on Sunday shows no classes today and Monday next', () => {
    const plan = planDay(week, 0, '09:00:00')
    expect(plan.today).toEqual([])
    expect(plan.next?.item.meetingId).toBe(1)
  })

  it('returns no next class for an empty timetable', () => {
    expect(planDay([], 3, '09:00:00').next).toBeNull()
  })
})

describe('day numbers', () => {
  it('keeps Sunday as 0 instead of mapping it onto Monday', () => {
    expect(apiDayOf(new Date(2026, 10, 15))).toBe(0)
    expect(apiDayOfIso('2026-11-11')).toBe(3)
  })
})
