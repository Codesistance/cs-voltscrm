import { Link, useNavigate } from 'react-router-dom'
import { Plus } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { DataTable, type Column } from '@/shared/components/DataTable'
import { Money } from '@/shared/components/Money'
import { PageHeader } from '@/shared/components/PageHeader'
import { Pagination } from '@/shared/components/Pagination'
import { usePagination } from '@/shared/hooks/usePagination'
import { useAuth } from '@/features/auth/AuthContext'
import { PERMISSIONS } from '@/features/auth/permissions'
import { useInventoryList } from '../api/queries'
import type { InventoryItemListItem } from '../api/types'
import { InventoryFilters } from '../components/InventoryFilters'

export function InventoryListPage() {
  const { page, pageSize, q, getParam, setPage, setSearch, setFilter } = usePagination()
  const category = getParam('category')
  const navigate = useNavigate()
  const { hasPermission } = useAuth()

  const { data, isLoading, isError, error, refetch } = useInventoryList({
    page,
    pageSize,
    q: q || undefined,
    categoryId: category || undefined,
  })

  const columns: Column<InventoryItemListItem>[] = [
    { header: 'Code', cell: (i) => <span className="font-medium">{i.itemCode}</span> },
    { header: 'Name', cell: (i) => i.name },
    { header: 'Category', cell: (i) => i.categoryName },
    {
      header: 'Stock',
      cell: (i) =>
        i.quantityOnHand == null ? (
          <span className="text-muted-foreground">—</span>
        ) : (
          <span className="inline-flex items-center gap-2">
            <span className="tabular-nums">
              {i.quantityOnHand} {i.unitOfMeasure}
            </span>
            {i.isLowStock && <Badge variant="warning">Low</Badge>}
          </span>
        ),
    },
    {
      header: 'Unit cost',
      headClassName: 'text-right',
      className: 'text-right',
      cell: (i) => <Money value={i.unitCost} />,
    },
    {
      header: '',
      className: 'text-right',
      cell: (i) => (
        <Button asChild variant="ghost" size="sm">
          <Link to={`/admin/inventory/${i.id}`}>View</Link>
        </Button>
      ),
    },
  ]

  return (
    <div className="space-y-6">
      <PageHeader
        title="Inventory"
        description="Equipment, resale stock, spare parts and service SKUs."
        actions={
          <>
            {hasPermission(PERMISSIONS.inventoryCategoriesManage) && (
              <Button variant="outline" onClick={() => navigate('/admin/inventory/categories')}>
                Manage categories
              </Button>
            )}
            {hasPermission(PERMISSIONS.inventoryManage) && (
              <Button onClick={() => navigate('/admin/inventory/new')}>
                <Plus className="size-4" /> New item
              </Button>
            )}
          </>
        }
      />
      <InventoryFilters
        q={q}
        category={category}
        onSearch={setSearch}
        onCategory={(value) => setFilter('category', value)}
      />
      <DataTable
        columns={columns}
        data={data?.items}
        rowKey={(i) => i.id}
        isLoading={isLoading}
        isError={isError}
        error={error}
        onRetry={refetch}
        emptyTitle="No inventory items"
        emptyDescription="Create your first item to get started."
      />
      {data && <Pagination page={page} pageSize={pageSize} total={data.total} onPageChange={setPage} />}
    </div>
  )
}
