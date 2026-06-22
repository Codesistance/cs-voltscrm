import { get, post } from '@/shared/api/http'

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
}

interface RefreshResponse {
  accessToken: string
  expiresIn: number
}

export const authApi = {
  login: (email: string, password: string) => post<LoginResponse>('/auth/login', { email, password }),
  refresh: () => post<RefreshResponse>('/auth/refresh', {}),
  logout: () => post<void>('/auth/logout', {}),
  me: () => get<AuthUser>('/auth/me'),
  setPassword: (email: string, token: string, newPassword: string) =>
    post<void>('/auth/set-password', { email, token, newPassword }),
  changePassword: (currentPassword: string, newPassword: string) =>
    post<void>('/auth/change-password', { currentPassword, newPassword }),
}
