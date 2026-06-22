import { Navigate } from 'react-router-dom'
import { useAuth } from './AuthContext'
import { homePathFor } from './areas'

/** Root landing: sends each authenticated user to the home of their own area. */
export function AreaRedirect() {
  const { user } = useAuth()
  return <Navigate to={homePathFor(user?.userType)} replace />
}
