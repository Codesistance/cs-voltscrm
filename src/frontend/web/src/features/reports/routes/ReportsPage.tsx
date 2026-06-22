import { useMemo, useState } from 'react'
import { Download } from 'lucide-react'
import { Bar, BarChart, CartesianGrid, Line, LineChart, XAxis, YAxis } from 'recharts'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Select } from '@/components/ui/select'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import {
  ChartContainer,
  ChartLegend,
  ChartLegendContent,
  ChartTooltip,
  ChartTooltipContent,
  type ChartConfig,
} from '@/components/ui/chart'
import { DataTable, type Column } from '@/shared/components/DataTable'
import { Money } from '@/shared/components/Money'
import { PageHeader } from '@/shared/components/PageHeader'
import { useCustomerList } from '@/features/customers/api/queries'
import { useAgingReport, useCollectionsReport, useCustomerStatement } from '../api/queries'
import type { StatementInvoice, StatementPayment } from '../api/types'

const collectionsChartConfig = {
  amount: { label: 'Amount', color: 'var(--chart-1)' },
} satisfies ChartConfig

const agingChartConfig = {
  amount: { label: 'Amount', color: 'var(--chart-2)' },
} satisfies ChartConfig

export function ReportsPage() {
  const [tab, setTab] = useState<'statement' | 'collections' | 'aging'>('collections')
  const [customerId, setCustomerId] = useState('')
  const customers = useCustomerList({ page: 1, pageSize: 200 })
  const statement = useCustomerStatement(customerId)
  const collections = useCollectionsReport()
  const aging = useAgingReport()

  const invoiceColumns: Column<StatementInvoice>[] = [
    { header: 'Due', cell: (r) => r.dueDate.slice(0, 10) },
    {
      header: 'Due amount',
      headClassName: 'text-right',
      className: 'text-right',
      cell: (r) => <Money value={r.amountDue} />,
    },
    {
      header: 'Balance',
      headClassName: 'text-right',
      className: 'text-right',
      cell: (r) => <Money value={r.balance} />,
    },
    { header: 'Status', cell: (r) => r.status },
  ]

  const paymentColumns: Column<StatementPayment>[] = [
    { header: 'Date', cell: (r) => r.paymentDate.slice(0, 10) },
    {
      header: 'Amount',
      headClassName: 'text-right',
      className: 'text-right',
      cell: (r) => <Money value={r.netAmount} />,
    },
    { header: 'Method', cell: (r) => r.method },
    { header: 'Status', cell: (r) => r.status },
  ]

  const exportRows = useMemo(() => {
    if (tab === 'statement' && statement.data) {
      return statement.data.invoices.map((row) => ({
        dueDate: row.dueDate,
        amountDue: row.amountDue.amount,
        balance: row.balance.amount,
        status: row.status,
      }))
    }
    if (tab === 'collections') {
      return (collections.data ?? []).map((row) => ({
        period: row.period,
        amount: row.amount,
        count: row.count,
      }))
    }
    return (aging.data ?? []).map((row) => ({
      bucket: row.bucket,
      amount: row.amount,
      count: row.count,
    }))
  }, [aging.data, collections.data, statement.data, tab])

  return (
    <div className="space-y-6">
      <PageHeader
        title="Reports"
        description="Statement, collections and aging reports."
        actions={
          <Button variant="outline" onClick={() => downloadCsv(`${tab}-report`, exportRows)}>
            <Download className="size-4" /> Export CSV
          </Button>
        }
      />
      <Tabs value={tab} onValueChange={(value) => setTab(value as typeof tab)}>
        <TabsList>
          <TabsTrigger value="statement">Statement</TabsTrigger>
          <TabsTrigger value="collections">Collections</TabsTrigger>
          <TabsTrigger value="aging">Aging</TabsTrigger>
        </TabsList>
        <TabsContent value="statement" className="space-y-4">
          <Select value={customerId} onChange={(e) => setCustomerId(e.target.value)} className="w-72">
            <option value="">Select customer…</option>
            {(customers.data?.items ?? []).map((c) => (
              <option key={c.id} value={c.id}>
                {c.accountNumber} · {c.fullName}
              </option>
            ))}
          </Select>
          {!customerId && <p className="text-sm text-muted-foreground">Choose a customer to view their statement.</p>}
          {statement.data && (
            <div className="space-y-4">
              <Card>
                <CardHeader>
                  <CardTitle>
                    {statement.data.customerName} · {statement.data.customerAccountNumber}
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-1 text-sm">
                  <p>
                    Total invoiced: <Money value={statement.data.totalInvoiced} />
                  </p>
                  <p>
                    Total paid: <Money value={statement.data.totalPaid} />
                  </p>
                  <p>
                    Outstanding: <Money value={statement.data.outstandingBalance} />
                  </p>
                </CardContent>
              </Card>
              <DataTable
                columns={invoiceColumns}
                data={statement.data.invoices}
                rowKey={(row) => row.id}
                isLoading={statement.isLoading}
                isError={statement.isError}
                error={statement.error}
                onRetry={statement.refetch}
                emptyTitle="No invoices on statement"
              />
              <DataTable
                columns={paymentColumns}
                data={statement.data.payments}
                rowKey={(row) => row.id}
                emptyTitle="No payments on statement"
              />
            </div>
          )}
        </TabsContent>
        <TabsContent value="collections">
          <CollectionsPanel data={collections.data ?? []} isLoading={collections.isLoading} />
        </TabsContent>
        <TabsContent value="aging">
          <AgingPanel data={aging.data ?? []} isLoading={aging.isLoading} />
        </TabsContent>
      </Tabs>
    </div>
  )
}

function CollectionsPanel({
  data,
  isLoading,
}: {
  data: { period: string; amount: number; count: number }[]
  isLoading: boolean
}) {
  if (isLoading) return <Card className="h-[340px]" />
  return (
    <Card>
      <CardHeader>
        <CardTitle>Collections trend (MTD)</CardTitle>
      </CardHeader>
      <CardContent>
        <ChartContainer config={collectionsChartConfig} className="h-[320px] w-full">
          <LineChart data={data}>
            <CartesianGrid vertical={false} />
            <XAxis dataKey="period" tickLine={false} axisLine={false} />
            <YAxis tickLine={false} axisLine={false} />
            <ChartTooltip content={<ChartTooltipContent />} />
            <Line type="monotone" dataKey="amount" stroke="var(--color-amount)" strokeWidth={2} dot={false} />
            <ChartLegend content={<ChartLegendContent />} />
          </LineChart>
        </ChartContainer>
      </CardContent>
    </Card>
  )
}

function AgingPanel({
  data,
  isLoading,
}: {
  data: { bucket: string; amount: number; count: number }[]
  isLoading: boolean
}) {
  if (isLoading) return <Card className="h-[340px]" />
  return (
    <Card>
      <CardHeader>
        <CardTitle>Aging buckets</CardTitle>
      </CardHeader>
      <CardContent>
        <ChartContainer config={agingChartConfig} className="h-[320px] w-full">
          <BarChart data={data}>
            <CartesianGrid vertical={false} />
            <XAxis dataKey="bucket" tickLine={false} axisLine={false} />
            <YAxis tickLine={false} axisLine={false} />
            <ChartTooltip content={<ChartTooltipContent />} />
            <Bar dataKey="amount" fill="var(--color-amount)" radius={6} />
          </BarChart>
        </ChartContainer>
      </CardContent>
    </Card>
  )
}

function downloadCsv(name: string, rows: Record<string, string | number>[]) {
  if (!rows.length) return
  const headers = Object.keys(rows[0])
  const lines = [
    headers.join(','),
    ...rows.map((row) =>
      headers
        .map((header) => {
          const value = String(row[header] ?? '')
          return `"${value.replaceAll('"', '""')}"`
        })
        .join(','),
    ),
  ]
  const blob = new Blob([lines.join('\n')], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `${name}-${new Date().toISOString().slice(0, 10)}.csv`
  a.click()
  URL.revokeObjectURL(url)
}
