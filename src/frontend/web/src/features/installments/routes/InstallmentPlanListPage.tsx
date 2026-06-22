import { Link, useNavigate } from 'react-router-dom'
import { Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { useAuth } from '@/features/auth/AuthContext'
import { PERMISSIONS } from '@/features/auth/permissions'
import { DataTable, type Column } from '@/shared/components/DataTable'
import { Money } from '@/shared/components/Money'
import { PageHeader } from '@/shared/components/PageHeader'
import { Pagination } from '@/shared/components/Pagination'
import { formatDate } from '@/shared/lib/format'
import { usePagination } from '@/shared/hooks/usePagination'
import { useInstallmentPlanList } from '../api/queries'
import type { InstallmentPlanListItem } from '../api/types'

export function InstallmentPlanListPage() {
  const { page, pageSize, setPage } = usePagination()
  const canManage = useAuth().hasPermission(PERMISSIONS.invoicesManage)
  const navigate = useNavigate()
  const { data, isLoading, isError, error, refetch } = useInstallmentPlanList({ page, pageSize })

  const columns: Column<InstallmentPlanListItem>[] = [
    {
      header: 'Customer',
      cell: (i) => (
        <Link to={`/admin/customers/${i.customerId}`} className="font-medium hover:underline">
          {i.customerName}
        </Link>
      ),
    },
    { header: 'Account', cell: (i) => i.customerAccountNumber },
    {
      header: 'Total',
      headClassName: 'text-right',
      className: 'text-right',
      cell: (i) => <Money value={i.totalAmount} />,
    },
    {
      header: 'Remaining',
      headClassName: 'text-right',
      className: 'text-right',
      cell: (i) => <Money value={i.remainingAmount} />,
    },
    { header: 'Installments', cell: (i) => i.installmentCount },
    { header: 'Start', cell: (i) => formatDate(i.startDate) },
    {
      header: '',
      className: 'text-right',
      cell: (i) => (
        <Button asChild variant="ghost" size="sm">
          <Link to={`/admin/installments/${i.id}`}>View</Link>
        </Button>
      ),
    },
  ]

  return (
    <div className="space-y-6">
      <PageHeader
        title="Installment plans"
        description="Monitor installment schedules and aging balances."
        actions={
          canManage ? (
            <Button onClick={() => navigate('/admin/installments/new')}>
              <Plus className="size-4" /> New plan
            </Button>
          ) : null
        }
      />
      <DataTable
        columns={columns}
        data={data?.items}
        rowKey={(i) => i.id}
        isLoading={isLoading}
        isError={isError}
        error={error}
        onRetry={refetch}
        emptyTitle="No installment plans"
        emptyDescription="No installment plans have been created yet."
      />
      {data && <Pagination page={page} pageSize={pageSize} total={data.total} onPageChange={setPage} />}
    </div>
  )
}
