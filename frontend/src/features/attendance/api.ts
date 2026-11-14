import { api } from '@/shared/api/client'
import type {
  AttendanceSheet,
  MyAttendance,
  SessionSummary,
  SubmitAttendance,
  SubmitAttendanceResult,
  TeachingSections,
} from './types'

export const attendanceApi = {
  teaching: () => api.get<TeachingSections>('/api/attendance/teaching').then((r) => r.data),
  sheet: (meetingId: number, date: string) =>
    api.get<AttendanceSheet>('/api/attendance/sheet', { params: { meetingId, date } }).then((r) => r.data),
  sessions: (sectionId: number) => api.get<SessionSummary[]>(`/api/attendance/sections/${sectionId}/sessions`).then((r) => r.data),
  submit: (body: SubmitAttendance) => api.post<SubmitAttendanceResult>('/api/attendance/sessions', body).then((r) => r.data),
  mine: () => api.get<MyAttendance>('/api/attendance/my').then((r) => r.data),
}
