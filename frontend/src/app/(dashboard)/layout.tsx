import type { Metadata } from 'next'
import type { ReactNode } from 'react'
import DashboardGroupLayout from './shell'

export const dynamic = 'force-dynamic'
export const metadata: Metadata = {
  title: { default: 'Tổng quan | Culinary Blog', template: '%s | Culinary Blog' },
  robots: { index: false, follow: false },
}

export default function DashboardLayout({ children }: { children: ReactNode }) {
  return <DashboardGroupLayout>{children}</DashboardGroupLayout>
}
