export interface ImportRowInput {
  accountNumber: string | null
  firstName: string | null
  lastName: string | null
  phone: string | null
  email: string | null
  city: string | null
  amount: number | null
}

export interface ImportPreviewRow {
  rowNumber: number
  values: Record<string, string>
}

export interface ImportDryRunResult {
  validRows: number
  invalidRows: number
  errors: string[]
}

export interface ImportCommitResult {
  importedRows: number
  skippedRows: number
  message?: string
}
