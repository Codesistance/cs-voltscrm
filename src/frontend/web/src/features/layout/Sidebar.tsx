import { NavLink } from 'react-router-dom'
import { Zap } from 'lucide-react'
import { useAuth } from '@/features/auth/AuthContext'
import { cn } from '@/lib/utils'
import type { NavItem } from './types'

export function Sidebar({ navItems, homeTo }: { navItems: NavItem[]; homeTo: string }) {
  const { hasPermission, user } = useAuth()
  const visible = navItems.filter(
    (item) =>
      (!item.permission || hasPermission(item.permission)) &&
      (!item.superAdmin || !!user?.isSuperAdmin),
  )

  return (
    <aside className="hidden w-60 shrink-0 flex-col border-r bg-background md:flex">
      <NavLink to={homeTo} className="flex h-14 items-center gap-2 border-b px-4">
        <Zap className="size-5 text-primary" />
        <span className="font-semibold">VoltsCRM</span>
      </NavLink>
      <nav className="flex-1 space-y-1 p-3">
        {visible.map(({ label, to, icon: Icon }) => (
          <NavLink
            key={to}
            to={to}
            end
            className={({ isActive }) =>
              cn(
                'flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground',
                isActive && 'bg-accent text-accent-foreground',
              )
            }
          >
            <Icon className="size-4" />
            {label}
          </NavLink>
        ))}
      </nav>
    </aside>
  )
}
