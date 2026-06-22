import { post } from '@/shared/api/http'
import { mapImportRows } from './importRows'
import type { ImportCommitResult, ImportDryRunResult } from './types'

export const importApi = {
  dryRun: (rows: Record<string, string>[], mapping: Record<string, string>) =>
    post<ImportDryRunResult>('/import/dry-run', { rows: mapImportRows(rows, mapping) }),
  commit: (rows: Record<string, string>[], mapping: Record<string, string>) =>
    post<ImportCommitResult>('/import/commit', { rows: mapImportRows(rows, mapping), mapping }),
}
