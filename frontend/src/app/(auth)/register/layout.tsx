import type { ReactNode } from 'react'
import { createPageMetadata } from '@/lib/seo'

export const metadata = createPageMetadata({
  title: 'Đăng ký',
  description: 'Tạo tài khoản Culinary Blog.',
  path: '/auth/register',
  index: false,
  follow: false,
})

export default function Layout({ children }: { children: ReactNode }) {
  return children
}
