import { get, post } from '@/shared/api/http'
import type {
  AvailableGateway,
  InitiatePaymentResult,
  PortalInvoicesPage,
  PortalPaymentsPage,
  PortalProfile,
  PortalSubscriptionsPage,
  PortalSummary,
} from './types'

const BASE = '/portal/me'

export const portalApi = {
  summary: () => get<PortalSummary>(`${BASE}/summary`),
  subscriptions: (page = 1, pageSize = 20) =>
    get<PortalSubscriptionsPage>(`${BASE}/subscriptions`, { params: { page, pageSize } }),
  invoices: (page = 1, pageSize = 20) =>
    get<PortalInvoicesPage>(`${BASE}/invoices`, { params: { page, pageSize } }),
  payments: (page = 1, pageSize = 20) =>
    get<PortalPaymentsPage>(`${BASE}/payments`, { params: { page, pageSize } }),
  profile: () => get<PortalProfile>(`${BASE}/profile`),
  gateways: () => get<AvailableGateway[]>(`${BASE}/gateways`),
  pay: (body: { invoiceId?: string; amount?: number; gatewayKey: string }) =>
    post<InitiatePaymentResult>(`${BASE}/payments`, body),
}
