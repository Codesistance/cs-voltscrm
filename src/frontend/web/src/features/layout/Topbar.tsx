import { LogOut } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { useAuth } from '@/features/auth/AuthContext'
import type { UserType } from '@/features/auth/api/authApi'

const USER_TYPE_LABEL: Record<UserType, string> = {
  Administration: 'Administrator',
  Agent: 'Agent',
  Customer: 'Customer',
}

export function Topbar() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  const handleLogout = async () => {
    await logout()
    navigate('/login', { replace: true })
  }

  return (
    <header className="flex h-14 items-center justify-between border-b bg-background px-6">
      <div className="font-semibold md:hidden">VoltsCRM</div>
      <div className="ml-auto flex items-center gap-3">
        <div className="text-right">
          <p className="text-sm font-medium leading-none">{user?.fullName}</p>
          <p className="text-xs text-muted-foreground">
            {user ? USER_TYPE_LABEL[user.userType] : ''}
          </p>
        </div>
        <Button variant="ghost" size="icon-sm" onClick={handleLogout} title="Sign out">
          <LogOut className="size-4" />
        </Button>
      </div>
    </header>
  )
}
