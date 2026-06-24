import { config } from '@/app/config'
import { get, post } from '@/shared/api/http'
import { tokenStore } from '@/shared/api/tokenStore'

export type UserType = 'Customer' | 'Agent' | 'Administration'

export interface AuthUser {
  id: string
  email: string
  fullName: string
  userType: UserType
  roles: string[]
  /** Granted permission keys (Administration users only). */
  permissions: string[]
  /** When true, the user must set a new password before using the app. */
  mustChangePassword: boolean
  /** True for super admins (implicitly hold every permission). */
  isSuperAdmin: boolean
}

interface LoginResponse {
  accessToken: string
  expiresIn: number
  user: AuthUser
  /** Present only in cookie-less mode (config.refreshInBody). */
  refreshToken?: string
}

interface RefreshResponse {
  accessToken: string
  expiresIn: number
  /** Present only in cookie-less mode (config.refreshInBody). */
  refreshToken?: string
}

// In cookie-less mode the refresh token rides in the body; otherwise the server reads
// the httpOnly cookie and the body stays empty.
const refreshBody = () => (config.refreshInBody ? { refreshToken: tokenStore.getRefresh() } : {})

export const authApi = {
  login: (email: string, password: string) => post<LoginResponse>('/auth/login', { email, password }),
  refresh: () => post<RefreshResponse>('/auth/refresh', refreshBody()),
  logout: () => post<void>('/auth/logout', refreshBody()),
  me: () => get<AuthUser>('/auth/me'),
  setPassword: (email: string, token: string, newPassword: string) =>
    post<void>('/auth/set-password', { email, token, newPassword }),
  changePassword: (currentPassword: string, newPassword: string) =>
    post<void>('/auth/change-password', { currentPassword, newPassword }),
}
