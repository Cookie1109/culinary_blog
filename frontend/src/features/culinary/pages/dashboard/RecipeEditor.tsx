import { useState } from 'react'
import { useParams, useNavigate, Link } from 'react-router'
import {
  Plus,
  Trash2,
  ImagePlus,
  ChevronLeft,
  Save,
  GripVertical,
  CheckCircle,
  FileText,
  Archive,
} from 'lucide-react'

interface Ingredient {
  id: string
  qty: string
  unit: string
  name: string
  notes: string
}

interface Step {
  id: string
  title: string
  description: string
}

interface RecipeForm {
  title: string
  description: string
  category: string
  difficulty: 'Easy' | 'Medium' | 'Hard'
  servings: number
  prepTime: number
  cookTime: number
  tags: string
  status: 'Draft' | 'Published' | 'Archived'
  imagePreview: string
  ingredients: Ingredient[]
  steps: Step[]
  nutrition: {
    calories: string
    protein: string
    carbs: string
    fat: string
    fiber: string
    sodium: string
  }
}

const CATEGORIES = [
  'Làm bánh',
  'Bữa sáng',
  'Món tráng miệng',
  'Món chính',
  'Salad',
  'Canh & Súp',
  'Món chay',
  'Món nhanh',
]

const STATUS_LABELS: Record<RecipeForm['status'], string> = {
  Draft: 'Bản nháp',
  Published: 'Đã xuất bản',
  Archived: 'Lưu trữ',
}

const EMPTY_FORM: RecipeForm = {
  title: '',
  description: '',
  category: 'Món chính',
  difficulty: 'Medium',
  servings: 4,
  prepTime: 15,
  cookTime: 30,
  tags: '',
  status: 'Draft',
  imagePreview: '',
  ingredients: [
    { id: 'i1', qty: '', unit: '', name: '', notes: '' },
    { id: 'i2', qty: '', unit: '', name: '', notes: '' },
  ],
  steps: [{ id: 's1', title: '', description: '' }],
  nutrition: {
    calories: '',
    protein: '',
    carbs: '',
    fat: '',
    fiber: '',
    sodium: '',
  },
}

const PREFILLED: RecipeForm = {
  title: 'Bánh mì men chua Sourdough mộc mạc',
  description:
    'Ổ bánh mì men tự nhiên với lớp vỏ giòn rụm và ruột bánh xốp mềm hoàn hảo. Rất thích hợp cho bữa sáng hoặc dùng kèm các món súp ấm nóng. Công thức này đòi hỏi men nuôi đạt chuẩn và một chút kiên nhẫn, nhưng thành quả sẽ hoàn toàn xứng đáng.',
  category: 'Làm bánh',
  difficulty: 'Hard',
  servings: 12,
  prepTime: 30,
  cookTime: 45,
  tags: 'sourdough, bánh mì, làm bánh, thủ công',
  status: 'Published',
  imagePreview: 'https://images.unsplash.com/photo-1585478259715-876acc5be8eb?w=800&h=500&fit=crop',
  ingredients: [
    {
      id: 'i1',
      qty: '500',
      unit: 'g',
      name: 'Bột mì làm bánh',
      notes: 'Bột không tẩy trắng, hàm lượng protein tối thiểu 12%',
    },
    { id: 'i2', qty: '350', unit: 'g', name: 'Nước lọc', notes: 'Nhiệt độ phòng (khoảng 24°C)' },
    {
      id: 'i3',
      qty: '100',
      unit: 'g',
      name: 'Men chua sourdough hoạt động tốt',
      notes: 'Men mới cho ăn và đang nổi bọt khí',
    },
    { id: 'i4', qty: '10', unit: 'g', name: 'Muối biển tinh', notes: '' },
  ],
  steps: [
    {
      id: 's1',
      title: 'Tự phân giải (Autolyse)',
      description:
        'Trộn đều bột mì và 325g nước. Nhồi cho đến khi hỗn hợp bột hòa quyện và không còn bột khô. Đậy kín và ủ trong 1 giờ.',
    },
    {
      id: 's2',
      title: 'Thêm men starter và muối',
      description:
        'Thêm men nuôi và lượng nước còn lại. Dùng đầu ngón tay ấn đều và rắc muối vào bột. Trộn đều kỹ trong 5 phút.',
    },
    {
      id: 's3',
      title: 'Ủ lên men khối (Bulk Fermentation)',
      description:
        'Ủ ở nhiệt độ phòng trong 4–5 giờ. Thực hiện thao tác kéo gập bột (stretch and fold) mỗi 30 phút một lần trong 2 giờ đầu.',
    },
    {
      id: 's4',
      title: 'Tạo hình',
      description:
        'Tạo hình khối cầu tròn nhẹ nhàng. Để bột nghỉ 20 phút. Tạo hình tròn chặt (boule) lần cuối rồi đặt mặt mịn úp xuống giỏ ủ banneton.',
    },
    {
      id: 's5',
      title: 'Ủ lạnh (Cold Retard)',
      description:
        'Bọc kín giỏ bột và ủ trong ngăn mát tủ lạnh qua đêm (12–16 giờ) để hương vị men phát triển đậm đà.',
    },
    {
      id: 's6',
      title: 'Nướng bánh',
      description:
        'Làm nóng trước nồi gang ở 260°C trong 1 giờ. Rạch mặt bánh và nướng đậy nắp 20 phút, sau đó mở nắp nướng tiếp ở 230°C trong 20–25 phút đến khi vỏ bánh nâu vàng giòn rụm.',
    },
  ],
  nutrition: {
    calories: '185',
    protein: '6',
    carbs: '37',
    fat: '1',
    fiber: '2',
    sodium: '320',
  },
}

const uid = () => Math.random().toString(36).slice(2, 9)

const TABS = ['Thông tin cơ bản', 'Nguyên liệu', 'Cách làm', 'Dinh dưỡng'] as const
type Tab = (typeof TABS)[number]

export function RecipeEditor() {
  const { id } = useParams<{ id?: string }>()
  const navigate = useNavigate()
  const isEditing = Boolean(id)
  const [form, setForm] = useState<RecipeForm>(isEditing ? PREFILLED : EMPTY_FORM)
  const [activeTab, setActiveTab] = useState<Tab>('Thông tin cơ bản')
  const [saved, setSaved] = useState(false)

  const update = <K extends keyof RecipeForm>(key: K, value: RecipeForm[K]) =>
    setForm((prev) => ({ ...prev, [key]: value }))

  const updateNutrition = (key: keyof RecipeForm['nutrition'], value: string) =>
    setForm((prev) => ({ ...prev, nutrition: { ...prev.nutrition, [key]: value } }))

  const addIngredient = () =>
    update('ingredients', [...form.ingredients, { id: uid(), qty: '', unit: '', name: '', notes: '' }])

  const removeIngredient = (id: string) =>
    update(
      'ingredients',
      form.ingredients.filter((i) => i.id !== id),
    )

  const updateIngredient = (id: string, key: keyof Ingredient, value: string) =>
    update(
      'ingredients',
      form.ingredients.map((i) => (i.id === id ? { ...i, [key]: value } : i)),
    )

  const addStep = () => update('steps', [...form.steps, { id: uid(), title: '', description: '' }])

  const removeStep = (id: string) =>
    update(
      'steps',
      form.steps.filter((s) => s.id !== id),
    )

  const updateStep = (id: string, key: keyof Step, value: string) =>
    update(
      'steps',
      form.steps.map((s) => (s.id === id ? { ...s, [key]: value } : s)),
    )

  const handleSave = () => {
    setSaved(true)
    setTimeout(() => setSaved(false), 2500)
  }

  const handlePublish = () => {
    update('status', 'Published')
    handleSave()
  }

  const inputCls =
    'w-full border border-border bg-background px-4 py-3 text-sm focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary transition-all placeholder:text-muted-foreground'
  const labelCls = 'block text-xs uppercase tracking-widest text-muted-foreground mb-2'

  return (
    <div className="max-w-4xl">
      {/* Header */}
      <div className="flex items-start justify-between gap-4 mb-8">
        <div>
          <Link
            to="/dashboard/recipes"
            className="flex items-center gap-1.5 text-xs uppercase tracking-widest text-muted-foreground hover:text-primary transition-colors mb-3"
          >
            <ChevronLeft size={14} /> Công thức của tôi
          </Link>
          <h1 className="font-serif text-3xl lg:text-4xl text-foreground">
            {isEditing ? 'Chỉnh sửa công thức' : 'Tạo công thức mới'}
          </h1>
        </div>
        <div className="flex items-center gap-3 shrink-0 pt-1">
          {form.status === 'Draft' && (
            <button
              onClick={handlePublish}
              className="hidden sm:flex items-center gap-2 border border-green-300 text-green-700 hover:bg-green-50 px-4 py-2.5 text-xs uppercase tracking-widest transition-colors"
            >
              <CheckCircle size={14} /> Xuất bản
            </button>
          )}
          <button
            onClick={handleSave}
            className="flex items-center gap-2 bg-primary text-primary-foreground px-5 py-2.5 text-sm uppercase tracking-widest hover:bg-primary/90 transition-colors"
          >
            <Save size={15} /> Lưu công thức
          </button>
        </div>
      </div>

      {saved && (
        <div className="flex items-center gap-2 bg-green-50 border border-green-200 text-green-700 px-4 py-3 text-sm mb-6">
          <CheckCircle size={16} /> Đã lưu công thức thành công.
        </div>
      )}

      {/* Status strip */}
      <div className="flex items-center gap-2 mb-8">
        <span className="text-xs uppercase tracking-widest text-muted-foreground">Trạng thái:</span>
        {(['Draft', 'Published', 'Archived'] as const).map((s) => (
          <button
            key={s}
            onClick={() => update('status', s)}
            className={`flex items-center gap-1.5 px-3 py-1.5 text-xs border transition-colors ${
              form.status === s
                ? s === 'Published'
                  ? 'bg-green-50 text-green-700 border-green-300'
                  : s === 'Draft'
                    ? 'bg-amber-50 text-amber-700 border-amber-300'
                    : 'bg-secondary text-muted-foreground border-border'
                : 'text-muted-foreground border-border hover:bg-secondary'
            }`}
          >
            {s === 'Published' ? (
              <CheckCircle size={12} />
            ) : s === 'Draft' ? (
              <FileText size={12} />
            ) : (
              <Archive size={12} />
            )}
            {STATUS_LABELS[s]}
          </button>
        ))}
      </div>

      {/* Tabs */}
      <div className="flex border-b border-border mb-8 overflow-x-auto">
        {TABS.map((tab) => (
          <button
            key={tab}
            onClick={() => setActiveTab(tab)}
            className={`px-5 py-3 text-sm font-medium whitespace-nowrap transition-colors border-b-2 -mb-px ${
              activeTab === tab
                ? 'border-primary text-primary'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            }`}
          >
            {tab}
          </button>
        ))}
      </div>

      {/* Tab: Basic Info */}
      {activeTab === 'Thông tin cơ bản' && (
        <div className="space-y-6">
          <div>
            <label className={labelCls}>Tên công thức / món ăn</label>
            <input
              type="text"
              value={form.title}
              onChange={(e) => update('title', e.target.value)}
              placeholder="Ví dụ: Risotto nấm rừng thơm ngậy"
              className={inputCls}
            />
          </div>

          <div>
            <label className={labelCls}>Mô tả</label>
            <textarea
              value={form.description}
              onChange={(e) => update('description', e.target.value)}
              placeholder="Mô tả ngắn gọn, hấp dẫn về món ăn này..."
              rows={3}
              className={`${inputCls} resize-none`}
            />
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div>
              <label className={labelCls}>Danh mục</label>
              <select
                value={form.category}
                onChange={(e) => update('category', e.target.value)}
                className={inputCls}
              >
                {CATEGORIES.map((c) => (
                  <option key={c} value={c}>
                    {c}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className={labelCls}>Độ khó</label>
              <select
                value={form.difficulty}
                onChange={(e) => update('difficulty', e.target.value as RecipeForm['difficulty'])}
                className={inputCls}
              >
                <option value="Easy">Dễ</option>
                <option value="Medium">Trung bình</option>
                <option value="Hard">Khó</option>
              </select>
            </div>
            <div>
              <label className={labelCls}>Khẩu phần</label>
              <input
                type="number"
                min={1}
                value={form.servings}
                onChange={(e) => update('servings', parseInt(e.target.value) || 1)}
                className={inputCls}
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className={labelCls}>Chuẩn bị (phút)</label>
              <input
                type="number"
                min={0}
                value={form.prepTime}
                onChange={(e) => update('prepTime', parseInt(e.target.value) || 0)}
                className={inputCls}
              />
            </div>
            <div>
              <label className={labelCls}>Thời gian nấu (phút)</label>
              <input
                type="number"
                min={0}
                value={form.cookTime}
                onChange={(e) => update('cookTime', parseInt(e.target.value) || 0)}
                className={inputCls}
              />
            </div>
          </div>

          <div>
            <label className={labelCls}>Thẻ phân loại (cách nhau bằng dấu phẩy)</label>
            <input
              type="text"
              value={form.tags}
              onChange={(e) => update('tags', e.target.value)}
              placeholder="Ví dụ: món chay, nhanh gọn, mùa hè"
              className={inputCls}
            />
          </div>

          {/* Cover Image */}
          <div>
            <label className={labelCls}>Ảnh đại diện món ăn</label>
            {form.imagePreview ? (
              <div className="relative">
                <img
                  src={form.imagePreview}
                  alt="Cover"
                  className="w-full h-56 object-cover border border-border"
                />
                <button
                  onClick={() => update('imagePreview', '')}
                  className="absolute top-3 right-3 bg-foreground text-background p-1.5 hover:bg-primary transition-colors"
                  aria-label="Xóa ảnh"
                >
                  <Trash2 size={14} />
                </button>
              </div>
            ) : (
              <div
                onClick={() =>
                  update(
                    'imagePreview',
                    'https://images.unsplash.com/photo-1476224203421-9ac39bcb3327?w=800&h=500&fit=crop',
                  )
                }
                className="border-2 border-dashed border-border h-48 flex flex-col items-center justify-center gap-3 cursor-pointer hover:border-primary hover:bg-secondary/40 transition-colors"
              >
                <ImagePlus size={28} strokeWidth={1.5} className="text-muted-foreground" />
                <div className="text-center">
                  <p className="text-sm text-foreground font-medium">Nhấp để tải lên ảnh đại diện</p>
                  <p className="text-xs text-muted-foreground mt-1">Hỗ trợ JPG, PNG, WebP — tối đa 10 MB</p>
                </div>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Tab: Ingredients */}
      {activeTab === 'Nguyên liệu' && (
        <div>
          <div className="grid grid-cols-12 gap-3 mb-2 text-xs uppercase tracking-widest text-muted-foreground px-1">
            <span className="col-span-1">Lượng</span>
            <span className="col-span-1">Đơn vị</span>
            <span className="col-span-4">Tên nguyên liệu</span>
            <span className="col-span-5">Ghi chú</span>
            <span className="col-span-1" />
          </div>
          <div className="space-y-2">
            {form.ingredients.map((ing, idx) => (
              <div key={ing.id} className="grid grid-cols-12 gap-3 items-center group">
                <div className="col-span-1 flex items-center gap-1">
                  <GripVertical
                    size={14}
                    className="text-muted-foreground opacity-0 group-hover:opacity-60 cursor-grab shrink-0"
                  />
                  <input
                    type="text"
                    value={ing.qty}
                    onChange={(e) => updateIngredient(ing.id, 'qty', e.target.value)}
                    placeholder="500"
                    className="w-full border border-border bg-background px-2 py-2.5 text-sm focus:outline-none focus:border-primary text-center"
                  />
                </div>
                <input
                  type="text"
                  value={ing.unit}
                  onChange={(e) => updateIngredient(ing.id, 'unit', e.target.value)}
                  placeholder="g"
                  className="col-span-1 border border-border bg-background px-2 py-2.5 text-sm focus:outline-none focus:border-primary text-center"
                />
                <input
                  type="text"
                  value={ing.name}
                  onChange={(e) => updateIngredient(ing.id, 'name', e.target.value)}
                  placeholder={`Nguyên liệu ${idx + 1}`}
                  className="col-span-4 border border-border bg-background px-3 py-2.5 text-sm focus:outline-none focus:border-primary"
                />
                <input
                  type="text"
                  value={ing.notes}
                  onChange={(e) => updateIngredient(ing.id, 'notes', e.target.value)}
                  placeholder="Ghi chú (tùy chọn)"
                  className="col-span-5 border border-border bg-background px-3 py-2.5 text-sm focus:outline-none focus:border-primary placeholder:text-muted-foreground"
                />
                <button
                  onClick={() => removeIngredient(ing.id)}
                  disabled={form.ingredients.length <= 1}
                  className="col-span-1 flex justify-center p-2 text-muted-foreground hover:text-red-500 transition-colors disabled:opacity-30"
                  aria-label="Xóa nguyên liệu"
                >
                  <Trash2 size={15} />
                </button>
              </div>
            ))}
          </div>
          <button
            onClick={addIngredient}
            className="mt-4 flex items-center gap-2 text-sm text-primary hover:text-primary/80 transition-colors uppercase tracking-widest"
          >
            <Plus size={16} /> Thêm nguyên liệu
          </button>
        </div>
      )}

      {/* Tab: Method */}
      {activeTab === 'Cách làm' && (
        <div className="space-y-6">
          {form.steps.map((step, idx) => (
            <div key={step.id} className="group">
              <div className="flex items-start gap-4">
                <div className="w-9 h-9 border border-border flex items-center justify-center font-serif text-lg text-primary shrink-0 mt-0.5">
                  {idx + 1}
                </div>
                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-2">
                    <input
                      type="text"
                      value={step.title}
                      onChange={(e) => updateStep(step.id, 'title', e.target.value)}
                      placeholder={`Tiêu đề bước ${idx + 1} (ví dụ: Sơ chế nguyên liệu)`}
                      className="flex-1 border border-border bg-background px-3 py-2 text-sm font-medium focus:outline-none focus:border-primary"
                    />
                    <button
                      onClick={() => removeStep(step.id)}
                      disabled={form.steps.length <= 1}
                      className="p-1.5 text-muted-foreground hover:text-red-500 transition-colors disabled:opacity-30 opacity-0 group-hover:opacity-100"
                      aria-label="Xóa bước này"
                    >
                      <Trash2 size={15} />
                    </button>
                  </div>
                  <textarea
                    value={step.description}
                    onChange={(e) => updateStep(step.id, 'description', e.target.value)}
                    placeholder="Mô tả chi tiết các thao tác thực hiện bước này..."
                    rows={3}
                    className="w-full border border-border bg-background px-3 py-2.5 text-sm focus:outline-none focus:border-primary resize-none placeholder:text-muted-foreground"
                  />
                </div>
              </div>
            </div>
          ))}
          <button
            onClick={addStep}
            className="flex items-center gap-2 text-sm text-primary hover:text-primary/80 transition-colors uppercase tracking-widest"
          >
            <Plus size={16} /> Thêm bước thực hiện
          </button>
        </div>
      )}

      {/* Tab: Nutrition */}
      {activeTab === 'Dinh dưỡng' && (
        <div>
          <p className="text-muted-foreground text-sm mb-6 leading-relaxed">
            Thông tin dinh dưỡng trên mỗi khẩu phần (không bắt buộc). Hãy để trống các mục nếu bạn không muốn
            hiển thị trên công thức xuất bản.
          </p>
          <div className="grid grid-cols-2 sm:grid-cols-3 gap-5">
            {(
              [
                { key: 'calories', label: 'Calo', unit: 'kcal' },
                { key: 'protein', label: 'Chất đạm (Protein)', unit: 'g' },
                { key: 'carbs', label: 'Carbohydrate (Tinh bột)', unit: 'g' },
                { key: 'fat', label: 'Chất béo', unit: 'g' },
                { key: 'fiber', label: 'Chất xơ', unit: 'g' },
                { key: 'sodium', label: 'Natri (Sodium)', unit: 'mg' },
              ] as { key: keyof RecipeForm['nutrition']; label: string; unit: string }[]
            ).map(({ key, label, unit }) => (
              <div key={key}>
                <label className={labelCls}>{label}</label>
                <div className="relative">
                  <input
                    type="number"
                    min={0}
                    value={form.nutrition[key]}
                    onChange={(e) => updateNutrition(key, e.target.value)}
                    placeholder="—"
                    className="w-full border border-border bg-background px-4 py-3 pr-12 text-sm focus:outline-none focus:border-primary placeholder:text-muted-foreground"
                  />
                  <span className="absolute right-3 top-1/2 -translate-y-1/2 text-xs text-muted-foreground">
                    {unit}
                  </span>
                </div>
              </div>
            ))}
          </div>

          {/* Preview */}
          {Object.values(form.nutrition).some((v) => v !== '') && (
            <div className="mt-8 bg-secondary border border-border p-6">
              <h3 className="font-serif text-lg mb-4">
                Xem trước dinh dưỡng{' '}
                <span className="text-sm text-muted-foreground font-sans font-normal">
                  (trên mỗi khẩu phần)
                </span>
              </h3>
              <div className="grid grid-cols-3 gap-4">
                {(
                  [
                    { key: 'calories', label: 'Calo', unit: 'kcal' },
                    { key: 'protein', label: 'Chất đạm', unit: 'g' },
                    { key: 'carbs', label: 'Tinh bột', unit: 'g' },
                    { key: 'fat', label: 'Chất béo', unit: 'g' },
                    { key: 'fiber', label: 'Chất xơ', unit: 'g' },
                    { key: 'sodium', label: 'Natri', unit: 'mg' },
                  ] as { key: keyof RecipeForm['nutrition']; label: string; unit: string }[]
                ).map(({ key, label, unit }) =>
                  form.nutrition[key] ? (
                    <div key={key} className="text-center">
                      <p className="font-serif text-2xl text-foreground">{form.nutrition[key]}</p>
                      <p className="text-xs text-muted-foreground uppercase tracking-widest">
                        {label} ({unit})
                      </p>
                    </div>
                  ) : null,
                )}
              </div>
            </div>
          )}
        </div>
      )}

      {/* Bottom save bar */}
      <div className="mt-10 pt-6 border-t border-border flex items-center justify-between gap-4">
        <button
          onClick={() => navigate('/dashboard/recipes')}
          className="text-sm text-muted-foreground hover:text-primary uppercase tracking-widest transition-colors"
        >
          Hủy thay đổi
        </button>
        <div className="flex gap-3">
          {form.status === 'Draft' && (
            <button
              onClick={handlePublish}
              className="flex items-center gap-2 border border-green-300 text-green-700 hover:bg-green-50 px-5 py-2.5 text-sm uppercase tracking-widest transition-colors"
            >
              <CheckCircle size={15} /> Xuất bản
            </button>
          )}
          <button
            onClick={handleSave}
            className="flex items-center gap-2 bg-primary text-primary-foreground px-6 py-2.5 text-sm uppercase tracking-widest hover:bg-primary/90 transition-colors"
          >
            <Save size={15} /> Lưu công thức
          </button>
        </div>
      </div>
    </div>
  )
}
