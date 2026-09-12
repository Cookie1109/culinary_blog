import { useState } from 'react'
import { Link } from 'react-router'
import { Plus, Pencil, Trash2, X, Check, BookOpen } from 'lucide-react'
import { useAuth } from '../contexts/AuthContext'

interface Category {
  id: string
  slug: string
  name: string
  description: string
  count: number
  image: string
}

const INITIAL_CATEGORIES: Category[] = [
  {
    id: 'c1',
    slug: 'baking',
    name: 'Làm bánh',
    description: 'Bánh mì, bánh ngọt, bánh nướng và nghệ thuật làm ấm căn bếp từ lò nướng.',
    count: 24,
    image: 'https://images.unsplash.com/photo-1509440159596-0249088772ff?w=600&h=400&fit=crop',
  },
  {
    id: 'c2',
    slug: 'dinner',
    name: 'Món chính',
    description: 'Những món ăn thịnh soạn, tròn vị cho bữa tối thường nhật lẫn ngày lễ sum vầy.',
    count: 42,
    image: 'https://images.unsplash.com/photo-1476224203421-9ac39bcb3327?w=600&h=400&fit=crop',
  },
  {
    id: 'c3',
    slug: 'vegetarian',
    name: 'Món chay',
    description: 'Các món ăn thuần thực vật tươi ngon, tràn đầy màu sắc và dồi dào dinh dưỡng.',
    count: 18,
    image: 'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?w=600&h=400&fit=crop',
  },
  {
    id: 'c4',
    slug: 'breakfast',
    name: 'Bữa sáng',
    description: 'Khởi đầu ngày mới: từ món trứng nhanh gọn đến mâm brunch cuối tuần rực rỡ.',
    count: 15,
    image: 'https://images.unsplash.com/photo-1484723091739-30a097e8f929?w=600&h=400&fit=crop',
  },
  {
    id: 'c5',
    slug: 'desserts',
    name: 'Món tráng miệng',
    description: 'Bánh ngọt, kem tươi và niềm vui trọn vẹn từ đường và sữa.',
    count: 31,
    image: 'https://images.unsplash.com/photo-1551024506-0bccd828d307?w=600&h=400&fit=crop',
  },
  {
    id: 'c6',
    slug: 'soups',
    name: 'Canh & Súp',
    description: 'Những tô súp ấm nóng, bổ dưỡng được ninh chậm đầy tinh túy.',
    count: 12,
    image: 'https://images.unsplash.com/photo-1547592180-85f173990554?w=600&h=400&fit=crop',
  },
  {
    id: 'c7',
    slug: 'salads',
    name: 'Salad',
    description: 'Tươi giòn, thanh mát và kết hợp sốt trộn hấp dẫn hơn bạn nghĩ.',
    count: 9,
    image: 'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?w=600&h=400&fit=crop',
  },
  {
    id: 'c8',
    slug: 'quick-meals',
    name: 'Món nhanh',
    description: 'Các món ngon sẵn sàng lên bàn ăn chỉ trong vòng 30 phút hoặc ít hơn.',
    count: 28,
    image: 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=600&h=400&fit=crop',
  },
]

function slugify(name: string) {
  return name
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-|-$/g, '')
}

export function Categories() {
  const { user } = useAuth()
  const isAdmin = user?.role === 'admin'

  const [categories, setCategories] = useState<Category[]>(INITIAL_CATEGORIES)
  const [showAddForm, setShowAddForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [newName, setNewName] = useState('')
  const [newDesc, setNewDesc] = useState('')
  const [editName, setEditName] = useState('')
  const [editDesc, setEditDesc] = useState('')

  const handleAdd = () => {
    if (!newName.trim()) return
    const cat: Category = {
      id: `c${Date.now()}`,
      slug: slugify(newName),
      name: newName.trim(),
      description: newDesc.trim(),
      count: 0,
      image: 'https://images.unsplash.com/photo-1466637574441-749b8f19452f?w=600&h=400&fit=crop',
    }
    setCategories((prev) => [...prev, cat])
    setNewName('')
    setNewDesc('')
    setShowAddForm(false)
  }

  const handleEditStart = (cat: Category) => {
    setEditingId(cat.id)
    setEditName(cat.name)
    setEditDesc(cat.description)
  }

  const handleEditSave = (id: string) => {
    setCategories((prev) =>
      prev.map((c) =>
        c.id === id
          ? { ...c, name: editName.trim(), description: editDesc.trim(), slug: slugify(editName) }
          : c,
      ),
    )
    setEditingId(null)
  }

  const handleDelete = (id: string) => {
    setCategories((prev) => prev.filter((c) => c.id !== id))
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16">
      <header className="flex flex-col sm:flex-row sm:items-end justify-between gap-6 mb-12 border-b border-border pb-8">
        <div>
          <h1 className="font-serif text-4xl lg:text-5xl text-foreground mb-2">Danh mục món ăn</h1>
          <p className="text-muted-foreground">Khám phá các công thức theo chủ đề ẩm thực.</p>
        </div>
        {isAdmin && (
          <button
            onClick={() => setShowAddForm((v) => !v)}
            className="flex items-center gap-2 bg-primary text-primary-foreground px-6 py-3 text-sm uppercase tracking-widest hover:bg-primary/90 transition-colors shrink-0"
          >
            <Plus size={16} />
            Thêm danh mục
          </button>
        )}
      </header>

      {/* Add form */}
      {isAdmin && showAddForm && (
        <div className="mb-10 bg-secondary border border-border p-6 lg:p-8">
          <h2 className="font-serif text-xl mb-5">Danh mục mới</h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-4">
            <div>
              <label className="block text-xs uppercase tracking-widest text-muted-foreground mb-2">
                Tên danh mục
              </label>
              <input
                type="text"
                value={newName}
                onChange={(e) => setNewName(e.target.value)}
                placeholder="Ví dụ: Mỳ Ý & Ngũ cốc"
                className="w-full border border-border bg-background px-4 py-3 text-sm focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary"
              />
            </div>
            <div>
              <label className="block text-xs uppercase tracking-widest text-muted-foreground mb-2">
                Mô tả
              </label>
              <input
                type="text"
                value={newDesc}
                onChange={(e) => setNewDesc(e.target.value)}
                placeholder="Mô tả ngắn gọn"
                className="w-full border border-border bg-background px-4 py-3 text-sm focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary"
              />
            </div>
          </div>
          <div className="flex gap-3">
            <button
              onClick={handleAdd}
              className="flex items-center gap-2 bg-primary text-primary-foreground px-5 py-2.5 text-sm uppercase tracking-widest hover:bg-primary/90 transition-colors"
            >
              <Check size={15} /> Lưu
            </button>
            <button
              onClick={() => setShowAddForm(false)}
              className="flex items-center gap-2 border border-border px-5 py-2.5 text-sm uppercase tracking-widest hover:bg-background transition-colors"
            >
              <X size={15} /> Hủy
            </button>
          </div>
        </div>
      )}

      {/* Grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
        {categories.map((cat) => (
          <div key={cat.id} className="group relative flex flex-col">
            {editingId === cat.id ? (
              <div className="border border-primary p-5 flex-1 bg-background">
                <div className="mb-3">
                  <label className="block text-xs uppercase tracking-widest text-muted-foreground mb-1.5">
                    Tên danh mục
                  </label>
                  <input
                    type="text"
                    value={editName}
                    onChange={(e) => setEditName(e.target.value)}
                    className="w-full border border-border bg-background px-3 py-2 text-sm focus:outline-none focus:border-primary"
                    autoFocus
                  />
                </div>
                <div className="mb-4">
                  <label className="block text-xs uppercase tracking-widest text-muted-foreground mb-1.5">
                    Mô tả
                  </label>
                  <textarea
                    value={editDesc}
                    onChange={(e) => setEditDesc(e.target.value)}
                    rows={2}
                    className="w-full border border-border bg-background px-3 py-2 text-sm focus:outline-none focus:border-primary resize-none"
                  />
                </div>
                <div className="flex gap-2">
                  <button
                    onClick={() => handleEditSave(cat.id)}
                    className="flex items-center gap-1.5 bg-primary text-primary-foreground px-4 py-2 text-xs uppercase tracking-widest hover:bg-primary/90 transition-colors"
                  >
                    <Check size={13} /> Lưu
                  </button>
                  <button
                    onClick={() => setEditingId(null)}
                    className="flex items-center gap-1.5 border border-border px-4 py-2 text-xs uppercase tracking-widest hover:bg-secondary transition-colors"
                  >
                    <X size={13} /> Hủy
                  </button>
                </div>
              </div>
            ) : (
              <>
                <Link
                  to={`/recipes?category=${cat.slug}`}
                  className="block overflow-hidden bg-muted mb-4 aspect-[4/3]"
                >
                  <img
                    src={cat.image}
                    alt={cat.name}
                    className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-700 ease-out"
                  />
                </Link>
                <div className="flex-1 flex flex-col">
                  <div className="flex items-start justify-between gap-2 mb-1">
                    <h2 className="font-serif text-xl text-foreground group-hover:text-primary transition-colors">
                      <Link to={`/recipes?category=${cat.slug}`}>{cat.name}</Link>
                    </h2>
                    {isAdmin && (
                      <div className="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity shrink-0">
                        <button
                          onClick={() => handleEditStart(cat)}
                          className="p-1.5 text-muted-foreground hover:text-primary transition-colors"
                          aria-label={`Chỉnh sửa ${cat.name}`}
                        >
                          <Pencil size={14} />
                        </button>
                        <button
                          onClick={() => handleDelete(cat.id)}
                          className="p-1.5 text-muted-foreground hover:text-red-500 transition-colors"
                          aria-label={`Xóa ${cat.name}`}
                        >
                          <Trash2 size={14} />
                        </button>
                      </div>
                    )}
                  </div>
                  <p className="text-sm text-muted-foreground leading-relaxed mb-3 flex-1">
                    {cat.description}
                  </p>
                  <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
                    <BookOpen size={13} strokeWidth={1.5} />
                    <span>{cat.count} công thức</span>
                  </div>
                </div>
              </>
            )}
          </div>
        ))}
      </div>
    </div>
  )
}
