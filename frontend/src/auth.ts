import NextAuth from 'next-auth'
import Credentials from 'next-auth/providers/credentials'
import Google from 'next-auth/providers/google'
import type { NextAuthConfig } from 'next-auth'
import { z } from 'zod'
import type { BackendSession } from '@/lib/api/auth-types'

const credentialsSchema = z.object({
  email: z.email(),
  password: z.string().min(1),
  mode: z.enum(['login', 'register']).default('login'),
  displayName: z.string().optional(),
})

const backendUrl = process.env.INTERNAL_API_BASE_URL ?? 'http://localhost:5000/api/v1'
const refreshes = new Map<string, Promise<BackendSession>>()

async function exchange(path: string, body: unknown): Promise<BackendSession> {
  const response = await fetch(backendUrl + path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
    cache: 'no-store',
  })
  if (!response.ok) throw new Error('Backend authentication failed.')
  const envelope = (await response.json()) as { data: BackendSession }
  return envelope.data
}

async function refresh(session: BackendSession): Promise<BackendSession> {
  const existing = refreshes.get(session.refreshToken)
  if (existing) return existing
  const pending = exchange('/auth/refresh', { refreshToken: session.refreshToken })
  refreshes.set(session.refreshToken, pending)
  try {
    return await pending
  } finally {
    refreshes.delete(session.refreshToken)
  }
}

const providers: NextAuthConfig['providers'] = [
  Credentials({
    credentials: {
      email: {},
      password: {},
      mode: {},
      displayName: {},
    },
    async authorize(credentials) {
      const parsed = credentialsSchema.safeParse(credentials)
      if (!parsed.success) return null
      const { email, password, mode, displayName } = parsed.data
      try {
        const backendSession = await exchange(
          mode === 'register' ? '/auth/register' : '/auth/login',
          mode === 'register' ? { displayName, email, password } : { email, password },
        )
        return {
          id: backendSession.user.id,
          name: backendSession.user.displayName,
          email: backendSession.user.email,
          backendSession,
        }
      } catch {
        return null
      }
    },
  }),
]

if (process.env.AUTH_GOOGLE_ID && process.env.AUTH_GOOGLE_SECRET) {
  providers.push(Google)
}

export const { handlers, auth } = NextAuth({
  providers,
  session: { strategy: 'jwt', maxAge: 7 * 24 * 60 * 60 },
  pages: { signIn: '/login' },
  callbacks: {
    async jwt({ token, user, account, trigger, session }) {
      if (account?.provider === 'google') {
        if (!account.id_token) throw new Error('Google ID token is missing.')
        token.backend = await exchange('/auth/google', { idToken: account.id_token })
      } else if (user?.backendSession) {
        token.backend = user.backendSession
      }

      if (!token.backend) return token
      const shouldRefresh =
        trigger === 'update' && (session as { refresh?: boolean } | undefined)?.refresh === true
      if (shouldRefresh || Date.now() >= Date.parse(token.backend.expiresAt) - 30_000) {
        try {
          token.backend = await refresh(token.backend)
          delete token.error
        } catch {
          token.error = 'RefreshTokenError'
          return token
        }
      }

      if (trigger === 'update') {
        try {
          const response = await fetch(backendUrl + '/auth/me', {
            headers: { Authorization: 'Bearer ' + token.backend.accessToken },
            cache: 'no-store',
          })
          if (response.ok) {
            token.backend.user = ((await response.json()) as { data: BackendSession['user'] }).data
          }
        } catch {
          // A profile sync failure does not invalidate a working session.
        }
      }

      return token
    },
    async session({ session, token }) {
      if (token.backend && !token.error) {
        session.backendUser = token.backend.user
        session.accessToken = token.backend.accessToken
      }
      session.error = token.error
      return session
    },
    authorized({ auth }) {
      return Boolean(auth?.accessToken && !auth.error)
    },
  },
  events: {
    async signOut(message) {
      if (!('token' in message) || !message.token?.backend) return
      try {
        await fetch(backendUrl + '/auth/logout', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ refreshToken: message.token.backend.refreshToken }),
        })
      } catch {
        // The Auth.js session is still cleared if the API is unavailable.
      }
    },
  },
})
