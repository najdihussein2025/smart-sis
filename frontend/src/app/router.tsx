import { createBrowserRouter, type RouteObject } from 'react-router'
import { AuthLayout } from '@/layouts/AuthLayout'
import { AdminLayout, InstructorLayout, StudentLayout } from '@/layouts/RoleLayouts'
import { LoginPage } from '@/features/auth/pages/LoginPage'
import { ForgotPasswordPage } from '@/features/auth/pages/ForgotPasswordPage'
import { ResetPasswordPage } from '@/features/auth/pages/ResetPasswordPage'
import { ProfilePage } from '@/features/profile/pages/ProfilePage'
import { AdminDashboard, InstructorDashboard, StudentDashboard } from '@/features/dashboard/pages/Dashboards'
import { ComingSoon } from '@/features/common/pages/ComingSoon'
import { NotFoundPage } from '@/features/common/pages/NotFoundPage'
import { HomeRedirect, RequireRole } from './RequireRole'

/** Routes not built yet: [path, title, phase]. Replace each with the real page when its phase lands. */
const soon = (items: [string, string, number][]): RouteObject[] =>
  items.map(([path, title, phase]) => ({ path, element: <ComingSoon title={title} phase={phase} /> }))

export const router = createBrowserRouter([
  { path: '/', element: <HomeRedirect /> },
  {
    element: <AuthLayout />,
    children: [
      { path: '/login', element: <LoginPage /> },
      { path: '/forgot-password', element: <ForgotPasswordPage /> },
      { path: '/reset-password', element: <ResetPasswordPage /> },
    ],
  },
  {
    path: '/student',
    element: (
      <RequireRole role="Student">
        <StudentLayout />
      </RequireRole>
    ),
    children: [
      { index: true, element: <StudentDashboard /> },
      { path: 'profile', element: <ProfilePage /> },
      ...soon([
        ['schedule', 'Schedule', 3],
        ['courses', 'Course catalog', 3],
        ['cart', 'Cart', 4],
        ['grades', 'Grades', 5],
        ['attendance', 'Attendance', 5],
        ['finance', 'Financial account', 3],
        ['support', 'Support', 6],
        ['assistant', 'Assistant', 7],
      ]),
    ],
  },
  {
    path: '/instructor',
    element: (
      <RequireRole role="Instructor">
        <InstructorLayout />
      </RequireRole>
    ),
    children: [
      { index: true, element: <InstructorDashboard /> },
      { path: 'profile', element: <ProfilePage /> },
      ...soon([
        ['schedule', 'Teaching schedule', 5],
        ['attendance', 'Attendance', 5],
        ['grading', 'Grading', 5],
        ['announcements', 'Announcements', 6],
      ]),
    ],
  },
  {
    path: '/admin',
    element: (
      <RequireRole role="Admin">
        <AdminLayout />
      </RequireRole>
    ),
    children: [
      { index: true, element: <AdminDashboard /> },
      { path: 'profile', element: <ProfilePage /> },
      ...soon([
        ['users', 'Users', 2],
        ['lookups', 'Lookups', 2],
        ['terms', 'Terms', 2],
        ['courses', 'Courses', 2],
        ['sections', 'Sections', 2],
        ['grade-components', 'Grade components', 2],
        ['payments', 'Payments', 6],
        ['announcements', 'Announcements', 6],
        ['support', 'Support inbox', 6],
        ['knowledge', 'Knowledge base', 7],
      ]),
    ],
  },
  { path: '*', element: <NotFoundPage /> },
])
