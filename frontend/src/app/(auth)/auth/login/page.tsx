import { createPageMetadata } from '@/lib/seo'

import Page from '@/app/(auth)/login/page'

export default Page
export const dynamic = 'force-dynamic'
export const metadata = createPageMetadata({
  title: 'Đăng nhập',
  description: 'Đăng nhập Culinary Blog.',
  path: '/auth/login',
  index: false,
  follow: false,
})
