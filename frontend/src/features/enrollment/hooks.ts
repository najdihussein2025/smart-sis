import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { enrollmentApi } from './api'

export const enrollmentKeys = { mine: ['enrollment', 'mine'] as const }

export const useMyEnrollments = () => useQuery({ queryKey: enrollmentKeys.mine, queryFn: enrollmentApi.mine })

/** Enroll, swap and drop change the cart, the catalog labels, the schedule and the dashboard numbers. */
function useInvalidateRegistration() {
  const queryClient = useQueryClient()
  return () => {
    for (const key of ['enrollment', 'cart', 'catalog', 'schedule', 'dashboard'])
      void queryClient.invalidateQueries({ queryKey: [key] })
  }
}

export function useEnroll() {
  const invalidate = useInvalidateRegistration()
  return useMutation({ mutationFn: enrollmentApi.enroll, onSuccess: invalidate })
}

export function useSwap() {
  const invalidate = useInvalidateRegistration()
  return useMutation({
    mutationFn: ({ from, to }: { from: number; to: number }) => enrollmentApi.swap(from, to),
    onSuccess: invalidate,
  })
}

export function useDrop() {
  const invalidate = useInvalidateRegistration()
  return useMutation({ mutationFn: enrollmentApi.drop, onSuccess: invalidate })
}
