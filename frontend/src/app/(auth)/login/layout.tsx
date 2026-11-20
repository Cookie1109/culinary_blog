import type { ReactNode } from 'react'
import { createPageMetadata } from '@/lib/seo'

export const metadata = createPageMetadata({
  title: 'Đăng nhập',
  description: 'Đăng nhập Culinary Blog.',
  path: '/auth/login',
  index: false,
  follow: false,
})

export default function Layout({ children }: { children: ReactNode }) {
  return children
}
