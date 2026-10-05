import { CategoryCard } from '@/features/culinary/components/CategoryCard'
import type { Category } from '@/lib/api/content-client'

export function Categories({ categories }: { categories: Category[] }) {
  return (
    <div className="mx-auto max-w-7xl px-4 py-16 sm:px-6 lg:px-8">
      <header className="mb-12">
        <h1 className="mb-3 font-serif text-3xl sm:text-4xl text-foreground lg:text-5xl">Danh mục món ăn</h1>
        <p className="text-base text-muted-foreground">Khám phá các công thức theo chủ đề ẩm thực.</p>
      </header>

      {categories.length === 0 ? (
        <p className="py-20 text-center text-muted-foreground">Chưa có danh mục nào.</p>
      ) : (
        <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-4">
          {categories.map((category) => (
            <CategoryCard key={category.id} category={category} />
          ))}
        </div>
      )}
    </div>
  )
}
