import type { FieldValues, Path, UseFormSetError } from 'react-hook-form'
import { ApiError } from '@/shared/api/http'

/** Convert a server property path (PascalCase, dotted) to its camelCase form path. */
function toFormPath(serverPath: string): string {
  return serverPath
    .split('.')
    .map((segment) => segment.charAt(0).toLowerCase() + segment.slice(1))
    .join('.')
}

/**
 * Maps an ApiError's field errors (from ValidationProblemDetails) onto a react-hook-form.
 * Returns true if any field errors were applied (so the caller can skip a generic toast).
 */
export function applyServerErrors<T extends FieldValues>(
  error: unknown,
  setError: UseFormSetError<T>,
): boolean {
  if (!(error instanceof ApiError) || !error.fieldErrors) return false

  let applied = false
  for (const [key, messages] of Object.entries(error.fieldErrors)) {
    if (!messages?.length) continue
    setError(toFormPath(key) as Path<T>, { type: 'server', message: messages[0] })
    applied = true
  }
  return applied
}
