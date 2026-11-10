import { auth } from '@/auth'
import type { NextMiddleware } from 'next/server'

const authenticate = auth as NextMiddleware

export async function middleware(...args: Parameters<NextMiddleware>) {
  const response = await authenticate(...args)
  // A private-page prefetch can finish after sign-out.
  response?.headers.delete('set-cookie')
  return response
}

export const config = {
  matcher: ['/dashboard/:path*', '/profile/:path*', '/recipes/new', '/recipes/:id/edit'],
}
