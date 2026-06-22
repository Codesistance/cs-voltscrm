import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { DataTable, type Column } from '@/shared/components/DataTable'
import { Money } from '@/shared/components/Money'
import { PageHeader } from '@/shared/components/PageHeader'
import { StatusPill } from '@/shared/components/StatusPill'
import { formatDate } from '@/shared/lib/format'
import { usePortalServices } from '../api/queries'
import type { PortalSubscription } from '../api/types'

export function PortalServicesPage() {
  const { data, isLoading, isError, error, refetch } = usePortalServices()
  const columns: Column<PortalSubscription>[] = [
    { header: 'Plan', cell: (s) => <span className="font-medium">{s.planName}</span> },
    { header: 'Start', cell: (s) => formatDate(s.startDate) },
    { header: 'End', cell: (s) => formatDate(s.endDate) },
    {
      header: 'Price',
      headClassName: 'text-right',
      className: 'text-right',
      cell: (s) => <Money value={s.effectivePrice} />,
    },
    { header: 'Status', cell: (s) => <StatusPill domain="subscription" status={s.status} /> },
  ]

  return (
    <div className="space-y-6">
      <PageHeader title="My services" description="Active and historic services on your account." />
      <Card>
        <CardHeader className="pb-2">
          <CardTitle className="text-base">Service subscriptions</CardTitle>
        </CardHeader>
        <CardContent>
          <DataTable
            columns={columns}
            data={data}
            rowKey={(s) => s.id}
            isLoading={isLoading}
            isError={isError}
            error={error}
            onRetry={refetch}
            emptyTitle="No services"
          />
        </CardContent>
      </Card>
    </div>
  )
}
