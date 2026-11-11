import { handlers } from '@/auth'
import type { NextRequest } from 'next/server'

export async function GET(request: NextRequest) {
  const response = await handlers.GET(request)
  // A session read may finish after logout. Only explicit auth operations should renew the cookie.
  if (request.nextUrl.pathname === '/api/auth/session') response.headers.delete('set-cookie')
  return response
}

export const { POST } = handlers
