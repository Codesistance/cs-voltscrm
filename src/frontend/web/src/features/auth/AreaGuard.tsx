import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from './AuthContext'
import type { UserType } from './api/authApi'
import { homePathFor } from './areas'

/**
 * Gates a whole UI area to a single user type. A user landing in the wrong area is bounced to their
 * own home area (full separation between Customer / Agent / Administration). Assumes the user is
 * already authenticated (nested under <ProtectedRoute>).
 */
export function AreaGuard({ area }: { area: UserType }) {
  const { user } = useAuth()
  if (!user) return <Navigate to="/login" replace />
  return user.userType === area ? <Outlet /> : <Navigate to={homePathFor(user.userType)} replace />
}
