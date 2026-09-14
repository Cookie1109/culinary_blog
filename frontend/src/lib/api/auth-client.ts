import { apiRequest } from '@/lib/api/client'
import { ApiProblem, problemDetailsSchema, readProblemDetails } from '@/lib/api/problem-details'
import { publicEnvironment } from '@/lib/env'

const REFRESH_TOKEN_KEY = 'culinary.refreshToken'

export interface AuthUser {
  id: string
  email: string
  displayName: string
  avatarUrl: string | null
  bio: string | null
  roles: ('Author' | 'Admin')[]
  emailConfirmed: boolean
  isActive: boolean
  createdAt: string
}

interface AuthSession {
  accessToken: string
  refreshToken: string
  expiresAt: string
  user: AuthUser
}

interface DataEnvelope<T> {
  data: T
}

let accessToken: string | null = null
let refreshPromise: Promise<AuthSession> | null = null

function readRefreshToken() {
  return typeof window === 'undefined' ? null : window.sessionStorage.getItem(REFRESH_TOKEN_KEY)
}

function persistSession(session: AuthSession) {
  accessToken = session.accessToken
  window.sessionStorage.setItem(REFRESH_TOKEN_KEY, session.refreshToken)
  return session
}

export function clearStoredSession() {
  accessToken = null
  if (typeof window !== 'undefined') window.sessionStorage.removeItem(REFRESH_TOKEN_KEY)
}

async function requestSession(path: string, body: unknown) {
  const response = await apiRequest<DataEnvelope<AuthSession>>(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
  return persistSession(response.data)
}

export function registerSession(displayName: string, email: string, password: string) {
  return requestSession('/auth/register', { displayName, email, password })
}

export function loginSession(email: string, password: string) {
  return requestSession('/auth/login', { email, password })
}

export async function refreshSession() {
  if (refreshPromise) return refreshPromise
  const refreshToken = readRefreshToken()
  if (!refreshToken) throw new Error('No refresh token is available.')

  refreshPromise = requestSession('/auth/refresh', { refreshToken })
    .catch((error) => {
      clearStoredSession()
      throw error
    })
    .finally(() => {
      refreshPromise = null
    })
  return refreshPromise
}

export async function logoutSession() {
  const refreshToken = readRefreshToken()
  try {
    if (refreshToken) {
      await apiRequest<void>('/auth/logout', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken }),
      })
    }
  } finally {
    clearStoredSession()
  }
}

export async function authenticatedApiRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const execute = () =>
    apiRequest<T>(path, {
      ...init,
      headers: {
        ...init?.headers,
        ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
      },
    })

  try {
    return await execute()
  } catch (error) {
    if (!(error instanceof ApiProblem) || error.problem.status !== 401 || !readRefreshToken()) {
      throw error
    }
    await refreshSession()
    return execute()
  }
}

export async function authenticatedUpload<T>(
  path: string,
  body: FormData,
  onProgress: (percent: number) => void,
): Promise<T> {
  if (!accessToken && readRefreshToken()) await refreshSession()

  const execute = () =>
    new Promise<T>((resolve, reject) => {
      const request = new XMLHttpRequest()
      request.open('POST', `${publicEnvironment.NEXT_PUBLIC_API_BASE_URL}${path}`)
      request.setRequestHeader('Accept', 'application/json')
      if (accessToken) request.setRequestHeader('Authorization', `Bearer ${accessToken}`)
      request.upload.onprogress = (event) => {
        if (event.lengthComputable) onProgress(Math.round((event.loaded / event.total) * 100))
      }
      request.onerror = () => reject(new Error('Không thể kết nối để tải ảnh lên.'))
      request.onload = () => {
        if (request.status >= 200 && request.status < 300) {
          resolve(JSON.parse(request.responseText) as T)
          return
        }

        try {
          const parsed = problemDetailsSchema.safeParse(JSON.parse(request.responseText) as unknown)
          reject(
            new ApiProblem(
              parsed.success
                ? parsed.data
                : {
                    title: request.statusText || 'Upload failed',
                    status: request.status,
                    code: 'HTTP_ERROR',
                  },
            ),
          )
        } catch {
          reject(
            new ApiProblem({
              title: request.statusText || 'Upload failed',
              status: request.status,
              code: 'HTTP_ERROR',
            }),
          )
        }
      }
      request.send(body)
    })

  try {
    return await execute()
  } catch (error) {
    if (!(error instanceof ApiProblem) || error.problem.status !== 401 || !readRefreshToken()) throw error
    await refreshSession()
    return execute()
  }
}

export async function authenticatedBlobUrl(path: string): Promise<string> {
  if (!accessToken && readRefreshToken()) await refreshSession()
  const apiPath = path.startsWith('/api/v1/') ? path.slice('/api/v1'.length) : path

  const execute = () =>
    fetch(`${publicEnvironment.NEXT_PUBLIC_API_BASE_URL}${apiPath}`, {
      headers: accessToken ? { Authorization: `Bearer ${accessToken}` } : undefined,
    })

  let response = await execute()
  if (response.status === 401 && readRefreshToken()) {
    await refreshSession()
    response = await execute()
  }
  if (!response.ok) throw await readProblemDetails(response)
  return URL.createObjectURL(await response.blob())
}

export async function updateProfile(data: {
  displayName: string
  avatarUrl?: string | null
  bio?: string | null
}) {
  const response = await authenticatedApiRequest<DataEnvelope<AuthUser>>('/auth/me', {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  })
  return response.data
}
