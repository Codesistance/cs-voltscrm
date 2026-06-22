import { useMemo, useState } from 'react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import type { AdminRoleDto, PermissionDto, SaveAdminRoleRequest } from '../api/accessApi'

interface Props {
  permissions: PermissionDto[]
  role?: AdminRoleDto
  saving: boolean
  onSave: (body: SaveAdminRoleRequest) => void
  onCancel: () => void
}

export function RoleEditor({ permissions, role, saving, onSave, onCancel }: Props) {
  const [name, setName] = useState(role?.name ?? '')
  const [description, setDescription] = useState(role?.description ?? '')
  const [selected, setSelected] = useState<Set<string>>(new Set(role?.permissions ?? []))

  const groups = useMemo(() => {
    const byGroup = new Map<string, PermissionDto[]>()
    for (const p of permissions) {
      const list = byGroup.get(p.group) ?? []
      list.push(p)
      byGroup.set(p.group, list)
    }
    return [...byGroup.entries()]
  }, [permissions])

  const toggle = (key: string) =>
    setSelected((prev) => {
      const next = new Set(prev)
      if (next.has(key)) next.delete(key)
      else next.add(key)
      return next
    })

  const submit = (e: React.FormEvent) => {
    e.preventDefault()
    onSave({ name: name.trim(), description: description.trim() || null, permissions: [...selected] })
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>{role ? `Edit role: ${role.name}` : 'New role'}</CardTitle>
      </CardHeader>
      <CardContent>
        <form onSubmit={submit} className="space-y-6">
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="role-name">Name</Label>
              <Input
                id="role-name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                required
                autoFocus
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="role-desc">Description</Label>
              <Input id="role-desc" value={description} onChange={(e) => setDescription(e.target.value)} />
            </div>
          </div>

          <fieldset className="space-y-4">
            <legend className="text-sm font-medium">Permissions</legend>
            {groups.map(([group, perms]) => (
              <div key={group} className="space-y-2">
                <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">{group}</p>
                <div className="grid gap-2 sm:grid-cols-2">
                  {perms.map((p) => (
                    <label key={p.key} className="flex items-start gap-2 text-sm">
                      <input
                        type="checkbox"
                        className="mt-0.5 size-4 rounded border-input"
                        checked={selected.has(p.key)}
                        onChange={() => toggle(p.key)}
                      />
                      <span>
                        {p.description}
                        <span className="block text-xs text-muted-foreground">{p.key}</span>
                      </span>
                    </label>
                  ))}
                </div>
              </div>
            ))}
          </fieldset>

          <div className="flex justify-end gap-2">
            <Button type="button" variant="outline" onClick={onCancel} disabled={saving}>
              Cancel
            </Button>
            <Button type="submit" disabled={saving || !name.trim()}>
              {saving ? 'Saving…' : 'Save role'}
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  )
}
