import type { ReactNode } from 'react'
import DashboardGroupLayout from './shell'

export const dynamic = 'force-dynamic'

export default function DashboardLayout({ children }: { children: ReactNode }) {
  return <DashboardGroupLayout>{children}</DashboardGroupLayout>
}
