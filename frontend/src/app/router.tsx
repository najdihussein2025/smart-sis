import { createBrowserRouter, type RouteObject } from 'react-router'
import { AuthLayout } from '@/layouts/AuthLayout'
import { AdminLayout, InstructorLayout, StudentLayout } from '@/layouts/RoleLayouts'
import { LoginPage } from '@/features/auth/pages/LoginPage'
import { ForgotPasswordPage } from '@/features/auth/pages/ForgotPasswordPage'
import { ResetPasswordPage } from '@/features/auth/pages/ResetPasswordPage'
import { ProfilePage } from '@/features/profile/pages/ProfilePage'
import { AdminDashboard, InstructorDashboard, StudentDashboard } from '@/features/dashboard/pages/Dashboards'
import { ComingSoon } from '@/features/common/pages/ComingSoon'
import { TermsPage } from '@/features/terms/pages/TermsPage'
import { CoursesPage } from '@/features/courses/pages/CoursesPage'
import { SectionsPage } from '@/features/sections/pages/SectionsPage'
import { UsersPage } from '@/features/users/pages/UsersPage'
import { CatalogPage } from '@/features/catalog/pages/CatalogPage'
import { CourseDetailPage } from '@/features/catalog/pages/CourseDetailPage'
import { CartPage } from '@/features/cart/pages/CartPage'
import { SchedulePage } from '@/features/schedule/pages/SchedulePage'
import { TeachingSchedulePage } from '@/features/schedule/pages/TeachingSchedulePage'
import { StudentFinancePage } from '@/features/finance/pages/StudentFinancePage'
import { AdminPaymentsPage } from '@/features/finance/pages/AdminPaymentsPage'
import { StudentSupportPage } from '@/features/support/pages/StudentSupportPage'
import { StudentSupportRequestPage } from '@/features/support/pages/StudentSupportRequestPage'
import { AdminSupportPage } from '@/features/support/pages/AdminSupportPage'
import { AdminSupportRequestPage } from '@/features/support/pages/AdminSupportRequestPage'
import { InstructorAttendancePage } from '@/features/attendance/pages/InstructorAttendancePage'
import { StudentAttendancePage } from '@/features/attendance/pages/StudentAttendancePage'
import { AnnouncementsPage } from '@/features/announcements/pages/AnnouncementsPage'
import { NotFoundPage } from '@/features/common/pages/NotFoundPage'
import { GradeComponentsPage } from '@/features/grading/pages/GradeComponentsPage'
import { InstructorGradingPage } from '@/features/grading/pages/InstructorGradingPage'
import { StudentGradesPage } from '@/features/grading/pages/StudentGradesPage'
import { HomeRedirect, RequireRole } from './RequireRole'

/** Routes not built yet: [path, title, phase]. Replace each with the real page when its phase lands. */
const soon = (items: [string, string, number][]): RouteObject[] =>
  items.map(([path, title, phase]) => ({ path, element: <ComingSoon title={title} phase={phase} /> }))

/** Exported so tests can mount the real routes in a memory router. */
export const routes: RouteObject[] = [
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
      { path: 'courses', element: <CatalogPage /> },
      { path: 'courses/:id', element: <CourseDetailPage /> },
      { path: 'cart', element: <CartPage /> },
      { path: 'schedule', element: <SchedulePage /> },
      { path: 'finance', element: <StudentFinancePage /> },
      { path: 'support', element: <StudentSupportPage /> },
      { path: 'support/:id', element: <StudentSupportRequestPage /> },
      { path: 'attendance', element: <StudentAttendancePage /> },
      { path: 'grades', element: <StudentGradesPage /> },
      { path: 'announcements', element: <AnnouncementsPage /> },
      ...soon([
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
      { path: 'schedule', element: <TeachingSchedulePage /> },
      { path: 'attendance', element: <InstructorAttendancePage /> },
      { path: 'grading', element: <InstructorGradingPage /> },
      { path: 'announcements', element: <AnnouncementsPage /> },
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
      { path: 'users', element: <UsersPage /> },
      { path: 'terms', element: <TermsPage /> },
      { path: 'courses', element: <CoursesPage /> },
      { path: 'sections', element: <SectionsPage /> },
      { path: 'payments', element: <AdminPaymentsPage /> },
      { path: 'support', element: <AdminSupportPage /> },
      { path: 'support/:id', element: <AdminSupportRequestPage /> },
      { path: 'grade-components', element: <GradeComponentsPage /> },
      { path: 'announcements', element: <AnnouncementsPage /> },
      ...soon([
        ['lookups', 'Lookups', 2],
        ['knowledge', 'Knowledge base', 7],
      ]),
    ],
  },
  { path: '*', element: <NotFoundPage /> },
]

export const router = createBrowserRouter(routes)
