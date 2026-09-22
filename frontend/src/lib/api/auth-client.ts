import { authenticatedAxios } from '@/lib/api/axios'
import type { AuthUser } from '@/lib/api/auth-types'

export type { AuthUser } from '@/lib/api/auth-types'

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
