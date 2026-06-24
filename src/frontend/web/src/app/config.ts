/** App-wide runtime config. API base defaults to '/api' (served by the Vite dev proxy → backend). */
export const config = {
  apiBase: import.meta.env.VITE_API_BASE ?? '/api',
  // When true, the refresh token is carried in the request/response body (and persisted
  // client-side) instead of an httpOnly cookie — required when the SPA and API are on
  // different origins over plain HTTP (no custom domain). Set by the deploy pipeline.
  refreshInBody: import.meta.env.VITE_REFRESH_IN_BODY === 'true',
  defaultCurrency: import.meta.env.VITE_DEFAULT_CURRENCY ?? 'NGN',
  defaultLocale: import.meta.env.VITE_DEFAULT_LOCALE ?? 'en-NG',
} as const
