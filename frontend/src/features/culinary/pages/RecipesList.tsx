'use client'

import { useQuery } from '@tanstack/react-query'
import { LayoutGrid, List } from 'lucide-react'
import { useEffect } from 'react'
import { useSearchParams } from 'react-router'
import { RecipeCard } from '@/features/culinary/components/RecipeCard'
import {
  listCategories,
  listPublishedRecipes,
  listPublishedRecipesByCategory,
} from '@/lib/api/content-client'
import { useUIStore } from '@/store/useUIStore'

const PAGE_SIZE = 12

export function RecipesList() {
  const [searchParams, setSearchParams] = useSearchParams()
  const category = searchParams.get('category') ?? ''
  const requestedPage = Number.parseInt(searchParams.get('page') ?? '1', 10)
  const page = Number.isFinite(requestedPage) && requestedPage > 0 ? requestedPage : 1
  const categoriesQuery = useQuery({ queryKey: ['categories'], queryFn: listCategories })
  const recipesQuery = useQuery({
    queryKey: ['public-recipes', category, page],
    queryFn: async () => {
      if (!category) return listPublishedRecipes(page, PAGE_SIZE)
      const result = await listPublishedRecipesByCategory(category, page, PAGE_SIZE)
      return { data: result.data.recipes, meta: result.meta }
    },
  })
  const recipes = recipesQuery.data?.data ?? []
  const viewMode = useUIStore((state) => state.viewMode)
  const setViewMode = useUIStore((state) => state.setViewMode)

  useEffect(() => {
    void useUIStore.persist.rehydrate()
  }, [])

  const selectCategory = (slug: string) => {
    const next = new URLSearchParams(searchParams)
    if (slug) next.set('category', slug)
    else next.delete('category')
    next.delete('page')
    setSearchParams(next)
  }

  return (
    <div className="mx-auto max-w-7xl px-4 py-16 sm:px-6 lg:px-8">
      <header className="mb-12 text-center">
        <h1 className="mb-4 font-serif text-4xl text-foreground md:text-5xl">Tất cả công thức</h1>
        <p className="mx-auto max-w-2xl text-lg text-muted-foreground">
          Khám phá những công thức đã được cộng đồng xuất bản.
        </p>
      </header>

      <div className="mb-16 flex flex-wrap items-center justify-center gap-4 border-b border-border pb-8">
        <button
          type="button"
          onClick={() => selectCategory('')}
          aria-pressed={!category}
          className={!category ? 'text-primary' : 'text-muted-foreground hover:text-foreground'}
        >
          Tất cả
        </button>
        {(categoriesQuery.data ?? []).map((item) => (
          <button
            key={item.id}
            type="button"
            onClick={() => selectCategory(item.slug)}
            aria-pressed={category === item.slug}
            className={
              category === item.slug ? 'text-primary' : 'text-muted-foreground hover:text-foreground'
            }
          >
            {item.name}
          </button>
        ))}
      </div>

      <div className="mb-6 flex justify-end gap-2" aria-label="Kiểu hiển thị công thức">
        <button
          type="button"
          onClick={() => setViewMode('grid')}
          aria-label="Hiển thị dạng lưới"
          aria-pressed={viewMode === 'grid'}
          className={`border p-2 ${viewMode === 'grid' ? 'border-primary text-primary' : 'border-border text-muted-foreground'}`}
        >
          <LayoutGrid size={18} aria-hidden="true" />
        </button>
        <button
          type="button"
          onClick={() => setViewMode('list')}
          aria-label="Hiển thị dạng danh sách"
          aria-pressed={viewMode === 'list'}
          className={`border p-2 ${viewMode === 'list' ? 'border-primary text-primary' : 'border-border text-muted-foreground'}`}
        >
          <List size={18} aria-hidden="true" />
        </button>
      </div>

      {recipesQuery.isPending ? (
        <p role="status" className="py-20 text-center text-muted-foreground">
          Đang tải công thức…
        </p>
      ) : recipesQuery.isError ? (
        <p role="alert" className="py-20 text-center text-red-600">
          Không thể tải danh sách công thức.
        </p>
      ) : recipes.length === 0 ? (
        <p className="py-20 text-center text-muted-foreground">Không có công thức phù hợp.</p>
      ) : (
        <div className={`grid grid-cols-1 gap-x-8 gap-y-16 ${viewMode === 'grid' ? 'md:grid-cols-2 lg:grid-cols-3' : 'mx-auto max-w-2xl'}`}>
          {recipes.map((recipe) => (
            <RecipeCard key={recipe.id} recipe={recipe} />
          ))}
        </div>
      )}

      {(recipesQuery.data?.meta.totalPages ?? 0) > 1 && (
        <nav className="mt-20 flex justify-center gap-2" aria-label="Phân trang công thức">
          {Array.from({ length: recipesQuery.data!.meta.totalPages }, (_, index) => index + 1).map(
            (pageNumber) => (
              <button
                key={pageNumber}
                type="button"
                aria-current={pageNumber === page ? 'page' : undefined}
                onClick={() => {
                  const next = new URLSearchParams(searchParams)
                  next.set('page', String(pageNumber))
                  setSearchParams(next)
                }}
                className={`h-10 w-10 border font-serif text-lg ${
                  pageNumber === page
                    ? 'border-foreground bg-foreground text-background'
                    : 'border-border text-muted-foreground hover:border-foreground'
                }`}
              >
                {pageNumber}
              </button>
            ),
          )}
        </nav>
      )}
    </div>
  )
}
