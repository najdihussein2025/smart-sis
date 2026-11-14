import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { attendanceApi } from './api'

export const attendanceKeys = {
  all: ['attendance'] as const,
  teaching: ['attendance', 'teaching'] as const,
  sheet: (meetingId: number, date: string) => ['attendance', 'sheet', meetingId, date] as const,
  sessions: (sectionId: number) => ['attendance', 'sessions', sectionId] as const,
  mine: ['attendance', 'mine'] as const,
}

export const useTeachingSections = () => useQuery({ queryKey: attendanceKeys.teaching, queryFn: attendanceApi.teaching })

/** The roster (or the recorded sheet) of a meeting on a date. Only runs once both are chosen and valid. */
export const useAttendanceSheet = (meetingId: number | null, date: string | null) =>
  useQuery({
    queryKey: attendanceKeys.sheet(meetingId ?? 0, date ?? ''),
    queryFn: () => attendanceApi.sheet(meetingId!, date!),
    enabled: meetingId !== null && !!date,
  })

export const useSessions = (sectionId: number | null) =>
  useQuery({
    queryKey: attendanceKeys.sessions(sectionId ?? 0),
    queryFn: () => attendanceApi.sessions(sectionId!),
    enabled: sectionId !== null,
  })

/** A submission changes the sheet, the history, absence counts and (auto-drop) the enrolled counts. */
export function useSubmitAttendance() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: attendanceApi.submit,
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: attendanceKeys.all }),
  })
}

export const useMyAttendance = () => useQuery({ queryKey: attendanceKeys.mine, queryFn: attendanceApi.mine })
