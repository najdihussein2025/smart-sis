import { useState } from 'react'
import { Link } from 'react-router'
import { ArrowLeftRight, ShoppingCart, Trash2 } from 'lucide-react'
import { useEnroll, useMyEnrollments } from '@/features/enrollment/hooks'
import { errorMessage } from '@/shared/api/problem'
import { useSavedMessage } from '@/shared/lib/useSavedMessage'
import { Alert } from '@/shared/ui/Alert'
import { Badge } from '@/shared/ui/Badge'
import { Button } from '@/shared/ui/Button'
import { Card } from '@/shared/ui/Card'
import { EmptyState } from '@/shared/ui/EmptyState'
import { MeetingList } from '@/shared/ui/MeetingList'
import { PageHeader } from '@/shared/ui/PageHeader'
import { QueryError } from '@/shared/ui/QueryError'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { WindowBadge } from '@/shared/ui/WindowBadge'
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

  return (
    <>
      <PageHeader eyebrow={cart.data?.termName ?? 'Enrollment'} title="Cart">
        <span className="inline-flex flex-wrap items-center gap-2">
          Choose sections and enroll in them together while registration is open.
          {cart.data && <WindowBadge window={cart.data.window} />}
        </span>
      </PageHeader>

      <div className="mb-4 space-y-3 empty:hidden">
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
      ) : (
        <div className="grid gap-6 lg:grid-cols-[1fr_300px]">
          <Card>
            {items.length === 0 ? (
              <EmptyState icon={ShoppingCart} title="Your cart is empty">
                Open the{' '}
                <Link to="/student/courses" className="font-medium text-brand underline">
                  course catalog
                </Link>{' '}
                and add the sections you want.
              </EmptyState>
            ) : (
              <>
                <label className="flex items-center gap-3 border-b border-line px-5 py-3 text-sm text-muted">
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
              </>
            )}
          </Card>

          <Card className="h-fit">
            <dl className="space-y-2 p-5 text-sm">
              <Row label="In cart" value={`${cart.data.cartCredits} credits`} />
              <Row label="Already enrolled" value={`${cart.data.enrolledCredits} credits`} />
              <Row label="Your limit" value={`${cart.data.maxCredits} credits per term`} />
            </dl>
            <div className="space-y-2 border-t border-line p-5">
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
                  : 'Each section is checked on its own: credit limit, prerequisites, same course, time conflicts and free seats.'}
              </p>
            </div>
          </Card>
        </div>
      )}

      <div className="mt-6">
        <EnrolledList onDropped={saved.show} />
      </div>

      <SwapDialog item={swapping} enrolled={enrolled} onClose={() => setSwapping(null)} onSwapped={saved.show} />
    </>
  )
}

function Row({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex justify-between gap-3">
      <dt className="text-muted">{label}</dt>
      <dd className="font-medium text-ink">{value}</dd>
    </div>
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
    <li className="flex flex-wrap items-start gap-3 px-5 py-4">
      <input type="checkbox" className="mt-1 size-4 accent-brand" checked={checked} onChange={onToggle} aria-label={`Select ${label}`} />
      <div className="min-w-0 flex-1 space-y-1.5">
        <p className="flex flex-wrap items-center gap-2">
          <span className="font-medium text-ink">{label}</span>
          <span className="text-ink">{item.courseName}</span>
          {item.seatsLeft === 0 && <Badge tone="danger">Full</Badge>}
        </p>
        <p className="text-sm text-muted">
          {item.credits} credits · {item.instructorName ?? 'Instructor to be announced'}
          {item.seatsLeft > 0 && ` · ${item.seatsLeft} seats left`}
        </p>
        <MeetingList meetings={item.meetings} label={`Meetings of ${label}`} />
      </div>
      <div className="flex gap-1 max-sm:w-full max-sm:justify-end">
        {canSwap && (
          <Button variant="ghost" className="h-8 px-2.5" onClick={onSwap} aria-label={`Swap into ${label}`}>
            <ArrowLeftRight className="size-4" aria-hidden /> Swap
          </Button>
        )}
        <Button variant="ghost" className="h-8 px-2.5" onClick={onRemove} loading={removing} aria-label={`Remove ${label} from cart`}>
          <Trash2 className="size-4" aria-hidden /> Remove
        </Button>
      </div>
    </li>
  )
}
