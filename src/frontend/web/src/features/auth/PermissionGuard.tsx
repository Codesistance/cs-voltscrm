import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from './AuthContext'

/**
 * Wraps admin routes that require one or more permissions. Server-side enforcement is authoritative;
 * this guard only keeps the UI honest. Redirects to /403 when the user lacks any required permission.
 */
export function PermissionGuard({ allow }: { allow: string[] }) {
  const { hasPermission } = useAuth()
  return hasPermission(...allow) ? <Outlet /> : <Navigate to="/403" replace />
}
