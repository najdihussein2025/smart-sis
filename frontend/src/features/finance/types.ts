// TODO: replace with generated types from `npm run gen:api` (FinanceDtos.cs).

/** Rule F2: follows the sum of the payments. */
export type ChargeStatus = 'Pending' | 'Partial' | 'Paid'

export interface Payment {
  id: number
  amount: number
  /** UTC ISO string. */
  paidAt: string
  note: string | null
}

/** One of the signed-in student's charges (GET /api/finance/my). */
export interface StudentCharge {
  id: number
  title: string
  termId: number
  termName: string
  paymentTypeName: string
  amount: number
  paid: number
  balance: number
  /** DateOnly, "yyyy-mm-dd". */
  dueDate: string
  status: ChargeStatus
  isOverdue: boolean
  payments: Payment[]
}

export interface MyFinance {
  totalCharged: number
  totalPaid: number
  outstandingBalance: number
  overdueBalance: number
  nextDueDate: string | null
  charges: StudentCharge[]
}

/** A charge as the admin sees it. */
export interface AdminCharge extends StudentCharge {
  studentId: number
  universityId: number
  studentName: string
  majorName: string
  batchId: number | null
}

export interface ChargeBatch {
  id: number
  title: string
  majorId: number
  majorName: string
  termId: number
  termName: string
  paymentTypeId: number
  paymentTypeName: string
  amount: number
  dueDate: string
  chargeCount: number
  paidCount: number
  createdAt: string
}

export interface FinanceOptions {
  terms: { id: number; name: string; isCurrent: boolean }[]
  majors: { id: number; name: string; activeStudents: number }[]
  paymentTypes: { id: number; name: string }[]
}

export interface CreateChargeBatchRequest {
  majorId: number
  termId: number
  paymentTypeId: number
  title: string
  amount: number
  dueDate: string
}

export interface CreateChargeRequest {
  universityId: number
  termId: number
  paymentTypeId: number
  title: string
  amount: number
  dueDate: string
}

export interface RecordPaymentRequest {
  amount: number
  note: string | null
}

export interface ChargeSearch {
  search?: string
  status?: ChargeStatus
  termId?: number
  majorId?: number
  batchId?: number
  page?: number
  pageSize?: number
}

export interface BatchSearch {
  termId?: number
  majorId?: number
  page?: number
  pageSize?: number
}
