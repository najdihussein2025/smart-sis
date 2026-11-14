import { Link } from 'react-router'
import { ShoppingCart, Trash2 } from 'lucide-react'
import { errorMessage } from '@/shared/api/problem'
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
import { useCart, useRemoveFromCart } from '../hooks'
import type { CartItem } from '../types'

/** PB-04 / T15: the sections the student plans to take, with credits against their cap, and remove. */
export function CartPage() {
  const cart = useCart()
  const remove = useRemoveFromCart()

  return (
    <>
      <PageHeader eyebrow={cart.data?.termName ?? 'Enrollment'} title="Cart">
        <span className="inline-flex flex-wrap items-center gap-2">
          Sections you plan to take. Choose them and enroll when registration is open.
          {cart.data && <WindowBadge window={cart.data.window} />}
        </span>
      </PageHeader>

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
            {remove.isError && (
              <div className="px-5 pt-4">
                <Alert>{errorMessage(remove.error)}</Alert>
              </div>
            )}
            {cart.data.items.length === 0 ? (
              <EmptyState icon={ShoppingCart} title="Your cart is empty">
                Open the <Link to="/student/courses" className="font-medium text-brand underline">course catalog</Link> and add the
                sections you want.
              </EmptyState>
            ) : (
              <ul className="divide-y divide-line" aria-label="Sections in your cart">
                {cart.data.items.map((item) => (
                  <CartRow
                    key={item.sectionId}
                    item={item}
                    removing={remove.isPending && remove.variables === item.sectionId}
                    onRemove={() => remove.mutate(item.sectionId)}
                  />
                ))}
              </ul>
            )}
          </Card>

          <Card className="h-fit">
            <dl className="space-y-2 p-5 text-sm">
              <Row label="In cart" value={`${cart.data.cartCredits} credits`} />
              <Row label="Already enrolled" value={`${cart.data.enrolledCredits} credits`} />
              <Row label="Your limit" value={`${cart.data.maxCredits} credits per term`} />
            </dl>
          </Card>
        </div>
      )}
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
  removing: boolean
  onRemove: () => void
}

function CartRow({ item, removing, onRemove }: RowProps) {
  const label = `${item.courseCode} ${item.sectionCode}`
  return (
    <li className="flex items-start gap-3 px-5 py-4">
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
      <Button variant="ghost" className="h-8 px-2.5" onClick={onRemove} loading={removing} aria-label={`Remove ${label} from cart`}>
        <Trash2 className="size-4" aria-hidden /> Remove
      </Button>
    </li>
  )
}
