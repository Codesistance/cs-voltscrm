import { useState } from 'react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Label } from '@/components/ui/label'
import type { Money as MoneyValue } from '@/shared/api/types'
import { ApiError } from '@/shared/api/http'
import { LoadingState } from '@/shared/components/LoadingState'
import { Money } from '@/shared/components/Money'
import { usePayInvoice, usePortalGateways } from '../api/queries'
import type { PortalInvoice } from '../api/types'
import { formatPortalInvoicePeriod } from '../api/types'

export function PayInvoiceDialog({
  open,
  onOpenChange,
  invoice,
  amount,
  title,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  invoice?: PortalInvoice
  amount?: MoneyValue
  title?: string
}) {
  const { data: gateways, isLoading: gatewaysLoading } = usePortalGateways()
  const payMut = usePayInvoice()
  const [selectedKey, setSelectedKey] = useState('')

  // Derived selection: the explicit choice, or auto-select when there's exactly one gateway.
  const gatewayKey = selectedKey || (gateways?.length === 1 ? gateways[0].keyName : '')

  const payAmount = invoice?.balance ?? amount
  const dialogTitle =
    title ??
    (invoice
      ? `Pay invoice ${formatPortalInvoicePeriod(invoice.periodYear, invoice.periodMonth)}`
      : 'Pay outstanding balance')

  const handleOpenChange = (next: boolean) => {
    if (!next) setSelectedKey('')
    onOpenChange(next)
  }

  const handlePay = async () => {
    if (!gatewayKey || !payAmount) return
    try {
      const result = await payMut.mutateAsync({
        invoiceId: invoice?.id,
        amount: invoice ? undefined : payAmount.amount,
        gatewayKey,
      })
      if (result.checkoutUrl) {
        window.location.href = result.checkoutUrl
        return
      }
      toast.success(result.status === 'Completed' ? 'Payment completed.' : 'Payment initiated.')
      onOpenChange(false)
    } catch (e) {
      toast.error(e instanceof ApiError ? e.message : "Couldn't process that payment.")
    }
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{dialogTitle}</DialogTitle>
        </DialogHeader>

        {payAmount && (
          <p className="text-sm text-muted-foreground">
            Amount due: <Money value={payAmount} className="font-medium text-foreground" />
          </p>
        )}

        {gatewaysLoading ? (
          <LoadingState label="Loading payment methods…" />
        ) : !gateways?.length ? (
          <p className="text-sm text-muted-foreground">No payment methods available — contact support.</p>
        ) : (
          <div className="space-y-1.5">
            <Label>Payment method</Label>
            <select
              value={gatewayKey}
              onChange={(e) => setSelectedKey(e.target.value)}
              className="h-9 w-full rounded-md border bg-background px-3 text-sm"
            >
              {gateways.length > 1 && <option value="">Select a payment method</option>}
              {gateways.map((g) => (
                <option key={g.keyName} value={g.keyName}>
                  {g.displayName}
                </option>
              ))}
            </select>
          </div>
        )}

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button disabled={payMut.isPending || !gatewayKey || !gateways?.length} onClick={handlePay}>
            {payMut.isPending ? 'Processing…' : 'Confirm payment'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
