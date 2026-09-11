import { createContext, useContext, useState, type ReactNode } from 'react'

export interface User {
  id: string
  name: string
  email: string
  avatar?: string
  bio?: string
  role: 'admin' | 'author' | 'reader'
}

interface AuthContextType {
  user: User | null
  isLoading: boolean
  login: (email: string, password: string) => Promise<void>
  loginWithGoogle: () => Promise<void>
  logout: () => void
  register: (name: string, email: string, password: string) => Promise<void>
  updateProfile: (data: Partial<Pick<User, 'name' | 'bio' | 'avatar'>>) => void
}

const AuthContext = createContext<AuthContextType | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [isLoading, setIsLoading] = useState(false)

  const login = async (email: string, _password: string) => {
    setIsLoading(true)
    await new Promise((r) => setTimeout(r, 800))
    setUser({
      id: 'u1',
      name: 'Eleanor Vance',
      email,
      avatar: 'https://images.unsplash.com/photo-1438761681033-6461ffad8d80?w=200&h=200&fit=crop&face',
      bio: 'Food writer, home cook, and devoted recipe developer. I believe in the power of good ingredients and honest techniques.',
      role: 'admin',
    })
    setIsLoading(false)
  }

  const loginWithGoogle = async () => {
    setIsLoading(true)
    await new Promise((r) => setTimeout(r, 600))
    setUser({
      id: 'u2',
      name: 'Eleanor Vance',
      email: 'eleanor@example.com',
      avatar: 'https://images.unsplash.com/photo-1438761681033-6461ffad8d80?w=200&h=200&fit=crop&face',
      bio: 'Food writer and home cook. Passionate about seasonal ingredients.',
      role: 'admin',
    })
    setIsLoading(false)
  }

  const logout = () => setUser(null)

  const register = async (name: string, email: string, _password: string) => {
    setIsLoading(true)
    await new Promise((r) => setTimeout(r, 800))
    setUser({ id: 'u3', name, email, role: 'author' })
    setIsLoading(false)
  }

  const updateProfile = (data: Partial<Pick<User, 'name' | 'bio' | 'avatar'>>) => {
    if (user) setUser({ ...user, ...data })
  }

  return (
    <AuthContext.Provider
      value={{ user, isLoading, login, loginWithGoogle, logout, register, updateProfile }}
    >
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
