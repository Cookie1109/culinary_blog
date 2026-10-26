'use client'

import { LoaderCircle, Search as SearchIcon, X } from 'lucide-react'
import { useEffect, useRef, useState, useTransition, type FormEvent } from 'react'
import { useSearchParams } from 'react-router'
import { useRouter } from 'next/navigation'
import { normalizeRecipeSearch, recipeSearchPages, recipeSearchValidation } from '@/lib/recipe-search'
import { FieldError } from '@/components/ui/field-error'
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
  initialSearchError = '',
  loadError = false,
}: {
  categories: Category[]
  result: PageEnvelope<Recipe>
  initialSearchError?: string
  loadError?: boolean
}) {
  const [searchParams, setSearchParams] = useSearchParams()
  const q = normalizeRecipeSearch(searchParams.get('q') ?? '')
  const category = searchParams.get('category') ?? ''
  const difficulty = searchParams.get('difficulty') ?? ''
  const requestedMaxTime = Number.parseInt(searchParams.get('maxTime') ?? '0', 10)
  const maxTime = Number.isFinite(requestedMaxTime) && requestedMaxTime > 0 ? requestedMaxTime : 0
  const requestedSort = searchParams.get('sort') ?? ''
  const sort = !q && requestedSort === 'relevance' ? 'newest' : requestedSort || (q ? 'relevance' : 'newest')
  const requestedPage = Number.parseInt(searchParams.get('page') ?? '1', 10)
  const page = Number.isFinite(requestedPage) && requestedPage > 0 ? requestedPage : 1
  const recipes = result.data
  const visiblePages = recipeSearchPages(page, result.meta.totalPages)
  const pageOutOfRange = result.meta.total > 0 && page > result.meta.totalPages
  const searchSummary = loadError ? (
    'Chưa tải được kết quả'
  ) : (
    <>
      <span className="font-medium text-foreground">{result.meta.total}</span> công thức được tìm thấy
      {q && (
        <>
          {' '}
          cho <strong>“{q}”</strong>
        </>
      )}
    </>
  )
  const [searchError, setSearchError] = useState(initialSearchError)
  const [searchValue, setSearchValue] = useState(q)
  const [timeValue, setTimeValue] = useState(maxTime ? Math.min(maxTime, 180) : MAX_TIME_SLIDER_VALUE)
  const [categoryModalOpen, setCategoryModalOpen] = useState(false)
  const [filtersOpen, setFiltersOpen] = useState(false)
  const [isPending, startTransition] = useTransition()
  const router = useRouter()
  const resultsRef = useRef<HTMLElement>(null)
  const inputRef = useRef<HTMLInputElement>(null)
  const lastNavigation = useRef('')
  const composing = useRef(false)
  const focusResults = useRef(false)
  const submittedSearch = useRef<string | null>(null)
  const hasActiveFilters = [...searchParams.keys()].some((key) => !['q', 'page', 'sort'].includes(key))
  const selectedCategory = categories.find((item) => item.slug === category)
  const firstCategories = categories.slice(0, 4)
  const visibleCategories =
    selectedCategory && !firstCategories.some((item) => item.id === selectedCategory.id)
      ? [...categories.slice(0, 3), selectedCategory]
      : firstCategories

  useEffect(() => {
    lastNavigation.current = searchParams.toString()
  }, [searchParams])
  useEffect(() => {
    const submitted = submittedSearch.current
    setSearchValue((value) => (submitted !== null && value !== submitted ? value : q))
    submittedSearch.current = null
    setSearchError(initialSearchError)
  }, [q, initialSearchError])
  useEffect(() => {
    if (!isPending && focusResults.current) {
      resultsRef.current?.focus({ preventScroll: true })
      resultsRef.current?.scrollIntoView?.({ block: 'start' })
      focusResults.current = false
    }
  }, [isPending, result])
  useEffect(() => setTimeValue(maxTime ? Math.min(maxTime, 180) : MAX_TIME_SLIDER_VALUE), [maxTime])

  const setFilter = (key: string, value: string) => {
    if (key === 'q' || key === 'page') {
      setFiltersOpen(false)
      focusResults.current = true
    }
    const next = new URLSearchParams(searchParams)
    if (value) next.set(key, value)
    else next.delete(key)
    if (key !== 'page') next.delete('page')
    if (key === 'q' && value !== q) {
      next.delete('sort')
      next.delete('sortBy')
      next.delete('sortOrder')
    }
    const destination = next.toString()
    if (destination === searchParams.toString()) {
      focusResults.current = false
      if (loadError) startTransition(() => router.refresh())
      else if (key === 'q' || key === 'page') {
        resultsRef.current?.focus({ preventScroll: true })
        resultsRef.current?.scrollIntoView?.({ block: 'start' })
      }
      return
    }
    if (destination === lastNavigation.current) return
    lastNavigation.current = destination
    startTransition(() => setSearchParams(next, { replace: false, scroll: false }))
  }

  const submitSearch = (event: FormEvent) => {
    event.preventDefault()
    if (composing.current) return
    const value = normalizeRecipeSearch(searchValue)
    const validation = recipeSearchValidation(value)
    if (validation) {
      setSearchError(validation)
      inputRef.current?.focus()
      return
    }
    submittedSearch.current = value
    setSearchValue(value)
    setSearchError('')
    setFilter('q', value)
  }

  const clearSearch = () => {
    setSearchValue('')
    setSearchError('')
    submittedSearch.current = ''
    if (q) setFilter('q', '')
    focusResults.current = false
    inputRef.current?.focus()
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
          noValidate
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
              ref={inputRef}
              id="recipes-search"
              type="search"
              maxLength={100}
              aria-invalid={Boolean(searchError)}
              aria-describedby={searchError ? 'recipes-search-error' : 'recipes-search-hint'}
              value={searchValue}
              onChange={(event) => {
                setSearchValue(event.target.value)
                setSearchError('')
              }}
              onCompositionStart={() => {
                composing.current = true
              }}
              onCompositionEnd={() => {
                composing.current = false
              }}
              onKeyDown={(event) => {
                if (event.key === 'Enter' && event.nativeEvent.isComposing) event.preventDefault()
              }}
              className="h-14 w-full border border-border bg-background pl-12 pr-12 text-base [&::-webkit-search-cancel-button]:appearance-none focus:border-primary focus:outline-none"
              placeholder="Ví dụ: phở bò, gà nướng, ca kho..."
            />
            {searchValue && (
              <button
                type="button"
                aria-label="Xóa từ khóa"
                onClick={clearSearch}
                className="absolute right-3 top-1/2 -translate-y-1/2 p-2 text-muted-foreground hover:text-primary"
              >
                <X size={18} aria-hidden="true" />
              </button>
            )}
          </div>
          <button
            disabled={isPending}
            type="submit"
            className="h-14 bg-primary px-8 text-sm font-semibold uppercase tracking-widest text-primary-foreground transition-colors hover:bg-primary/90"
          >
            {isPending ? 'Đang tìm…' : 'Tìm kiếm'}
          </button>
        </form>
        <p id="recipes-search-hint" className="mt-3 text-sm text-muted-foreground">
          Tìm theo tên món. Bạn có thể gõ tiếng Việt không dấu.
        </p>
        <FieldError id="recipes-search-error" message={searchError} />
      </header>

      <div className="mb-6 flex flex-wrap items-center justify-between gap-3 lg:hidden">
        <p className="min-w-0 max-w-full break-words text-sm" aria-live="polite">
          {searchSummary}
        </p>
        <button
          type="button"
          aria-expanded={filtersOpen}
          aria-controls="recipes-filters"
          onClick={() => setFiltersOpen((open) => !open)}
          className="border border-border px-4 py-2 text-sm"
        >
          {filtersOpen ? 'Ẩn bộ lọc' : 'Bộ lọc'}
        </button>
      </div>
      {(q || category || difficulty || maxTime > 0) && (
        <div className="mb-3 flex flex-wrap gap-2" aria-label="Bộ lọc đang áp dụng">
          {q && (
            <button
              type="button"
              aria-label="Bỏ từ khóa"
              onClick={clearSearch}
              className="flex max-w-full items-center gap-2 rounded-full border border-primary/30 bg-primary/5 px-3 py-1.5 text-sm text-primary"
            >
              <span className="truncate">Từ khóa: {q}</span>
              <X size={14} aria-hidden="true" />
            </button>
          )}
          {category && (
            <button
              type="button"
              onClick={() => setFilter('category', '')}
              className="flex items-center gap-2 rounded-full border border-border px-3 py-1.5 text-sm"
              aria-label="Bỏ lọc danh mục"
            >
              {selectedCategory?.name ?? category}
              <X size={14} aria-hidden="true" />
            </button>
          )}
          {difficulty && (
            <button
              type="button"
              onClick={() => setFilter('difficulty', '')}
              className="flex items-center gap-2 rounded-full border border-border px-3 py-1.5 text-sm"
              aria-label="Bỏ lọc độ khó"
            >
              {difficulties.find((item) => item.value === difficulty)?.label ?? difficulty}
              <X size={14} aria-hidden="true" />
            </button>
          )}
          {maxTime > 0 && (
            <button
              type="button"
              onClick={() => setFilter('maxTime', '')}
              className="flex items-center gap-2 rounded-full border border-border px-3 py-1.5 text-sm"
              aria-label="Bỏ lọc thời gian"
            >
              Tối đa {maxTime} phút
              <X size={14} aria-hidden="true" />
            </button>
          )}
        </div>
      )}
      {(q || category || difficulty || maxTime || sort !== 'newest') && (
        <button
          type="button"
          onClick={() => {
            setFiltersOpen(false)
            startTransition(() => setSearchParams(new URLSearchParams(), { replace: false, scroll: false }))
          }}
          className="mb-6 text-sm text-primary underline"
        >
          Xóa tìm kiếm và bộ lọc
        </button>
      )}

      <div className="grid gap-10 lg:grid-cols-[14rem_minmax(0,1fr)] lg:gap-8 xl:gap-12">
        <aside
          id="recipes-filters"
          className={`${filtersOpen ? 'grid' : 'hidden'} gap-8 border-y border-border py-6 sm:grid-cols-3 lg:sticky lg:top-24 lg:block lg:self-start lg:border-y-0 lg:py-0`}
          aria-label="Bộ lọc công thức"
        >
          <fieldset className="lg:mb-6">
            <legend className="w-full border-b border-border pb-3 font-serif text-xs uppercase tracking-widest text-muted-foreground lg:pb-2">
              Danh mục
            </legend>
            <div className="mt-3 flex flex-wrap gap-x-5 gap-y-1 lg:mt-2 lg:flex-col lg:items-start lg:gap-y-0">
              <button
                type="button"
                aria-pressed={!category}
                onClick={() => setFilter('category', '')}
                className={`min-h-11 py-1.5 text-sm transition-colors lg:min-h-0 lg:py-1 ${!category ? 'text-primary' : 'text-foreground hover:text-primary'}`}
              >
                Tất cả
              </button>
              {visibleCategories.map((item) => (
                <button
                  key={item.id}
                  type="button"
                  aria-pressed={category === item.slug}
                  onClick={() => selectCategory(item.slug)}
                  className={`min-h-11 py-1.5 text-left text-sm transition-colors lg:min-h-0 lg:py-1 ${category === item.slug ? 'text-primary' : 'text-foreground hover:text-primary'}`}
                >
                  {item.name}
                </button>
              ))}
              {categories.length > 4 && (
                <button
                  type="button"
                  onClick={() => setCategoryModalOpen(true)}
                  aria-haspopup="dialog"
                  className="mt-1 border-b border-primary py-1 text-left text-sm font-medium text-primary transition-colors hover:text-foreground lg:mt-0"
                >
                  Xem thêm danh mục
                </button>
              )}
            </div>
          </fieldset>

          <fieldset className="lg:mb-6">
            <legend className="w-full border-b border-border pb-3 font-serif text-xs uppercase tracking-widest text-muted-foreground lg:pb-2">
              Độ khó
            </legend>
            <div className="mt-3 flex flex-wrap gap-x-5 gap-y-1 lg:mt-2 lg:flex-col lg:items-start lg:gap-y-0">
              <button
                type="button"
                aria-pressed={!difficulty}
                onClick={() => setFilter('difficulty', '')}
                className={`min-h-11 py-1.5 text-sm transition-colors lg:min-h-0 lg:py-1 ${!difficulty ? 'text-primary' : 'text-foreground hover:text-primary'}`}
              >
                Tất cả
              </button>
              {difficulties.map((item) => (
                <button
                  key={item.value}
                  type="button"
                  aria-pressed={difficulty === item.value}
                  onClick={() => setFilter('difficulty', item.value)}
                  className={`min-h-11 py-1.5 text-left text-sm transition-colors lg:min-h-0 lg:py-1 ${difficulty === item.value ? 'text-primary' : 'text-foreground hover:text-primary'}`}
                >
                  {item.label}
                </button>
              ))}
            </div>
          </fieldset>

          <fieldset>
            <legend className="w-full border-b border-border pb-3 font-serif text-xs uppercase tracking-widest text-muted-foreground lg:pb-2">
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
              className="mt-5 w-full accent-primary lg:mt-3"
            />
            <div className="mt-1 flex justify-between text-xs text-muted-foreground">
              <span>10 phút</span>
              <span>{timeValue === MAX_TIME_SLIDER_VALUE ? '3+ giờ' : `${timeValue} phút`}</span>
            </div>
          </fieldset>
          <button
            type="button"
            disabled={isPending}
            onClick={() => {
              setFiltersOpen(false)
              resultsRef.current?.scrollIntoView?.({ block: 'start' })
            }}
            className="min-h-11 bg-primary px-4 py-2 text-sm text-primary-foreground sm:col-span-3 lg:hidden"
          >
            Xem {result.meta.total} kết quả
          </button>
        </aside>

        <section
          ref={resultsRef}
          tabIndex={-1}
          aria-busy={isPending}
          className="scroll-mt-24"
          aria-label="Danh sách công thức"
        >
          <div className="mb-6 flex flex-wrap items-center justify-between gap-4">
            <p
              className="hidden min-w-0 max-w-full break-words text-sm text-muted-foreground lg:block"
              aria-live="polite"
            >
              {searchSummary}
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
              {q && <option value="relevance">Phù hợp nhất</option>}
              <option value="newest">Mới nhất</option>
              <option value="quickest">Nấu nhanh nhất</option>
              <option value="az">Tên A–Z</option>
            </select>
          </div>

          <div className="sr-only" aria-live="polite" aria-atomic="true">
            {loadError
              ? 'Chưa tải được kết quả tìm kiếm.'
              : `Hiển thị ${recipes.length} trên tổng số ${result.meta.total} công thức, trang ${result.meta.page} trên ${result.meta.totalPages}`}
          </div>

          {loadError ? (
            <div role="alert" className="border border-border bg-card p-8 text-center">
              <h2 className="font-serif text-2xl">Chưa tải được kết quả</h2>
              <p className="mt-3 text-sm text-muted-foreground">
                Chưa lấy được dữ liệu. Bạn có thể thử lại hoặc điều chỉnh từ khóa, bộ lọc.
              </p>
              <button
                type="button"
                onClick={() => startTransition(() => router.refresh())}
                disabled={isPending}
                className="mt-5 bg-primary px-5 py-2.5 text-sm text-primary-foreground"
              >
                Thử lại
              </button>
            </div>
          ) : initialSearchError ? (
            <div className="border border-border bg-card px-6 py-12 text-center">
              <p className="text-muted-foreground">Điều chỉnh từ khóa để xem công thức phù hợp.</p>
              <button
                type="button"
                onClick={() => inputRef.current?.focus()}
                className="mt-4 border border-border px-4 py-2 text-sm"
              >
                Sửa từ khóa
              </button>
            </div>
          ) : recipes.length === 0 ? (
            <div role="status" className="border border-border bg-card px-6 py-12 text-center">
              <SearchIcon size={28} className="mx-auto mb-4 text-muted-foreground" aria-hidden="true" />
              <h2 className="font-serif text-2xl">
                {pageOutOfRange ? 'Trang này không còn kết quả.' : 'Không có công thức phù hợp.'}
              </h2>
              <p className="mt-3 text-sm text-muted-foreground">
                {pageOutOfRange
                  ? 'Về trang đầu để xem các công thức còn phù hợp.'
                  : (result.message ?? 'Thử tên món ngắn hơn, kiểm tra chính tả hoặc bỏ bớt bộ lọc.')}
              </p>
              <div className="mt-5 flex flex-wrap justify-center gap-3">
                <button
                  type="button"
                  onClick={() => inputRef.current?.focus()}
                  className="border border-border px-4 py-2 text-sm"
                >
                  Sửa từ khóa
                </button>
                <button
                  type="button"
                  onClick={() =>
                    pageOutOfRange
                      ? setFilter('page', '1')
                      : startTransition(() =>
                          setSearchParams(
                            hasActiveFilters && q ? new URLSearchParams({ q }) : new URLSearchParams(),
                            { replace: false, scroll: false },
                          ),
                        )
                  }
                  className="bg-primary px-4 py-2 text-sm text-primary-foreground"
                >
                  {pageOutOfRange ? 'Về trang đầu' : hasActiveFilters ? 'Bỏ bộ lọc' : 'Xem tất cả công thức'}
                </button>
              </div>
            </div>
          ) : (
            <div className="grid grid-cols-1 gap-x-6 gap-y-12 md:grid-cols-2 xl:grid-cols-3">
              {recipes.map((recipe) => (
                <RecipeCard key={recipe.id} recipe={recipe} searchQuery={q} />
              ))}
            </div>
          )}

          {result.meta.totalPages > 1 && !pageOutOfRange && (
            <nav className="mt-16 flex flex-wrap justify-center gap-2" aria-label="Phân trang công thức">
              <button
                type="button"
                disabled={page <= 1 || isPending}
                aria-label="Trang trước"
                onClick={() => setFilter('page', String(page - 1))}
                className="min-h-11 border border-border px-3 text-sm disabled:opacity-40"
              >
                Trước
              </button>
              {visiblePages.map((pageNumber, index) => (
                <span key={pageNumber} className="flex items-center gap-2">
                  {index > 0 && pageNumber - visiblePages[index - 1] > 1 && (
                    <span aria-hidden="true" className="px-1">
                      …
                    </span>
                  )}
                  <button
                    type="button"
                    aria-label={`Trang ${pageNumber}`}
                    aria-current={pageNumber === page ? 'page' : undefined}
                    disabled={isPending}
                    onClick={() => setFilter('page', String(pageNumber))}
                    className={`min-h-11 min-w-11 border px-2 font-serif text-lg ${pageNumber === page ? 'border-foreground bg-foreground text-background' : 'border-border text-muted-foreground hover:border-foreground'}`}
                  >
                    {pageNumber}
                  </button>
                </span>
              ))}
              <button
                type="button"
                disabled={page >= result.meta.totalPages || isPending}
                aria-label="Trang sau"
                onClick={() => setFilter('page', String(page + 1))}
                className="min-h-11 border border-border px-3 text-sm disabled:opacity-40"
              >
                Sau
              </button>
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
