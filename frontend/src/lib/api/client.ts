import { publicEnvironment } from '@/lib/env'
import { readProblemDetails } from '@/lib/api/problem-details'

export async function apiRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${publicEnvironment.NEXT_PUBLIC_API_BASE_URL}${path}`, {
    ...init,
    headers: {
      Accept: 'application/json',
      ...init?.headers,
    },
  })

  if (!response.ok) {
    throw await readProblemDetails(response)
  }

  return (await response.json()) as T
}

export type { paths as ApiPaths, operations as ApiOperations } from './schema'
