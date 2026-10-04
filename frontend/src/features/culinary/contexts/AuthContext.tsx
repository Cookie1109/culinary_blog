'use client'

import { useQueryClient } from '@tanstack/react-query'
import { signIn, signOut, useSession } from 'next-auth/react'
import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from 'react'
import {
  loginWithPassword,
  registerWithPassword,
  revokeBackendSession,
  rotateBackendSession,
  updateProfile as updateRemoteProfile,
} from '@/lib/api/auth-client'
import { configureSessionRefresh } from '@/lib/api/axios'
import type { AuthUser } from '@/lib/api/auth-types'
import { clearRefreshToken, readRefreshToken, storeRefreshToken } from '@/lib/api/session-token-store'

export interface User {
  id: string
  name: string
  email: string
  avatar?: string
  bio?: string
  role: 'admin' | 'author'
}

interface AuthContextType {
  user: User | null
  isLoading: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
  register: (name: string, email: string, password: string) => Promise<void>
  updateProfile: (data: Partial<Pick<User, 'name' | 'bio' | 'avatar'>>) => Promise<void>
}

const AuthContext = createContext<AuthContextType | null>(null)

function mapUser(user: AuthUser): User {
  return {
    id: user.id,
    name: user.displayName,
    email: user.email,
    avatar: user.avatarUrl ?? undefined,
    bio: user.bio ?? undefined,
    role: user.roles.includes('Admin') ? 'admin' : 'author',
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const { data: session, status, update } = useSession()
  const [busy, setBusy] = useState(false)
  const [bootstrapping, setBootstrapping] = useState(true)
  const user = session?.backendUser ? mapUser(session.backendUser) : null

  const establishAuthSession = useCallback(
    async (backendSession: Awaited<ReturnType<typeof rotateBackendSession>>) => {
      storeRefreshToken(backendSession)
      try {
        const result = await signIn('credentials', {
          accessToken: backendSession.accessToken,
          expiresAt: backendSession.expiresAt,
          redirect: false,
        })
        if (result?.error) throw new Error(result.error)
        return update()
      } catch (error) {
        clearRefreshToken()
        try {
          await revokeBackendSession(backendSession.refreshToken)
        } catch {
          // Preserve the original Auth.js failure.
        }
        throw error
      }
    },
    [update],
  )

  const refreshSession = useCallback(async () => {
    const refreshToken = readRefreshToken()
    if (!refreshToken) return null
    try {
      return await establishAuthSession(await rotateBackendSession(refreshToken))
    } catch {
      clearRefreshToken()
      await signOut({ redirect: false })
      return null
    }
  }, [establishAuthSession])

  useEffect(() => {
    configureSessionRefresh(refreshSession)
    return () => configureSessionRefresh(null)
  }, [refreshSession])

  useEffect(() => {
    if (status === 'loading') return
    const expiresSoon =
      !session?.accessTokenExpiresAt || Date.now() >= Date.parse(session.accessTokenExpiresAt) - 30_000
    if (readRefreshToken() && (!session?.accessToken || expiresSoon)) {
      void refreshSession().finally(() => setBootstrapping(false))
      return
    }
    if (session?.backendUser && (!session.accessToken || expiresSoon)) {
      void signOut({ redirect: false }).finally(() => setBootstrapping(false))
      return
    }
    setBootstrapping(false)
  }, [refreshSession, session?.accessToken, session?.accessTokenExpiresAt, session?.backendUser, status])

  const login = async (email: string, password: string) => {
    setBusy(true)
    try {
      await establishAuthSession(await loginWithPassword(email, password))
    } finally {
      setBusy(false)
    }
  }

  const register = async (name: string, email: string, password: string) => {
    setBusy(true)
    try {
      await establishAuthSession(await registerWithPassword(name, email, password))
    } finally {
      setBusy(false)
    }
  }

  const logout = async () => {
    const refreshToken = readRefreshToken()
    clearRefreshToken()
    if (refreshToken) {
      try {
        await revokeBackendSession(refreshToken)
      } catch {
        // Local and Auth.js state are still cleared if the API is unavailable.
      }
    }
    await signOut({ redirect: false })
    queryClient.removeQueries()
  }

  const updateProfile = async (data: Partial<Pick<User, 'name' | 'bio' | 'avatar'>>) => {
    await updateRemoteProfile({
      displayName: data.name ?? user?.name ?? '',
      ...(data.bio !== undefined ? { bio: data.bio } : {}),
      ...(data.avatar !== undefined ? { avatarUrl: data.avatar } : {}),
    })
    await update()
  }

  return (
    <AuthContext.Provider
      value={{
        user,
        isLoading: status === 'loading' || bootstrapping || busy,
        login,
        logout,
        register,
        updateProfile,
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used within AuthProvider')
  return context
}
