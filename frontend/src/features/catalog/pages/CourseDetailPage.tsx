import { Link, useParams } from 'react-router'
import { ArrowLeft, BookOpen, Check, Layers, ShoppingCart } from 'lucide-react'
import { useAddToCart } from '@/features/cart/hooks'
import { errorMessage } from '@/shared/api/problem'
import { Alert } from '@/shared/ui/Alert'
import { Badge } from '@/shared/ui/Badge'
import { Button } from '@/shared/ui/Button'
import { Card } from '@/shared/ui/Card'
import { CardHeader } from '@/shared/ui/CardHeader'
import { EmptyState } from '@/shared/ui/EmptyState'
import { MeetingList } from '@/shared/ui/MeetingList'
import { PageHeader } from '@/shared/ui/PageHeader'
import { QueryError } from '@/shared/ui/QueryError'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { useCatalogCourse } from '../hooks'
import type { CatalogSection } from '../types'

/** PB-04 / T15: course details, prerequisites and this term's sections, each with "Add to cart" / "In Cart" / "Enrolled". */
export function CourseDetailPage() {
  const id = Number(useParams().id)
  const course = useCatalogCourse(id)
  const add = useAddToCart()

  const back = (
    <Link to="/student/courses" className="mb-4 inline-flex items-center gap-1.5 text-sm text-muted hover:text-ink">
      <ArrowLeft className="size-4" aria-hidden /> Course catalog
    </Link>
  )

  if (course.isPending)
    return (
      <>
        {back}
        <Card>
          <TableSkeleton label="Loading course" />
        </Card>
      </>
    )
  if (course.isError)
    return (
      <>
        {back}
        <Card>
          <QueryError error={course.error} onRetry={() => course.refetch()} />
        </Card>
      </>
    )

  const c = course.data
  const enrolledSection = c.sections.find((s) => s.status === 'Enrolled')
  return (
    <>
      {back}
      <PageHeader eyebrow={`${c.code} · ${c.credits} credits`} title={c.name} icon={BookOpen}>
        {c.departmentName}
      </PageHeader>

      <div className="grid gap-6 lg:grid-cols-[1fr_320px]">
        <Card className="lg:order-2">
          <div className="space-y-5 p-5 text-sm">
            <div>
              <h2 className="font-display text-base font-semibold text-ink">About this course</h2>
              <p className="mt-1 text-muted">{c.description ?? 'No description yet.'}</p>
            </div>
            <div>
              <h2 className="font-display text-base font-semibold text-ink">Prerequisites</h2>
              {c.prerequisites.length === 0 ? (
                <p className="mt-1 text-muted">None.</p>
              ) : (
                <ul className="mt-2 space-y-1.5">
                  {c.prerequisites.map((p) => (
                    <li key={p.id} className="flex items-center justify-between gap-2">
                      <span>
                        <span className="font-semibold text-ink">{p.code}</span> <span className="text-muted">{p.name}</span>
                      </span>
                      {p.passed ? (
                        <Badge tone="success" dot>
                          Passed
                        </Badge>
                      ) : (
                        <Badge tone="warning" dot>
                          Not passed
                        </Badge>
                      )}
                    </li>
                  ))}
                </ul>
              )}
              <p className="mt-2 text-xs text-muted">A prerequisite counts once passed with D or better.</p>
            </div>
          </div>
        </Card>

        <Card className="lg:order-1">
          <CardHeader title="Sections this term" icon={Layers} />
          {add.isError && (
            <div className="px-5 pt-4">
              <Alert>{errorMessage(add.error)}</Alert>
            </div>
          )}
          {enrolledSection && c.sections.some((s) => s.status !== 'Enrolled') && (
            <div className="px-5 pt-4">
              <Alert tone="warning" title={`You're already enrolled in section ${enrolledSection.sectionCode}.`}>
                A course can only be held in one section. To change section, add the one you want to your cart, then use Swap
                there (during enrollment or add/drop).
              </Alert>
            </div>
          )}
          {c.sections.length === 0 ? (
            <EmptyState icon={Layers} title="Not offered this term">
              This course has no sections in the current term. Look for it again next term.
            </EmptyState>
          ) : (
            <ul className="divide-y divide-line">
              {c.sections.map((s) => (
                <SectionRow
                  key={s.id}
                  label={`${c.code} ${s.sectionCode}`}
                  section={s}
                  adding={add.isPending && add.variables === s.id}
                  onAdd={() => add.mutate(s.id)}
                />
              ))}
            </ul>
          )}
        </Card>
      </div>
    </>
  )
}

function SectionRow({ section: s, label, adding, onAdd }: { section: CatalogSection; label: string; adding: boolean; onAdd: () => void }) {
  return (
    <li className="flex flex-wrap items-start gap-4 px-5 py-4">
      <div className="min-w-0 flex-1 space-y-1.5">
        <p className="flex flex-wrap items-center gap-2">
          <span className="font-semibold text-ink">Section {s.sectionCode}</span>
          {s.seatsLeft === 0 ? (
            <Badge tone="danger" dot>
              Full
            </Badge>
          ) : (
            <span className="tabular text-sm whitespace-nowrap text-muted">
              {s.seatsLeft} of {s.capacity} seats left
            </span>
          )}
        </p>
        <p className="text-sm text-muted">{s.instructorName ?? 'Instructor to be announced'}</p>
        <MeetingList meetings={s.meetings} label={`Meetings of ${label}`} />
      </div>
      {s.status === 'Enrolled' ? (
        <Button variant="secondary" disabled aria-label={`${label}: Enrolled`}>
          <Check className="size-4" aria-hidden /> Enrolled
        </Button>
      ) : s.status === 'InCart' ? (
        <Button variant="secondary" disabled aria-label={`${label}: In Cart`}>
          <ShoppingCart className="size-4" aria-hidden /> In Cart
        </Button>
      ) : (
        <Button onClick={onAdd} loading={adding} aria-label={`Add ${label} to cart`}>
          <ShoppingCart className="size-4" aria-hidden /> Add to cart
        </Button>
      )}
    </li>
  )
}
