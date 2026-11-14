import { useQuery } from '@tanstack/react-query'
import { scheduleApi } from './api'

/** Keys start with 'schedule' so enroll / swap / drop invalidate them. */
export const scheduleKeys = {
  all: ['schedule'] as const,
  my: ['schedule', 'my'] as const,
  teaching: ['schedule', 'teaching'] as const,
}

export const useMySchedule = () => useQuery({ queryKey: scheduleKeys.my, queryFn: scheduleApi.my })
export const useTeachingSchedule = () => useQuery({ queryKey: scheduleKeys.teaching, queryFn: scheduleApi.teaching })
