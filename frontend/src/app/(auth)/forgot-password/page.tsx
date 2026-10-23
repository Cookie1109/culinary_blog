import type { Metadata } from 'next'
import { PasswordForm } from '@/features/culinary/pages/auth/PasswordForm'

export const metadata: Metadata = { title: 'Quên mật khẩu', robots: { index: false, follow: false } }

export default function ForgotPasswordPage() {
  return <PasswordForm mode="forgot" />
}
