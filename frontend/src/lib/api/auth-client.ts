import { authenticatedAxios } from '@/lib/api/axios'
import type { AuthUser, BackendSession } from '@/lib/api/auth-types'
import { publicEnvironment } from '@/lib/env'

export type { AuthUser } from '@/lib/api/auth-types'

async function exchangeSession(path: string, body: unknown): Promise<BackendSession> {
  const response = await fetch(`${publicEnvironment.NEXT_PUBLIC_API_BASE_URL}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
    cache: 'no-store',
  })
  if (!response.ok) throw new Error('Authentication failed.')
  return ((await response.json()) as { data: BackendSession }).data
}

export function loginWithPassword(email: string, password: string) {
  return exchangeSession('/auth/login', { email, password })
}

export function registerWithPassword(displayName: string, email: string, password: string) {
  return exchangeSession('/auth/register', { displayName, email, password })
}

export function rotateBackendSession(refreshToken: string) {
  return exchangeSession('/auth/refresh', { refreshToken })
}

export async function revokeBackendSession(refreshToken: string) {
  await fetch(`${publicEnvironment.NEXT_PUBLIC_API_BASE_URL}/auth/logout`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken }),
    cache: 'no-store',
  })
}

export async function authenticatedApiRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers)
  const response = await authenticatedAxios.request<T>({
    url: path,
    method: init?.method ?? 'GET',
    headers: Object.fromEntries(headers.entries()),
    data: init?.body,
  })
  return response.data
}

export async function authenticatedUpload<T>(
  path: string,
  body: FormData,
  onProgress: (percent: number) => void,
  version: number,
): Promise<T> {
  const response = await authenticatedAxios.post<T>(path, body, {
    headers: { 'If-Match': '"' + version + '"' },
    onUploadProgress: (event) => {
      if (event.total) onProgress(Math.round((event.loaded / event.total) * 100))
    },
  })
  return response.data
}

export async function authenticatedBlobUrl(path: string): Promise<string> {
  const apiPath = path.startsWith('/api/v1/') ? path.slice('/api/v1'.length) : path
  const response = await authenticatedAxios.get<Blob>(apiPath, { responseType: 'blob' })
  return URL.createObjectURL(response.data)
}

export async function updateProfile(data: {
  displayName: string
  avatarUrl?: string | null
  bio?: string | null
}) {
  const response = await authenticatedApiRequest<{ data: AuthUser }>('/auth/me', {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  })
  return response.data
}
