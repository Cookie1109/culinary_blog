'use client'

import { useAuth } from '../../contexts/AuthContext'
import { PersonalOverview } from './PersonalOverview'

export function Dashboard() {
  const { user } = useAuth()
  if (!user) {
    return <p role="status">Đang kiểm tra phiên đăng nhập…</p>
  }
  return <PersonalOverview />
}
