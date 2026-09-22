import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios'
import { getSession } from 'next-auth/react'
import type { Session } from 'next-auth'
import { ApiProblem, problemDetailsSchema } from '@/lib/api/problem-details'
import { publicEnvironment } from '@/lib/env'

type RetriedConfig = InternalAxiosRequestConfig & { retried?: boolean }
let refreshSession: (() => Promise<Session | null>) | null = null

export function configureSessionRefresh(refresh: (() => Promise<Session | null>) | null) {
  refreshSession = refresh
}

export const authenticatedAxios = axios.create({
  baseURL: publicEnvironment.NEXT_PUBLIC_API_BASE_URL,
  headers: { Accept: 'application/json' },
})

authenticatedAxios.interceptors.request.use(async (config) => {
  const session = await getSession()
  if (session?.accessToken && !config.headers.has('Authorization')) {
    config.headers.set('Authorization', 'Bearer ' + session.accessToken)
  }
  return config
})

authenticatedAxios.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const config = error.config as RetriedConfig | undefined
    if (error.response?.status === 401 && config && !config.retried && refreshSession) {
      config.retried = true
      const session = await refreshSession()
      if (session?.accessToken) {
        config.headers.set('Authorization', 'Bearer ' + session.accessToken)
        return authenticatedAxios(config)
      }
    }

    const parsed = problemDetailsSchema.safeParse(error.response?.data)
    throw new ApiProblem(
      parsed.success
        ? parsed.data
        : {
            title: error.response?.statusText || 'Request failed',
            status: error.response?.status ?? 0,
            code: 'HTTP_ERROR',
          },
    )
  },
)
