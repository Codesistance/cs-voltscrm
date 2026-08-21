import { useState } from 'react'
import { toast } from 'sonner'
import { KeyRound, Mail, Pencil, Plus, Trash2 } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { ApiError } from '@/shared/api/http'
import { ConfirmDialog } from '@/shared/components/ConfirmDialog'
import { ErrorState } from '@/shared/components/ErrorState'
import { LoadingState } from '@/shared/components/LoadingState'
import { PageHeader } from '@/shared/components/PageHeader'
import { ResetPasswordDialog } from '@/shared/components/ResetPasswordDialog'
import { LocationPicker } from '@/features/location/components/LocationPicker'
import type { LocationInput } from '@/features/location/types'
import { type Agent } from '../api/agentsApi'
import {
  useAgents,
  useCreateAgent,
  useDeactivateAgent,
  useResendInvite,
  useResetAgentPassword,
  useUpdateAgent,
} from '../api/queries'

type Editing = { mode: 'new' } | { mode: 'edit'; agent: Agent } | null

export function AgentsPage() {
  const { data: agents, isLoading, isError, error, refetch } = useAgents()
  const [editing, setEditing] = useState<Editing>(null)
  const [toDelete, setToDelete] = useState<Agent | null>(null)
  const [resetting, setResetting] = useState<Agent | null>(null)

  const createMut = useCreateAgent()
  const editId = editing?.mode === 'edit' ? editing.agent.id : ''
  const updateMut = useUpdateAgent(editId)
  const deactivateMut = useDeactivateAgent()
  const resendMut = useResendInvite()
  const resetPasswordMut = useResetAgentPassword()

  if (isLoading) return <LoadingState label="Loading agents…" />
  if (isError) return <ErrorState error={error} onRetry={refetch} />

  return (
    <div className="space-y-6">
      <PageHeader
        title="Agents"
        description="Field-servicing staff. Creating an agent emails them a set-password invite."
        actions={
          <Button onClick={() => setEditing({ mode: 'new' })}>
            <Plus className="size-4" /> New agent
          </Button>
        }
      />

      {agents && agents.length === 0 ? (
        <p className="text-sm text-muted-foreground">No agents yet. Create one to send an invite.</p>
      ) : (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {agents?.map((a) => (
            <Card key={a.id}>
              <CardHeader className="pb-3">
                <div className="flex items-center justify-between gap-2">
                  <CardTitle className="flex items-center gap-2">
                    {a.firstName} {a.lastName}
                    {!a.isActive && <Badge variant="muted">Inactive</Badge>}
                  </CardTitle>
                  <div className="flex gap-1">
                    <Button
                      variant="ghost"
                      size="icon-sm"
                      title="Resend invite"
                      disabled={resendMut.isPending}
                      onClick={async () => {
                        try {
                          await resendMut.mutateAsync(a.id)
                          toast.success('Invite re-sent.')
                        } catch (e) {
                          toast.error(e instanceof ApiError ? e.message : "Couldn't resend the invite.")
                        }
                      }}
                    >
                      <Mail className="size-4" />
                    </Button>
                    <Button variant="ghost" size="icon-sm" title="Reset password" onClick={() => setResetting(a)}>
                      <KeyRound className="size-4" />
                    </Button>
                    <Button variant="ghost" size="icon-sm" title="Edit" onClick={() => setEditing({ mode: 'edit', agent: a })}>
                      <Pencil className="size-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="icon-sm"
                      title="Deactivate"
                      disabled={!a.isActive || deactivateMut.isPending}
                      onClick={() => setToDelete(a)}
                    >
                      <Trash2 className="size-4" />
                    </Button>
                  </div>
                </div>
              </CardHeader>
              <CardContent className="space-y-1 text-xs text-muted-foreground">
                <p>{a.email}</p>
                {(a.location.address.city || a.location.address.country) && (
                  <p>Location: {[a.location.address.city, a.location.address.country].filter(Boolean).join(', ')}</p>
                )}
                {a.territory && <p>Territory: {a.territory}</p>}
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      <Dialog open={!!editing} onOpenChange={(open) => !open && setEditing(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{editing?.mode === 'edit' ? 'Edit agent' : 'New agent'}</DialogTitle>
          </DialogHeader>
          {editing && (
            <AgentForm
              agent={editing.mode === 'edit' ? editing.agent : undefined}
              saving={createMut.isPending || updateMut.isPending}
              onCancel={() => setEditing(null)}
              onSave={async (body) => {
                try {
                  if (editing.mode === 'edit') {
                    await updateMut.mutateAsync({
                      firstName: body.firstName,
                      lastName: body.lastName,
                      phone: body.phone,
                      location: body.location,
                      territory: body.territory,
                    })
                    toast.success('Agent updated.')
                  } else {
                    await createMut.mutateAsync(body)
                    toast.success('Agent created — invite sent.')
                  }
                  setEditing(null)
                } catch (e) {
                  toast.error(e instanceof ApiError ? e.message : "Couldn't save that agent.")
                }
              }}
            />
          )}
        </DialogContent>
      </Dialog>

      {resetting && (
        <ResetPasswordDialog
          open={!!resetting}
          onOpenChange={(open) => !open && setResetting(null)}
          subjectLabel={`${resetting.firstName} ${resetting.lastName}`}
          onReset={(newPassword) => resetPasswordMut.mutateAsync({ id: resetting.id, newPassword })}
        />
      )}

      <ConfirmDialog
        open={!!toDelete}
        onOpenChange={(open) => !open && setToDelete(null)}
        title="Deactivate agent?"
        description={`"${toDelete?.firstName} ${toDelete?.lastName}" will lose access.`}
        confirmText="Deactivate"
        destructive
        loading={deactivateMut.isPending}
        onConfirm={async () => {
          if (!toDelete) return
          try {
            await deactivateMut.mutateAsync(toDelete.id)
            toast.success('Agent deactivated.')
            setToDelete(null)
          } catch (e) {
            toast.error(e instanceof ApiError ? e.message : "Couldn't deactivate that agent.")
          }
        }}
      />
    </div>
  )
}

interface AgentFormValue {
  email: string
  firstName: string
  lastName: string
  phone: string | null
  location: LocationInput
  territory: string | null
}

const emptyLocation: LocationInput = {
  address: { street: '', city: '', region: '', country: '' },
  coordinates: null,
}

function AgentForm({
  agent,
  saving,
  onCancel,
  onSave,
}: {
  agent?: Agent
  saving: boolean
  onCancel: () => void
  onSave: (body: AgentFormValue) => void
}) {
  const [email, setEmail] = useState(agent?.email ?? '')
  const [firstName, setFirstName] = useState(agent?.firstName ?? '')
  const [lastName, setLastName] = useState(agent?.lastName ?? '')
  const [phone, setPhone] = useState(agent?.phone ?? '')
  const [location, setLocation] = useState<LocationInput>(agent?.location ?? emptyLocation)
  const [territory, setTerritory] = useState(agent?.territory ?? '')

  const isEdit = !!agent
  const locationValid = location.address.city.trim() !== '' && location.address.country.trim() !== ''

  return (
    <form
      className="space-y-4"
      onSubmit={(e) => {
        e.preventDefault()
        onSave({
          email: email.trim(),
          firstName: firstName.trim(),
          lastName: lastName.trim(),
          phone: phone.trim() || null,
          location,
          territory: territory.trim() || null,
        })
      }}
    >
      <div className="space-y-2">
        <Label htmlFor="agent-email">Email</Label>
        <Input
          id="agent-email"
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          disabled={isEdit}
          required
        />
      </div>
      <div className="grid grid-cols-2 gap-3">
        <div className="space-y-2">
          <Label htmlFor="agent-first">First name</Label>
          <Input id="agent-first" value={firstName} onChange={(e) => setFirstName(e.target.value)} required />
        </div>
        <div className="space-y-2">
          <Label htmlFor="agent-last">Last name</Label>
          <Input id="agent-last" value={lastName} onChange={(e) => setLastName(e.target.value)} required />
        </div>
      </div>
      <div className="space-y-2">
        <Label htmlFor="agent-phone">Phone (optional)</Label>
        <Input id="agent-phone" value={phone} onChange={(e) => setPhone(e.target.value)} />
      </div>
      <div className="space-y-2">
        <Label>Location</Label>
        <LocationPicker value={location} onChange={setLocation} disabled={saving} />
      </div>
      <div className="space-y-2">
        <Label htmlFor="agent-territory">Territory (optional)</Label>
        <Input id="agent-territory" value={territory} onChange={(e) => setTerritory(e.target.value)} />
      </div>
      <div className="flex justify-end gap-2">
        <Button type="button" variant="outline" onClick={onCancel} disabled={saving}>
          Cancel
        </Button>
        <Button
          type="submit"
          disabled={saving || !email.trim() || !firstName.trim() || !lastName.trim() || !locationValid}
        >
          {saving ? 'Saving…' : isEdit ? 'Save changes' : 'Create & invite'}
        </Button>
      </div>
    </form>
  )
}
