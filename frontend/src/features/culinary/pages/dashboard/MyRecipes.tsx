import { useState } from 'react'
import { Link } from 'react-router'
import { PlusCircle, Pencil, Trash2, Eye, Search } from 'lucide-react'

interface Recipe {
  id: string
  title: string
  slug: string
  category: string
  status: 'Published' | 'Draft' | 'Archived'
  date: string
  views: number
}

const ALL_RECIPES: Recipe[] = [
  {
    id: '1',
    title: 'Rustic Sourdough Boule',
    slug: 'rustic-sourdough-boule',
    category: 'Làm bánh',
    status: 'Published',
    date: '2026-09-01',
    views: 1247,
  },
  {
    id: '2',
    title: 'Wild Mushroom Risotto',
    slug: 'wild-mushroom-risotto',
    category: 'Món chính',
    status: 'Published',
    date: '2026-08-28',
    views: 892,
  },
  {
    id: '3',
    title: 'Heirloom Tomato Galette',
    slug: 'heirloom-tomato-galette',
    category: 'Món chay',
    status: 'Published',
    date: '2026-08-15',
    views: 634,
  },
  {
    id: '4',
    title: 'Cast Iron Ribeye',
    slug: 'cast-iron-ribeye',
    category: 'Món chính',
    status: 'Draft',
    date: '2026-09-10',
    views: 0,
  },
  {
    id: '5',
    title: 'Chocolate Soufflé',
    slug: 'chocolate-souffle',
    category: 'Món tráng miệng',
    status: 'Draft',
    date: '2026-09-09',
    views: 0,
  },
  {
    id: '6',
    title: 'Summer Gazpacho',
    slug: 'summer-gazpacho',
    category: 'Canh & Súp',
    status: 'Archived',
    date: '2026-07-01',
    views: 421,
  },
  {
    id: '7',
    title: 'Slow-Roasted Tomatoes',
    slug: 'slow-roasted-tomatoes',
    category: 'Món chay',
    status: 'Published',
    date: '2026-09-05',
    views: 318,
  },
  {
    id: '8',
    title: 'Brown Butter Financiers',
    slug: 'brown-butter-financiers',
    category: 'Làm bánh',
    status: 'Published',
    date: '2026-08-20',
    views: 756,
  },
]

const STATUS_BADGE: Record<string, string> = {
  Published: 'bg-green-50 text-green-700 border border-green-200',
  Draft: 'bg-amber-50 text-amber-700 border border-amber-200',
  Archived: 'bg-secondary text-muted-foreground border border-border',
}

const STATUS_FILTERS = ['All', 'Published', 'Draft', 'Archived'] as const
const STATUS_FILTER_LABELS: Record<string, string> = {
  All: 'Tất cả',
  Published: 'Đã xuất bản',
  Draft: 'Bản nháp',
  Archived: 'Đã lưu trữ',
}

export function MyRecipes() {
  const [recipes, setRecipes] = useState<Recipe[]>(ALL_RECIPES)
  const [statusFilter, setStatusFilter] = useState<string>('All')
  const [query, setQuery] = useState('')
  const [confirmDelete, setConfirmDelete] = useState<string | null>(null)

  const filtered = recipes.filter((r) => {
    const matchStatus = statusFilter === 'All' || r.status === statusFilter
    const matchQuery =
      query === '' ||
      r.title.toLowerCase().includes(query.toLowerCase()) ||
      r.category.toLowerCase().includes(query.toLowerCase())
    return matchStatus && matchQuery
  })

  const handleDelete = (id: string) => {
    setRecipes((prev) => prev.filter((r) => r.id !== id))
    setConfirmDelete(null)
  }

  const changeStatus = (id: string, status: Recipe['status']) => {
    setRecipes((prev) => prev.map((r) => (r.id === id ? { ...r, status } : r)))
  }

  return (
    <div>
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-8">
        <div>
          <h1 className="font-serif text-3xl lg:text-4xl text-foreground">Công thức của tôi</h1>
          <p className="text-muted-foreground mt-1">Tổng cộng {recipes.length} công thức</p>
        </div>
        <Link
          to="/dashboard/recipes/new"
          className="flex items-center gap-2 bg-primary text-primary-foreground px-5 py-3 text-sm uppercase tracking-widest hover:bg-primary/90 transition-colors shrink-0"
        >
          <PlusCircle size={16} /> Tạo công thức mới
        </Link>
      </div>

      {/* Filters bar */}
      <div className="flex flex-col sm:flex-row gap-3 mb-6">
        {/* Search */}
        <div className="relative flex-1 max-w-sm">
          <Search
            size={16}
            strokeWidth={1.5}
            className="absolute left-3.5 top-1/2 -translate-y-1/2 text-muted-foreground pointer-events-none"
          />
          <input
            type="text"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Tìm kiếm công thức…"
            className="w-full border border-border bg-background pl-10 pr-4 py-2.5 text-sm focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary placeholder:text-muted-foreground"
          />
        </div>

        {/* Status filter */}
        <div className="flex gap-1 border border-border bg-background">
          {STATUS_FILTERS.map((s) => (
            <button
              key={s}
              onClick={() => setStatusFilter(s)}
              className={`px-4 py-2.5 text-xs uppercase tracking-widest font-medium transition-colors ${
                statusFilter === s
                  ? 'bg-foreground text-background'
                  : 'text-muted-foreground hover:text-foreground hover:bg-secondary'
              }`}
            >
              {STATUS_FILTER_LABELS[s]}
            </button>
          ))}
        </div>
      </div>

      {/* Table */}
      <div className="bg-background border border-border overflow-hidden">
        {filtered.length === 0 ? (
          <div className="text-center py-16 px-4">
            <p className="font-serif text-xl text-foreground mb-2">Không tìm thấy công thức nào</p>
            <p className="text-muted-foreground text-sm">
              Hãy thử điều chỉnh bộ lọc hoặc tạo một công thức mới.
            </p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border bg-secondary/30">
                  <th className="text-left px-6 py-3 text-xs uppercase tracking-widest text-muted-foreground font-medium">
                    Tiêu đề
                  </th>
                  <th className="text-left px-6 py-3 text-xs uppercase tracking-widest text-muted-foreground font-medium hidden sm:table-cell">
                    Danh mục
                  </th>
                  <th className="text-left px-6 py-3 text-xs uppercase tracking-widest text-muted-foreground font-medium">
                    Trạng thái
                  </th>
                  <th className="text-right px-6 py-3 text-xs uppercase tracking-widest text-muted-foreground font-medium hidden lg:table-cell">
                    Lượt xem
                  </th>
                  <th className="text-right px-6 py-3 text-xs uppercase tracking-widest text-muted-foreground font-medium hidden md:table-cell">
                    Ngày tạo
                  </th>
                  <th className="px-6 py-3" />
                </tr>
              </thead>
              <tbody>
                {filtered.map((recipe) => (
                  <tr
                    key={recipe.id}
                    className="border-b border-border last:border-0 hover:bg-secondary/30 transition-colors group"
                  >
                    <td className="px-6 py-4">
                      <span className="font-medium text-foreground">{recipe.title}</span>
                    </td>
                    <td className="px-6 py-4 text-muted-foreground hidden sm:table-cell">
                      {recipe.category}
                    </td>
                    <td className="px-6 py-4">
                      <select
                        value={recipe.status}
                        onChange={(e) => changeStatus(recipe.id, e.target.value as Recipe['status'])}
                        className={`px-2.5 py-1 text-xs border cursor-pointer focus:outline-none focus:ring-1 focus:ring-primary ${STATUS_BADGE[recipe.status]}`}
                      >
                        <option value="Draft">Bản nháp</option>
                        <option value="Published">Đã xuất bản</option>
                        <option value="Archived">Đã lưu trữ</option>
                      </select>
                    </td>
                    <td className="px-6 py-4 text-right text-muted-foreground hidden lg:table-cell">
                      <span className="flex items-center justify-end gap-1.5">
                        <Eye size={13} strokeWidth={1.5} />
                        {recipe.views > 0 ? recipe.views.toLocaleString() : '—'}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-right text-muted-foreground hidden md:table-cell">
                      {recipe.date}
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex items-center justify-end gap-2 opacity-0 group-hover:opacity-100 transition-opacity">
                        {recipe.status === 'Published' && (
                          <Link
                            to={`/recipes/${recipe.slug}`}
                            className="p-1.5 text-muted-foreground hover:text-primary transition-colors"
                            aria-label="Xem"
                            target="_blank"
                          >
                            <Eye size={15} />
                          </Link>
                        )}
                        <Link
                          to={`/dashboard/recipes/${recipe.id}/edit`}
                          className="p-1.5 text-muted-foreground hover:text-primary transition-colors"
                          aria-label="Chỉnh sửa"
                        >
                          <Pencil size={15} />
                        </Link>
                        {confirmDelete === recipe.id ? (
                          <span className="flex items-center gap-2 text-xs">
                            <button
                              onClick={() => handleDelete(recipe.id)}
                              className="text-red-600 hover:text-red-700 font-medium"
                            >
                              Xóa
                            </button>
                            <button
                              onClick={() => setConfirmDelete(null)}
                              className="text-muted-foreground hover:text-foreground"
                            >
                              Hủy
                            </button>
                          </span>
                        ) : (
                          <button
                            onClick={() => setConfirmDelete(recipe.id)}
                            className="p-1.5 text-muted-foreground hover:text-red-500 transition-colors"
                            aria-label="Xóa"
                          >
                            <Trash2 size={15} />
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      <p className="mt-4 text-xs text-muted-foreground">
        Đang hiển thị {filtered.length} trên tổng số {recipes.length} công thức
      </p>
    </div>
  )
}
