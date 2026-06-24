import axios from 'axios'
import { config } from '@/app/config'
import { tokenStore } from './tokenStore'

export const apiClient = axios.create({
  baseURL: config.apiBase,
  headers: { 'Content-Type': 'application/json' },
  // Cookie mode sends the httpOnly refresh-token cookie automatically; body mode carries
  // the refresh token in the request body instead, so credentials aren't needed.
  withCredentials: !config.refreshInBody,
  timeout: 15_000,
})

apiClient.interceptors.request.use((req) => {
  const token = tokenStore.get()
  if (token) req.headers.Authorization = `Bearer ${token}`
  return req
})

apiClient.interceptors.response.use(
  (res) => res,
  async (error) => {
    const originalRequest = error.config
    const url: string = originalRequest?.url ?? ''
    // Don't attempt a silent refresh for the auth endpoints themselves (e.g. a 401 from /auth/login
    // is a real "bad credentials" error, not an expired access token).
    const isAuthRoute = url.includes('/auth/')

    if (error.response?.status === 401 && !originalRequest._retry && !isAuthRoute) {
      originalRequest._retry = true
      try {
        // Attempt silent refresh — via the httpOnly cookie, or the stored token in body mode.
        const { data } = await axios.post(
          `${config.apiBase}/auth/refresh`,
          config.refreshInBody ? { refreshToken: tokenStore.getRefresh() } : {},
          { withCredentials: !config.refreshInBody },
        )
        tokenStore.set(data.accessToken)
        tokenStore.setRefresh(data.refreshToken)
        originalRequest.headers.Authorization = `Bearer ${data.accessToken}`
        return apiClient(originalRequest)
      } catch {
        tokenStore.clear()
        window.location.href = '/login'
      }
    }

    return Promise.reject(error)
  }
)
