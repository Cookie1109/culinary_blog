import type { Metadata } from 'next'
import { notFound, redirect } from 'next/navigation'
import { Link } from 'react-router'
import { auth } from '@/auth'
import { RecipeDetail } from '@/features/culinary/pages/RecipeDetail'
import type { Recipe } from '@/lib/api/content-client'

export const dynamic = 'force-dynamic'
export const revalidate = 0

export const metadata: Metadata = {
  title: 'Xem trước công thức',
  robots: { index: false, follow: false },
}

export default async function RecipePreviewPage({ params }: { params: Promise<{ id: string }> }) {
  const session = await auth()
  if (!session?.accessToken) redirect('/login')

  const { id } = await params
  const apiBaseUrl = process.env.INTERNAL_API_BASE_URL ?? 'http://localhost:5000/api/v1'
  const response = await fetch(`${apiBaseUrl}/me/recipes/${encodeURIComponent(id)}`, {
    cache: 'no-store',
    headers: {
      Accept: 'application/json',
      Authorization: `Bearer ${session.accessToken}`,
    },
  })

  if (response.status === 404 || response.status === 403) notFound()
  if (!response.ok) throw new Error(`Private recipe API returned ${response.status}.`)

  const recipe = ((await response.json()) as { data: Recipe }).data

  return (
    <div>
      <aside className="border-b border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">
        <div className="mx-auto flex max-w-7xl flex-wrap items-center justify-between gap-3">
          <p>Bản xem trước riêng tư · {recipe.status === 'published' ? 'Đã xuất bản' : 'Chưa xuất bản'}</p>
          <Link className="font-medium underline" to={`/dashboard/recipes/${recipe.id}/edit`}>
            Quay lại chỉnh sửa
          </Link>
        </div>
      </aside>
      <RecipeDetail recipe={recipe} privatePreview />
    </div>
  )
}
