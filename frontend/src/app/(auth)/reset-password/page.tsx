import type { Metadata } from 'next'
import { PasswordForm } from '@/features/culinary/pages/auth/PasswordForm'

export const metadata: Metadata = {
  title: 'Đặt lại mật khẩu',
  robots: { index: false, follow: false },
  referrer: 'no-referrer',
}

export default function ResetPasswordPage() {
  return <PasswordForm mode="reset" />
}
