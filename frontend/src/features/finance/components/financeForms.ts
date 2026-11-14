import { z } from 'zod'
import { formatMoney } from '@/shared/lib/format'
import type { CreateChargeBatchRequest, CreateChargeRequest, RecordPaymentRequest } from '../types'

/** Mirrors FinanceFieldRules on the API (UX only; the API checks again). */
export const MAX_AMOUNT = 1_000_000

const amount = z
  .string()
  .trim()
  .min(1, 'Enter an amount.')
  .regex(/^\d+(\.\d{1,2})?$/, 'Enter an amount like 1500 or 1500.50.')
  .refine((v) => Number(v) > 0, 'Enter an amount greater than zero.')
  .refine((v) => Number(v) <= MAX_AMOUNT, 'Enter an amount of at most 1,000,000.')

const title = z.string().trim().min(1, 'Enter a title.').max(128, 'Use at most 128 characters.')
const pick = (message: string) => z.string().min(1, message)

// ---------- Charge a whole major ----------

export const batchSchema = z.object({
  majorId: pick('Choose a major.'),
  termId: pick('Choose a term.'),
  paymentTypeId: pick('Choose a payment type.'),
  title,
  amount,
  dueDate: pick('Choose a due date.'),
})
export type BatchFormValues = z.infer<typeof batchSchema>
export const batchFields = ['majorId', 'termId', 'paymentTypeId', 'title', 'amount', 'dueDate'] as const satisfies readonly (keyof BatchFormValues)[]
export const emptyBatch: BatchFormValues = { majorId: '', termId: '', paymentTypeId: '', title: '', amount: '', dueDate: '' }

export const toBatchRequest = (v: BatchFormValues): CreateChargeBatchRequest => ({
  majorId: Number(v.majorId),
  termId: Number(v.termId),
  paymentTypeId: Number(v.paymentTypeId),
  title: v.title.trim(),
  amount: Number(v.amount),
  dueDate: v.dueDate,
})

// ---------- One student ----------

export const chargeSchema = z.object({
  universityId: z.string().trim().regex(/^\d{4,9}$/, "Enter the student's University ID (4 to 9 digits)."),
  termId: pick('Choose a term.'),
  paymentTypeId: pick('Choose a payment type.'),
  title,
  amount,
  dueDate: pick('Choose a due date.'),
})
export type ChargeFormValues = z.infer<typeof chargeSchema>
export const chargeFields = ['universityId', 'termId', 'paymentTypeId', 'title', 'amount', 'dueDate'] as const satisfies readonly (keyof ChargeFormValues)[]
export const emptyCharge: ChargeFormValues = { universityId: '', termId: '', paymentTypeId: '', title: '', amount: '', dueDate: '' }

export const toChargeRequest = (v: ChargeFormValues): CreateChargeRequest => ({
  universityId: Number(v.universityId),
  termId: Number(v.termId),
  paymentTypeId: Number(v.paymentTypeId),
  title: v.title.trim(),
  amount: Number(v.amount),
  dueDate: v.dueDate,
})

// ---------- Record a payment ----------

/** Rule F3 in the browser: a payment can't be more than what is still owed. */
export const paymentSchema = (balance: number) =>
  z.object({
    amount: amount.refine(
      (v) => Math.round(Number(v) * 100) <= Math.round(balance * 100),
      `Payment can't be more than the remaining balance (${formatMoney(balance)}).`,
    ),
    note: z.string().trim().max(512, 'Use at most 512 characters.'),
  })
export type PaymentFormValues = z.infer<ReturnType<typeof paymentSchema>>
export const paymentFields = ['amount', 'note'] as const satisfies readonly (keyof PaymentFormValues)[]

export const toPaymentRequest = (v: PaymentFormValues): RecordPaymentRequest => ({
  amount: Number(v.amount),
  note: v.note.trim() || null,
})
