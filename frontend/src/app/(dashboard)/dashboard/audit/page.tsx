import type { Metadata } from 'next'
import { redirect } from 'next/navigation'
import { auth } from '@/auth'
import { AuditLogAdmin } from '@/features/culinary/pages/dashboard/AuditLogAdmin'

export const metadata: Metadata = {
  title: 'Lịch sử hệ thống',
  robots: { index: false, follow: false },
}

export default async function HistoryPage() {
  const session = await auth()
  if (session?.backendUser && !session.backendUser.roles.includes('Admin')) redirect('/dashboard')
  return <AuditLogAdmin />
}
