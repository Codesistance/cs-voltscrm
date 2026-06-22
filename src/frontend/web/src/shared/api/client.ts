import axios from 'axios'
import { tokenStore } from './tokenStore'

export const apiClient = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
  withCredentials: true, // sends httpOnly refresh-token cookie automatically
  timeout: 15_000,
})

apiClient.interceptors.request.use((config) => {
  const token = tokenStore.get()
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
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
        // Attempt silent refresh using the httpOnly cookie
        const { data } = await axios.post('/api/auth/refresh', {}, { withCredentials: true })
        tokenStore.set(data.accessToken)
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
