import { useState } from 'react'
import { Pencil } from 'lucide-react'
import { toast } from 'sonner'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { ApiError } from '@/shared/api/http'
import { ErrorState } from '@/shared/components/ErrorState'
import { LoadingState } from '@/shared/components/LoadingState'
import { PageHeader } from '@/shared/components/PageHeader'
import {
  usePaymentGateways,
  useSetPaymentGatewayVisibility,
  useUpsertPaymentGateway,
} from '../api/queries'
import type { PaymentGatewayConfig } from '../api/types'

const MASKED = '••••••••'

function isSecretField(key: string): boolean {
  const lower = key.toLowerCase()
  return ['secret', 'key', 'password', 'token'].some((part) => lower.includes(part))
}

export function PaymentGatewaySettingsPage() {
  const { data: gateways, isLoading, isError, error, refetch } = usePaymentGateways()
  const visibilityMut = useSetPaymentGatewayVisibility()
  const [editing, setEditing] = useState<PaymentGatewayConfig | null>(null)

  if (isLoading) return <LoadingState label="Loading payment gateways…" />
  if (isError) return <ErrorState error={error} onRetry={refetch} />

  return (
    <div className="space-y-6">
      <PageHeader
        title="Payment gateways"
        description="Manage payment provider registry entries and customer-facing visibility."
      />

      {gateways && gateways.length === 0 ? (
        <p className="text-sm text-muted-foreground">No payment gateways configured.</p>
      ) : (
        <div className="grid gap-4 md:grid-cols-2">
          {gateways?.map((gateway) => {
            const visibilityDisabled = !gateway.implemented && !gateway.visibility

            return (
              <Card key={gateway.keyName}>
                <CardHeader className="pb-3">
                  <div className="flex items-start justify-between gap-3">
                    <div className="min-w-0 space-y-1">
                      <CardTitle className="truncate">{gateway.displayName}</CardTitle>
                      <p className="font-mono text-xs text-muted-foreground">{gateway.keyName}</p>
                    </div>
                    {gateway.implemented ? (
                      <Badge variant="success">Implemented</Badge>
                    ) : (
                      <Badge variant="muted">Not implemented</Badge>
                    )}
                  </div>
                </CardHeader>
                <CardContent className="flex items-center justify-between gap-4">
                  <VisibilityToggle
                    checked={gateway.visibility}
                    disabled={visibilityMut.isPending || visibilityDisabled}
                    tooltip={
                      visibilityDisabled ? 'No adapter implemented for this gateway' : undefined
                    }
                    onCheckedChange={async (visible) => {
                      try {
                        await visibilityMut.mutateAsync({ keyName: gateway.keyName, visible })
                        toast.success(visible ? 'Gateway is now visible to customers.' : 'Gateway hidden from customers.')
                      } catch (e) {
                        toast.error(e instanceof ApiError ? e.message : "Couldn't update gateway visibility.")
                      }
                    }}
                  />
                  <Button variant="outline" size="sm" onClick={() => setEditing(gateway)}>
                    <Pencil className="size-4" />
                    Edit
                  </Button>
                </CardContent>
              </Card>
            )
          })}
        </div>
      )}

      {editing && (
        <EditGatewayDialog key={editing.keyName} gateway={editing} onClose={() => setEditing(null)} />
      )}
    </div>
  )
}

function VisibilityToggle({
  checked,
  disabled,
  tooltip,
  onCheckedChange,
}: {
  checked: boolean
  disabled?: boolean
  tooltip?: string
  onCheckedChange: (checked: boolean) => void
}) {
  const control = (
    <label className={`flex items-center gap-2 text-sm ${disabled ? 'cursor-not-allowed opacity-60' : 'cursor-pointer'}`}>
      <input
        type="checkbox"
        checked={checked}
        disabled={disabled}
        onChange={(e) => onCheckedChange(e.target.checked)}
        className="size-4 rounded border"
      />
      Visible to customers
    </label>
  )

  if (tooltip && disabled) {
    return (
      <Tooltip>
        <TooltipTrigger asChild>
          <span tabIndex={0}>{control}</span>
        </TooltipTrigger>
        <TooltipContent>{tooltip}</TooltipContent>
      </Tooltip>
    )
  }

  return control
}

function EditGatewayDialog({
  gateway,
  onClose,
}: {
  gateway: PaymentGatewayConfig
  onClose: () => void
}) {
  const upsertMut = useUpsertPaymentGateway()
  // Seeded once on mount; the parent passes key={gateway.keyName}, so switching gateways
  // remounts this dialog with fresh state rather than syncing via an effect.
  const [displayName, setDisplayName] = useState(gateway.displayName)
  const [data, setData] = useState<Record<string, string>>({ ...gateway.data })

  const dataKeys = Object.keys(data).sort()

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle>Edit {gateway.displayName}</DialogTitle>
          <CardDescription className="text-left">
            Secret values are masked — leave unchanged to keep the stored value.
          </CardDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="space-y-1.5">
            <Label>Display name</Label>
            <Input value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
          </div>

          {dataKeys.length === 0 ? (
            <p className="text-sm text-muted-foreground">No configuration fields for this gateway.</p>
          ) : (
            dataKeys.map((key) => (
              <div key={key} className="space-y-1.5">
                <Label>{key}</Label>
                <Input
                  value={data[key] ?? ''}
                  type={isSecretField(key) ? 'password' : 'text'}
                  placeholder={data[key] === MASKED ? 'Stored value is masked — leave unchanged to keep' : undefined}
                  onChange={(e) => setData((prev) => ({ ...prev, [key]: e.target.value }))}
                />
              </div>
            ))
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onClose}>
            Cancel
          </Button>
          <Button
            disabled={upsertMut.isPending || !displayName.trim()}
            onClick={async () => {
              try {
                await upsertMut.mutateAsync({
                  keyName: gateway.keyName,
                  body: {
                    displayName: displayName.trim(),
                    visibility: gateway.visibility,
                    data,
                  },
                })
                toast.success('Gateway settings saved.')
                onClose()
              } catch (e) {
                toast.error(e instanceof ApiError ? e.message : "Couldn't save gateway settings.")
              }
            }}
          >
            {upsertMut.isPending ? 'Saving…' : 'Save'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
