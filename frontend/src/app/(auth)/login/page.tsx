import { Login } from '@/features/culinary/pages/auth/Login'

export const dynamic = 'force-dynamic'

export default function LoginPage() {
  return <Login googleEnabled={Boolean(process.env.AUTH_GOOGLE_ID && process.env.AUTH_GOOGLE_SECRET)} />
}
