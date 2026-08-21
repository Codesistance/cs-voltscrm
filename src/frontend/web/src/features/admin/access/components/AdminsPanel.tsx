import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { KeyRound, Plus, ShieldCheck } from 'lucide-react'
import { toast } from 'sonner'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { ApiError } from '@/shared/api/http'
import { ErrorState } from '@/shared/components/ErrorState'
import { LoadingState } from '@/shared/components/LoadingState'
import { ResetPasswordDialog } from '@/shared/components/ResetPasswordDialog'
import { useAuth } from '@/features/auth/AuthContext'
import { accessApi, type AdminRoleDto, type AdminUserDto, type CreateAdminRequest } from '../api/accessApi'

export function AdminsPanel() {
  const qc = useQueryClient()
  const { user } = useAuth()
  const [editingId, setEditingId] = useState<string | null>(null)
  const [draftRoleIds, setDraftRoleIds] = useState<Set<string>>(new Set())
  const [creating, setCreating] = useState(false)
  const [resetting, setResetting] = useState<AdminUserDto | null>(null)
  const resetPasswordMut = useMutation({
    mutationFn: ({ id, newPassword }: { id: string; newPassword?: string }) => accessApi.resetPassword(id, newPassword),
  })

  const adminsQuery = useQuery({ queryKey: ['access', 'admins'], queryFn: accessApi.admins })
  const rolesQuery = useQuery({ queryKey: ['access', 'roles'], queryFn: accessApi.roles })

  const assignMutation = useMutation({
    mutationFn: ({ id, roleIds }: { id: string; roleIds: string[] }) => accessApi.assignRoles(id, roleIds),
    onSuccess: () => {
      toast.success('Saved — their roles are up to date.')
      setEditingId(null)
      void qc.invalidateQueries({ queryKey: ['access'] })
    },
    onError: (e) => toast.error(e instanceof ApiError ? e.message : "Couldn't save those role changes — give it another go."),
  })

  const createMutation = useMutation({
    mutationFn: (body: CreateAdminRequest) => accessApi.createAdmin(body),
    onSuccess: () => {
      toast.success('Administrator created — invite sent.')
      setCreating(false)
      void qc.invalidateQueries({ queryKey: ['access'] })
    },
    onError: (e) => toast.error(e instanceof ApiError ? e.message : "Couldn't create that administrator — give it another go."),
  })

  if (adminsQuery.isPending || rolesQuery.isPending) return <LoadingState label="Loading admins…" />
  if (adminsQuery.isError) return <ErrorState error={adminsQuery.error} onRetry={() => adminsQuery.refetch()} />
  if (rolesQuery.isError) return <ErrorState error={rolesQuery.error} onRetry={() => rolesQuery.refetch()} />

  const roleById = new Map(rolesQuery.data.map((r) => [r.id, r]))

  const startEdit = (admin: AdminUserDto) => {
    setEditingId(admin.id)
    setDraftRoleIds(new Set(admin.roleIds))
  }

  const toggle = (roleId: string) =>
    setDraftRoleIds((prev) => {
      const next = new Set(prev)
      if (next.has(roleId)) next.delete(roleId)
      else next.add(roleId)
      return next
    })

  return (
    <div className="space-y-4">
      <div className="flex justify-end">
        <Button size="sm" onClick={() => setCreating(true)}>
          <Plus className="size-4" /> New administrator
        </Button>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
      {adminsQuery.data.map((admin) => {
        const editing = editingId === admin.id
        return (
          <Card key={admin.id}>
            <CardHeader className="pb-3">
              <div className="flex items-center justify-between gap-2">
                <CardTitle className="flex items-center gap-2">
                  {admin.fullName || admin.email}
                  {admin.isSuperAdmin && (
                    <Badge variant="info" className="gap-1">
                      <ShieldCheck className="size-3" /> Super
                    </Badge>
                  )}
                </CardTitle>
                {!editing && (
                  <div className="flex gap-1">
                    <Button variant="ghost" size="icon-sm" title="Reset password" onClick={() => setResetting(admin)}>
                      <KeyRound className="size-4" />
                    </Button>
                    <Button variant="outline" size="sm" onClick={() => startEdit(admin)}>
                      Edit roles
                    </Button>
                  </div>
                )}
              </div>
              <CardDescription>{admin.email}</CardDescription>
            </CardHeader>
            <CardContent className="space-y-3">
              {admin.isSuperAdmin && (
                <p className="text-xs text-muted-foreground">
                  Super admins always hold every permission, regardless of assigned roles.
                </p>
              )}

              {editing ? (
                <div className="space-y-3">
                  <div className="space-y-2">
                    {rolesQuery.data.map((role: AdminRoleDto) => (
                      <label key={role.id} className="flex items-center gap-2 text-sm">
                        <input
                          type="checkbox"
                          className="size-4 rounded border-input"
                          checked={draftRoleIds.has(role.id)}
                          onChange={() => toggle(role.id)}
                        />
                        {role.name}
                      </label>
                    ))}
                  </div>
                  <div className="flex justify-end gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setEditingId(null)}
                      disabled={assignMutation.isPending}
                    >
                      Cancel
                    </Button>
                    <Button
                      size="sm"
                      disabled={assignMutation.isPending}
                      onClick={() => assignMutation.mutate({ id: admin.id, roleIds: [...draftRoleIds] })}
                    >
                      {assignMutation.isPending ? 'Saving…' : 'Save'}
                    </Button>
                  </div>
                </div>
              ) : (
                <div className="flex flex-wrap gap-1">
                  {admin.roleIds.length === 0 ? (
                    <span className="text-xs text-muted-foreground">No roles assigned</span>
                  ) : (
                    admin.roleIds.map((id) => (
                      <Badge key={id} variant="secondary">
                        {roleById.get(id)?.name ?? 'Unknown'}
                      </Badge>
                    ))
                  )}
                </div>
              )}
            </CardContent>
          </Card>
        )
      })}
      </div>

      <Dialog open={creating} onOpenChange={(open) => !open && setCreating(false)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>New administrator</DialogTitle>
          </DialogHeader>
          <CreateAdminForm
            roles={rolesQuery.data}
            canGrantSuper={user?.isSuperAdmin ?? false}
            saving={createMutation.isPending}
            onCancel={() => setCreating(false)}
            onSave={(body) => createMutation.mutate(body)}
          />
        </DialogContent>
      </Dialog>

      {resetting && (
        <ResetPasswordDialog
          open={!!resetting}
          onOpenChange={(open) => !open && setResetting(null)}
          subjectLabel={resetting.fullName || resetting.email}
          onReset={(newPassword) => resetPasswordMut.mutateAsync({ id: resetting.id, newPassword })}
        />
      )}
    </div>
  )
}

function CreateAdminForm({
  roles,
  canGrantSuper,
  saving,
  onCancel,
  onSave,
}: {
  roles: AdminRoleDto[]
  canGrantSuper: boolean
  saving: boolean
  onCancel: () => void
  onSave: (body: CreateAdminRequest) => void
}) {
  const [email, setEmail] = useState('')
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [roleIds, setRoleIds] = useState<Set<string>>(new Set())
  const [isSuperAdmin, setIsSuperAdmin] = useState(false)

  const toggle = (id: string) =>
    setRoleIds((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })

  // System roles (e.g. Super Administrator) are assigned via the super-admin flag, not as a role.
  const assignableRoles = roles.filter((r) => !r.isSystem)

  return (
    <form
      className="space-y-4"
      onSubmit={(e) => {
        e.preventDefault()
        onSave({
          email: email.trim(),
          firstName: firstName.trim(),
          lastName: lastName.trim(),
          roleIds: [...roleIds],
          isSuperAdmin,
        })
      }}
    >
      <div className="space-y-2">
        <Label htmlFor="admin-email">Email</Label>
        <Input id="admin-email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
      </div>
      <div className="grid grid-cols-2 gap-3">
        <div className="space-y-2">
          <Label htmlFor="admin-first">First name</Label>
          <Input id="admin-first" value={firstName} onChange={(e) => setFirstName(e.target.value)} required />
        </div>
        <div className="space-y-2">
          <Label htmlFor="admin-last">Last name</Label>
          <Input id="admin-last" value={lastName} onChange={(e) => setLastName(e.target.value)} required />
        </div>
      </div>
      <div className="space-y-2">
        <Label>Roles</Label>
        {assignableRoles.length === 0 ? (
          <p className="text-xs text-muted-foreground">No roles yet — create one in the Roles tab.</p>
        ) : (
          <div className="space-y-1">
            {assignableRoles.map((role) => (
              <label key={role.id} className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  className="size-4 rounded border-input"
                  checked={roleIds.has(role.id)}
                  disabled={isSuperAdmin}
                  onChange={() => toggle(role.id)}
                />
                {role.name}
              </label>
            ))}
          </div>
        )}
      </div>
      {canGrantSuper && (
        <label className="flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            className="size-4 rounded border-input"
            checked={isSuperAdmin}
            onChange={(e) => setIsSuperAdmin(e.target.checked)}
          />
          Super administrator (holds every permission)
        </label>
      )}
      <div className="flex justify-end gap-2">
        <Button type="button" variant="outline" onClick={onCancel} disabled={saving}>
          Cancel
        </Button>
        <Button type="submit" disabled={saving || !email.trim() || !firstName.trim() || !lastName.trim()}>
          {saving ? 'Saving…' : 'Create & invite'}
        </Button>
      </div>
    </form>
  )
}
