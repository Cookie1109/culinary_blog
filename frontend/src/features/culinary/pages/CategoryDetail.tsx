import { ArrowLeft } from 'lucide-react'
import { Link } from 'react-router'
import { RecipeCard } from '@/features/culinary/components/RecipeCard'
import type { CategoryDetailEnvelope } from '@/lib/api/content-client'

export function CategoryDetail({ result }: { result: CategoryDetailEnvelope }) {
  const { category, recipes } = result.data
  const { page, total, totalPages } = result.meta

  return (
    <div className="mx-auto max-w-7xl px-4 py-16 sm:px-6 lg:px-8">
      <Link
        to="/categories"
        className="mb-8 inline-flex items-center gap-2 text-sm text-muted-foreground transition-colors hover:text-primary"
      >
        <ArrowLeft size={16} aria-hidden="true" /> Tất cả danh mục
      </Link>

      <header className="mb-14 border-b border-border pb-10">
        <p className="mb-3 text-sm uppercase tracking-widest text-primary">Danh mục</p>
        <h1 className="mb-4 font-serif text-4xl text-foreground md:text-5xl">{category.name}</h1>
        <p className="max-w-2xl text-lg leading-relaxed text-muted-foreground">
          {category.description ?? 'Khám phá các công thức đã xuất bản trong danh mục này.'}
        </p>
        <p className="mt-5 text-sm text-muted-foreground">{total} công thức</p>
      </header>

      {recipes.length === 0 ? (
        <div className="py-20 text-center">
          <h2 className="mb-3 font-serif text-2xl">Chưa có công thức</h2>
          <p className="text-muted-foreground">Danh mục này chưa có công thức nào được xuất bản.</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-x-8 gap-y-16 md:grid-cols-2 lg:grid-cols-3">
          {recipes.map((recipe) => (
            <RecipeCard key={recipe.id} recipe={recipe} />
          ))}
        </div>
      )}

      {totalPages > 1 && (
        <nav className="mt-20 flex flex-wrap justify-center gap-2" aria-label="Phân trang danh mục">
          {Array.from({ length: totalPages }, (_, index) => index + 1).map((pageNumber) => (
            <Link
              key={pageNumber}
              to={
                pageNumber === 1
                  ? `/categories/${category.slug}`
                  : `/categories/${category.slug}?page=${pageNumber}`
              }
              aria-current={pageNumber === page ? 'page' : undefined}
              className={`grid h-10 w-10 place-items-center border font-serif text-lg ${
                pageNumber === page
                  ? 'border-foreground bg-foreground text-background'
                  : 'border-border text-muted-foreground hover:border-foreground'
              }`}
            >
              {pageNumber}
            </Link>
          ))}
        </nav>
      )}
    </div>
  )
}
