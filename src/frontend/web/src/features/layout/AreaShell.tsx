import { Outlet } from 'react-router-dom'
import { Sidebar } from './Sidebar'
import { Topbar } from './Topbar'
import type { NavItem } from './types'

/** Shared chrome for a user-type area (Admin / Field / Portal). Each area supplies its own nav. */
export function AreaShell({ navItems, homeTo }: { navItems: NavItem[]; homeTo: string }) {
  return (
    <div className="flex min-h-screen bg-muted/30">
      <Sidebar navItems={navItems} homeTo={homeTo} />
      <div className="flex min-w-0 flex-1 flex-col">
        <Topbar />
        <main className="flex-1 p-6">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
