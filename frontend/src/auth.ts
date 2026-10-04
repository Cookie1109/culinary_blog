import NextAuth from 'next-auth'
import Credentials from 'next-auth/providers/credentials'
import Google from 'next-auth/providers/google'
import type { NextAuthConfig } from 'next-auth'
import { z } from 'zod'
import type { BackendAccessSession, BackendSession, AuthUser } from '@/lib/api/auth-types'

const credentialsSchema = z.object({
  accessToken: z.string().min(1),
  expiresAt: z.iso.datetime({ offset: true }),
})

const backendUrl = process.env.INTERNAL_API_BASE_URL ?? 'http://localhost:5000/api/v1'

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

async function revoke(refreshToken: string) {
  await fetch(backendUrl + '/auth/logout', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken }),
    cache: 'no-store',
  })
}

async function readBackendUser(accessToken: string): Promise<AuthUser> {
  const response = await fetch(backendUrl + '/auth/me', {
    headers: { Authorization: 'Bearer ' + accessToken },
    cache: 'no-store',
  })
  if (!response.ok) throw new Error('Backend access token validation failed.')
  return ((await response.json()) as { data: AuthUser }).data
}

function withoutRefreshToken(session: BackendSession): BackendAccessSession {
  return {
    accessToken: session.accessToken,
    expiresAt: session.expiresAt,
    user: session.user,
  }
}

const providers: NextAuthConfig['providers'] = [
  Credentials({
    credentials: {
      accessToken: {},
      expiresAt: {},
    },
    async authorize(credentials) {
      const parsed = credentialsSchema.safeParse(credentials)
      if (!parsed.success) return null
      try {
        const backendUser = await readBackendUser(parsed.data.accessToken)
        return {
          id: backendUser.id,
          name: backendUser.displayName,
          email: backendUser.email,
          backendSession: {
            accessToken: parsed.data.accessToken,
            expiresAt: parsed.data.expiresAt,
            user: backendUser,
          },
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
    async jwt({ token, user, account, trigger }) {
      if (account?.provider === 'google') {
        if (!account.id_token) throw new Error('Google ID token is missing.')
        const backendSession = await exchange('/auth/google', { idToken: account.id_token })
        token.backend = withoutRefreshToken(backendSession)
        await revoke(backendSession.refreshToken)
      } else if (user?.backendSession) {
        token.backend = user.backendSession
      }

      if (!token.backend) return token

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
      if (token.backend) {
        session.backendUser = token.backend.user
        session.accessToken = token.backend.accessToken
        session.accessTokenExpiresAt = token.backend.expiresAt
      }
      return session
    },
    authorized({ auth }) {
      return Boolean(auth?.backendUser)
    },
  },
})
