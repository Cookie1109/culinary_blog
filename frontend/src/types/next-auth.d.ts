import type { BackendAccessSession, AuthUser } from '@/lib/api/auth-types'

declare module 'next-auth' {
  interface User {
    backendSession?: BackendAccessSession
  }

  interface Session {
    accessToken?: string
    accessTokenExpiresAt?: string
    backendUser?: AuthUser
  }
}

declare module '@auth/core/jwt' {
  interface JWT {
    backend?: BackendAccessSession
  }
}
