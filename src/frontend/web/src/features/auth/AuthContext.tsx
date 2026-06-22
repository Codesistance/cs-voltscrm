import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from 'react'
import { queryClient } from '@/app/queryClient'
import { tokenStore } from '@/shared/api/tokenStore'
import { authApi, type AuthUser, type UserType } from './api/authApi'

export type Role = 'Administrator' | 'Agent' | 'Customer'
export type { UserType }
type AuthStatus = 'loading' | 'authenticated' | 'unauthenticated'

interface AuthContextValue {
  user: AuthUser | null
  status: AuthStatus
  login: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
  /** Re-fetch the current user (e.g. after a forced password change clears the flag). */
  refreshUser: () => Promise<void>
  hasRole: (...roles: Role[]) => boolean
  /** True if the user holds every one of the given permission keys (Administration only). */
  hasPermission: (...permissions: string[]) => boolean
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [status, setStatus] = useState<AuthStatus>('loading')

  // Silent bootstrap: try to restore a session from the httpOnly refresh cookie on load.
  useEffect(() => {
    let active = true
    void (async () => {
      try {
        const { accessToken } = await authApi.refresh()
        tokenStore.set(accessToken)
        const me = await authApi.me()
        if (!active) return
        setUser(me)
        setStatus('authenticated')
      } catch {
        if (!active) return
        tokenStore.clear()
        setUser(null)
        setStatus('unauthenticated')
      }
    })()
    return () => {
      active = false
    }
  }, [])

  const login = useCallback(async (email: string, password: string) => {
    const res = await authApi.login(email, password)
    tokenStore.set(res.accessToken)
    setUser(res.user)
    setStatus('authenticated')
  }, [])

  const logout = useCallback(async () => {
    try {
      await authApi.logout()
    } catch {
      // ignore — clear the local session regardless
    }
    tokenStore.clear()
    queryClient.clear()
    setUser(null)
    setStatus('unauthenticated')
  }, [])

  const refreshUser = useCallback(async () => {
    const me = await authApi.me()
    setUser(me)
  }, [])

  const hasRole = useCallback(
    (...roles: Role[]) => !!user && roles.some((role) => user.roles.includes(role)),
    [user],
  )

  const hasPermission = useCallback(
    (...permissions: string[]) => !!user && permissions.every((p) => user.permissions.includes(p)),
    [user],
  )

  return (
    <AuthContext.Provider value={{ user, status, login, logout, refreshUser, hasRole, hasPermission }}>
      {children}
    </AuthContext.Provider>
  )
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
