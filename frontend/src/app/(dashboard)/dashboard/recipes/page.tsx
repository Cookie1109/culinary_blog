import { redirect } from 'next/navigation'
import { MyRecipes } from '@/features/culinary/pages/dashboard/MyRecipes'

export default async function RecipesPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | string[] | undefined>>
}) {
  const params = await searchParams
  if (params.scope === 'all') {
    const query = new URLSearchParams()
    if (typeof params.status === 'string') query.set('status', params.status)
    redirect(`/dashboard/system${query.size ? `?${query}` : ''}`)
  }
  return <MyRecipes />
}
