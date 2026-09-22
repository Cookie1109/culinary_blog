'use client'

import { useQueryClient } from '@tanstack/react-query'
import { signIn, signOut, useSession } from 'next-auth/react'
import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { updateProfile as updateRemoteProfile } from '@/lib/api/auth-client'
import { configureSessionRefresh } from '@/lib/api/axios'
import type { AuthUser } from '@/lib/api/auth-types'

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
  const user = session?.backendUser ? mapUser(session.backendUser) : null

  useEffect(() => {
    configureSessionRefresh(() => update({ refresh: true }))
    return () => configureSessionRefresh(null)
  }, [update])

  const login = async (email: string, password: string) => {
    setBusy(true)
    try {
      const result = await signIn('credentials', { email, password, redirect: false })
      if (result?.error) throw new Error(result.error)
      await update()
    } finally {
      setBusy(false)
    }
  }

  const register = async (name: string, email: string, password: string) => {
    setBusy(true)
    try {
      const result = await signIn('credentials', {
        email,
        password,
        displayName: name,
        mode: 'register',
        redirect: false,
      })
      if (result?.error) throw new Error(result.error)
      await update()
    } finally {
      setBusy(false)
    }
  }

  const logout = async () => {
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
      value={{ user, isLoading: status === 'loading' || busy, login, logout, register, updateProfile }}
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
