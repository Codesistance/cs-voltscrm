import { useState } from 'react'
import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { Download } from 'lucide-react'
import { toast } from 'sonner'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { ApiError } from '@/shared/api/http'
import { ErrorState } from '@/shared/components/ErrorState'
import { LoadingState } from '@/shared/components/LoadingState'
import { PageHeader } from '@/shared/components/PageHeader'
import { auditApi, type AuditFilters } from '../api/auditApi'

const PAGE_SIZE = 50

interface Draft {
  action: string
  actorEmail: string
  outcome: string
  from: string
  to: string
}

const emptyDraft: Draft = { action: '', actorEmail: '', outcome: '', from: '', to: '' }

/** Read-only view of the security audit trail. Super admins only (route + API enforced). */
export function AuditLogPage() {
  const [draft, setDraft] = useState<Draft>(emptyDraft)
  const [applied, setApplied] = useState<Draft>(emptyDraft)
  const [page, setPage] = useState(1)
  const [exporting, setExporting] = useState(false)

  const filters: AuditFilters = {
    action: applied.action || undefined,
    actorEmail: applied.actorEmail || undefined,
    outcome: applied.outcome || undefined,
    from: applied.from ? new Date(applied.from).toISOString() : undefined,
    to: applied.to ? new Date(applied.to).toISOString() : undefined,
    page,
    pageSize: PAGE_SIZE,
  }

  const { data: actions } = useQuery({ queryKey: ['audit-actions'], queryFn: auditApi.actions })
  const { data, isLoading, isError, error, refetch, isFetching } = useQuery({
    queryKey: ['audit', filters],
    queryFn: () => auditApi.list(filters),
    placeholderData: keepPreviousData,
  })

  const apply = () => {
    setApplied(draft)
    setPage(1)
  }
  const clear = () => {
    setDraft(emptyDraft)
    setApplied(emptyDraft)
    setPage(1)
  }

  const exportCsv = async () => {
    setExporting(true)
    try {
      const blob = await auditApi.exportCsv({ ...filters, page: undefined, pageSize: undefined })
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = 'audit-log.csv'
      a.click()
      URL.revokeObjectURL(url)
    } catch (e) {
      toast.error(e instanceof ApiError ? e.message : "Couldn't export the audit log.")
    } finally {
      setExporting(false)
    }
  }

  const total = data?.total ?? 0
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))

  return (
    <div className="space-y-6">
      <PageHeader
        title="Audit Log"
        description="Append-only record of sensitive admin and auth actions. Super admins only."
        actions={
          <Button variant="outline" onClick={exportCsv} disabled={exporting}>
            <Download className="size-4" /> {exporting ? 'Exporting…' : 'Export CSV'}
          </Button>
        }
      />

      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
        <div className="space-y-1">
          <Label htmlFor="f-action">Action</Label>
          <select
            id="f-action"
            className="h-9 w-full rounded-md border bg-background px-3 text-sm"
            value={draft.action}
            onChange={(e) => setDraft({ ...draft, action: e.target.value })}
          >
            <option value="">All actions</option>
            {actions?.map((a) => (
              <option key={a} value={a}>
                {a}
              </option>
            ))}
          </select>
        </div>
        <div className="space-y-1">
          <Label htmlFor="f-outcome">Outcome</Label>
          <select
            id="f-outcome"
            className="h-9 w-full rounded-md border bg-background px-3 text-sm"
            value={draft.outcome}
            onChange={(e) => setDraft({ ...draft, outcome: e.target.value })}
          >
            <option value="">Any</option>
            <option value="success">Success</option>
            <option value="failure">Failure</option>
          </select>
        </div>
        <div className="space-y-1">
          <Label htmlFor="f-actor">Actor email</Label>
          <Input
            id="f-actor"
            placeholder="contains…"
            value={draft.actorEmail}
            onChange={(e) => setDraft({ ...draft, actorEmail: e.target.value })}
          />
        </div>
        <div className="space-y-1">
          <Label htmlFor="f-from">From</Label>
          <Input
            id="f-from"
            type="date"
            value={draft.from}
            onChange={(e) => setDraft({ ...draft, from: e.target.value })}
          />
        </div>
        <div className="space-y-1">
          <Label htmlFor="f-to">To</Label>
          <Input
            id="f-to"
            type="date"
            value={draft.to}
            onChange={(e) => setDraft({ ...draft, to: e.target.value })}
          />
        </div>
      </div>
      <div className="flex gap-2">
        <Button onClick={apply}>Apply filters</Button>
        <Button variant="outline" onClick={clear}>
          Clear
        </Button>
      </div>

      {isLoading ? (
        <LoadingState label="Loading audit log…" />
      ) : isError ? (
        <ErrorState error={error} onRetry={refetch} />
      ) : (
        <>
          <div className="overflow-x-auto rounded-md border">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>When (UTC)</TableHead>
                  <TableHead>Action</TableHead>
                  <TableHead>Outcome</TableHead>
                  <TableHead>Actor</TableHead>
                  <TableHead>Target</TableHead>
                  <TableHead>IP</TableHead>
                  <TableHead>Details</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data && data.items.length > 0 ? (
                  data.items.map((e) => (
                    <TableRow key={e.id}>
                      <TableCell className="whitespace-nowrap font-mono text-xs">
                        {new Date(e.occurredAt).toISOString().replace('T', ' ').slice(0, 19)}
                      </TableCell>
                      <TableCell className="whitespace-nowrap font-medium">{e.action}</TableCell>
                      <TableCell>
                        <Badge variant={e.outcome === 'success' ? 'secondary' : 'destructive'}>{e.outcome}</Badge>
                      </TableCell>
                      <TableCell className="text-xs">{e.actorEmail ?? e.actorUserId ?? '—'}</TableCell>
                      <TableCell className="text-xs">{e.targetLabel ?? e.targetId ?? '—'}</TableCell>
                      <TableCell className="whitespace-nowrap font-mono text-xs">{e.ipAddress ?? '—'}</TableCell>
                      <TableCell className="max-w-xs truncate font-mono text-xs" title={e.details ?? ''}>
                        {e.details ?? '—'}
                      </TableCell>
                    </TableRow>
                  ))
                ) : (
                  <TableRow>
                    <TableCell colSpan={7} className="py-8 text-center text-sm text-muted-foreground">
                      No audit events match these filters.
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </div>

          <div className="flex items-center justify-between text-sm text-muted-foreground">
            <span>
              {total} event{total === 1 ? '' : 's'}
              {isFetching && ' · updating…'}
            </span>
            <div className="flex items-center gap-2">
              <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
                Previous
              </Button>
              <span>
                Page {page} of {totalPages}
              </span>
              <Button
                variant="outline"
                size="sm"
                disabled={page >= totalPages}
                onClick={() => setPage((p) => p + 1)}
              >
                Next
              </Button>
            </div>
          </div>
        </>
      )}
    </div>
  )
}
