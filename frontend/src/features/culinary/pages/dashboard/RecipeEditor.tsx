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
  'Baking',
  'Breakfast',
  'Desserts',
  'Dinner',
  'Salads',
  'Soups',
  'Vegetarian',
  'Quick Meals',
]

const EMPTY_FORM: RecipeForm = {
  title: '',
  description: '',
  category: 'Dinner',
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
  title: 'Rustic Sourdough Boule',
  description:
    'A deeply flavorful, crusty sourdough bread with an airy crumb. Perfect for morning toast or pairing with hearty soups. This recipe requires a mature starter and patience, but the results are entirely worth the effort.',
  category: 'Baking',
  difficulty: 'Hard',
  servings: 12,
  prepTime: 30,
  cookTime: 45,
  tags: 'sourdough, bread, baking, artisan',
  status: 'Published',
  imagePreview: 'https://images.unsplash.com/photo-1585478259715-876acc5be8eb?w=800&h=500&fit=crop',
  ingredients: [
    { id: 'i1', qty: '500', unit: 'g', name: 'Bread flour', notes: 'Unbleached, at least 12% protein' },
    { id: 'i2', qty: '350', unit: 'g', name: 'Water', notes: 'Room temperature (around 75°F)' },
    { id: 'i3', qty: '100', unit: 'g', name: 'Active sourdough starter', notes: 'Recently fed and bubbly' },
    { id: 'i4', qty: '10', unit: 'g', name: 'Fine sea salt', notes: '' },
  ],
  steps: [
    {
      id: 's1',
      title: 'Autolyse',
      description:
        'Combine flour and 325g water. Mix until a shaggy dough forms and no dry flour remains. Cover and rest for 1 hour.',
    },
    {
      id: 's2',
      title: 'Add Starter and Salt',
      description:
        'Add starter and remaining water. Dimple the dough and pinch in salt. Mix thoroughly for 5 minutes.',
    },
    {
      id: 's3',
      title: 'Bulk Fermentation',
      description:
        'Ferment at room temperature for 4–5 hours. Perform stretch and folds every 30 minutes during the first 2 hours.',
    },
    {
      id: 's4',
      title: 'Shape',
      description:
        'Pre-shape into a loose round. Rest 20 minutes. Final shape into a tight boule and place seam-side up in a banneton.',
    },
    {
      id: 's5',
      title: 'Cold Retard',
      description: 'Cover and refrigerate overnight (12–16 hours) to develop flavor.',
    },
    {
      id: 's6',
      title: 'Bake',
      description:
        'Preheat Dutch oven at 500°F for 1 hour. Score and bake covered 20 min, then uncovered at 450°F for 20–25 min until deeply browned.',
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

const TABS = ['Basic Info', 'Ingredients', 'Method', 'Nutrition'] as const
type Tab = (typeof TABS)[number]

export function RecipeEditor() {
  const { id } = useParams<{ id?: string }>()
  const navigate = useNavigate()
  const isEditing = Boolean(id)
  const [form, setForm] = useState<RecipeForm>(isEditing ? PREFILLED : EMPTY_FORM)
  const [activeTab, setActiveTab] = useState<Tab>('Basic Info')
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
            <ChevronLeft size={14} /> My Recipes
          </Link>
          <h1 className="font-serif text-3xl lg:text-4xl text-foreground">
            {isEditing ? 'Edit Recipe' : 'New Recipe'}
          </h1>
        </div>
        <div className="flex items-center gap-3 shrink-0 pt-1">
          {form.status === 'Draft' && (
            <button
              onClick={handlePublish}
              className="hidden sm:flex items-center gap-2 border border-green-300 text-green-700 hover:bg-green-50 px-4 py-2.5 text-xs uppercase tracking-widest transition-colors"
            >
              <CheckCircle size={14} /> Publish
            </button>
          )}
          <button
            onClick={handleSave}
            className="flex items-center gap-2 bg-primary text-primary-foreground px-5 py-2.5 text-sm uppercase tracking-widest hover:bg-primary/90 transition-colors"
          >
            <Save size={15} /> Save
          </button>
        </div>
      </div>

      {saved && (
        <div className="flex items-center gap-2 bg-green-50 border border-green-200 text-green-700 px-4 py-3 text-sm mb-6">
          <CheckCircle size={16} /> Recipe saved successfully.
        </div>
      )}

      {/* Status strip */}
      <div className="flex items-center gap-2 mb-8">
        <span className="text-xs uppercase tracking-widest text-muted-foreground">Status:</span>
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
            {s}
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
      {activeTab === 'Basic Info' && (
        <div className="space-y-6">
          <div>
            <label className={labelCls}>Recipe Title</label>
            <input
              type="text"
              value={form.title}
              onChange={(e) => update('title', e.target.value)}
              placeholder="e.g. Wild Mushroom Risotto"
              className={inputCls}
            />
          </div>

          <div>
            <label className={labelCls}>Description</label>
            <textarea
              value={form.description}
              onChange={(e) => update('description', e.target.value)}
              placeholder="A short, enticing description of the recipe..."
              rows={3}
              className={`${inputCls} resize-none`}
            />
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div>
              <label className={labelCls}>Category</label>
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
              <label className={labelCls}>Difficulty</label>
              <select
                value={form.difficulty}
                onChange={(e) => update('difficulty', e.target.value as RecipeForm['difficulty'])}
                className={inputCls}
              >
                <option>Easy</option>
                <option>Medium</option>
                <option>Hard</option>
              </select>
            </div>
            <div>
              <label className={labelCls}>Servings</label>
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
              <label className={labelCls}>Prep Time (mins)</label>
              <input
                type="number"
                min={0}
                value={form.prepTime}
                onChange={(e) => update('prepTime', parseInt(e.target.value) || 0)}
                className={inputCls}
              />
            </div>
            <div>
              <label className={labelCls}>Cook Time (mins)</label>
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
            <label className={labelCls}>Tags (comma separated)</label>
            <input
              type="text"
              value={form.tags}
              onChange={(e) => update('tags', e.target.value)}
              placeholder="e.g. vegetarian, quick, summer"
              className={inputCls}
            />
          </div>

          {/* Cover Image */}
          <div>
            <label className={labelCls}>Cover Image</label>
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
                  aria-label="Remove image"
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
                  <p className="text-sm text-foreground font-medium">Click to upload cover image</p>
                  <p className="text-xs text-muted-foreground mt-1">JPG, PNG, WebP — max 10 MB</p>
                </div>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Tab: Ingredients */}
      {activeTab === 'Ingredients' && (
        <div>
          <div className="grid grid-cols-12 gap-3 mb-2 text-xs uppercase tracking-widest text-muted-foreground px-1">
            <span className="col-span-1">Qty</span>
            <span className="col-span-1">Unit</span>
            <span className="col-span-4">Ingredient</span>
            <span className="col-span-5">Notes</span>
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
                  placeholder={`Ingredient ${idx + 1}`}
                  className="col-span-4 border border-border bg-background px-3 py-2.5 text-sm focus:outline-none focus:border-primary"
                />
                <input
                  type="text"
                  value={ing.notes}
                  onChange={(e) => updateIngredient(ing.id, 'notes', e.target.value)}
                  placeholder="Optional note"
                  className="col-span-5 border border-border bg-background px-3 py-2.5 text-sm focus:outline-none focus:border-primary placeholder:text-muted-foreground"
                />
                <button
                  onClick={() => removeIngredient(ing.id)}
                  disabled={form.ingredients.length <= 1}
                  className="col-span-1 flex justify-center p-2 text-muted-foreground hover:text-red-500 transition-colors disabled:opacity-30"
                  aria-label="Remove ingredient"
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
            <Plus size={16} /> Add Ingredient
          </button>
        </div>
      )}

      {/* Tab: Method */}
      {activeTab === 'Method' && (
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
                      placeholder={`Step ${idx + 1} title (e.g. Autolyse)`}
                      className="flex-1 border border-border bg-background px-3 py-2 text-sm font-medium focus:outline-none focus:border-primary"
                    />
                    <button
                      onClick={() => removeStep(step.id)}
                      disabled={form.steps.length <= 1}
                      className="p-1.5 text-muted-foreground hover:text-red-500 transition-colors disabled:opacity-30 opacity-0 group-hover:opacity-100"
                      aria-label="Remove step"
                    >
                      <Trash2 size={15} />
                    </button>
                  </div>
                  <textarea
                    value={step.description}
                    onChange={(e) => updateStep(step.id, 'description', e.target.value)}
                    placeholder="Describe this step in detail..."
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
            <Plus size={16} /> Add Step
          </button>
        </div>
      )}

      {/* Tab: Nutrition */}
      {activeTab === 'Nutrition' && (
        <div>
          <p className="text-muted-foreground text-sm mb-6 leading-relaxed">
            Optional nutritional information per serving. Leave fields blank to omit them from the published
            recipe.
          </p>
          <div className="grid grid-cols-2 sm:grid-cols-3 gap-5">
            {(
              [
                { key: 'calories', label: 'Calories', unit: 'kcal' },
                { key: 'protein', label: 'Protein', unit: 'g' },
                { key: 'carbs', label: 'Carbohydrates', unit: 'g' },
                { key: 'fat', label: 'Total Fat', unit: 'g' },
                { key: 'fiber', label: 'Dietary Fiber', unit: 'g' },
                { key: 'sodium', label: 'Sodium', unit: 'mg' },
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
                Nutrition Preview{' '}
                <span className="text-sm text-muted-foreground font-sans font-normal">(per serving)</span>
              </h3>
              <div className="grid grid-cols-3 gap-4">
                {(
                  [
                    { key: 'calories', label: 'Calories', unit: 'kcal' },
                    { key: 'protein', label: 'Protein', unit: 'g' },
                    { key: 'carbs', label: 'Carbs', unit: 'g' },
                    { key: 'fat', label: 'Fat', unit: 'g' },
                    { key: 'fiber', label: 'Fiber', unit: 'g' },
                    { key: 'sodium', label: 'Sodium', unit: 'mg' },
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
          Discard Changes
        </button>
        <div className="flex gap-3">
          {form.status === 'Draft' && (
            <button
              onClick={handlePublish}
              className="flex items-center gap-2 border border-green-300 text-green-700 hover:bg-green-50 px-5 py-2.5 text-sm uppercase tracking-widest transition-colors"
            >
              <CheckCircle size={15} /> Publish
            </button>
          )}
          <button
            onClick={handleSave}
            className="flex items-center gap-2 bg-primary text-primary-foreground px-6 py-2.5 text-sm uppercase tracking-widest hover:bg-primary/90 transition-colors"
          >
            <Save size={15} /> Save
          </button>
        </div>
      </div>
    </div>
  )
}
