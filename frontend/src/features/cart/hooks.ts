import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { cartApi } from './api'

export const cartKeys = { all: ['cart'] as const }

export const useCart = () => useQuery({ queryKey: cartKeys.all, queryFn: cartApi.get })

/** Adding or removing changes the "In Cart" labels in the catalog too. */
function useCartMutation<T>(fn: (sectionId: number) => Promise<T>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: fn,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: cartKeys.all })
      void queryClient.invalidateQueries({ queryKey: ['catalog'] })
    },
  })
}

export const useAddToCart = () => useCartMutation(cartApi.add)
export const useRemoveFromCart = () => useCartMutation(cartApi.remove)
