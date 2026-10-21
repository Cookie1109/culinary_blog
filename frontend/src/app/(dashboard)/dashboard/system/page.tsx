import { redirect } from 'next/navigation'
import { auth } from '@/auth'
import { SystemOverview } from '@/features/culinary/pages/dashboard/SystemOverview'

export default async function SystemPage() {
  const session = await auth()
  if (session?.backendUser && !session.backendUser.roles.includes('Admin')) redirect('/dashboard')
  return <SystemOverview />
}
