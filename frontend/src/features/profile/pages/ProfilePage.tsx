import { Mail, Phone, CalendarDays, GraduationCap, Building2, MapPin } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { formatDate, initials } from '@/shared/lib/format'
import { Card } from '@/shared/ui/Card'
import { PageHeader } from '@/shared/ui/PageHeader'
import { QueryError } from '@/shared/ui/QueryError'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { useProfile } from '../hooks'

function Row({ icon: Icon, label, value }: { icon: LucideIcon; label: string; value: string }) {
  return (
    <div className="flex items-start gap-3 py-3.5">
      <Icon className="mt-px size-[18px] shrink-0 text-muted" aria-hidden />
      {/* On phones the label sits above the value, so a long email gets the full width. */}
      <div className="min-w-0 flex-1 sm:flex sm:gap-3">
        <span className="block text-sm text-muted sm:w-36 sm:shrink-0">{label}</span>
        <span className="block text-sm font-medium text-ink [overflow-wrap:anywhere]">{value}</span>
      </div>
    </div>
  )
}

export function ProfilePage() {
  const { data, isPending, isError, error, refetch } = useProfile()

  if (isPending || isError)
    return (
      <>
        <PageHeader eyebrow="Account" title="Profile" />
        <Card>
          {isPending ? (
            <TableSkeleton label="Loading your profile" rows={5} />
          ) : (
            <QueryError error={error} onRetry={() => void refetch()} />
          )}
        </Card>
      </>
    )

  return (
    <>
      <PageHeader eyebrow="Account" title="Profile" />
      <Card className="overflow-hidden">
        <div className="flex items-center gap-4 border-b border-line bg-brand-50/60 p-5 sm:gap-5 sm:p-6">
          {data.photoUrl ? (
            <img src={data.photoUrl} alt="" className="size-16 rounded-full object-cover" />
          ) : (
            <span aria-hidden className="grid size-16 shrink-0 place-items-center rounded-full bg-brand font-display text-xl text-white">
              {initials(data.fullName)}
            </span>
          )}
          <div className="min-w-0">
            <p className="font-display text-xl font-semibold break-words">{data.fullName}</p>
            <p className="text-sm text-muted">
              {data.role} · ID {data.universityId}
            </p>
          </div>
        </div>
        <div className="divide-y divide-line px-5 sm:px-6">
          <Row icon={Mail} label="Email" value={data.email} />
          <Row icon={Phone} label="Phone" value={data.phone ?? '—'} />
          <Row icon={CalendarDays} label="Date of birth" value={formatDate(data.dateOfBirth)} />
          {data.student && (
            <>
              <Row icon={GraduationCap} label="Major" value={data.student.majorName} />
              <Row icon={MapPin} label="Campus" value={data.student.campusName} />
              <Row icon={CalendarDays} label="Enrolled since" value={formatDate(data.student.enrollmentDate)} />
              <Row icon={GraduationCap} label="Credit cap / term" value={`${data.student.maxCredits} credits`} />
            </>
          )}
          {data.instructor && <Row icon={Building2} label="Department" value={data.instructor.departmentName} />}
        </div>
      </Card>
    </>
  )
}
