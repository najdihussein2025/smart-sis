import {
  BookOpen, CalendarDays, ClipboardCheck, CreditCard, GraduationCap, LayoutDashboard, LifeBuoy, Megaphone,
  MessageSquareText, ShoppingCart, Users, Building2, CalendarRange, Layers, ListChecks, FileStack, Inbox,
} from 'lucide-react'
import { RoleShell, type NavItem } from './RoleShell'

const studentNav: NavItem[] = [
  { to: '/student', label: 'Dashboard', icon: LayoutDashboard, end: true },
  { to: '/student/schedule', label: 'Schedule', icon: CalendarDays },
  { to: '/student/courses', label: 'Course catalog', icon: BookOpen },
  { to: '/student/cart', label: 'Cart', icon: ShoppingCart },
  { to: '/student/grades', label: 'Grades', icon: GraduationCap },
  { to: '/student/attendance', label: 'Attendance', icon: ClipboardCheck },
  { to: '/student/finance', label: 'Financial account', icon: CreditCard },
  { to: '/student/support', label: 'Support', icon: LifeBuoy },
  { to: '/student/assistant', label: 'Assistant', icon: MessageSquareText },
]

const instructorNav: NavItem[] = [
  { to: '/instructor', label: 'Dashboard', icon: LayoutDashboard, end: true },
  { to: '/instructor/schedule', label: 'Teaching schedule', icon: CalendarDays },
  { to: '/instructor/attendance', label: 'Attendance', icon: ClipboardCheck },
  { to: '/instructor/grading', label: 'Grading', icon: ListChecks },
  { to: '/instructor/announcements', label: 'Announcements', icon: Megaphone },
]

const adminNav: NavItem[] = [
  { to: '/admin', label: 'Dashboard', icon: LayoutDashboard, end: true },
  { to: '/admin/users', label: 'Users', icon: Users },
  { to: '/admin/lookups', label: 'Lookups', icon: Building2 },
  { to: '/admin/terms', label: 'Terms', icon: CalendarRange },
  { to: '/admin/courses', label: 'Courses', icon: BookOpen },
  { to: '/admin/sections', label: 'Sections', icon: Layers },
  { to: '/admin/grade-components', label: 'Grade components', icon: ListChecks },
  { to: '/admin/payments', label: 'Payments', icon: CreditCard },
  { to: '/admin/announcements', label: 'Announcements', icon: Megaphone },
  { to: '/admin/support', label: 'Support inbox', icon: Inbox },
  { to: '/admin/knowledge', label: 'Knowledge base', icon: FileStack },
]

export const StudentLayout = () => <RoleShell areaLabel="Student" nav={studentNav} />
export const InstructorLayout = () => <RoleShell areaLabel="Instructor" nav={instructorNav} />
export const AdminLayout = () => <RoleShell areaLabel="Admin" nav={adminNav} />
