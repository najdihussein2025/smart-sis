import { useQuery } from '@tanstack/react-query'
import { dashboardApi } from './api'

/** The numbers come from other features, so they are refetched whenever a dashboard mounts (no staleTime). */
export const dashboardKeys = {
  student: ['dashboard', 'student'] as const,
  instructor: ['dashboard', 'instructor'] as const,
  admin: ['dashboard', 'admin'] as const,
}

export const useStudentDashboard = () => useQuery({ queryKey: dashboardKeys.student, queryFn: dashboardApi.student })
export const useInstructorDashboard = () => useQuery({ queryKey: dashboardKeys.instructor, queryFn: dashboardApi.instructor })
export const useAdminDashboard = () => useQuery({ queryKey: dashboardKeys.admin, queryFn: dashboardApi.admin })
