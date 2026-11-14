import { useEffect, useMemo } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { applyFieldErrors, errorMessage, getProblem } from '@/shared/api/problem'
import { formatDateOnly, formatMoney } from '@/shared/lib/format'
import { Alert } from '@/shared/ui/Alert'
import { Button } from '@/shared/ui/Button'
import { Dialog } from '@/shared/ui/Dialog'
import { TextAreaField } from '@/shared/ui/TextAreaField'
import { TextField } from '@/shared/ui/TextField'
import { useRecordPayment } from '../hooks'
import type { AdminCharge } from '../types'
import { paymentFields, paymentSchema, toPaymentRequest, type PaymentFormValues } from './financeForms'
import { PaymentHistory } from './PaymentHistory'

interface Props {
  /** The charge to pay; null when closed. */
  charge: AdminCharge | null
  onClose: () => void
  onSaved: (charge: AdminCharge, amount: number) => void
}

/** Record a full or partial payment (rule F2). The remaining balance is shown and enforced here too (rule F3). */
export function PaymentDialog({ charge, onClose, onSaved }: Props) {
  const save = useRecordPayment()
  const balance = charge?.balance ?? 0
  const schema = useMemo(() => paymentSchema(balance), [balance])
  const { register, handleSubmit, formState, reset, setError, setValue } = useForm<PaymentFormValues>({
    resolver: zodResolver(schema),
    defaultValues: { amount: '', note: '' },
  })

  useEffect(() => {
    if (!charge) return
    reset({ amount: '', note: '' })
    save.reset()
    // Only when another charge is opened.
  }, [charge?.id])

  if (!charge) return null

  const onSubmit = handleSubmit((values) =>
    save.mutate(
      { chargeId: charge.id, body: toPaymentRequest(values) },
      {
        onSuccess: (updated) => onSaved(updated, Number(values.amount)),
        onError: (error) => applyFieldErrors(error, paymentFields, setError),
      },
    ),
  )

  const problem = getProblem(save.error)
  const showTopError = save.isError && !(problem?.errors && paymentFields.some((f) => problem.errors?.[f]))
  const e = formState.errors

  return (
    <Dialog
      open
      onClose={onClose}
      title="Record payment"
      description={`${charge.studentName} (${charge.universityId}) · ${charge.title}`}
      actions={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="payment-form" loading={save.isPending}>
            Record payment
          </Button>
        </>
      }
    >
      <form id="payment-form" onSubmit={onSubmit} noValidate className="space-y-5" onChange={() => save.isError && save.reset()}>
        {showTopError && <Alert>{errorMessage(save.error)}</Alert>}

        <dl className="grid grid-cols-3 gap-3 rounded-lg bg-canvas p-3 text-sm">
          <div>
            <dt className="text-muted">Amount</dt>
            <dd className="font-medium tabular-nums text-ink">{formatMoney(charge.amount)}</dd>
          </div>
          <div>
            <dt className="text-muted">Paid</dt>
            <dd className="font-medium tabular-nums text-ink">{formatMoney(charge.paid)}</dd>
          </div>
          <div>
            <dt className="text-muted">Remaining balance</dt>
            <dd className="font-semibold tabular-nums text-ink">{formatMoney(charge.balance)}</dd>
          </div>
        </dl>
        <p className="-mt-2 text-[13px] text-muted">Due {formatDateOnly(charge.dueDate)}</p>

        <div className="flex items-end gap-2">
          <div className="flex-1">
            <TextField label="Payment amount" inputMode="decimal" placeholder="0.00" error={e.amount?.message} {...register('amount')} />
          </div>
          <Button
            variant="secondary"
            type="button"
            className="mb-0.5 h-11"
            onClick={() => setValue('amount', charge.balance.toFixed(2), { shouldValidate: true })}
            aria-label={`Pay the full balance of ${formatMoney(charge.balance)}`}
          >
            Full balance
          </Button>
        </div>
        <TextAreaField label="Note (optional)" placeholder="e.g. receipt number" rows={2} error={e.note?.message} {...register('note')} />

        <div>
          <h3 className="mb-1.5 text-sm font-semibold text-ink">Earlier payments</h3>
          <PaymentHistory payments={charge.payments} label="Earlier payments" />
        </div>
      </form>
    </Dialog>
  )
}
