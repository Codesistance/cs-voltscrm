import { get, post, put } from '@/shared/api/http'
import type { Id } from '@/shared/api/types'
import type { Location, LocationInput } from '@/features/location/types'
import type { AgentKpis } from '@/features/agent/api/agentHomeApi'

export interface Agent {
  id: Id
  userId: string
  email: string
  firstName: string
  lastName: string
  phone: string | null
  location: Location
  territory: string | null
  isActive: boolean
}

export interface CreateAgent {
  email: string
  firstName: string
  lastName: string
  phone?: string | null
  location: LocationInput
  territory?: string | null
}

export interface UpdateAgent {
  firstName: string
  lastName: string
  phone?: string | null
  location: LocationInput
  territory?: string | null
}

export interface ResetPasswordResult {
  temporaryPassword: string | null
}

export const agentsApi = {
  list: () => get<Agent[]>('/agents'),
  kpis: (id: Id) => get<AgentKpis>(`/agents/${id}/kpis`),
  create: (body: CreateAgent) => post<Agent>('/agents', body),
  update: (id: Id, body: UpdateAgent) => put<void>(`/agents/${id}`, body),
  deactivate: (id: Id) => post<void>(`/agents/${id}/deactivate`, {}),
  resendInvite: (id: Id) => post<void>(`/agents/${id}/resend-invite`, {}),
  resetPassword: (id: Id, newPassword?: string) =>
    post<ResetPasswordResult>(`/agents/${id}/reset-password`, { newPassword }),
}
