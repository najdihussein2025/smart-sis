import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { financeApi } from './api'
import type { BatchSearch, ChargeSearch, CreateChargeBatchRequest, CreateChargeRequest, RecordPaymentRequest } from './types'

export const financeKeys = {
  all: ['finance'] as const,
  mine: ['finance', 'my'] as const,
  options: ['finance', 'options'] as const,
  charges: (q: ChargeSearch) => ['finance', 'charges', q] as const,
  batches: (q: BatchSearch) => ['finance', 'batches', q] as const,
}

/** The signed-in student's account. */
export const useMyFinance = () => useQuery({ queryKey: financeKeys.mine, queryFn: financeApi.mine })

export const useFinanceOptions = () => useQuery({ queryKey: financeKeys.options, queryFn: financeApi.options, staleTime: 60_000 })

export const useCharges = (q: ChargeSearch) =>
  useQuery({ queryKey: financeKeys.charges(q), queryFn: () => financeApi.charges(q), placeholderData: keepPreviousData })

export const useBatches = (q: BatchSearch, enabled = true) =>
  useQuery({ queryKey: financeKeys.batches(q), queryFn: () => financeApi.batches(q), placeholderData: keepPreviousData, enabled })

/** Any write changes charges, batches (paid counts) and possibly the options (nothing today, cheap to refresh). */
function useFinanceMutation<TArgs, TResult>(fn: (args: TArgs) => Promise<TResult>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: fn,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: financeKeys.all }),
  })
}

export const useCreateBatch = () => useFinanceMutation((body: CreateChargeBatchRequest) => financeApi.createBatch(body))

export const useCreateCharge = () => useFinanceMutation((body: CreateChargeRequest) => financeApi.createCharge(body))

export const useRecordPayment = () =>
  useFinanceMutation(({ chargeId, body }: { chargeId: number; body: RecordPaymentRequest }) => financeApi.recordPayment(chargeId, body))
