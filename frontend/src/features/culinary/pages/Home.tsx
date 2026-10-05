'use client'

import { useEffect, useRef, useState } from 'react'
import { ArrowRight, ChevronLeft, ChevronRight, LoaderCircle } from 'lucide-react'
import Image from 'next/image'
import { Link } from 'react-router'
import { CategoryCard } from '@/features/culinary/components/CategoryCard'
import { RecipeCard } from '@/features/culinary/components/RecipeCard'
import { listPublishedRecipes, type Category, type PageEnvelope, type Recipe } from '@/lib/api/content-client'
import { publicMediaUrl } from '@/lib/media-url'

const IMAGE_PLACEHOLDER =
  'data:image/svg+xml,%3Csvg xmlns="http://www.w3.org/2000/svg" width="16" height="12"%3E%3Crect width="16" height="12" fill="%23eee9e2"/%3E%3C/svg%3E'

function recipeImage(recipe: Recipe) {
  const primaryImage = recipe.images.find((image) => image.isPrimary)
  return primaryImage?.originalUrl ?? primaryImage?.mediumUrl ?? recipe.primaryImageUrl
}

function scrollCarousel(element: HTMLDivElement | null, direction: -1 | 1) {
  if (!element) return

  const maxScroll = element.scrollWidth - element.clientWidth
  const atStart = element.scrollLeft <= 4
  const atEnd = element.scrollLeft >= maxScroll - 4
  const left =
    direction === 1 && atEnd
      ? 0
      : direction === -1 && atStart
        ? maxScroll
        : element.scrollLeft + direction * element.clientWidth

  element.scrollTo({ left, behavior: 'smooth' })
}

export function Home({
  recipePage,
  categories,
}: {
  recipePage: PageEnvelope<Recipe>
  categories: Category[]
}) {
  const latestRecipes = recipePage.data.slice(0, 5)
  const [recipes, setRecipes] = useState(recipePage.data)
  const [loadedPage, setLoadedPage] = useState(recipePage.meta.page)
  const [totalPages, setTotalPages] = useState(recipePage.meta.totalPages)
  const [loadingMoreRecipes, setLoadingMoreRecipes] = useState(false)
  const [collectionError, setCollectionError] = useState(false)
  const [openingRecipeSlug, setOpeningRecipeSlug] = useState<string | null>(null)
  const [currentRecipeIndex, setCurrentRecipeIndex] = useState(0)
  const [touchStartX, setTouchStartX] = useState<number | null>(null)
  const [dragOffset, setDragOffset] = useState(0)
  const [isDragging, setIsDragging] = useState(false)
  const collectionRef = useRef<HTMLDivElement>(null)
  const categoriesRef = useRef<HTMLDivElement>(null)
  const loadingMoreRecipesRef = useRef(false)
  const featured = latestRecipes[currentRecipeIndex]
  const featuredImage = featured ? recipeImage(featured) : null
  void featuredImage

  const dragDistanceThreshold = 50

  useEffect(() => {
    if (latestRecipes.length <= 1 || isDragging) return

    const timer = window.setInterval(() => {
      setCurrentRecipeIndex((current) => (current + 1) % latestRecipes.length)
    }, 15_000)

    return () => window.clearInterval(timer)
  }, [latestRecipes.length, isDragging])

  const showPreviousRecipe = () => {
    setCurrentRecipeIndex((current) => (current === 0 ? latestRecipes.length - 1 : current - 1))
  }

  const showNextRecipe = () => {
    setCurrentRecipeIndex((current) => (current + 1) % latestRecipes.length)
  }

  const handleTouchStart = (e: React.TouchEvent) => {
    setTouchStartX(e.touches[0].clientX)
    setIsDragging(true)
    setDragOffset(0)
  }

  const handleTouchMove = (e: React.TouchEvent) => {
    if (touchStartX === null) return
    setDragOffset(e.touches[0].clientX - touchStartX)
  }

  const handleTouchEnd = () => {
    if (touchStartX === null) return
    if (dragOffset < -dragDistanceThreshold) {
      showNextRecipe()
    } else if (dragOffset > dragDistanceThreshold) {
      showPreviousRecipe()
    }
    setTouchStartX(null)
    setDragOffset(0)
    setIsDragging(false)
  }

  const handleMouseDown = (e: React.MouseEvent) => {
    if (e.button !== 0) return
    if ((e.target as HTMLElement).closest('a, button')) return
    setTouchStartX(e.clientX)
    setIsDragging(true)
    setDragOffset(0)
  }

  const handleMouseMove = (e: React.MouseEvent) => {
    if (!isDragging || touchStartX === null) return
    setDragOffset(e.clientX - touchStartX)
  }

  const handleMouseUp = () => {
    if (!isDragging || touchStartX === null) return
    if (dragOffset < -dragDistanceThreshold) {
      showNextRecipe()
    } else if (dragOffset > dragDistanceThreshold) {
      showPreviousRecipe()
    }
    setTouchStartX(null)
    setDragOffset(0)
    setIsDragging(false)
  }

  const handleMouseLeave = () => {
    if (isDragging) {
      handleMouseUp()
    }
  }

  const showNextCollectionRecipes = async () => {
    const carousel = collectionRef.current
    if (!carousel) return

    const atEnd = carousel.scrollLeft >= carousel.scrollWidth - carousel.clientWidth - 4
    if (!atEnd || loadedPage >= totalPages) {
      scrollCarousel(carousel, 1)
      return
    }

    if (loadingMoreRecipesRef.current) return
    loadingMoreRecipesRef.current = true
    setLoadingMoreRecipes(true)
    setCollectionError(false)

    try {
      const nextPage = await listPublishedRecipes(loadedPage + 1, recipePage.meta.pageSize)
      setRecipes((current) => {
        const knownIds = new Set(current.map((recipe) => recipe.id))
        return [...current, ...nextPage.data.filter((recipe) => !knownIds.has(recipe.id))]
      })
      setLoadedPage(nextPage.meta.page)
      setTotalPages(nextPage.meta.totalPages)
      window.requestAnimationFrame(() => {
        window.requestAnimationFrame(() => scrollCarousel(collectionRef.current, 1))
      })
    } catch {
      setCollectionError(true)
    } finally {
      loadingMoreRecipesRef.current = false
      setLoadingMoreRecipes(false)
    }
  }

  return (
    <div className="pb-24">
      {/* Hero Carousel: Full Width Sliding Track with Gesture Support */}
      <section
        className="relative w-full overflow-hidden bg-stone-950 text-white min-h-[520px] sm:min-h-[580px] lg:min-h-[640px] flex items-center select-none cursor-grab active:cursor-grabbing touch-pan-y"
        aria-label="Công thức mới nhất"
        aria-roledescription="carousel"
        onTouchStart={handleTouchStart}
        onTouchMove={handleTouchMove}
        onTouchEnd={handleTouchEnd}
        onTouchCancel={handleTouchEnd}
        onMouseDown={handleMouseDown}
        onMouseMove={handleMouseMove}
        onMouseUp={handleMouseUp}
        onMouseLeave={handleMouseLeave}
      >
        {/* Sliding Track for horizontal swipe effect */}
        <div
          id="latest-recipes-carousel"
          className="flex w-full h-[520px] sm:h-[580px] lg:h-[640px]"
          style={{
            transform: `translateX(calc(-${currentRecipeIndex * 100}% + ${dragOffset}px))`,
            transition: isDragging ? 'none' : 'transform 700ms cubic-bezier(0.2, 0.8, 0.2, 1)',
          }}
        >
          {latestRecipes.map((recipe, index) => {
            const isActive = index === currentRecipeIndex
            const image = recipeImage(recipe)
            return (
              <div
                key={recipe.id}
                className="relative min-w-full w-full h-full shrink-0 overflow-hidden"
                aria-hidden={!isActive}
              >
                {/* Full-width background image */}
                {image ? (
                  <Image
                    src={`${publicMediaUrl(image)}?v=${recipe.version}`}
                    alt={recipe.title}
                    fill
                    priority={index === 0}
                    loading={index === 0 ? 'eager' : 'lazy'}
                    quality={90}
                    sizes="100vw"
                    placeholder="blur"
                    blurDataURL={IMAGE_PLACEHOLDER}
                    draggable={false}
                    className="object-cover select-none pointer-events-none"
                  />
                ) : (
                  <div className="flex h-full w-full items-center justify-center bg-stone-900 font-serif text-3xl text-stone-500">
                    {recipe.title}
                  </div>
                )}

                {/* Reduced Opacity Gradient Overlay for clearer and brighter dish visuals */}
                <div className="absolute inset-0 bg-gradient-to-t from-black/65 via-black/30 to-black/5 lg:bg-gradient-to-r lg:from-black/70 lg:via-black/25 lg:to-transparent pointer-events-none" />

                {/* Text overlay content without frame/box on subtitle */}
                <div className="relative z-10 mx-auto flex h-full max-w-7xl flex-col justify-end lg:justify-center px-4 py-16 sm:px-6 lg:px-8 pointer-events-none">
                  <div className="max-w-2xl">
                    <span className="mb-3 block text-sm font-semibold uppercase tracking-widest text-white drop-shadow-[0_1px_2px_rgba(0,0,0,0.8)]">
                      Công thức mới nhất
                    </span>
                    <h1 className="mb-4 font-serif text-3xl sm:text-5xl leading-[1.1] text-white lg:text-7xl break-words drop-shadow-[0_2px_4px_rgba(0,0,0,0.7)]">
                      {recipe.title}
                    </h1>
                    <p className="mb-8 max-w-xl text-base sm:text-lg text-stone-200 line-clamp-3 leading-relaxed drop-shadow-[0_1px_3px_rgba(0,0,0,0.8)]">
                      {recipe.description}
                    </p>
                    <div className="pointer-events-auto">
                      <Link
                        to={`/recipes/${recipe.slug}`}
                        aria-label={`Khám phá công thức: ${recipe.title}`}
                        onClick={(e) => {
                          if (Math.abs(dragOffset) > 5) e.preventDefault()
                          else setOpeningRecipeSlug(recipe.slug)
                        }}
                        className="inline-flex items-center gap-2.5 bg-primary px-7 py-3.5 text-sm font-semibold uppercase tracking-wider text-white shadow-lg shadow-primary/30 transition-all duration-200 hover:bg-primary/90 hover:gap-3.5 hover:shadow-xl active:scale-95 cursor-pointer"
                      >
                        {openingRecipeSlug === recipe.slug ? (
                          <>
                            <LoaderCircle size={18} className="animate-spin" aria-hidden="true" />
                            Đang mở
                          </>
                        ) : (
                          <>
                            Khám phá công thức
                            <ArrowRight size={18} aria-hidden="true" />
                          </>
                        )}
                      </Link>
                    </div>
                  </div>
                </div>
              </div>
            )
          })}
        </div>

        {/* Screen-reader only accessible controls for assistive devices */}
        {latestRecipes.length > 1 && (
          <div className="sr-only">
            <button
              type="button"
              onClick={showPreviousRecipe}
              aria-label="Xem công thức mới trước đó"
              aria-controls="latest-recipes-carousel"
            >
              Trước
            </button>
            <button
              type="button"
              onClick={showNextRecipe}
              aria-label="Xem công thức mới tiếp theo"
              aria-controls="latest-recipes-carousel"
            >
              Tiếp
            </button>
          </div>
        )}

        {/* 5 Dots Indicator: Clean floating dots without outer frame */}
        {latestRecipes.length > 1 && (
          <div
            className="absolute bottom-6 sm:bottom-8 left-1/2 z-20 flex -translate-x-1/2 items-center gap-3"
            role="tablist"
            aria-label="Chọn công thức mới nhất"
          >
            {latestRecipes.map((recipe, index) => {
              const isActive = index === currentRecipeIndex
              return (
                <button
                  key={recipe.id}
                  type="button"
                  role="tab"
                  aria-selected={isActive}
                  aria-label={`Chuyển đến công thức ${index + 1}: ${recipe.title}`}
                  onClick={() => setCurrentRecipeIndex(index)}
                  className={`rounded-full transition-all duration-300 focus-visible:outline focus-visible:outline-2 focus-visible:outline-white cursor-pointer drop-shadow-md ${
                    isActive ? 'h-3.5 w-8 bg-primary shadow-md' : 'h-2.5 w-2.5 bg-white/60 hover:bg-white'
                  }`}
                />
              )
            })}
          </div>
        )}
      </section>

      {/* Featured Recipes Collection */}
      <section className="mx-auto max-w-7xl px-4 pt-20 sm:px-6 lg:px-8" aria-labelledby="featured-title">
        <div className="mb-10 flex items-end justify-between border-b border-border pb-6">
          <h2 id="featured-title" className="font-serif text-2xl text-foreground sm:text-3xl">
            Bộ sưu tập nổi bật
          </h2>
          <Link
            to="/recipes"
            aria-label="Xem tất cả công thức nổi bật"
            className="text-sm font-medium uppercase tracking-wide text-primary transition-colors hover:text-foreground"
          >
            Xem tất cả
          </Link>
        </div>

        {recipes.length === 0 ? (
          <p className="py-16 text-center text-muted-foreground">Chưa có công thức nào được xuất bản.</p>
        ) : (
          <div className="relative px-10 sm:px-14">
            <div className="pointer-events-none absolute left-10 right-10 top-0 z-10 aspect-square sm:left-14 sm:right-14 md:aspect-[3/1]">
              <button
                type="button"
                onClick={() => scrollCarousel(collectionRef.current, -1)}
                aria-label="Xem các công thức nổi bật trước"
                aria-controls="featured-recipes-carousel"
                className="pointer-events-auto absolute -left-10 top-1/2 flex h-12 w-10 -translate-y-1/2 cursor-pointer items-center justify-center text-foreground/60 transition-all duration-200 hover:text-primary active:scale-95 sm:-left-14"
              >
                <ChevronLeft size={36} strokeWidth={1.5} aria-hidden="true" />
              </button>

              <button
                type="button"
                onClick={() => void showNextCollectionRecipes()}
                aria-label="Xem thêm công thức nổi bật"
                aria-controls="featured-recipes-carousel"
                aria-busy={loadingMoreRecipes}
                className="pointer-events-auto absolute -right-10 top-1/2 flex h-12 w-10 -translate-y-1/2 cursor-pointer items-center justify-center text-foreground/60 transition-all duration-200 hover:text-primary active:scale-95 sm:-right-14"
              >
                {loadingMoreRecipes ? (
                  <LoaderCircle size={28} className="animate-spin text-primary" aria-hidden="true" />
                ) : (
                  <ChevronRight size={36} strokeWidth={1.5} aria-hidden="true" />
                )}
              </button>
            </div>

            <div
              ref={collectionRef}
              id="featured-recipes-carousel"
              className="flex snap-x snap-mandatory gap-8 overflow-x-auto scroll-smooth px-1 pb-4 [scrollbar-width:none] [&::-webkit-scrollbar]:hidden"
            >
              {recipes.map((recipe) => (
                <div
                  key={recipe.id}
                  className="min-w-0 shrink-0 basis-full snap-start md:basis-[calc((100%-4rem)/3)]"
                >
                  <RecipeCard recipe={recipe} headingLevel="h3" />
                </div>
              ))}
            </div>
            {collectionError && (
              <p className="mt-3 text-center text-sm text-primary" role="alert">
                Không thể tải thêm công thức. Vui lòng thử lại.
              </p>
            )}
          </div>
        )}
      </section>

      {/* Featured Categories */}
      <section className="mx-auto max-w-7xl px-4 pt-20 sm:px-6 lg:px-8" aria-labelledby="categories-title">
        <div className="mb-10 flex items-end justify-between border-b border-border pb-6">
          <div>
            <p className="mb-2 text-sm uppercase tracking-widest text-primary">Khám phá theo chủ đề</p>
            <h2 id="categories-title" className="font-serif text-2xl text-foreground sm:text-3xl">
              Danh mục nổi bật
            </h2>
          </div>
          <Link
            to="/categories"
            aria-label="Xem tất cả danh mục nổi bật"
            className="text-sm font-medium uppercase tracking-wide text-primary transition-colors hover:text-foreground"
          >
            Xem tất cả
          </Link>
        </div>

        {categories.length === 0 ? (
          <p className="py-12 text-center text-muted-foreground">Chưa có danh mục nào.</p>
        ) : (
          <div className="relative px-10 sm:px-14">
            <div className="pointer-events-none absolute left-10 right-10 top-0 z-10 aspect-[4/3] sm:left-14 sm:right-14 sm:aspect-[8/3] lg:aspect-[16/3]">
              <button
                type="button"
                onClick={() => scrollCarousel(categoriesRef.current, -1)}
                aria-label="Xem các danh mục trước"
                aria-controls="featured-categories-carousel"
                className="pointer-events-auto absolute -left-10 top-1/2 flex h-12 w-10 -translate-y-1/2 cursor-pointer items-center justify-center text-foreground/60 transition-all duration-200 hover:text-primary active:scale-95 sm:-left-14"
              >
                <ChevronLeft size={36} strokeWidth={1.5} aria-hidden="true" />
              </button>

              <button
                type="button"
                onClick={() => scrollCarousel(categoriesRef.current, 1)}
                aria-label="Xem thêm danh mục"
                aria-controls="featured-categories-carousel"
                className="pointer-events-auto absolute -right-10 top-1/2 flex h-12 w-10 -translate-y-1/2 cursor-pointer items-center justify-center text-foreground/60 transition-all duration-200 hover:text-primary active:scale-95 sm:-right-14"
              >
                <ChevronRight size={36} strokeWidth={1.5} aria-hidden="true" />
              </button>
            </div>

            <div
              ref={categoriesRef}
              id="featured-categories-carousel"
              className="flex snap-x snap-mandatory gap-5 overflow-x-auto scroll-smooth px-1 pb-4 [scrollbar-width:none] [&::-webkit-scrollbar]:hidden"
            >
              {categories.map((category) => (
                <div
                  key={category.id}
                  className="min-w-0 shrink-0 basis-full snap-start sm:basis-[calc((100%-1.25rem)/2)] lg:basis-[calc((100%-3.75rem)/4)]"
                >
                  <CategoryCard category={category} headingLevel="h3" />
                </div>
              ))}
            </div>
          </div>
        )}
      </section>
    </div>
  )
}
