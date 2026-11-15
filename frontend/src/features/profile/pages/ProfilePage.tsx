import { Building2, Cake, CalendarCheck, Gauge, GraduationCap, Mail, MapPin, Phone, UserRound } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { formatDateOnly, initials } from '@/shared/lib/format'
import { Badge } from '@/shared/ui/Badge'
import { Card } from '@/shared/ui/Card'
import { CardHeader } from '@/shared/ui/CardHeader'
import { PageHeader } from '@/shared/ui/PageHeader'
import { QueryError } from '@/shared/ui/QueryError'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { useProfile } from '../hooks'
import type { UserProfile } from '../types'

function Row({ icon: Icon, label, value }: { icon: LucideIcon; label: string; value: string }) {
  return (
    // On phones the value sits under the label, so a long email gets the full width.
    <div className="py-3.5 sm:flex sm:items-center sm:gap-3">
      <dt className="flex items-center gap-3 text-sm text-muted sm:w-52 sm:shrink-0">
        <span className="grid size-8 shrink-0 place-items-center rounded-lg bg-brand-50 text-brand">
          <Icon className="size-4" aria-hidden />
        </span>
        {label}
      </dt>
      <dd className="tabular mt-1 pl-11 text-sm font-medium text-ink [overflow-wrap:anywhere] sm:mt-0 sm:pl-0">{value}</dd>
    </div>
  )
}

const descriptions: Record<UserProfile['role'], string> = {
  Student: 'Your contact and study details as the university has them. Ask the registrar to correct anything wrong.',
  Instructor: 'Your contact and department details as the university has them. Ask an administrator to correct anything wrong.',
  Admin: 'Your account details as the university has them.',
}

/** The signed-in user's own details, for every role. Read-only: the registrar keeps these records. */
export function ProfilePage() {
  const { data, isPending, isError, error, refetch } = useProfile()

  if (isPending || isError)
    return (
      <>
        <PageHeader eyebrow="Account" title="Profile" icon={UserRound} />
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
      <PageHeader eyebrow="Account" title="Profile" icon={UserRound}>
        {descriptions[data.role]}
      </PageHeader>
      <div className="space-y-6">
        <Card className="overflow-hidden">
          <div className="flex items-center gap-4 bg-brand-50/60 p-5 sm:gap-5 sm:p-6">
            {data.photoUrl ? (
              <img src={data.photoUrl} alt="" className="size-16 rounded-full object-cover" />
            ) : (
              <span aria-hidden className="grid size-16 shrink-0 place-items-center rounded-full bg-brand font-display text-xl text-white">
                {initials(data.fullName)}
              </span>
            )}
            <div className="min-w-0 space-y-1">
              <p className="font-display text-xl font-semibold break-words text-ink">{data.fullName}</p>
              <p className="flex flex-wrap items-center gap-2 text-sm text-muted">
                <Badge tone="outline">{data.role}</Badge>
                <span className="tabular">ID {data.universityId}</span>
              </p>
            </div>
          </div>
        </Card>

        <Card>
          <CardHeader title="Contact" />
          <dl className="divide-y divide-line px-4 sm:px-5">
            <Row icon={Mail} label="Email" value={data.email} />
            <Row icon={Phone} label="Phone" value={data.phone ?? 'Not on file'} />
            <Row icon={Cake} label="Date of birth" value={data.dateOfBirth ? formatDateOnly(data.dateOfBirth) : 'Not on file'} />
          </dl>
        </Card>

        {data.student && (
          <Card>
            <CardHeader title="Studies" />
            <dl className="divide-y divide-line px-4 sm:px-5">
              <Row icon={GraduationCap} label="Major" value={data.student.majorName} />
              <Row icon={MapPin} label="Campus" value={data.student.campusName} />
              <Row icon={CalendarCheck} label="Enrolled since" value={formatDateOnly(data.student.enrollmentDate)} />
              <Row icon={Gauge} label="Credit cap per term" value={`${data.student.maxCredits} credits`} />
            </dl>
          </Card>
        )}

        {data.instructor && (
          <Card>
            <CardHeader title="Teaching" />
            <dl className="divide-y divide-line px-4 sm:px-5">
              <Row icon={Building2} label="Department" value={data.instructor.departmentName} />
            </dl>
          </Card>
        )}
      </div>
    </>
  )
}
