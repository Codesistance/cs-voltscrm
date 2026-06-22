import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { Id } from '@/shared/api/types'
import { agentsApi, type CreateAgent, type UpdateAgent } from './agentsApi'

export const agentKeys = {
  all: ['agents'] as const,
}

export function useAgents() {
  return useQuery({ queryKey: agentKeys.all, queryFn: agentsApi.list })
}

export function useCreateAgent() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: CreateAgent) => agentsApi.create(body),
    onSuccess: () => qc.invalidateQueries({ queryKey: agentKeys.all }),
  })
}

export function useUpdateAgent(id: Id) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: UpdateAgent) => agentsApi.update(id, body),
    onSuccess: () => qc.invalidateQueries({ queryKey: agentKeys.all }),
  })
}

export function useDeactivateAgent() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: Id) => agentsApi.deactivate(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: agentKeys.all }),
  })
}

export function useResendInvite() {
  return useMutation({ mutationFn: (id: Id) => agentsApi.resendInvite(id) })
}
