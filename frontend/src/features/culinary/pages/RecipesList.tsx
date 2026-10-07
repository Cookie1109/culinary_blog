'use client'

import { LoaderCircle, Search as SearchIcon } from 'lucide-react'
import { useEffect, useState, useTransition, type FormEvent } from 'react'
import { useSearchParams } from 'react-router'
import { Modal } from '@/components/ui/modal'
import { RecipeCard } from '@/features/culinary/components/RecipeCard'
import type { Category, PageEnvelope, Recipe } from '@/lib/api/content-client'

const difficulties = [
  { value: 'easy', label: 'Dễ' },
  { value: 'medium', label: 'Trung bình' },
  { value: 'hard', label: 'Nâng cao' },
  { value: 'expert', label: 'Chuyên gia' },
]

const MAX_TIME_SLIDER_VALUE = 190

export function RecipesList({
  categories,
  result,
}: {
  categories: Category[]
  result: PageEnvelope<Recipe>
}) {
  const [searchParams, setSearchParams] = useSearchParams()
  const q = searchParams.get('q') ?? ''
  const category = searchParams.get('category') ?? ''
  const difficulty = searchParams.get('difficulty') ?? ''
  const requestedMaxTime = Number.parseInt(searchParams.get('maxTime') ?? '0', 10)
  const maxTime = Number.isFinite(requestedMaxTime) && requestedMaxTime > 0 ? requestedMaxTime : 0
  const sort = searchParams.get('sort') ?? 'newest'
  const requestedPage = Number.parseInt(searchParams.get('page') ?? '1', 10)
  const page = Number.isFinite(requestedPage) && requestedPage > 0 ? requestedPage : 1
  const recipes = result.data
  const [searchValue, setSearchValue] = useState(q)
  const [timeValue, setTimeValue] = useState(maxTime ? Math.min(maxTime, 180) : MAX_TIME_SLIDER_VALUE)
  const [categoryModalOpen, setCategoryModalOpen] = useState(false)
  const [isPending, startTransition] = useTransition()
  const selectedCategory = categories.find((item) => item.slug === category)
  const firstCategories = categories.slice(0, 4)
  const visibleCategories =
    selectedCategory && !firstCategories.some((item) => item.id === selectedCategory.id)
      ? [...categories.slice(0, 3), selectedCategory]
      : firstCategories

  useEffect(() => setSearchValue(q), [q])
  useEffect(() => setTimeValue(maxTime ? Math.min(maxTime, 180) : MAX_TIME_SLIDER_VALUE), [maxTime])

  const setFilter = (key: string, value: string) => {
    const next = new URLSearchParams(searchParams)
    if (value) next.set(key, value)
    else next.delete(key)
    if (key !== 'page') next.delete('page')
    startTransition(() => setSearchParams(next))
  }

  const submitSearch = (event: FormEvent) => {
    event.preventDefault()
    setFilter('q', searchValue.trim())
  }

  const applyTimeFilter = (value: number) => {
    setFilter('maxTime', value === MAX_TIME_SLIDER_VALUE ? '' : String(value))
  }

  const selectCategory = (slug: string) => {
    setCategoryModalOpen(false)
    setFilter('category', slug)
  }

  return (
    <div className="mx-auto max-w-7xl px-4 py-12 sm:px-6 sm:py-16 lg:px-8">
      {isPending && (
        <div
          className="fixed right-4 top-24 z-40 flex items-center gap-2 border border-border bg-card px-4 py-3 text-sm shadow-lg"
          role="status"
          aria-live="polite"
        >
          <LoaderCircle size={18} className="animate-spin text-primary" aria-hidden="true" />
          Đang cập nhật công thức…
        </div>
      )}
      <header className="mb-10 text-center">
        <h1 className="mb-7 font-serif text-4xl text-foreground sm:text-5xl">Tìm kiếm công thức</h1>
        <form
          onSubmit={submitSearch}
          className="mx-auto flex max-w-3xl flex-col sm:flex-row"
          role="search"
          aria-label="Tìm kiếm công thức"
        >
          <label htmlFor="recipes-search" className="sr-only">
            Từ khóa tìm kiếm
          </label>
          <div className="relative min-w-0 flex-1">
            <SearchIcon
              size={20}
              aria-hidden="true"
              className="pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-muted-foreground"
            />
            <input
              id="recipes-search"
              type="search"
              value={searchValue}
              onChange={(event) => setSearchValue(event.target.value)}
              className="h-14 w-full border border-border bg-background pl-12 pr-4 text-base focus:border-primary focus:outline-none"
              placeholder="Tìm theo tên, nguyên liệu hoặc danh mục..."
            />
          </div>
          <button
            type="submit"
            className="h-14 bg-primary px-8 text-sm font-semibold uppercase tracking-widest text-primary-foreground transition-colors hover:bg-primary/90"
          >
            Tìm kiếm
          </button>
        </form>
      </header>

      <div className="grid gap-10 lg:grid-cols-[14rem_minmax(0,1fr)] lg:gap-8 xl:gap-12">
        <aside
          className="grid gap-8 border-y border-border py-6 sm:grid-cols-3 lg:sticky lg:top-24 lg:block lg:self-start lg:border-y-0 lg:py-0"
          aria-label="Bộ lọc công thức"
        >
          <fieldset className="lg:mb-10">
            <legend className="w-full border-b border-border pb-3 font-serif text-xs uppercase tracking-widest text-muted-foreground">
              Danh mục
            </legend>
            <div className="mt-3 flex flex-wrap gap-x-5 gap-y-1 lg:flex-col lg:items-start">
              <button
                type="button"
                aria-pressed={!category}
                onClick={() => setFilter('category', '')}
                className={`py-1.5 text-sm transition-colors ${!category ? 'text-primary' : 'text-foreground hover:text-primary'}`}
              >
                Tất cả
              </button>
              {visibleCategories.map((item) => (
                <button
                  key={item.id}
                  type="button"
                  aria-pressed={category === item.slug}
                  onClick={() => selectCategory(item.slug)}
                  className={`py-1.5 text-left text-sm transition-colors ${category === item.slug ? 'text-primary' : 'text-foreground hover:text-primary'}`}
                >
                  {item.name}
                </button>
              ))}
              {categories.length > 4 && (
                <button
                  type="button"
                  onClick={() => setCategoryModalOpen(true)}
                  aria-haspopup="dialog"
                  className="mt-1 border-b border-primary py-1 text-left text-sm font-medium text-primary transition-colors hover:text-foreground"
                >
                  Xem thêm danh mục
                </button>
              )}
            </div>
          </fieldset>

          <fieldset className="lg:mb-10">
            <legend className="w-full border-b border-border pb-3 font-serif text-xs uppercase tracking-widest text-muted-foreground">
              Độ khó
            </legend>
            <div className="mt-3 flex flex-wrap gap-x-5 gap-y-1 lg:flex-col lg:items-start">
              <button
                type="button"
                aria-pressed={!difficulty}
                onClick={() => setFilter('difficulty', '')}
                className={`py-1.5 text-sm transition-colors ${!difficulty ? 'text-primary' : 'text-foreground hover:text-primary'}`}
              >
                Tất cả
              </button>
              {difficulties.map((item) => (
                <button
                  key={item.value}
                  type="button"
                  aria-pressed={difficulty === item.value}
                  onClick={() => setFilter('difficulty', item.value)}
                  className={`py-1.5 text-left text-sm transition-colors ${difficulty === item.value ? 'text-primary' : 'text-foreground hover:text-primary'}`}
                >
                  {item.label}
                </button>
              ))}
            </div>
          </fieldset>

          <fieldset>
            <legend className="w-full border-b border-border pb-3 font-serif text-xs uppercase tracking-widest text-muted-foreground">
              Thời gian tối đa
            </legend>
            <label htmlFor="recipes-maxtime-range" className="sr-only">
              Thời gian nấu tối đa
            </label>
            <input
              id="recipes-maxtime-range"
              type="range"
              min="10"
              max={MAX_TIME_SLIDER_VALUE}
              step="10"
              value={timeValue}
              onChange={(event) => setTimeValue(Number(event.target.value))}
              onPointerUp={(event) => applyTimeFilter(Number(event.currentTarget.value))}
              onKeyUp={(event) => applyTimeFilter(Number(event.currentTarget.value))}
              onBlur={(event) => applyTimeFilter(Number(event.currentTarget.value))}
              className="mt-5 w-full accent-primary"
            />
            <div className="mt-1 flex justify-between text-xs text-muted-foreground">
              <span>10 phút</span>
              <span>{timeValue === MAX_TIME_SLIDER_VALUE ? '3+ giờ' : `${timeValue} phút`}</span>
            </div>
          </fieldset>
        </aside>

        <section aria-label="Danh sách công thức">
          <div className="mb-6 flex flex-wrap items-center justify-between gap-4">
            <p className="text-sm text-muted-foreground" aria-live="polite">
              <span className="font-medium text-foreground">{result.meta.total}</span> công thức được tìm thấy
            </p>
            <label htmlFor="recipes-sort-select" className="sr-only">
              Sắp xếp công thức
            </label>
            <select
              id="recipes-sort-select"
              value={sort}
              onChange={(event) => setFilter('sort', event.target.value)}
              className="border border-border bg-background px-4 py-2.5 text-sm"
            >
              <option value="newest">Mới nhất</option>
              <option value="quickest">Nấu nhanh nhất</option>
              <option value="az">Tên A–Z</option>
            </select>
          </div>

          <div className="sr-only" aria-live="polite" aria-atomic="true">
            {`Hiển thị ${recipes.length} trên tổng số ${result.meta.total} công thức, trang ${result.meta.page} trên ${result.meta.totalPages}`}
          </div>

          {recipes.length === 0 ? (
            <p className="py-20 text-center text-muted-foreground">Không có công thức phù hợp.</p>
          ) : (
            <div className="grid grid-cols-1 gap-x-6 gap-y-12 md:grid-cols-2 xl:grid-cols-3">
              {recipes.map((recipe) => (
                <RecipeCard key={recipe.id} recipe={recipe} />
              ))}
            </div>
          )}

          {result.meta.totalPages > 1 && (
            <nav className="mt-16 flex flex-wrap justify-center gap-2" aria-label="Phân trang công thức">
              {Array.from({ length: result.meta.totalPages }, (_, index) => index + 1).map((pageNumber) => (
                <button
                  key={pageNumber}
                  type="button"
                  aria-label={`Trang ${pageNumber}`}
                  aria-current={pageNumber === page ? 'page' : undefined}
                  onClick={() => setFilter('page', String(pageNumber))}
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
        </section>
      </div>

      {categoryModalOpen && (
        <Modal open title="Tất cả danh mục" onClose={() => setCategoryModalOpen(false)}>
          <div className="grid max-h-[60vh] grid-cols-1 gap-2 overflow-y-auto pr-1 sm:grid-cols-2">
            <button
              type="button"
              aria-pressed={!category}
              onClick={() => selectCategory('')}
              className={`border px-4 py-3 text-left text-sm transition-colors ${
                !category
                  ? 'border-primary bg-primary text-primary-foreground'
                  : 'border-border hover:border-primary hover:text-primary'
              }`}
            >
              Tất cả
            </button>
            {categories.map((item) => (
              <button
                key={item.id}
                type="button"
                aria-pressed={category === item.slug}
                onClick={() => selectCategory(item.slug)}
                className={`border px-4 py-3 text-left text-sm transition-colors ${
                  category === item.slug
                    ? 'border-primary bg-primary text-primary-foreground'
                    : 'border-border hover:border-primary hover:text-primary'
                }`}
              >
                {item.name}
              </button>
            ))}
          </div>
        </Modal>
      )}
    </div>
  )
}
