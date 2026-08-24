import { post } from '@/shared/api/http'

export interface PhoenixResetResult {
  email: string
  /** Freshly generated temporary password — shown once; the user must change it at next login. */
  temporaryPassword: string
  /** True when a disabled account was re-activated as part of recovery. */
  reactivated: boolean
}

export const phoenixApi = {
  /** Reset the account with this email to a fresh temporary password (super-admin only). */
  reset: (email: string) => post<PhoenixResetResult>('/admin/phoenix/reset', { email }),
}
