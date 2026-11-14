import { api } from '@/shared/api/client'
import type { AdminDashboard, InstructorDashboard, StudentDashboard } from './types'

export const dashboardApi = {
  student: () => api.get<StudentDashboard>('/api/dashboard/student').then((r) => r.data),
  instructor: () => api.get<InstructorDashboard>('/api/dashboard/instructor').then((r) => r.data),
  admin: () => api.get<AdminDashboard>('/api/dashboard/admin').then((r) => r.data),
}
