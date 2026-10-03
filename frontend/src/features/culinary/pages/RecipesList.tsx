'use client'

import { LayoutGrid, List } from 'lucide-react'
import { useEffect } from 'react'
import { useSearchParams } from 'react-router'
import { RecipeCard } from '@/features/culinary/components/RecipeCard'
import type { Category, PageEnvelope, Recipe } from '@/lib/api/content-client'
import { useUIStore } from '@/store/useUIStore'

export function RecipesList({
  categories,
  result,
}: {
  categories: Category[]
  result: PageEnvelope<Recipe>
}) {
  const [searchParams, setSearchParams] = useSearchParams()
  const category = searchParams.get('category') ?? ''
  const difficulty = searchParams.get('difficulty') ?? ''
  const requestedMaxTime = Number.parseInt(searchParams.get('maxTime') ?? '0', 10)
  const maxTime = Number.isFinite(requestedMaxTime) && requestedMaxTime > 0 ? requestedMaxTime : 0
  const sort = searchParams.get('sort') ?? 'newest'
  const requestedPage = Number.parseInt(searchParams.get('page') ?? '1', 10)
  const page = Number.isFinite(requestedPage) && requestedPage > 0 ? requestedPage : 1
  const recipes = result.data
  const viewMode = useUIStore((state) => state.viewMode)
  const setViewMode = useUIStore((state) => state.setViewMode)

  useEffect(() => {
    void useUIStore.persist.rehydrate()
  }, [])

  const setFilter = (key: string, value: string) => {
    const next = new URLSearchParams(searchParams)
    if (value) next.set(key, value)
    else next.delete(key)
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

      <section
        className="mb-10 grid gap-4 border-y border-border py-6 sm:grid-cols-2 lg:grid-cols-4"
        aria-label="Bộ lọc công thức"
      >
        <label className="text-sm">
          <span className="mb-2 block text-muted-foreground">Danh mục</span>
          <select
            value={category}
            onChange={(event) => setFilter('category', event.target.value)}
            className="w-full border border-border bg-background px-3 py-2"
          >
            <option value="">Tất cả danh mục</option>
            {categories.map((item) => (
              <option key={item.id} value={item.slug}>
                {item.name}
              </option>
            ))}
          </select>
        </label>
        <label className="text-sm">
          <span className="mb-2 block text-muted-foreground">Độ khó</span>
          <select
            value={difficulty}
            onChange={(event) => setFilter('difficulty', event.target.value)}
            className="w-full border border-border bg-background px-3 py-2"
          >
            <option value="">Tất cả độ khó</option>
            <option value="easy">Dễ</option>
            <option value="medium">Trung bình</option>
            <option value="hard">Nâng cao</option>
            <option value="expert">Chuyên gia</option>
          </select>
        </label>
        <label className="text-sm">
          <span className="mb-2 block text-muted-foreground">Tổng thời gian</span>
          <select
            value={maxTime || ''}
            onChange={(event) => setFilter('maxTime', event.target.value)}
            className="w-full border border-border bg-background px-3 py-2"
          >
            <option value="">Không giới hạn</option>
            <option value="30">Tối đa 30 phút</option>
            <option value="60">Tối đa 60 phút</option>
            <option value="120">Tối đa 120 phút</option>
          </select>
        </label>
        <label className="text-sm">
          <span className="mb-2 block text-muted-foreground">Sắp xếp</span>
          <select
            value={sort}
            onChange={(event) => setFilter('sort', event.target.value)}
            className="w-full border border-border bg-background px-3 py-2"
          >
            <option value="newest">Mới nhất</option>
            <option value="quickest">Nấu nhanh nhất</option>
            <option value="az">Tên A–Z</option>
          </select>
        </label>
      </section>

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

      {recipes.length === 0 ? (
        <p className="py-20 text-center text-muted-foreground">Không có công thức phù hợp.</p>
      ) : (
        <div
          className={`grid grid-cols-1 gap-x-8 gap-y-16 ${viewMode === 'grid' ? 'md:grid-cols-2 lg:grid-cols-3' : 'mx-auto max-w-2xl'}`}
        >
          {recipes.map((recipe) => (
            <RecipeCard key={recipe.id} recipe={recipe} />
          ))}
        </div>
      )}

      {result.meta.totalPages > 1 && (
        <nav className="mt-20 flex justify-center gap-2" aria-label="Phân trang công thức">
          {Array.from({ length: result.meta.totalPages }, (_, index) => index + 1).map((pageNumber) => (
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
          ))}
        </nav>
      )}
    </div>
  )
}
