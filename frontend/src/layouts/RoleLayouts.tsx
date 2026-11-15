import {
  BookOpen, CalendarDays, ClipboardCheck, CreditCard, GraduationCap, LayoutDashboard, LifeBuoy, Megaphone,
  MessageSquareText, ShoppingCart, Users, Building2, CalendarRange, Layers, ListChecks, FileStack, Inbox, Compass,
} from 'lucide-react'
import { RoleShell, type NavGroup } from './RoleShell'

// Groups keep long menus scannable (skill rule nav-hierarchy). Page eyebrows use the same group names.
const studentNav: NavGroup[] = [
  { items: [{ to: '/student', label: 'Dashboard', icon: LayoutDashboard, end: true }] },
  {
    label: 'Academics',
    items: [
      { to: '/student/schedule', label: 'Schedule', icon: CalendarDays },
      { to: '/student/courses', label: 'Course catalog', icon: BookOpen },
      { to: '/student/cart', label: 'Cart', icon: ShoppingCart },
      { to: '/student/grades', label: 'Grades', icon: GraduationCap },
      { to: '/student/attendance', label: 'Attendance', icon: ClipboardCheck },
    ],
  },
  { label: 'Money', items: [{ to: '/student/finance', label: 'Financial account', icon: CreditCard }] },
  {
    label: 'Campus',
    items: [
      { to: '/student/announcements', label: 'Announcements', icon: Megaphone },
      { to: '/student/career', label: 'Career', icon: Compass },
    ],
  },
  {
    label: 'Help',
    items: [
      { to: '/student/support', label: 'Support', icon: LifeBuoy },
      { to: '/student/assistant', label: 'Assistant', icon: MessageSquareText },
    ],
  },
]

const instructorNav: NavGroup[] = [
  { items: [{ to: '/instructor', label: 'Dashboard', icon: LayoutDashboard, end: true }] },
  {
    label: 'Teaching',
    items: [
      { to: '/instructor/schedule', label: 'Teaching schedule', icon: CalendarDays },
      { to: '/instructor/attendance', label: 'Attendance', icon: ClipboardCheck },
      { to: '/instructor/grading', label: 'Grading', icon: ListChecks },
    ],
  },
  { label: 'Campus', items: [{ to: '/instructor/announcements', label: 'Announcements', icon: Megaphone }] },
]

const adminNav: NavGroup[] = [
  { items: [{ to: '/admin', label: 'Dashboard', icon: LayoutDashboard, end: true }] },
  { label: 'People', items: [{ to: '/admin/users', label: 'Users', icon: Users }] },
  {
    label: 'Academics',
    items: [
      { to: '/admin/terms', label: 'Terms', icon: CalendarRange },
      { to: '/admin/courses', label: 'Courses', icon: BookOpen },
      { to: '/admin/sections', label: 'Sections', icon: Layers },
      { to: '/admin/grade-components', label: 'Grade components', icon: ListChecks },
    ],
  },
  { label: 'Money', items: [{ to: '/admin/payments', label: 'Payments', icon: CreditCard }] },
  {
    label: 'Communication',
    items: [
      { to: '/admin/announcements', label: 'Announcements', icon: Megaphone },
      { to: '/admin/support', label: 'Support inbox', icon: Inbox },
    ],
  },
  {
    label: 'Setup',
    items: [
      { to: '/admin/lookups', label: 'Lookups', icon: Building2 },
      { to: '/admin/knowledge', label: 'Knowledge base', icon: FileStack },
      { to: '/admin/career', label: 'Career', icon: Compass },
    ],
  },
]

export const StudentLayout = () => <RoleShell areaLabel="Student" nav={studentNav} />
export const InstructorLayout = () => <RoleShell areaLabel="Instructor" nav={instructorNav} />
export const AdminLayout = () => <RoleShell areaLabel="Admin" nav={adminNav} />
