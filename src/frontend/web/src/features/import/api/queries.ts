import { useMutation } from '@tanstack/react-query'
import { importApi } from './importApi'

export function useImportDryRun() {
  return useMutation({
    mutationFn: ({
      rows,
      mapping,
    }: {
      rows: Record<string, string>[]
      mapping: Record<string, string>
    }) => importApi.dryRun(rows, mapping),
  })
}

export function useImportCommit() {
  return useMutation({
    mutationFn: ({ rows, mapping }: { rows: Record<string, string>[]; mapping: Record<string, string> }) =>
      importApi.commit(rows, mapping),
  })
}
