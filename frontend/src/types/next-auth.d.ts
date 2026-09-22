import type { BackendSession, AuthUser } from '@/lib/api/auth-types'

declare module 'next-auth' {
  interface User {
    backendSession?: BackendSession
  }

  interface Session {
    accessToken?: string
    backendUser?: AuthUser
    error?: 'RefreshTokenError'
  }
}

declare module '@auth/core/jwt' {
  interface JWT {
    backend?: BackendSession
    error?: 'RefreshTokenError'
  }
}
