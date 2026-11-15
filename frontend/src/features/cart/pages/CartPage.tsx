import { useState } from 'react'
import { ArrowLeftRight, BookOpen, ShoppingCart, Trash2 } from 'lucide-react'
import { useEnroll, useMyEnrollments } from '@/features/enrollment/hooks'
import { errorMessage } from '@/shared/api/problem'
import { useSavedMessage } from '@/shared/lib/useSavedMessage'
import { Alert } from '@/shared/ui/Alert'
import { Badge } from '@/shared/ui/Badge'
import { Button } from '@/shared/ui/Button'
import { Card } from '@/shared/ui/Card'
import { CardHeader } from '@/shared/ui/CardHeader'
import { EmptyState } from '@/shared/ui/EmptyState'
import { MeetingList } from '@/shared/ui/MeetingList'
import { LinkButton } from '@/shared/ui/LinkButton'
import { PageHeader } from '@/shared/ui/PageHeader'
import { QueryError } from '@/shared/ui/QueryError'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { WindowBadge } from '@/shared/ui/WindowBadge'
import { CreditMeter } from '../components/CreditMeter'
import { EnrolledList } from '../components/EnrolledList'
import { EnrollResults } from '../components/EnrollResults'
import { SwapDialog } from '../components/SwapDialog'
import { useCart, useRemoveFromCart } from '../hooks'
import type { CartItem } from '../types'

/**
 * PB-04 / T15 and PB-05 / T19: the sections the student plans to take. They pick several and enroll at once; each
 * section that fails shows its own reason (window, credit cap, prerequisite, same course, time conflict, full).
 * PB-07 / T25: enrolled sections are listed underneath with Drop.
 */
export function CartPage() {
  const cart = useCart()
  const mine = useMyEnrollments()
  const remove = useRemoveFromCart()
  const enroll = useEnroll()
  const saved = useSavedMessage()
  const [selected, setSelected] = useState<number[]>([])
  const [swapping, setSwapping] = useState<CartItem | null>(null)

  const items = cart.data?.items ?? []
  // Drop ids that left the cart (enrolled or removed) so the count stays right.
  const chosen = selected.filter((id) => items.some((i) => i.sectionId === id))
  const allChosen = items.length > 0 && chosen.length === items.length
  const toggle = (id: number) => setSelected((s) => (s.includes(id) ? s.filter((x) => x !== id) : [...s, id]))
  const closed = cart.data?.window === 'Closed'
  const enrolled = mine.data?.items ?? []

  const meter = cart.data && (
    <Card className="h-fit">
      <CreditMeter enrolled={cart.data.enrolledCredits} inCart={cart.data.cartCredits} max={cart.data.maxCredits} />
      {items.length > 0 && (
        <div className="space-y-2 border-t border-line p-4 sm:p-5">
          <Button
            className="w-full"
            disabled={chosen.length === 0 || closed}
            loading={enroll.isPending}
            onClick={() => enroll.mutate(chosen, { onSuccess: () => setSelected([]) })}
          >
            Enroll in {chosen.length || ''} {chosen.length === 1 ? 'section' : 'sections'}
          </Button>
          <p className="text-xs text-muted">
            {closed
              ? 'Registration is closed. You can enroll during the enrollment or add/drop period.'
              : chosen.length === 0
                ? 'Tick the sections you want, then enroll. Each one is checked on its own.'
                : 'Each section is checked on its own: credit limit, prerequisites, same course, time conflicts and free seats.'}
          </p>
        </div>
      )}
    </Card>
  )

  return (
    <>
      <PageHeader eyebrow={cart.data?.termName ?? 'Academics'} title="Cart" icon={ShoppingCart}>
        <span className="inline-flex flex-wrap items-center gap-2">
          Choose sections and enroll in them together while registration is open.
          {cart.data && <WindowBadge window={cart.data.window} />}
        </span>
      </PageHeader>

      <div className="mb-6 space-y-3 empty:hidden">
        {saved.message && <Alert tone="success">{saved.message}</Alert>}
        {enroll.data && <EnrollResults result={enroll.data} />}
        {enroll.isError && <Alert>{errorMessage(enroll.error)}</Alert>}
        {remove.isError && <Alert>{errorMessage(remove.error)}</Alert>}
      </div>

      {cart.isPending ? (
        <Card>
          <TableSkeleton label="Loading cart" />
        </Card>
      ) : cart.isError ? (
        <Card>
          <QueryError error={cart.error} onRetry={() => cart.refetch()} />
        </Card>
      ) : items.length === 0 ? (
        // Nothing to enroll in: what the student already has comes first, the way to fill the cart after it.
        <div className="space-y-6">
          <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_300px]">
            <EnrolledList onDropped={saved.show} />
            {meter}
          </div>
          <Card>
            <EmptyState
              icon={ShoppingCart}
              title="Your cart is empty"
              action={
                <LinkButton
                  to="/student/courses"
                >
                  <BookOpen className="size-4" aria-hidden /> Browse catalog
                </LinkButton>
              }
            >
              Add the sections you want from the course catalog, then enroll in them here together.
            </EmptyState>
          </Card>
        </div>
      ) : (
        <div className="space-y-6">
          <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_300px]">
            <Card>
              <CardHeader
                title="In your cart"
                description={`${items.length} ${items.length === 1 ? 'section' : 'sections'}`}
                icon={ShoppingCart}
              />
              <label className="flex min-h-11 items-center gap-3 border-b border-line px-4 py-2 text-sm text-muted sm:px-5">
                <input
                  type="checkbox"
                  className="size-4 accent-brand"
                  checked={allChosen}
                  onChange={() => setSelected(allChosen ? [] : items.map((i) => i.sectionId))}
                />
                Select all
              </label>
              <ul className="divide-y divide-line" aria-label="Sections in your cart">
                {items.map((item) => (
                  <CartRow
                    key={item.sectionId}
                    item={item}
                    checked={chosen.includes(item.sectionId)}
                    onToggle={() => toggle(item.sectionId)}
                    canSwap={enrolled.length > 0 && !closed}
                    onSwap={() => setSwapping(item)}
                    removing={remove.isPending && remove.variables === item.sectionId}
                    onRemove={() => remove.mutate(item.sectionId)}
                  />
                ))}
              </ul>
            </Card>
            {meter}
          </div>
          <EnrolledList onDropped={saved.show} />
        </div>
      )}

      <SwapDialog item={swapping} enrolled={enrolled} onClose={() => setSwapping(null)} onSwapped={saved.show} />
    </>
  )
}

interface RowProps {
  item: CartItem
  checked: boolean
  onToggle: () => void
  canSwap: boolean
  onSwap: () => void
  removing: boolean
  onRemove: () => void
}

function CartRow({ item, checked, onToggle, canSwap, onSwap, removing, onRemove }: RowProps) {
  const label = `${item.courseCode} ${item.sectionCode}`
  return (
    <li className="flex flex-wrap items-start gap-3 px-4 py-4 sm:px-5">
      <input type="checkbox" className="mt-1 size-4 accent-brand" checked={checked} onChange={onToggle} aria-label={`Select ${label}`} />
      <div className="min-w-0 flex-1 space-y-1.5">
        <p className="text-ink">
          <span className="font-semibold">{label}</span> <span>{item.courseName}</span>
        </p>
        {item.seatsLeft === 0 && (
          <p>
            <Badge tone="danger" dot>
              Full
            </Badge>
          </p>
        )}
        <p className="tabular text-sm text-muted">
          {item.credits} credits · {item.instructorName ?? 'Instructor to be announced'}
          {item.seatsLeft > 0 && ` · ${item.seatsLeft} seats left`}
        </p>
        <MeetingList meetings={item.meetings} label={`Meetings of ${label}`} />
      </div>
      <div className="flex gap-1 max-sm:w-full max-sm:justify-end">
        {canSwap && (
          <Button variant="ghost" size="sm" onClick={onSwap} aria-label={`Swap into ${label}`}>
            <ArrowLeftRight className="size-4" aria-hidden /> Swap
          </Button>
        )}
        <Button variant="danger-ghost" size="sm" onClick={onRemove} loading={removing} aria-label={`Remove ${label} from cart`}>
          <Trash2 className="size-4" aria-hidden /> Remove
        </Button>
      </div>
    </li>
  )
}
