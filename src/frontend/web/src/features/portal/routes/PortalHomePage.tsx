import { useState } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { ErrorState } from '@/shared/components/ErrorState'
import { LoadingState } from '@/shared/components/LoadingState'
import { Money } from '@/shared/components/Money'
import { useAuth } from '@/features/auth/AuthContext'
import { PayInvoiceDialog } from '../components/PayInvoiceDialog'
import { usePortalSummary } from '../api/queries'
import { hasOutstandingBalance } from '../api/types'

export function PortalHomePage() {
  const { user } = useAuth()
  const firstName = user?.fullName?.split(' ')[0] ?? 'there'
  const { data, isLoading, isError, error, refetch } = usePortalSummary()
  const [payOpen, setPayOpen] = useState(false)

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Welcome, {firstName}</h1>
        <p className="text-sm text-muted-foreground">Your account at a glance.</p>
      </div>
      {isLoading && <LoadingState label="Loading account summary…" />}
      {isError && <ErrorState error={error} onRetry={refetch} />}
      {data && (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <Card>
            <CardHeader className="pb-2">
              <CardDescription>Outstanding balance</CardDescription>
              <CardTitle className="text-2xl">
                <Money value={data.outstandingBalance} />
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              <p className="text-xs text-muted-foreground">Open invoice balances</p>
              {hasOutstandingBalance(data.outstandingBalance) && (
                <Button size="sm" onClick={() => setPayOpen(true)}>
                  Pay
                </Button>
              )}
            </CardContent>
          </Card>
          <Card>
            <CardHeader className="pb-2">
              <CardDescription>Pending invoices</CardDescription>
              <CardTitle className="text-2xl">{data.pendingInvoices.toLocaleString()}</CardTitle>
            </CardHeader>
            <CardContent className="text-xs text-muted-foreground">Awaiting payment</CardContent>
          </Card>
          <Card>
            <CardHeader className="pb-2">
              <CardDescription>Active services</CardDescription>
              <CardTitle className="text-2xl">{data.activeSubscriptions.toLocaleString()}</CardTitle>
            </CardHeader>
            <CardContent className="text-xs text-muted-foreground">Subscriptions currently active</CardContent>
          </Card>
          <Card>
            <CardHeader className="pb-2">
              <CardDescription>Paid this month</CardDescription>
              <CardTitle className="text-2xl">
                <Money value={data.paidThisMonth} />
              </CardTitle>
            </CardHeader>
            <CardContent className="text-xs text-muted-foreground">Completed payments MTD</CardContent>
          </Card>
        </div>
      )}

      {data && (
        <PayInvoiceDialog
          open={payOpen}
          onOpenChange={setPayOpen}
          amount={data.outstandingBalance}
        />
      )}
    </div>
  )
}
