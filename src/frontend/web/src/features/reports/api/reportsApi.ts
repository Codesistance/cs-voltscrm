import { get } from '@/shared/api/http'
import type { Id } from '@/shared/api/types'
import type { AgingReport, CollectionSummary, CustomerStatement, DashboardSummary } from './types'

const BASE = '/reports'

function monthRange() {
  const now = new Date()
  const from = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), 1))
  const to = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth() + 1, 0))
  return {
    from: from.toISOString().slice(0, 10),
    to: to.toISOString().slice(0, 10),
  }
}

export const reportsApi = {
  customerStatement: (customerId: Id, from?: string, to?: string) =>
    get<CustomerStatement>(`${BASE}/customers/${customerId}/statement`, { params: { from, to } }),
  collections: () => {
    const { from, to } = monthRange()
    return get<CollectionSummary>(`${BASE}/collections`, { params: { from, to } })
  },
  aging: () => get<AgingReport>(`${BASE}/aging`),
  dashboardSummary: () => get<DashboardSummary>(`${BASE}/dashboard-summary`),
}
