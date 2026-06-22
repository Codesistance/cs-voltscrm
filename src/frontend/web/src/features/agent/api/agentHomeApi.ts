import { get } from '@/shared/api/http'

export interface AgentKpis {
  assignedCustomers: number
  visitsToday: number
  paymentsCollected: number
  paymentsCurrency: string
  openTasks: number
}

export const agentHomeApi = {
  kpis: () => get<AgentKpis>('/agents/me/kpis'),
}
