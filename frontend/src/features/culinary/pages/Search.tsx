'use client'

import { useQuery } from '@tanstack/react-query'
import { Search as SearchIcon, X } from 'lucide-react'
import { useEffect, useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router'
import { RecipeCard } from '@/features/culinary/components/RecipeCard'
import { listCategories, listPublishedRecipes } from '@/lib/api/content-client'

const PAGE_SIZE = 9

function normalizeText(value: string) {
  return value
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/đ/g, 'd')
    .toLowerCase()
}

export function Search() {
  const [searchParams, setSearchParams] = useSearchParams()
  const q = searchParams.get('q') ?? ''
  const category = searchParams.get('category') ?? ''
  const difficulty = searchParams.get('difficulty') ?? ''
  const maxTime = Number.parseInt(searchParams.get('maxTime') ?? '0', 10)
  const sort = searchParams.get('sort') ?? 'newest'
  const requestedPage = Number.parseInt(searchParams.get('page') ?? '1', 10)
  const page = Number.isFinite(requestedPage) && requestedPage > 0 ? requestedPage : 1
  const [inputValue, setInputValue] = useState(q)
  const categoriesQuery = useQuery({ queryKey: ['categories'], queryFn: listCategories })
  const recipesQuery = useQuery({
    queryKey: ['public-recipes', 'search-source'],
    queryFn: () => listPublishedRecipes(1, 50),
  })

  useEffect(() => setInputValue(q), [q])

  const setFilter = (key: string, value: string) => {
    const next = new URLSearchParams(searchParams)
    if (value) next.set(key, value)
    else next.delete(key)
    if (key !== 'page') next.delete('page')
    setSearchParams(next)
  }

  const submitSearch = (event: FormEvent) => {
    event.preventDefault()
    setFilter('q', inputValue.trim())
  }

  const query = normalizeText(q)
  const results = [...(recipesQuery.data?.data ?? [])]
    .filter((recipe) => {
      const searchable = normalizeText(
        `${recipe.title} ${recipe.description} ${recipe.category.name} ${recipe.author.displayName}`,
      )
      return (
        (!query || searchable.includes(query)) &&
        (!category || recipe.category.slug === category) &&
        (!difficulty || recipe.difficulty === difficulty) &&
        (!maxTime || recipe.prepTime + recipe.cookTime <= maxTime)
      )
    })
    .sort((left, right) => {
      if (sort === 'quickest') return left.prepTime + left.cookTime - (right.prepTime + right.cookTime)
      if (sort === 'az') return left.title.localeCompare(right.title, 'vi')
      return (
        new Date(right.publishedAt ?? right.createdAt).getTime() -
        new Date(left.publishedAt ?? left.createdAt).getTime()
      )
    })
  const totalPages = Math.ceil(results.length / PAGE_SIZE)
  const paginated = results.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE)
  const hasFilters = Boolean(q || category || difficulty || maxTime)

  return (
    <div className="mx-auto max-w-7xl px-4 py-16 sm:px-6 lg:px-8">
      <header className="mb-10">
        <h1 className="mb-6 font-serif text-4xl text-foreground md:text-5xl">Tìm kiếm công thức</h1>
        <form onSubmit={submitSearch} className="flex max-w-2xl gap-3" role="search">
          <label htmlFor="recipe-search" className="sr-only">
            Từ khóa tìm kiếm
          </label>
          <input
            id="recipe-search"
            type="search"
            value={inputValue}
            onChange={(event) => setInputValue(event.target.value)}
            className="min-w-0 flex-1 border border-border bg-background px-4 py-3 focus:border-primary focus:outline-none"
            placeholder="Tên món, mô tả, danh mục hoặc tác giả"
          />
          <button
            type="submit"
            className="inline-flex items-center gap-2 bg-primary px-6 py-3 text-primary-foreground"
          >
            <SearchIcon size={17} aria-hidden="true" /> Tìm
          </button>
        </form>
      </header>

      <section className="mb-10 grid gap-4 border-y border-border py-6 sm:grid-cols-3" aria-label="Bộ lọc">
        <label className="text-sm">
          <span className="mb-2 block text-muted-foreground">Danh mục</span>
          <select
            value={category}
            onChange={(event) => setFilter('category', event.target.value)}
            className="w-full border border-border bg-background px-3 py-2"
          >
            <option value="">Tất cả</option>
            {(categoriesQuery.data ?? []).map((item) => (
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
            <option value="">Tất cả</option>
            <option value="easy">Dễ</option>
            <option value="medium">Trung bình</option>
            <option value="hard">Nâng cao</option>
            <option value="expert">Chuyên gia</option>
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
            <option value="az">Theo tên (A–Z)</option>
          </select>
        </label>
      </section>

      <div className="mb-6 flex items-center justify-between gap-4">
        <p className="text-sm text-muted-foreground">
          <span className="font-medium text-foreground">{results.length}</span> công thức phù hợp
        </p>
        {hasFilters && (
          <button
            type="button"
            onClick={() => setSearchParams({})}
            className="flex items-center gap-1.5 text-sm text-primary"
          >
            <X size={14} aria-hidden="true" /> Xóa bộ lọc
          </button>
        )}
      </div>

      {recipesQuery.isPending ? (
        <p role="status" className="py-20 text-center text-muted-foreground">
          Đang tìm công thức…
        </p>
      ) : recipesQuery.isError ? (
        <p role="alert" className="py-20 text-center text-red-600">
          Không thể tải dữ liệu tìm kiếm.
        </p>
      ) : paginated.length === 0 ? (
        <div className="py-24 text-center">
          <SearchIcon size={40} aria-hidden="true" className="mx-auto mb-4 text-muted-foreground" />
          <p className="font-serif text-xl">Không tìm thấy công thức phù hợp</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-x-6 gap-y-12 sm:grid-cols-2 xl:grid-cols-3">
          {paginated.map((recipe) => (
            <RecipeCard key={recipe.id} recipe={recipe} />
          ))}
        </div>
      )}

      {totalPages > 1 && (
        <nav className="mt-16 flex justify-center gap-2" aria-label="Phân trang kết quả tìm kiếm">
          {Array.from({ length: totalPages }, (_, index) => index + 1).map((pageNumber) => (
            <button
              key={pageNumber}
              type="button"
              aria-current={pageNumber === page ? 'page' : undefined}
              onClick={() => setFilter('page', String(pageNumber))}
              className={`h-10 w-10 border ${pageNumber === page ? 'bg-foreground text-background' : ''}`}
            >
              {pageNumber}
            </button>
          ))}
        </nav>
      )}
    </div>
  )
}
