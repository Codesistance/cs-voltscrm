/** Shared API types used across every feature's resource module. */

export type Id = string

/** Money normalised at the API boundary from either the value-object or flattened server shape. */
export interface Money {
  amount: number
  currency: string
}

export interface ListParams {
  page?: number
  pageSize?: number
  sort?: string
  q?: string
  [key: string]: unknown
}

/** Standard list envelope returned by every paged endpoint. */
export interface Paginated<T> {
  items: T[]
  page: number
  pageSize: number
  total: number
}

/** ASP.NET RFC7807 ProblemDetails / ValidationProblemDetails. */
export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  errors?: Record<string, string[]>
  traceId?: string
}
