import { useSearchParams } from 'react-router-dom'

/** Keeps page / pageSize / search / arbitrary filters in the URL query string. */
export function usePagination(defaults?: { pageSize?: number }) {
  const [params, setParams] = useSearchParams()

  const page = Math.max(1, Number(params.get('page')) || 1)
  const pageSize = Number(params.get('pageSize')) || defaults?.pageSize || 20
  const q = params.get('q') ?? ''

  const update = (next: Record<string, string | number | undefined>) => {
    setParams(
      (prev) => {
        const sp = new URLSearchParams(prev)
        for (const [key, value] of Object.entries(next)) {
          if (value === undefined || value === '') sp.delete(key)
          else sp.set(key, String(value))
        }
        return sp
      },
      { replace: true },
    )
  }

  return {
    page,
    pageSize,
    q,
    getParam: (key: string) => params.get(key) ?? '',
    setPage: (p: number) => update({ page: p }),
    setSearch: (value: string) => update({ q: value || undefined, page: 1 }),
    setFilter: (key: string, value: string | undefined) => update({ [key]: value || undefined, page: 1 }),
  }
}
