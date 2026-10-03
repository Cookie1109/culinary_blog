import { ChefHat, Clock, Timer, Users } from 'lucide-react'
import Image from 'next/image'
import { Link } from 'react-router'
import type { Nutrition, Recipe } from '@/lib/api/content-client'
import { privateMediaUrl, publicMediaUrl } from '@/lib/media-url'

const IMAGE_PLACEHOLDER =
  'data:image/svg+xml,%3Csvg xmlns="http://www.w3.org/2000/svg" width="16" height="9"%3E%3Crect width="16" height="9" fill="%23eee9e2"/%3E%3C/svg%3E'

const DIFFICULTY_LABELS: Record<Recipe['difficulty'], string> = {
  easy: 'Dễ',
  medium: 'Trung bình',
  hard: 'Nâng cao',
  expert: 'Chuyên gia',
}

const NUTRITION_LABELS: Array<[keyof Nutrition, string, string]> = [
  ['calories', 'Năng lượng', 'kcal'],
  ['protein', 'Protein', 'g'],
  ['carbohydrates', 'Carbohydrate', 'g'],
  ['fat', 'Chất béo', 'g'],
  ['fiber', 'Chất xơ', 'g'],
  ['sodium', 'Natri', 'mg'],
]

export function RecipeDetail({
  recipe,
  privatePreview = false,
}: {
  recipe: Recipe
  privatePreview?: boolean
}) {
  const ingredients = [...recipe.ingredients].sort((left, right) => left.orderIndex - right.orderIndex)
  const steps = [...recipe.steps].sort((left, right) => left.stepNumber - right.stepNumber)
  const images = recipe.images
    .filter((image) => image.processingStatus === 'ready')
    .sort(
      (left, right) => Number(right.isPrimary) - Number(left.isPrimary) || left.orderIndex - right.orderIndex,
    )
  const heroImage = recipe.primaryImageUrl ?? images[0]?.mediumUrl ?? images[0]?.originalUrl
  const imageSource = privatePreview ? privateMediaUrl : publicMediaUrl
  const nutrition = recipe.nutrition
    ? NUTRITION_LABELS.filter(([key]) => recipe.nutrition?.[key] != null)
    : []

  return (
    <article className="pb-24">
      <header className="mx-auto max-w-4xl px-4 pb-12 pt-12 text-center sm:px-6 lg:px-8">
        <nav aria-label="Đường dẫn" className="mb-8 text-sm text-muted-foreground">
          <Link to="/recipes" className="hover:text-primary">
            Công thức
          </Link>
          <span className="px-2" aria-hidden="true">
            /
          </span>
          <Link to={`/categories/${recipe.category.slug}`} className="hover:text-primary">
            {recipe.category.name}
          </Link>
        </nav>
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
        <div className="flex flex-wrap items-center justify-center gap-3 border-y border-border py-4 text-sm text-muted-foreground sm:gap-6">
          <span className="font-medium text-foreground">Bởi {recipe.author.displayName}</span>
          <span aria-hidden="true">&bull;</span>
          <time dateTime={recipe.publishedAt ?? recipe.createdAt}>
            {new Date(recipe.publishedAt ?? recipe.createdAt).toLocaleDateString('vi-VN')}
          </time>
        </div>
      </header>

      <div className="mx-auto mb-16 max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="relative aspect-video w-full overflow-hidden bg-muted">
          {heroImage ? (
            <Image
              src={imageSource(heroImage)}
              alt={recipe.title}
              fill
              priority
              sizes="(max-width: 1280px) 100vw, 1280px"
              placeholder="blur"
              blurDataURL={IMAGE_PLACEHOLDER}
              unoptimized={privatePreview}
              className="object-cover"
            />
          ) : (
            <div className="grid h-full place-items-center px-6 text-center font-serif text-3xl text-muted-foreground">
              {recipe.title}
            </div>
          )}
        </div>
      </div>

      <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
        <dl className="mb-16 grid grid-cols-2 border border-border bg-secondary sm:grid-cols-4">
          <div className="border-b border-r border-border p-5 sm:border-b-0">
            <dt className="mb-2 flex items-center gap-2 text-xs uppercase tracking-wider text-muted-foreground">
              <Clock size={15} aria-hidden="true" /> Chuẩn bị
            </dt>
            <dd className="font-serif text-xl">{recipe.prepTime} phút</dd>
          </div>
          <div className="border-b border-border p-5 sm:border-b-0 sm:border-r">
            <dt className="mb-2 flex items-center gap-2 text-xs uppercase tracking-wider text-muted-foreground">
              <ChefHat size={15} aria-hidden="true" /> Nấu
            </dt>
            <dd className="font-serif text-xl">{recipe.cookTime} phút</dd>
          </div>
          <div className="border-r border-border p-5">
            <dt className="mb-2 flex items-center gap-2 text-xs uppercase tracking-wider text-muted-foreground">
              <Users size={15} aria-hidden="true" /> Khẩu phần
            </dt>
            <dd className="font-serif text-xl">{recipe.servings}</dd>
          </div>
          <div className="p-5">
            <dt className="mb-2 text-xs uppercase tracking-wider text-muted-foreground">Độ khó</dt>
            <dd className="font-serif text-xl">{DIFFICULTY_LABELS[recipe.difficulty]}</dd>
          </div>
        </dl>

        <div className="grid grid-cols-1 gap-12 lg:grid-cols-12">
          <section className="lg:col-span-5" aria-labelledby="ingredients-title">
            <h2 id="ingredients-title" className="mb-6 border-b border-border pb-3 font-serif text-3xl">
              Nguyên liệu
            </h2>
            {ingredients.length === 0 ? (
              <p className="text-muted-foreground">Tác giả chưa bổ sung nguyên liệu.</p>
            ) : (
              <ul className="divide-y divide-border">
                {ingredients.map((ingredient) => (
                  <li key={ingredient.id} className="flex items-start justify-between gap-5 py-4">
                    <div>
                      <span className="font-medium">{ingredient.name}</span>
                      {ingredient.notes && (
                        <span className="mt-1 block text-sm text-muted-foreground">{ingredient.notes}</span>
                      )}
                    </div>
                    {(ingredient.quantity != null || ingredient.unit) && (
                      <span className="shrink-0 text-muted-foreground">
                        {ingredient.quantity != null
                          ? new Intl.NumberFormat('vi-VN').format(ingredient.quantity)
                          : ''}{' '}
                        {ingredient.unit}
                      </span>
                    )}
                  </li>
                ))}
              </ul>
            )}
          </section>

          <section className="lg:col-span-7" aria-labelledby="nutrition-title">
            <h2 id="nutrition-title" className="mb-6 border-b border-border pb-3 font-serif text-3xl">
              Dinh dưỡng mỗi khẩu phần
            </h2>
            {nutrition.length === 0 ? (
              <p className="text-muted-foreground">Chưa có thông tin dinh dưỡng.</p>
            ) : (
              <dl className="grid grid-cols-2 gap-px overflow-hidden border border-border bg-border sm:grid-cols-3">
                {nutrition.map(([key, label, unit]) => (
                  <div key={key} className="bg-card p-5">
                    <dt className="text-sm text-muted-foreground">{label}</dt>
                    <dd className="mt-1 font-serif text-2xl">
                      {recipe.nutrition?.[key]} <span className="text-sm">{unit}</span>
                    </dd>
                  </div>
                ))}
              </dl>
            )}
          </section>
        </div>

        <section className="mt-20" aria-labelledby="steps-title">
          <h2 id="steps-title" className="mb-8 border-b border-border pb-3 font-serif text-3xl">
            Các bước thực hiện
          </h2>
          {recipe.instructions && (
            <p className="mb-10 whitespace-pre-line text-lg leading-relaxed text-muted-foreground">
              {recipe.instructions}
            </p>
          )}
          {steps.length === 0 ? (
            <p className="text-muted-foreground">Tác giả chưa bổ sung các bước thực hiện.</p>
          ) : (
            <ol className="space-y-10">
              {steps.map((step) => (
                <li key={step.id} className="grid gap-5 sm:grid-cols-[4rem_1fr]">
                  <span
                    className="grid h-12 w-12 place-items-center rounded-full bg-primary font-serif text-xl text-primary-foreground"
                    aria-hidden="true"
                  >
                    {step.stepNumber}
                  </span>
                  <div>
                    <div className="mb-3 flex flex-wrap items-center justify-between gap-3">
                      <h3 className="font-serif text-2xl">{step.title}</h3>
                      {step.timerMinutes != null && (
                        <span className="inline-flex items-center gap-1.5 text-sm text-muted-foreground">
                          <Timer size={15} aria-hidden="true" /> {step.timerMinutes} phút
                        </span>
                      )}
                    </div>
                    <p className="whitespace-pre-line leading-relaxed text-muted-foreground">
                      {step.description}
                    </p>
                    {step.imageUrl && (
                      <div className="relative mt-5 aspect-video w-full overflow-hidden bg-muted">
                        <Image
                          src={imageSource(step.imageUrl)}
                          alt={`Bước ${step.stepNumber}: ${step.title}`}
                          fill
                          sizes="(max-width: 1024px) 100vw, 768px"
                          placeholder="blur"
                          blurDataURL={IMAGE_PLACEHOLDER}
                          unoptimized={privatePreview}
                          className="object-cover"
                        />
                      </div>
                    )}
                  </div>
                </li>
              ))}
            </ol>
          )}
        </section>

        {images.length > 1 && (
          <section className="mt-20" aria-labelledby="gallery-title">
            <h2 id="gallery-title" className="mb-8 border-b border-border pb-3 font-serif text-3xl">
              Thư viện ảnh
            </h2>
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {images.map((image) => (
                <div key={image.id} className="relative aspect-square overflow-hidden bg-muted">
                  <Image
                    src={imageSource(image.mediumUrl ?? image.originalUrl)}
                    alt={image.altText ?? `${recipe.title} — ảnh món ăn`}
                    fill
                    sizes="(max-width: 639px) 100vw, (max-width: 1023px) 50vw, 33vw"
                    placeholder="blur"
                    blurDataURL={IMAGE_PLACEHOLDER}
                    unoptimized={privatePreview}
                    className="object-cover"
                  />
                </div>
              ))}
            </div>
          </section>
        )}
      </div>
    </article>
  )
}
