import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { LoadingState } from '@/shared/components/LoadingState'
import { useAuth } from './AuthContext'

export function ProtectedRoute() {
  const { status, user } = useAuth()
  const location = useLocation()

  if (status === 'loading') {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <LoadingState label="Loading VoltsCRM…" />
      </div>
    )
  }

  if (status === 'unauthenticated') {
    return <Navigate to="/login" replace state={{ from: location }} />
  }

  // Force users with a pending password change onto the change-password screen.
  if (user?.mustChangePassword && location.pathname !== '/change-password') {
    return <Navigate to="/change-password" replace />
  }

  return <Outlet />
}
