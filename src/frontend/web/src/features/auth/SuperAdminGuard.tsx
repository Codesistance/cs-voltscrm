import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from './AuthContext'

/**
 * Wraps routes that only a super admin may open. Server-side enforcement is authoritative; this
 * guard only keeps the UI honest. Redirects to /403 for anyone who isn't a super admin.
 */
export function SuperAdminGuard() {
  const { user } = useAuth()
  return user?.isSuperAdmin ? <Outlet /> : <Navigate to="/403" replace />
}
