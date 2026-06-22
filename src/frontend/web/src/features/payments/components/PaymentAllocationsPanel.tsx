import { Link } from 'react-router-dom'
import { DataTable, type Column } from '@/shared/components/DataTable'
import { Money } from '@/shared/components/Money'
import { config } from '@/app/config'
import type { PaymentAllocation } from '../api/types'

interface Props {
  allocations: PaymentAllocation[]
}

export function PaymentAllocationsPanel({ allocations }: Props) {
  const columns: Column<PaymentAllocation>[] = [
    {
      header: 'Target',
      cell: (a) => {
        if (a.invoiceId) {
          return (
            <Link to={`/admin/invoices/${a.invoiceId}`} className="font-medium hover:underline">
              Invoice
            </Link>
          )
        }
        if (a.installmentId) {
          return <span className="font-medium">Installment {a.installmentId.slice(0, 8)}…</span>
        }
        return '—'
      },
    },
    {
      header: 'Amount',
      headClassName: 'text-right',
      className: 'text-right',
      cell: (a) => <Money value={{ amount: a.amount, currency: config.defaultCurrency }} />,
    },
  ]

  return (
    <div className="space-y-3">
      <h2 className="text-lg font-semibold">Allocations</h2>
      <DataTable
        columns={columns}
        data={allocations}
        rowKey={(a) => a.id}
        emptyTitle="No allocations"
        emptyDescription="Allocations will be available once invoices and installment plans exist."
      />
    </div>
  )
}
