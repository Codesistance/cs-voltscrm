import { get, put } from '@/shared/api/http'
import type {
  AutoDebitSettings,
  PaymentGatewayConfig,
  TokenVendingSettings,
  UpdateAutoDebitSettings,
  UpsertPaymentGatewayConfig,
  UpdateTokenVendingSettings,
} from './types'

const BASE = '/settings'

export const settingsApi = {
  paymentGateways: {
    list: () => get<PaymentGatewayConfig[]>(`${BASE}/payment-gateways`),
    upsert: (keyName: string, body: UpsertPaymentGatewayConfig) =>
      put<PaymentGatewayConfig>(`${BASE}/payment-gateways/${keyName}`, body),
    setVisibility: (keyName: string, visible: boolean) =>
      put<PaymentGatewayConfig>(`${BASE}/payment-gateways/${keyName}/visibility`, { visible }),
  },
  autoDebit: {
    get: () => get<AutoDebitSettings | null>(`${BASE}/auto-debit`),
    save: (body: UpdateAutoDebitSettings) => put<AutoDebitSettings>(`${BASE}/auto-debit`, body),
  },
  tokenVending: {
    get: () => get<TokenVendingSettings | null>(`${BASE}/token-vending`),
    save: (body: UpdateTokenVendingSettings) => put<TokenVendingSettings>(`${BASE}/token-vending`, body),
  },
}
