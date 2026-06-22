import { MutationCache, QueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { ApiError } from '@/shared/api/http'

export const queryClient = new QueryClient({
  // Global safety net so no mutation failure is silent. Field-level validation errors are
  // surfaced inline on the form, so we skip toasting those.
  mutationCache: new MutationCache({
    onError: (error) => {
      if (error instanceof ApiError && error.fieldErrors) return
      toast.error(error instanceof ApiError ? error.message : "That didn't go through — mind trying again?")
    },
  }),
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
})
