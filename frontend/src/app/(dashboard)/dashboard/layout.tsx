'use client'

import { useEffect, type ReactNode } from 'react'
import { usePathname, useRouter } from 'next/navigation'
import { DashboardLayout } from '@/features/culinary/components/layout/DashboardLayout'
import { useAuth } from '@/features/culinary/contexts/AuthContext'
import { LegacyOutletProvider } from '@/lib/router-compat'

export default function DashboardShell({ children }: { children: ReactNode }) {
  const { user, isLoading } = useAuth()
  const pathname = usePathname()
  const router = useRouter()

  useEffect(() => {
    if (!isLoading && !user) {
      router.replace(`/login?returnTo=${encodeURIComponent(pathname)}`)
    }
  }, [isLoading, pathname, router, user])

  if (isLoading || !user) {
    return (
      <div
        className="min-h-[50vh] flex items-center justify-center text-sm text-muted-foreground"
        role="status"
      >
        Đang kiểm tra phiên đăng nhập…
      </div>
    )
  }

  return (
    <LegacyOutletProvider outlet={children}>
      <DashboardLayout />
    </LegacyOutletProvider>
  )
}
