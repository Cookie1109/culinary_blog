import { redirect } from 'next/navigation'
import { auth } from '@/auth'
import { AuditLogAdmin } from '@/features/culinary/pages/dashboard/AuditLogAdmin'

export default async function HistoryPage() {
  const session = await auth()
  if (session?.backendUser && !session.backendUser.roles.includes('Admin')) redirect('/dashboard')
  return <AuditLogAdmin />
}
