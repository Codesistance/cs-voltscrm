import { config } from '@/app/config'

// Access token lives in memory only — never touches localStorage (XSS safe).
//
// The refresh token is normally stored server-side in an httpOnly SameSite=Strict cookie.
// In cookie-less mode (config.refreshInBody — used when the SPA and API are on different
// origins over plain HTTP and a Secure cookie can't be carried) it is persisted in
// localStorage so the session survives a reload. This is an accepted XSS tradeoff for the
// no-custom-domain bring-up only; the secure cookie path keeps the refresh token out of JS.
const REFRESH_KEY = 'voltscrm.refreshToken'

let accessToken: string | null = null

export const tokenStore = {
  get: (): string | null => accessToken,
  set: (token: string): void => {
    accessToken = token
  },
  clear: (): void => {
    accessToken = null
    if (config.refreshInBody) localStorage.removeItem(REFRESH_KEY)
  },

  // Refresh-token helpers — only used in cookie-less mode.
  getRefresh: (): string | null => (config.refreshInBody ? localStorage.getItem(REFRESH_KEY) : null),
  setRefresh: (token: string | null | undefined): void => {
    if (!config.refreshInBody) return
    if (token) localStorage.setItem(REFRESH_KEY, token)
    else localStorage.removeItem(REFRESH_KEY)
  },
}
