import Page from '@/app/(public)/categories/[slug]/page'

export default Page
export { generateMetadata } from '@/app/(public)/categories/[slug]/page'

export const dynamic = 'force-dynamic'
export const revalidate = 0

export function generateStaticParams() {
  return []
}
