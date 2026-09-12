import { useState, useEffect, useCallback } from 'react'
import { Link, useSearchParams } from 'react-router'
import { Search as SearchIcon, SlidersHorizontal, X, Clock } from 'lucide-react'

interface Recipe {
  id: string
  title: string
  slug: string
  category: string
  image: string
  time: number
  difficulty: 'Easy' | 'Medium' | 'Hard'
  description: string
}

const ALL_RECIPES: Recipe[] = [
  {
    id: '1',
    title: 'Rustic Sourdough Boule',
    slug: 'rustic-sourdough-boule',
    category: 'Baking',
    image: 'https://images.unsplash.com/photo-1585478259715-876acc5be8eb?w=600&h=600&fit=crop',
    time: 75,
    difficulty: 'Hard',
    description: 'A deeply flavorful, crusty sourdough bread with an airy crumb.',
  },
  {
    id: '2',
    title: 'Wild Mushroom Risotto',
    slug: 'wild-mushroom-risotto',
    category: 'Dinner',
    image: 'https://images.unsplash.com/photo-1626844131082-256783844137?w=600&h=600&fit=crop',
    time: 55,
    difficulty: 'Medium',
    description: 'Creamy Arborio rice with a medley of wild mushrooms and aged Parmesan.',
  },
  {
    id: '3',
    title: 'Heirloom Tomato Galette',
    slug: 'heirloom-tomato-galette',
    category: 'Vegetarian',
    image: 'https://images.unsplash.com/photo-1595854341625-f33ee10dbf94?w=600&h=600&fit=crop',
    time: 55,
    difficulty: 'Medium',
    description: 'A free-form rustic tart with summer tomatoes and herbed ricotta.',
  },
  {
    id: '4',
    title: 'Cast Iron Ribeye',
    slug: 'cast-iron-ribeye',
    category: 'Dinner',
    image: 'https://images.unsplash.com/photo-1544025162-d76694265947?w=600&h=600&fit=crop',
    time: 25,
    difficulty: 'Easy',
    description: 'A perfectly seared ribeye with herb butter and roasted garlic.',
  },
  {
    id: '5',
    title: 'Classic French Omelette',
    slug: 'classic-french-omelette',
    category: 'Breakfast',
    image: 'https://images.unsplash.com/photo-1510693206972-df098062cb71?w=600&h=600&fit=crop',
    time: 15,
    difficulty: 'Medium',
    description: 'Delicate, pale, and creamy. The French way with eggs.',
  },
  {
    id: '6',
    title: 'Miso Glazed Eggplant',
    slug: 'miso-glazed-eggplant',
    category: 'Vegetarian',
    image: 'https://images.unsplash.com/photo-1580476262798-bddd9f4b7369?w=600&h=600&fit=crop',
    time: 40,
    difficulty: 'Easy',
    description: 'Tender roasted eggplant with a sweet and savory miso glaze.',
  },
  {
    id: '7',
    title: 'Slow-Roasted Tomatoes',
    slug: 'slow-roasted-tomatoes',
    category: 'Vegetarian',
    image: 'https://images.unsplash.com/photo-1563379926898-05f4575a45d8?w=600&h=600&fit=crop',
    time: 120,
    difficulty: 'Easy',
    description: 'Intensely sweet and savory tomatoes slow-roasted with olive oil and herbs.',
  },
  {
    id: '8',
    title: 'Brown Butter Financiers',
    slug: 'brown-butter-financiers',
    category: 'Baking',
    image: 'https://images.unsplash.com/photo-1558961363-fa8fdf82db35?w=600&h=600&fit=crop',
    time: 35,
    difficulty: 'Easy',
    description: 'Nutty, moist French almond cakes made with browned butter.',
  },
  {
    id: '9',
    title: 'Vietnamese Pho Bo',
    slug: 'vietnamese-pho-bo',
    category: 'Soups',
    image: 'https://images.unsplash.com/photo-1503764654157-72d979d9af2f?w=600&h=600&fit=crop',
    time: 180,
    difficulty: 'Hard',
    description: 'A deeply aromatic beef broth with rice noodles, fresh herbs, and spice.',
  },
  {
    id: '10',
    title: 'Avocado Toast',
    slug: 'avocado-toast',
    category: 'Breakfast',
    image: 'https://images.unsplash.com/photo-1541519227354-08fa5d50c820?w=600&h=600&fit=crop',
    time: 10,
    difficulty: 'Easy',
    description: 'Perfectly ripe avocado on toasted sourdough with lemon and chili flakes.',
  },
  {
    id: '11',
    title: 'Chocolate Mousse',
    slug: 'chocolate-mousse',
    category: 'Desserts',
    image: 'https://images.unsplash.com/photo-1541783245831-57d6fb0926d3?w=600&h=600&fit=crop',
    time: 30,
    difficulty: 'Medium',
    description: 'Silky, airy mousse made with dark chocolate and barely whipped cream.',
  },
  {
    id: '12',
    title: 'Roasted Carrot Soup',
    slug: 'roasted-carrot-soup',
    category: 'Soups',
    image: 'https://images.unsplash.com/photo-1547592180-85f173990554?w=600&h=600&fit=crop',
    time: 50,
    difficulty: 'Easy',
    description: 'Sweet roasted carrots blended with ginger, coconut milk, and lime.',
  },
]

const CATEGORIES = ['All', 'Baking', 'Breakfast', 'Desserts', 'Dinner', 'Soups', 'Vegetarian']
const CATEGORY_LABELS: Record<string, string> = {
  All: 'Tất cả',
  Baking: 'Làm bánh',
  Breakfast: 'Bữa sáng',
  Desserts: 'Món tráng miệng',
  Dinner: 'Món chính',
  Soups: 'Canh & Súp',
  Vegetarian: 'Món chay',
}

const DIFFICULTIES = ['All', 'Easy', 'Medium', 'Hard']
const DIFFICULTY_LABELS: Record<string, string> = {
  All: 'Tất cả',
  Easy: 'Dễ',
  Medium: 'Trung bình',
  Hard: 'Nâng cao',
}

const SORT_OPTIONS = [
  { value: 'newest', label: 'Mới nhất' },
  { value: 'quickest', label: 'Nấu nhanh nhất' },
  { value: 'az', label: 'Theo tên (A – Z)' },
]

const PAGE_SIZE = 9

export function Search() {
  const [searchParams, setSearchParams] = useSearchParams()
  const [filtersOpen, setFiltersOpen] = useState(false)

  const q = searchParams.get('q') ?? ''
  const cat = searchParams.get('category') ?? 'All'
  const diff = searchParams.get('difficulty') ?? 'All'
  const maxTime = parseInt(searchParams.get('maxTime') ?? '999', 10)
  const sort = searchParams.get('sort') ?? 'newest'
  const page = parseInt(searchParams.get('page') ?? '1', 10)

  const [inputValue, setInputValue] = useState(q)

  useEffect(() => {
    setInputValue(q)
  }, [q])

  const set = useCallback(
    (key: string, value: string) => {
      setSearchParams((prev) => {
        const next = new URLSearchParams(prev)
        if (value === '' || value === 'All' || value === '999') next.delete(key)
        else next.set(key, value)
        next.delete('page')
        return next
      })
    },
    [setSearchParams],
  )

  const clearAll = () => setSearchParams({})

  const results = ALL_RECIPES.filter((r) => {
    const matchQ =
      q === '' ||
      r.title.toLowerCase().includes(q.toLowerCase()) ||
      r.description.toLowerCase().includes(q.toLowerCase()) ||
      r.category.toLowerCase().includes(q.toLowerCase()) ||
      (CATEGORY_LABELS[r.category]?.toLowerCase() ?? '').includes(q.toLowerCase())
    const matchCat = cat === 'All' || r.category === cat
    const matchDiff = diff === 'All' || r.difficulty === diff
    const matchTime = r.time <= maxTime
    return matchQ && matchCat && matchDiff && matchTime
  }).sort((a, b) => {
    if (sort === 'quickest') return a.time - b.time
    if (sort === 'az') return a.title.localeCompare(b.title)
    return parseInt(b.id) - parseInt(a.id)
  })

  const totalPages = Math.ceil(results.length / PAGE_SIZE)
  const paginated = results.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE)
  const hasFilters = q || cat !== 'All' || diff !== 'All' || maxTime < 999

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12 lg:py-16">
      {/* Search bar */}
      <div className="max-w-3xl mx-auto mb-10">
        <h1 className="font-serif text-4xl lg:text-5xl text-foreground mb-6 text-center">
          Tìm kiếm công thức
        </h1>
        <div className="relative">
          <SearchIcon
            size={20}
            strokeWidth={1.5}
            className="absolute left-4 top-1/2 -translate-y-1/2 text-muted-foreground pointer-events-none"
          />
          <input
            type="search"
            value={inputValue}
            onChange={(e) => setInputValue(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === 'Enter') set('q', inputValue)
            }}
            placeholder="Tìm theo tên món, nguyên liệu hoặc danh mục…"
            className="w-full border border-border bg-background pl-12 pr-28 py-4 text-base focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary placeholder:text-muted-foreground"
          />
          <button
            onClick={() => set('q', inputValue)}
            className="absolute right-0 top-0 bottom-0 px-6 bg-primary text-primary-foreground text-sm font-medium uppercase tracking-widest hover:bg-primary/90 transition-colors"
          >
            Tìm kiếm
          </button>
        </div>
      </div>

      <div className="flex flex-col lg:flex-row gap-8">
        {/* Sidebar filters */}
        <aside className={`lg:w-56 shrink-0 ${filtersOpen ? 'block' : 'hidden lg:block'}`}>
          <div className="sticky top-24 space-y-8">
            {/* Category */}
            <div>
              <h3 className="text-xs uppercase tracking-widest text-muted-foreground font-medium mb-3 pb-2 border-b border-border">
                Danh mục
              </h3>
              <ul className="space-y-1">
                {CATEGORIES.map((c) => (
                  <li key={c}>
                    <button
                      onClick={() => set('category', c)}
                      className={`w-full text-left py-1.5 text-sm transition-colors ${
                        cat === c || (c === 'All' && cat === 'All')
                          ? 'text-primary font-medium'
                          : 'text-foreground hover:text-primary'
                      }`}
                    >
                      {CATEGORY_LABELS[c] ?? c}
                    </button>
                  </li>
                ))}
              </ul>
            </div>

            {/* Difficulty */}
            <div>
              <h3 className="text-xs uppercase tracking-widest text-muted-foreground font-medium mb-3 pb-2 border-b border-border">
                Độ khó
              </h3>
              <ul className="space-y-1">
                {DIFFICULTIES.map((d) => (
                  <li key={d}>
                    <button
                      onClick={() => set('difficulty', d)}
                      className={`w-full text-left py-1.5 text-sm transition-colors ${
                        diff === d ? 'text-primary font-medium' : 'text-foreground hover:text-primary'
                      }`}
                    >
                      {DIFFICULTY_LABELS[d] ?? d}
                    </button>
                  </li>
                ))}
              </ul>
            </div>

            {/* Max time */}
            <div>
              <h3 className="text-xs uppercase tracking-widest text-muted-foreground font-medium mb-3 pb-2 border-b border-border">
                Thời gian: {maxTime >= 999 ? 'Tất cả' : `${maxTime} phút`}
              </h3>
              <input
                type="range"
                min={10}
                max={180}
                step={10}
                value={Math.min(maxTime, 180)}
                onChange={(e) => set('maxTime', e.target.value === '180' ? '999' : e.target.value)}
                className="w-full accent-primary"
              />
              <div className="flex justify-between text-xs text-muted-foreground mt-1">
                <span>10 phút</span>
                <span>Trên 3 giờ</span>
              </div>
            </div>

            {hasFilters && (
              <button
                onClick={clearAll}
                className="text-xs text-muted-foreground hover:text-primary uppercase tracking-widest flex items-center gap-1.5 transition-colors"
              >
                <X size={13} /> Xóa tất cả bộ lọc
              </button>
            )}
          </div>
        </aside>

        {/* Results */}
        <div className="flex-1 min-w-0">
          {/* Toolbar */}
          <div className="flex items-center justify-between mb-6 gap-4 flex-wrap">
            <div className="flex items-center gap-3">
              <button
                onClick={() => setFiltersOpen((v) => !v)}
                className="lg:hidden flex items-center gap-2 text-sm border border-border px-4 py-2 hover:bg-secondary transition-colors"
              >
                <SlidersHorizontal size={15} /> Bộ lọc
              </button>
              <p className="text-sm text-muted-foreground">
                <span className="font-medium text-foreground">{results.length}</span> công thức phù hợp
                {q && (
                  <span>
                    {' '}
                    cho "<span className="text-foreground">{q}</span>"
                  </span>
                )}
              </p>
            </div>
            <select
              value={sort}
              onChange={(e) => set('sort', e.target.value)}
              className="border border-border bg-background px-3 py-2 text-sm focus:outline-none focus:border-primary"
            >
              {SORT_OPTIONS.map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
            </select>
          </div>

          {paginated.length === 0 ? (
            <div className="text-center py-24">
              <SearchIcon size={40} strokeWidth={1} className="mx-auto text-muted-foreground mb-4" />
              <p className="font-serif text-xl text-foreground mb-2">Không tìm thấy công thức phù hợp</p>
              <p className="text-muted-foreground text-sm">
                Hãy thử tìm kiếm với từ khóa khác hoặc xóa bớt tiêu chí lọc.
              </p>
              {hasFilters && (
                <button onClick={clearAll} className="mt-6 text-sm text-primary hover:underline">
                  Xóa tất cả bộ lọc
                </button>
              )}
            </div>
          ) : (
            <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-x-6 gap-y-12">
              {paginated.map((recipe) => (
                <div key={recipe.id} className="group">
                  <Link
                    to={`/recipes/${recipe.slug}`}
                    className="block overflow-hidden bg-muted mb-4 aspect-square"
                  >
                    <img
                      src={recipe.image}
                      alt={recipe.title}
                      className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-700 ease-out"
                    />
                  </Link>
                  <div className="flex items-center justify-between mb-1.5">
                    <span className="text-muted-foreground text-xs uppercase tracking-widest">
                      {CATEGORY_LABELS[recipe.category] ?? recipe.category}
                    </span>
                    <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
                      <Clock size={13} strokeWidth={1.5} />
                      <span>{recipe.time} phút</span>
                    </div>
                  </div>
                  <h3 className="font-serif text-xl group-hover:text-primary transition-colors">
                    <Link to={`/recipes/${recipe.slug}`}>{recipe.title}</Link>
                  </h3>
                  <p className="text-sm text-muted-foreground mt-1.5 line-clamp-2 leading-relaxed">
                    {recipe.description}
                  </p>
                </div>
              ))}
            </div>
          )}

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="mt-16 flex justify-center gap-2 flex-wrap">
              {Array.from({ length: totalPages }, (_, i) => i + 1).map((p) => (
                <button
                  key={p}
                  onClick={() =>
                    setSearchParams((prev) => {
                      const n = new URLSearchParams(prev)
                      n.set('page', String(p))
                      return n
                    })
                  }
                  className={`w-10 h-10 border font-serif text-lg transition-colors ${
                    p === page
                      ? 'border-foreground bg-foreground text-background'
                      : 'border-border text-muted-foreground hover:border-foreground hover:text-foreground'
                  }`}
                >
                  {p}
                </button>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
