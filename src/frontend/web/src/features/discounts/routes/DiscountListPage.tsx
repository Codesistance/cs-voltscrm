import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { ConfirmDialog } from '@/shared/components/ConfirmDialog'
import { DataTable, type Column } from '@/shared/components/DataTable'
import { PageHeader } from '@/shared/components/PageHeader'
import { Pagination } from '@/shared/components/Pagination'
import { StatusPill } from '@/shared/components/StatusPill'
import { formatDateTime } from '@/shared/lib/format'
import { usePagination } from '@/shared/hooks/usePagination'
import { useAuth } from '@/features/auth/AuthContext'
import { PERMISSIONS } from '@/features/auth/permissions'
import { useDiscountList, useRevokeDiscount } from '../api/queries'
import type { DiscountGrant } from '../api/types'
import { DiscountFilters } from '../components/DiscountFilters'

export function DiscountListPage() {
  const { page, pageSize, getParam, setPage, setFilter } = usePagination()
  const status = getParam('status')
  const customerId = getParam('customerId')
  const navigate = useNavigate()
  const canManage = useAuth().hasPermission(PERMISSIONS.discountsManage)
  const revokeMut = useRevokeDiscount()
  const [revokeTarget, setRevokeTarget] = useState<DiscountGrant | null>(null)

  const { data, isLoading, isError, error, refetch } = useDiscountList({
    page,
    pageSize,
    status: status || undefined,
    customerId: customerId || undefined,
  })

  const columns: Column<DiscountGrant>[] = [
    {
      header: 'Customer',
      cell: (d) => (
        <Link to={`/admin/customers/${d.customerId}`} className="font-medium hover:underline">
          {d.customerName || `${d.customerId.slice(0, 8)}…`}
        </Link>
      ),
    },
    { header: 'Type', cell: (d) => d.discountType },
    {
      header: 'Value',
      cell: (d) => (d.discountType === 'Percentage' ? `${d.value}%` : d.value.toFixed(2)),
    },
    { header: 'Scope', cell: (d) => d.scope },
    { header: 'Reason', cell: (d) => d.reason ?? '—' },
    { header: 'Granted', cell: (d) => formatDateTime(d.grantedAt) },
    { header: 'Status', cell: (d) => <StatusPill domain="discount" status={d.status} /> },
    {
      header: '',
      className: 'text-right',
      cell: (d) =>
        canManage && d.status === 'Active' ? (
          <Button variant="ghost" size="sm" onClick={() => setRevokeTarget(d)}>
            Revoke
          </Button>
        ) : null,
    },
  ]

  return (
    <div className="space-y-6">
      <PageHeader
        title="Discounts"
        description="Grant and revoke customer discount credits."
        actions={
          canManage ? (
            <Button onClick={() => navigate('/admin/discounts/new')}>
              <Plus className="size-4" /> Grant discount
            </Button>
          ) : null
        }
      />
      <DiscountFilters
        status={status}
        customerId={customerId}
        onStatus={(value) => setFilter('status', value)}
        onCustomer={(value) => setFilter('customerId', value)}
      />
      <DataTable
        columns={columns}
        data={data?.items}
        rowKey={(d) => d.id}
        isLoading={isLoading}
        isError={isError}
        error={error}
        onRetry={refetch}
        emptyTitle="No discounts"
        emptyDescription="Grant a discount to create your first record."
      />
      {data && <Pagination page={page} pageSize={pageSize} total={data.total} onPageChange={setPage} />}

      <ConfirmDialog
        open={revokeTarget !== null}
        onOpenChange={(open) => !open && setRevokeTarget(null)}
        title="Revoke discount?"
        description="This discount grant will be revoked and can no longer be applied."
        confirmText="Revoke"
        destructive
        loading={revokeMut.isPending}
        onConfirm={async () => {
          if (!revokeTarget) return
          await revokeMut.mutateAsync(revokeTarget.id)
          toast.success('Discount revoked.')
          setRevokeTarget(null)
        }}
      />
    </div>
  )
}
