export interface AuthUser {
  id: string
  email: string
  displayName: string
  avatarUrl: string | null
  bio: string | null
  roles: ('Author' | 'Admin')[]
  emailConfirmed: boolean
  isActive: boolean
  createdAt: string
}

export interface BackendSession {
  accessToken: string
  refreshToken: string
  expiresAt: string
  user: AuthUser
}
