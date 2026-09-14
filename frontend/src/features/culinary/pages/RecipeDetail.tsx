import { ChefHat, Clock, Users } from 'lucide-react'
import type { Recipe } from '@/lib/api/content-client'

const DIFFICULTY_LABELS: Record<Recipe['difficulty'], string> = {
  easy: 'Dễ',
  medium: 'Trung bình',
  hard: 'Nâng cao',
  expert: 'Chuyên gia',
}

export function RecipeDetail({ recipe }: { recipe: Recipe }) {
  return (
    <article className="pb-24">
      <header className="mx-auto max-w-4xl px-4 pb-12 pt-16 text-center sm:px-6 lg:px-8">
        <div className="mb-6 flex items-center justify-center gap-2 text-sm uppercase tracking-widest text-primary">
          <span>{recipe.category.name}</span>
          <span aria-hidden="true">&bull;</span>
          <span>{DIFFICULTY_LABELS[recipe.difficulty]}</span>
        </div>
        <h1 className="mb-6 font-serif text-4xl leading-[1.1] text-foreground sm:text-5xl lg:text-6xl">
          {recipe.title}
        </h1>
        <p className="mx-auto mb-8 max-w-2xl text-lg leading-relaxed text-muted-foreground">
          {recipe.description}
        </p>
        <div className="flex flex-wrap items-center justify-center gap-6 border-y border-border py-4 text-sm text-muted-foreground">
          <span className="font-medium text-foreground">{recipe.author.displayName}</span>
          <span aria-hidden="true">&bull;</span>
          <time dateTime={recipe.publishedAt ?? recipe.createdAt}>
            {new Date(recipe.publishedAt ?? recipe.createdAt).toLocaleDateString('vi-VN')}
          </time>
        </div>
      </header>

      {recipe.primaryImageUrl && (
        <div className="mx-auto mb-16 max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="aspect-video w-full overflow-hidden bg-muted">
            <img src={recipe.primaryImageUrl} alt={recipe.title} className="h-full w-full object-cover" />
          </div>
        </div>
      )}

      <div className="mx-auto grid max-w-5xl grid-cols-1 gap-12 px-4 sm:px-6 lg:grid-cols-12 lg:px-8">
        <aside className="lg:col-span-4" aria-label="Thông tin công thức">
          <dl className="space-y-6 bg-secondary p-8">
            <div className="flex items-center justify-between gap-4 border-b border-border/50 pb-4">
              <dt className="flex items-center gap-2 text-sm uppercase tracking-wider text-muted-foreground">
                <Clock size={16} aria-hidden="true" /> Chuẩn bị
              </dt>
              <dd className="font-serif text-lg">{recipe.prepTime} phút</dd>
            </div>
            <div className="flex items-center justify-between gap-4 border-b border-border/50 pb-4">
              <dt className="flex items-center gap-2 text-sm uppercase tracking-wider text-muted-foreground">
                <ChefHat size={16} aria-hidden="true" /> Nấu
              </dt>
              <dd className="font-serif text-lg">{recipe.cookTime} phút</dd>
            </div>
            <div className="flex items-center justify-between gap-4">
              <dt className="flex items-center gap-2 text-sm uppercase tracking-wider text-muted-foreground">
                <Users size={16} aria-hidden="true" /> Khẩu phần
              </dt>
              <dd className="font-serif text-lg">{recipe.servings}</dd>
            </div>
          </dl>
        </aside>

        <section className="lg:col-span-8" aria-labelledby="instructions-title">
          <h2 id="instructions-title" className="mb-8 border-b border-border pb-2 font-serif text-3xl">
            Hướng dẫn
          </h2>
          {recipe.instructions ? (
            <p className="whitespace-pre-line text-lg leading-relaxed text-foreground">
              {recipe.instructions}
            </p>
          ) : (
            <p className="text-muted-foreground">Tác giả chưa bổ sung hướng dẫn chi tiết.</p>
          )}
        </section>
      </div>
    </article>
  )
}
