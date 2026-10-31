import { auth } from '@/auth'
import type { NextAuthRequest } from 'next-auth'
import { NextResponse, type NextMiddleware, type NextFetchEvent, type NextRequest } from 'next/server'

const authorizeRequest: (request: NextAuthRequest, event: NextFetchEvent) => ReturnType<NextMiddleware> = (
  request,
) => {
  if (!request.auth?.backendUser) {
    const login = new URL('/auth/login', request.url)
    login.searchParams.set('returnTo', request.nextUrl.pathname + request.nextUrl.search)
    return NextResponse.redirect(login)
  }
  const roles = request.auth?.backendUser?.roles ?? []
  const path = request.nextUrl.pathname
  if (path.startsWith('/dashboard') && !roles.some((role) => role === 'Author' || role === 'Admin'))
    return new NextResponse(null, { status: 403 })
  if (/^\/dashboard\/(categories|audit|system)(\/|$)/.test(path) && !roles.includes('Admin'))
    return new NextResponse(null, { status: 403 })
  return NextResponse.next()
}
const authenticated = auth(authorizeRequest)

export async function middleware(request: NextRequest, event: NextFetchEvent) {
  // Block internal URLs before Auth.js evaluates its private-route policy.
  if (request.nextUrl.pathname === '/render-public' || request.nextUrl.pathname.startsWith('/render-public/'))
    return new NextResponse(null, { status: 404 })
  if (/^\/recipes\/[^/]+$/.test(request.nextUrl.pathname) && request.nextUrl.pathname !== '/recipes/new') {
    try {
      const api = process.env.INTERNAL_API_BASE_URL ?? 'http://localhost:5000/api/v1'
      const result = await fetch(`${api}${request.nextUrl.pathname}`, {
        redirect: 'manual',
        signal: AbortSignal.timeout(3000),
      })
      await result.body?.cancel()
      const location = result.headers.get('location')
      if (result.status === 301 && location?.startsWith('/api/v1/recipes/')) {
        return NextResponse.redirect(
          new URL(location.slice('/api/v1'.length), process.env.SITE_URL ?? request.url),
          301,
        )
      }
    } catch {
      // Cached public pages remain available when the API cannot resolve a historical slug.
    }
    return NextResponse.next()
  }
  const response = await authenticated(request, event)
  // A late private-page prefetch must not recreate a cookie after sign-out.
  // Explicit Auth.js sign-in/session endpoints own cookie renewal.
  response?.headers.delete('set-cookie')
  response?.headers.set('Cache-Control', 'private, no-store')
  return response
}

export const config = {
  runtime: 'nodejs',
  matcher: [
    '/dashboard/:path*',
    '/profile/:path*',
    '/recipes/new',
    '/recipes/:slug',
    '/recipes/:id/edit',
    '/render-public/:path*',
  ],
}
