import { Mail, Phone, CalendarDays, GraduationCap, Building2, MapPin } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { errorMessage } from '@/shared/api/problem'
import { formatDate, initials } from '@/shared/lib/format'
import { Alert } from '@/shared/ui/Alert'
import { Button } from '@/shared/ui/Button'
import { Card } from '@/shared/ui/Card'
import { PageHeader } from '@/shared/ui/PageHeader'
import { Spinner } from '@/shared/ui/Spinner'
import { useProfile } from '../hooks'

function Row({ icon: Icon, label, value }: { icon: LucideIcon; label: string; value: string }) {
  return (
    <div className="flex items-center gap-3 py-3.5">
      <Icon className="size-[18px] text-muted" aria-hidden />
      <span className="w-36 shrink-0 text-sm text-muted">{label}</span>
      <span className="text-sm font-medium text-ink">{value}</span>
    </div>
  )
}

export function ProfilePage() {
  const { data, isPending, isError, error, refetch } = useProfile()

  if (isPending) return <Spinner className="text-brand" />
  if (isError)
    return (
      <div className="space-y-3">
        <Alert>{errorMessage(error)}</Alert>
        <Button variant="secondary" onClick={() => refetch()}>Try again</Button>
      </div>
    )

  return (
    <>
      <PageHeader eyebrow="Account" title="Profile" />
      <Card className="overflow-hidden">
        <div className="flex items-center gap-5 border-b border-line bg-brand-50/60 p-6">
          {data.photoUrl ? (
            <img src={data.photoUrl} alt="" className="size-16 rounded-full object-cover" />
          ) : (
            <span className="grid size-16 place-items-center rounded-full bg-brand font-display text-xl text-white">
              {initials(data.fullName)}
            </span>
          )}
          <div>
            <p className="font-display text-xl font-semibold">{data.fullName}</p>
            <p className="text-sm text-muted">
              {data.role} · ID {data.universityId}
            </p>
          </div>
        </div>
        <div className="divide-y divide-line px-6">
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
