import { useQuery } from '@tanstack/react-query'
import { agentHomeApi } from './agentHomeApi'

export const agentHomeKeys = {
  kpis: () => ['agent', 'me', 'kpis'] as const,
}

export function useAgentKpis() {
  return useQuery({
    queryKey: agentHomeKeys.kpis(),
    queryFn: agentHomeApi.kpis,
  })
}
