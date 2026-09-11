'use client'

import type { ReactNode } from 'react'
import { DashboardLayout } from '@/features/culinary/components/layout/DashboardLayout'
import { LegacyOutletProvider } from '@/lib/router-compat'

export default function DashboardShell({ children }: { children: ReactNode }) {
  return (
    <LegacyOutletProvider outlet={children}>
      <DashboardLayout />
    </LegacyOutletProvider>
  )
}
