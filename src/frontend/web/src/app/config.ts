/** App-wide runtime config. API base defaults to '/api' (served by the Vite dev proxy → backend). */
export const config = {
  apiBase: import.meta.env.VITE_API_BASE ?? '/api',
  defaultCurrency: import.meta.env.VITE_DEFAULT_CURRENCY ?? 'NGN',
  defaultLocale: import.meta.env.VITE_DEFAULT_LOCALE ?? 'en-NG',
} as const
