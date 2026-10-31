import Page from '@/app/(public)/categories/[slug]/page'

export default Page
export { generateMetadata } from '@/app/(public)/categories/[slug]/page'

export const revalidate = 600

export function generateStaticParams() {
  return []
}
