'use client'

import type { ReactNode } from 'react'
import { AppLayout } from '@/features/culinary/components/layout/AppLayout'
import { LegacyOutletProvider } from '@/lib/router-compat'

export default function SiteLayout({ children }: { children: ReactNode }) {
  return (
    <LegacyOutletProvider outlet={children}>
      <AppLayout />
    </LegacyOutletProvider>
  )
}
