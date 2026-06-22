import { useMemo, useState } from 'react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { PageHeader } from '@/shared/components/PageHeader'
import type { ImportDryRunResult } from '../api/types'
import { useImportCommit, useImportDryRun } from '../api/queries'

type Step = 1 | 2 | 3 | 4

export function ImportWizardPage() {
  const [step, setStep] = useState<Step>(1)
  const [fileName, setFileName] = useState('')
  const [headers, setHeaders] = useState<string[]>([])
  const [rows, setRows] = useState<Record<string, string>[]>([])
  const [mapping, setMapping] = useState<Record<string, string>>({})
  const [dryRunResult, setDryRunResult] = useState<ImportDryRunResult | null>(null)
  const [commitMessage, setCommitMessage] = useState('')
  const dryRunMut = useImportDryRun()
  const commitMut = useImportCommit()

  const mappedHeaders = useMemo(() => Object.values(mapping).filter(Boolean), [mapping])
  const canCommit = rows.length > 0 && headers.length > 0 && mappedHeaders.length > 0

  const onFileChange = async (file: File | null) => {
    if (!file) return
    const text = await file.text()
    const parsed = parseCsv(text)
    setFileName(file.name)
    setHeaders(parsed.headers)
    setRows(parsed.rows)
    setMapping(
      parsed.headers.reduce<Record<string, string>>((acc, header) => {
        acc[header] = normalizeHeader(header)
        return acc
      }, {}),
    )
    setStep(2)
    setDryRunResult(null)
    setCommitMessage('')
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Import wizard"
        description="Upload CSV files, map columns, preview records and commit."
      />

      <Card>
        <CardHeader>
          <CardTitle>Step {step} of 4</CardTitle>
          <CardDescription>{stepLabel(step)}</CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          {step === 1 && (
            <div className="space-y-3">
              <Input type="file" accept=".csv,text/csv" onChange={(e) => onFileChange(e.target.files?.[0] ?? null)} />
              <p className="text-sm text-muted-foreground">Select a CSV to start the import flow.</p>
            </div>
          )}

          {step === 2 && (
            <div className="space-y-4">
              <p className="text-sm text-muted-foreground">File: {fileName}</p>
              <div className="grid gap-3 md:grid-cols-2">
                {headers.map((header) => (
                  <div key={header} className="space-y-1">
                    <p className="text-sm font-medium">{header}</p>
                    <Select
                      value={mapping[header] ?? ''}
                      onChange={(e) => setMapping((prev) => ({ ...prev, [header]: e.target.value }))}
                    >
                      <option value="">Ignore column</option>
                      <option value="accountNumber">accountNumber</option>
                      <option value="firstName">firstName</option>
                      <option value="lastName">lastName</option>
                      <option value="phone">phone</option>
                      <option value="email">email</option>
                      <option value="city">city</option>
                      <option value="amount">amount</option>
                    </Select>
                  </div>
                ))}
              </div>
              <Button onClick={() => setStep(3)}>Continue to preview</Button>
            </div>
          )}

          {step === 3 && (
            <div className="space-y-4">
              <Table>
                <TableHeader>
                  <TableRow>
                    {headers.map((header) => (
                      <TableHead key={header}>{header}</TableHead>
                    ))}
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {rows.slice(0, 10).map((row, index) => (
                    <TableRow key={index}>
                      {headers.map((header) => (
                        <TableCell key={`${index}-${header}`}>{row[header] ?? ''}</TableCell>
                      ))}
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  disabled={dryRunMut.isPending || !canCommit}
                  onClick={async () => {
                    const result = await dryRunMut.mutateAsync({ rows, mapping })
                    setDryRunResult(result)
                    toast.success(`Dry run complete: ${result.validRows} valid, ${result.invalidRows} invalid.`)
                    setStep(4)
                  }}
                >
                  {dryRunMut.isPending ? 'Running dry run…' : 'Run dry run'}
                </Button>
                <Button onClick={() => setStep(4)} disabled={!canCommit}>
                  Skip to commit
                </Button>
              </div>
            </div>
          )}

          {step === 4 && (
            <div className="space-y-4">
              <p className="text-sm text-muted-foreground">
                Ready to commit {rows.length} rows with {mappedHeaders.length} mapped columns.
              </p>

              {dryRunResult && (
                <div className="rounded-md border bg-muted/40 p-3 text-sm space-y-2">
                  <p>
                    Dry run: <strong>{dryRunResult.validRows}</strong> valid,{' '}
                    <strong>{dryRunResult.invalidRows}</strong> invalid
                  </p>
                  {dryRunResult.errors.length > 0 && (
                    <ul className="list-disc pl-5 text-destructive">
                      {dryRunResult.errors.slice(0, 20).map((err, index) => (
                        <li key={index}>{err}</li>
                      ))}
                      {dryRunResult.errors.length > 20 && (
                        <li>…and {dryRunResult.errors.length - 20} more</li>
                      )}
                    </ul>
                  )}
                </div>
              )}

              {commitMessage && (
                <p className="rounded-md border bg-muted/40 p-3 text-sm">{commitMessage}</p>
              )}

              <div className="flex gap-2">
                <Button
                  disabled={!canCommit || commitMut.isPending}
                  onClick={async () => {
                    const result = await commitMut.mutateAsync({ rows, mapping })
                    setCommitMessage(
                      result.message ??
                        `Imported ${result.importedRows} row(s), skipped ${result.skippedRows}.`,
                    )
                    toast.success(result.message ?? `Imported ${result.importedRows} row(s).`)
                  }}
                >
                  {commitMut.isPending ? 'Committing…' : 'Commit import'}
                </Button>
                <Button
                  variant="outline"
                  onClick={() => {
                    setStep(1)
                    setHeaders([])
                    setRows([])
                    setMapping({})
                    setFileName('')
                    setDryRunResult(null)
                    setCommitMessage('')
                  }}
                >
                  Start over
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}

function stepLabel(step: Step): string {
  if (step === 1) return 'Upload CSV'
  if (step === 2) return 'Map columns'
  if (step === 3) return 'Preview rows'
  return 'Commit import'
}

function parseCsv(text: string): { headers: string[]; rows: Record<string, string>[] } {
  const lines = text
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter(Boolean)
  if (!lines.length) return { headers: [], rows: [] }

  const headers = parseCsvLine(lines[0])
  const rows = lines.slice(1).map((line) => {
    const values = parseCsvLine(line)
    return headers.reduce<Record<string, string>>((acc, header, index) => {
      acc[header] = values[index] ?? ''
      return acc
    }, {})
  })
  return { headers, rows }
}

function parseCsvLine(line: string): string[] {
  const values: string[] = []
  let current = ''
  let inQuotes = false
  for (let i = 0; i < line.length; i += 1) {
    const char = line[i]
    if (char === '"' && line[i + 1] === '"') {
      current += '"'
      i += 1
      continue
    }
    if (char === '"') {
      inQuotes = !inQuotes
      continue
    }
    if (char === ',' && !inQuotes) {
      values.push(current)
      current = ''
      continue
    }
    current += char
  }
  values.push(current)
  return values.map((value) => value.trim())
}

function normalizeHeader(header: string): string {
  const normalized = header.toLowerCase().replaceAll(/[^a-z0-9]+/g, '')
  if (normalized.includes('account')) return 'accountNumber'
  if (normalized.includes('firstname')) return 'firstName'
  if (normalized.includes('lastname')) return 'lastName'
  if (normalized.includes('phone')) return 'phone'
  if (normalized.includes('email')) return 'email'
  if (normalized.includes('city')) return 'city'
  if (normalized.includes('amount')) return 'amount'
  return ''
}
