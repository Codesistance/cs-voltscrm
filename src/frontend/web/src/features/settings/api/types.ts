export interface PaymentGatewayConfig {
  keyName: string
  displayName: string
  visibility: boolean
  implemented: boolean
  data: Record<string, string>
}

export interface UpsertPaymentGatewayConfig {
  displayName: string
  visibility: boolean
  data: Record<string, string>
}

export interface AutoDebitSettings {
  provider: string
  retryDays: number
  enabled: boolean
}

export interface UpdateAutoDebitSettings {
  provider: string
  retryDays: number
  enabled: boolean
}

export interface TokenVendingSettings {
  provider: string
  apiKey: string
  active: boolean
}

export interface UpdateTokenVendingSettings {
  provider: string
  /** Omit or leave blank to keep the stored key. */
  apiKey?: string | null
  active: boolean
}
