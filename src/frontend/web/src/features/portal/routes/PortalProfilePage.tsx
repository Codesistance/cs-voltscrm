import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { ErrorState } from '@/shared/components/ErrorState'
import { LoadingState } from '@/shared/components/LoadingState'
import { PageHeader } from '@/shared/components/PageHeader'
import { StatusPill } from '@/shared/components/StatusPill'
import { useAuth } from '@/features/auth/AuthContext'
import { usePortalProfile } from '../api/queries'
import type { PortalProfile } from '../api/types'

function formatAddress(address: PortalProfile['address']) {
  return [address.street, address.city, address.region, address.country].filter(Boolean).join(', ')
}

export function PortalProfilePage() {
  const { user } = useAuth()
  const { data, isLoading, isError, error, refetch } = usePortalProfile()

  return (
    <div className="space-y-6">
      <PageHeader title="Profile" description="Your account profile details." />
      <Card className="max-w-2xl">
        <CardHeader>
          <CardTitle>Contact details</CardTitle>
          <CardDescription>Kept in sync with your customer account.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-2 text-sm">
          {isLoading && (
            <>
              <LoadingState label="Loading profile…" />
              {user && (
                <div className="space-y-2 pt-2 text-muted-foreground">
                  <p>
                    <span className="font-medium text-foreground">Name:</span> {user.fullName}
                  </p>
                  <p>
                    <span className="font-medium text-foreground">Email:</span> {user.email}
                  </p>
                </div>
              )}
            </>
          )}
          {isError && <ErrorState error={error} onRetry={refetch} />}
          {!isLoading && !isError && data && (
            <>
              <p>
                <span className="font-medium">Account number:</span> {data.accountNumber}
              </p>
              <p>
                <span className="font-medium">Name:</span> {data.fullName}
              </p>
              <p>
                <span className="font-medium">Phone:</span> {data.phone}
              </p>
              <p>
                <span className="font-medium">Email:</span> {data.email ?? '—'}
              </p>
              <p className="flex items-center gap-2">
                <span className="font-medium">Status:</span>
                <StatusPill domain="customer" status={data.status} />
              </p>
              <p>
                <span className="font-medium">Address:</span> {formatAddress(data.address)}
              </p>
            </>
          )}
          {!isLoading && !isError && !data && (
            <p className="text-muted-foreground">Profile details are not available.</p>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
