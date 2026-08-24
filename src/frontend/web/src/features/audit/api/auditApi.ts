import { get } from '@/shared/api/http'

export interface AuditEvent {
  id: string
  occurredAt: string
  action: string
  outcome: string
  actorUserId?: string | null
  actorEmail?: string | null
  targetType?: string | null
  targetId?: string | null
  targetLabel?: string | null
  ipAddress?: string | null
  userAgent?: string | null
  details?: string | null
}

export interface AuditPage {
  items: AuditEvent[]
  total: number
  page: number
  pageSize: number
}

export interface AuditFilters {
  action?: string
  actorEmail?: string
  targetId?: string
  outcome?: string
  from?: string
  to?: string
  page?: number
  pageSize?: number
}

function qs(filters: AuditFilters): string {
  const sp = new URLSearchParams()
  for (const [key, value] of Object.entries(filters)) {
    if (value !== undefined && value !== null && value !== '') sp.set(key, String(value))
  }
  const s = sp.toString()
  return s ? `?${s}` : ''
}

export const auditApi = {
  list: (filters: AuditFilters = {}) => get<AuditPage>(`/admin/audit${qs(filters)}`),
  actions: () => get<string[]>('/admin/audit/actions'),
  // Fetched with auth (Bearer) as a blob so the caller can trigger a client-side download.
  exportCsv: (filters: AuditFilters = {}) =>
    get<Blob>(`/admin/audit/export.csv${qs(filters)}`, { responseType: 'blob' }),
}
