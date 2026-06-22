import { useState } from 'react'
import { cn } from '@/lib/utils'
import { AdminsPanel } from '../components/AdminsPanel'
import { RolesPanel } from '../components/RolesPanel'

type Tab = 'roles' | 'admins'

export function AccessPage() {
  const [tab, setTab] = useState<Tab>('roles')

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Access Control</h1>
        <p className="text-sm text-muted-foreground">
          Manage admin roles, their permissions, and which roles each administrator holds.
        </p>
      </div>

      <div className="flex gap-1 border-b" role="tablist">
        {(['roles', 'admins'] as const).map((t) => (
          <button
            key={t}
            type="button"
            role="tab"
            aria-selected={tab === t}
            onClick={() => setTab(t)}
            className={cn(
              '-mb-px border-b-2 px-4 py-2 text-sm font-medium capitalize transition-colors',
              tab === t
                ? 'border-primary text-foreground'
                : 'border-transparent text-muted-foreground hover:text-foreground',
            )}
          >
            {t === 'roles' ? 'Roles' : 'Administrators'}
          </button>
        ))}
      </div>

      {tab === 'roles' ? <RolesPanel /> : <AdminsPanel />}
    </div>
  )
}
