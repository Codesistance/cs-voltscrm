import { AxiosError, type AxiosRequestConfig } from 'axios'
import { apiClient } from './client'
import type { ProblemDetails } from './types'

/** Normalised error every API call rejects with, so error handling is uniform across the app. */
export class ApiError extends Error {
  readonly status: number
  readonly fieldErrors?: Record<string, string[]>
  readonly traceId?: string

  constructor(
    message: string,
    opts: { status: number; fieldErrors?: Record<string, string[]>; traceId?: string },
  ) {
    super(message)
    this.name = 'ApiError'
    this.status = opts.status
    this.fieldErrors = opts.fieldErrors
    this.traceId = opts.traceId
  }
}

function toApiError(error: unknown): ApiError {
  if (error instanceof AxiosError) {
    const status = error.response?.status ?? 0
    const data = (error.response?.data ?? {}) as ProblemDetails & { error?: string }
    const fallback =
      status === 429
        ? 'Easy does it — too many tries in a row. Give it a moment and try again.'
        : status === 0
          ? "Can't reach the server right now — check your connection and try again."
          : status >= 500
            ? 'Something went wrong on our end — give it a moment and try again.'
            : error.message
    const message = data.title ?? data.detail ?? data.error ?? fallback
    return new ApiError(message, { status, fieldErrors: data.errors, traceId: data.traceId })
  }
  return new ApiError(error instanceof Error ? error.message : 'Unknown error', { status: 0 })
}

async function request<T>(config: AxiosRequestConfig): Promise<T> {
  try {
    const res = await apiClient.request<T>(config)
    return res.data
  } catch (error) {
    throw toApiError(error)
  }
}

export const get = <T>(url: string, config?: AxiosRequestConfig) =>
  request<T>({ ...config, method: 'GET', url })

export const post = <T>(url: string, data?: unknown, config?: AxiosRequestConfig) =>
  request<T>({ ...config, method: 'POST', url, data })

export const put = <T>(url: string, data?: unknown, config?: AxiosRequestConfig) =>
  request<T>({ ...config, method: 'PUT', url, data })

export const del = <T>(url: string, config?: AxiosRequestConfig) =>
  request<T>({ ...config, method: 'DELETE', url })
