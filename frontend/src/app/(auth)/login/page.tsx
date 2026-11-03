import { Login } from '@/features/culinary/pages/auth/Login'
import { auth } from '@/auth'
import { redirect } from 'next/navigation'

export const dynamic = 'force-dynamic'

export default async function LoginPage() {
  if ((await auth())?.backendUser) redirect('/')
  return <Login googleEnabled={Boolean(process.env.AUTH_GOOGLE_ID && process.env.AUTH_GOOGLE_SECRET)} />
}
