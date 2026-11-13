import { handlers } from '@/auth'
import type { NextRequest } from 'next/server'

export async function GET(request: NextRequest) {
  const response = await handlers.GET(request)
  // Session reads can finish after logout; explicit POST updates own cookie renewal.
  if (request.nextUrl.pathname === '/api/auth/session') response.headers.delete('set-cookie')
  return response
}

export const { POST } = handlers
