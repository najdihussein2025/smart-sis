import { useState, type ReactNode } from 'react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { isAxiosError } from 'axios'
import { AuthProvider } from '@/shared/auth/AuthProvider'

export function Providers({ children }: { children: ReactNode }) {
  const [client] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            refetchOnWindowFocus: false,
            // Don't retry client errors (401/403/404/409); retry network/5xx once.
            retry: (count, error) => !(isAxiosError(error) && (error.response?.status ?? 500) < 500) && count < 1,
          },
        },
      }),
  )

  return (
    <QueryClientProvider client={client}>
      <AuthProvider>{children}</AuthProvider>
    </QueryClientProvider>
  )
}
