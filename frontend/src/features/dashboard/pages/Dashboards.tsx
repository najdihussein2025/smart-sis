import { BookOpen, CalendarDays, CreditCard, GraduationCap, Layers, Users } from 'lucide-react'
import { useProfile } from '@/features/profile/hooks'
import { PageHeader } from '@/shared/ui/PageHeader'
import { StatCard } from '@/shared/ui/StatCard'

function greeting() {
  const h = new Date().getHours()
  return h < 12 ? 'Good morning' : h < 18 ? 'Good afternoon' : 'Good evening'
}

function firstName(full?: string) {
  return full?.split(' ')[0] ?? ''
}

// TODO(phase-3): real numbers from /api/dashboard. Until then the cards show "—", never fake values.

export function StudentDashboard() {
  const { data } = useProfile()
  return (
    <>
      <PageHeader eyebrow={data?.student?.majorName ?? 'Student'} title={`${greeting()}, ${firstName(data?.fullName)}`}>
        ID {data?.universityId ?? '…'}
        {data?.student && ` · ${data.student.campusName}`}
      </PageHeader>
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard label="Cumulative GPA" value="—" hint="Out of 4.0" icon={GraduationCap} />
        <StatCard label="Credits this term" value="—" hint={data?.student ? `Cap ${data.student.maxCredits}` : undefined} icon={BookOpen} />
        <StatCard label="Classes today" value="—" icon={CalendarDays} />
        <StatCard label="Outstanding balance" value="—" icon={CreditCard} />
      </div>
    </>
  )
}

export function InstructorDashboard() {
  const { data } = useProfile()
  return (
    <>
      <PageHeader eyebrow={data?.instructor?.departmentName ?? 'Instructor'} title={`${greeting()}, ${firstName(data?.fullName)}`}>
        ID {data?.universityId ?? '…'}
      </PageHeader>
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <StatCard label="Active courses" value="—" icon={Layers} />
        <StatCard label="Classes today" value="—" icon={CalendarDays} />
        <StatCard label="Ungraded components" value="—" icon={GraduationCap} />
      </div>
    </>
  )
}

export function AdminDashboard() {
  const { data } = useProfile()
  return (
    <>
      <PageHeader eyebrow="Administration" title={`${greeting()}, ${firstName(data?.fullName)}`} />
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard label="Active students" value="—" icon={Users} />
        <StatCard label="Sections this term" value="—" icon={Layers} />
        <StatCard label="Open support requests" value="—" icon={BookOpen} />
        <StatCard label="Pending charges" value="—" icon={CreditCard} />
      </div>
    </>
  )
}
