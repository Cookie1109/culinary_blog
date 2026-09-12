'use client'

import { useQueryClient } from '@tanstack/react-query'
import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import {
  clearStoredSession,
  loginSession,
  logoutSession,
  refreshSession,
  registerSession,
  updateProfile as updateRemoteProfile,
  type AuthUser,
} from '@/lib/api/auth-client'

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
  const [user, setUser] = useState<User | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    let active = true
    refreshSession()
      .then((session) => {
        if (active) setUser(mapUser(session.user))
      })
      .catch(() => clearStoredSession())
      .finally(() => {
        if (active) setIsLoading(false)
      })
    return () => {
      active = false
    }
  }, [])

  const login = async (email: string, password: string) => {
    setIsLoading(true)
    try {
      const session = await loginSession(email, password)
      setUser(mapUser(session.user))
    } finally {
      setIsLoading(false)
    }
  }

  const register = async (name: string, email: string, password: string) => {
    setIsLoading(true)
    try {
      const session = await registerSession(name, email, password)
      setUser(mapUser(session.user))
    } finally {
      setIsLoading(false)
    }
  }

  const logout = async () => {
    try {
      await logoutSession()
    } finally {
      setUser(null)
      queryClient.removeQueries()
    }
  }

  const updateProfile = async (data: Partial<Pick<User, 'name' | 'bio' | 'avatar'>>) => {
    const updated = await updateRemoteProfile({
      displayName: data.name ?? user?.name ?? '',
      ...(data.bio !== undefined ? { bio: data.bio } : {}),
      ...(data.avatar !== undefined ? { avatarUrl: data.avatar } : {}),
    })
    setUser(mapUser(updated))
  }

  return (
    <AuthContext.Provider value={{ user, isLoading, login, logout, register, updateProfile }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used within AuthProvider')
  return context
}
