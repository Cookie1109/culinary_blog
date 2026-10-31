import { createPageMetadata } from '@/lib/seo'

import Page from '@/app/(auth)/register/page'

export default Page
export const metadata = createPageMetadata({
  title: 'Đăng ký',
  description: 'Tạo tài khoản Culinary Blog.',
  path: '/auth/register',
  index: false,
  follow: false,
})
