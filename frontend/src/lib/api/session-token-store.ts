import type { BackendSession } from '@/lib/api/auth-types'

const REFRESH_TOKEN_KEY = 'culinary.refresh-token'

export function readRefreshToken() {
  return typeof window === 'undefined' ? null : window.sessionStorage.getItem(REFRESH_TOKEN_KEY)
}

export function storeRefreshToken(session: BackendSession) {
  window.sessionStorage.setItem(REFRESH_TOKEN_KEY, session.refreshToken)
}

export function clearRefreshToken() {
  if (typeof window !== 'undefined') window.sessionStorage.removeItem(REFRESH_TOKEN_KEY)
}
