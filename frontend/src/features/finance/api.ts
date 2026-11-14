import { api } from '@/shared/api/client'
import type { PagedResult } from '@/shared/api/types'
import type {
  AdminCharge,
  BatchSearch,
  ChargeBatch,
  ChargeSearch,
  CreateChargeBatchRequest,
  CreateChargeRequest,
  FinanceOptions,
  MyFinance,
  RecordPaymentRequest,
} from './types'

const admin = '/api/admin/finance'

export const financeApi = {
  mine: () => api.get<MyFinance>('/api/finance/my').then((r) => r.data),

  options: () => api.get<FinanceOptions>(`${admin}/options`).then((r) => r.data),
  charges: (q: ChargeSearch) => api.get<PagedResult<AdminCharge>>(`${admin}/charges`, { params: q }).then((r) => r.data),
  batches: (q: BatchSearch) => api.get<PagedResult<ChargeBatch>>(`${admin}/batches`, { params: q }).then((r) => r.data),
  createBatch: (body: CreateChargeBatchRequest) => api.post<ChargeBatch>(`${admin}/batches`, body).then((r) => r.data),
  createCharge: (body: CreateChargeRequest) => api.post<AdminCharge>(`${admin}/charges`, body).then((r) => r.data),
  recordPayment: (chargeId: number, body: RecordPaymentRequest) =>
    api.post<AdminCharge>(`${admin}/charges/${chargeId}/payments`, body).then((r) => r.data),
}
