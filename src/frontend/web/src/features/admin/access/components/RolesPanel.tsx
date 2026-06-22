import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Lock, Pencil, Plus, Trash2 } from 'lucide-react'
import { toast } from 'sonner'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { ApiError } from '@/shared/api/http'
import { ErrorState } from '@/shared/components/ErrorState'
import { LoadingState } from '@/shared/components/LoadingState'
import { accessApi, type AdminRoleDto, type SaveAdminRoleRequest } from '../api/accessApi'
import { RoleEditor } from './RoleEditor'

type Editing = { mode: 'new' } | { mode: 'edit'; role: AdminRoleDto } | null

export function RolesPanel() {
  const qc = useQueryClient()
  const [editing, setEditing] = useState<Editing>(null)

  const rolesQuery = useQuery({ queryKey: ['access', 'roles'], queryFn: accessApi.roles })
  const permsQuery = useQuery({ queryKey: ['access', 'permissions'], queryFn: accessApi.permissions })

  const invalidate = () => qc.invalidateQueries({ queryKey: ['access'] })

  const createMutation = useMutation({
    mutationFn: (body: SaveAdminRoleRequest) => accessApi.createRole(body),
    onSuccess: () => {
      toast.success('New role created.')
      setEditing(null)
      void invalidate()
    },
    onError: (e) => toast.error(e instanceof ApiError ? e.message : "Couldn't create that role — give it another go."),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, body }: { id: string; body: SaveAdminRoleRequest }) => accessApi.updateRole(id, body),
    onSuccess: () => {
      toast.success("Saved — the role's up to date.")
      setEditing(null)
      void invalidate()
    },
    onError: (e) => toast.error(e instanceof ApiError ? e.message : "Couldn't save those role changes — give it another go."),
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => accessApi.deleteRole(id),
    onSuccess: () => {
      toast.success('Role removed.')
      void invalidate()
    },
    onError: (e) => toast.error(e instanceof ApiError ? e.message : "Couldn't remove that role — give it another go."),
  })

  if (rolesQuery.isPending || permsQuery.isPending) return <LoadingState label="Loading roles…" />
  if (rolesQuery.isError) return <ErrorState error={rolesQuery.error} onRetry={() => rolesQuery.refetch()} />
  if (permsQuery.isError) return <ErrorState error={permsQuery.error} onRetry={() => permsQuery.refetch()} />

  const saving = createMutation.isPending || updateMutation.isPending

  if (editing) {
    return (
      <RoleEditor
        permissions={permsQuery.data}
        role={editing.mode === 'edit' ? editing.role : undefined}
        saving={saving}
        onCancel={() => setEditing(null)}
        onSave={(body) =>
          editing.mode === 'edit'
            ? updateMutation.mutate({ id: editing.role.id, body })
            : createMutation.mutate(body)
        }
      />
    )
  }

  return (
    <div className="space-y-4">
      <div className="flex justify-end">
        <Button size="sm" onClick={() => setEditing({ mode: 'new' })}>
          <Plus className="size-4" /> New role
        </Button>
      </div>
      <div className="grid gap-4 md:grid-cols-2">
        {rolesQuery.data.map((role) => (
          <Card key={role.id}>
            <CardHeader className="pb-3">
              <div className="flex items-center justify-between gap-2">
                <CardTitle className="flex items-center gap-2">
                  {role.name}
                  {role.isSystem && <Lock className="size-3.5 text-muted-foreground" />}
                </CardTitle>
                <div className="flex gap-1">
                  <Button
                    variant="ghost"
                    size="icon-sm"
                    title={role.isSystem ? 'System roles cannot be edited' : 'Edit role'}
                    disabled={role.isSystem}
                    onClick={() => setEditing({ mode: 'edit', role })}
                  >
                    <Pencil className="size-4" />
                  </Button>
                  <Button
                    variant="ghost"
                    size="icon-sm"
                    title={role.isSystem ? 'System roles cannot be deleted' : 'Delete role'}
                    disabled={role.isSystem || deleteMutation.isPending}
                    onClick={() => {
                      if (confirm(`Delete role "${role.name}"? This cannot be undone.`)) deleteMutation.mutate(role.id)
                    }}
                  >
                    <Trash2 className="size-4" />
                  </Button>
                </div>
              </div>
              {role.description && <CardDescription>{role.description}</CardDescription>}
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex flex-wrap gap-1">
                {role.permissions.length === 0 ? (
                  <span className="text-xs text-muted-foreground">No permissions</span>
                ) : (
                  role.permissions.map((p) => (
                    <Badge key={p} variant="muted">
                      {p}
                    </Badge>
                  ))
                )}
              </div>
              <p className="text-xs text-muted-foreground">
                {role.userCount} {role.userCount === 1 ? 'admin' : 'admins'} assigned
              </p>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  )
}
