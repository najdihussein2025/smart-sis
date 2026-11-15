import { Gauge } from 'lucide-react'
import { cn } from '@/shared/lib/cn'
import { CardHeader } from '@/shared/ui/CardHeader'

/**
 * The one credit figure on the cart page (E2): enrolled + cart against the student's cap, as text and as a bar. The bar
 * has two segments (enrolled, in cart) and a legend in words, so colour is never the only signal.
 */
export function CreditMeter({ enrolled, inCart, max }: { enrolled: number; inCart: number; max: number }) {
  const total = enrolled + inCart
  const over = total - max
  const scale = Math.max(max, total, 1)
  const pct = (n: number) => `${(n / scale) * 100}%`

  return (
    <>
      <CardHeader title="Credits this term" icon={Gauge} />
      <div className="space-y-3 p-4 sm:p-5">
        <p className="tabular text-ink">
          <span className="font-display text-3xl font-semibold tracking-tight">{total}</span>
          <span className="text-muted"> of {max} credits</span>
        </p>
        <div
          role="meter"
          aria-label="Credits used"
          aria-valuemin={0}
          aria-valuemax={max}
          aria-valuenow={Math.min(total, max)}
          aria-valuetext={`${enrolled} enrolled and ${inCart} in cart, ${total} of ${max} credits`}
          className="flex h-2.5 overflow-hidden rounded-full bg-canvas ring-1 ring-inset ring-line"
        >
          <span className={cn('h-full', over > 0 ? 'bg-danger' : 'bg-brand')} style={{ width: pct(enrolled) }} />
          <span className={cn('h-full', over > 0 ? 'bg-danger/50' : 'bg-accent')} style={{ width: pct(inCart) }} />
        </div>
        <ul className="tabular flex flex-wrap gap-x-4 gap-y-1 text-xs text-muted">
          <li className="inline-flex items-center gap-1.5">
            <span aria-hidden className="size-2 rounded-full bg-brand" /> {enrolled} enrolled
          </li>
          <li className="inline-flex items-center gap-1.5">
            <span aria-hidden className="size-2 rounded-full bg-accent" /> {inCart} in cart
          </li>
          <li>{over > 0 ? <span className="font-medium text-danger">{over} over your limit</span> : `${max - total} left`}</li>
        </ul>
      </div>
    </>
  )
}
