import { Clock, Users, Printer, Share2, ChefHat, BookmarkPlus } from 'lucide-react'

const DUMMY_RECIPE = {
  title: 'Rustic Sourdough Boule',
  description:
    'A deeply flavorful, crusty sourdough bread with an airy crumb. Perfect for morning toast or pairing with hearty soups. This recipe requires a mature starter and patience, but the results are entirely worth the effort.',
  author: 'Eleanor Vance',
  date: 'Oct 12, 2026',
  prepTime: 30,
  cookTime: 45,
  servings: 12,
  difficulty: 'Hard',
  image: 'https://images.unsplash.com/photo-1585478259715-876acc5be8eb?w=1600&h=900&fit=crop',
  ingredients: [
    { qty: '500', unit: 'g', name: 'Bread flour', notes: 'Unbleached, at least 12% protein' },
    { qty: '350', unit: 'g', name: 'Water', notes: 'At room temperature (around 75°F)' },
    { qty: '100', unit: 'g', name: 'Active sourdough starter', notes: 'Recently fed and bubbly' },
    { qty: '10', unit: 'g', name: 'Fine sea salt', notes: '' },
    { qty: '', unit: '', name: 'Rice flour', notes: 'For dusting the banneton' },
  ],
  steps: [
    {
      step: 1,
      title: 'Autolyse',
      description:
        'In a large mixing bowl, combine the bread flour and 325g of the water. Mix by hand until a shaggy dough forms and no dry flour remains. Cover with a damp towel and let rest for 1 hour.',
    },
    {
      step: 2,
      title: 'Add Starter and Salt',
      description:
        'Add the active starter and the remaining 25g of water to the dough. Dimple the dough with wet fingers and pinch the salt over the top. Mix thoroughly by folding the dough over itself for about 5 minutes.',
    },
    {
      step: 3,
      title: 'Bulk Fermentation',
      description:
        'Cover the bowl and let the dough ferment at room temperature for 4-5 hours. During the first 2 hours, perform a set of stretch and folds every 30 minutes to build strength.',
    },
    {
      step: 4,
      title: 'Shaping',
      description:
        'Turn the dough out onto a lightly floured surface. Pre-shape into a loose round and let rest for 20 minutes. Final shape the dough into a tight boule and place seam-side up in a rice flour-dusted banneton.',
    },
    {
      step: 5,
      title: 'Cold Retard',
      description:
        'Place the banneton in a plastic bag or cover tightly, then refrigerate overnight (12-16 hours) to develop flavor.',
    },
    {
      step: 6,
      title: 'Baking',
      description:
        'Preheat a Dutch oven in the oven at 500°F (260°C) for 1 hour. Turn the dough out onto parchment paper, score the top with a lame or sharp razor, and carefully transfer to the Dutch oven. Bake covered for 20 minutes, then remove the lid, reduce heat to 450°F (230°C), and bake for another 20-25 minutes until deeply browned.',
    },
  ],
}

const DIFFICULTY_MAP: Record<string, string> = {
  Easy: 'Dễ',
  Medium: 'Trung bình',
  Hard: 'Nâng cao',
}

export function RecipeDetail() {
  return (
    <article className="pb-24">
      {/* Header / Hero */}
      <header className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 pt-16 pb-12 text-center">
        <div className="flex items-center justify-center gap-2 text-sm uppercase tracking-widest text-primary mb-6">
          <span>Làm bánh</span>
          <span>&bull;</span>
          <span>{DIFFICULTY_MAP[DUMMY_RECIPE.difficulty] ?? DUMMY_RECIPE.difficulty}</span>
        </div>
        <h1 className="text-4xl sm:text-5xl lg:text-6xl font-serif text-foreground leading-[1.1] mb-6">
          {DUMMY_RECIPE.title}
        </h1>
        <p className="text-lg text-muted-foreground max-w-2xl mx-auto leading-relaxed mb-8">
          {DUMMY_RECIPE.description}
        </p>

        <div className="flex flex-wrap items-center justify-center gap-6 text-sm text-muted-foreground border-y border-border py-4">
          <div className="flex items-center gap-2">
            <img
              src="https://images.unsplash.com/photo-1438761681033-6461ffad8d80?w=100&h=100&fit=crop"
              alt={DUMMY_RECIPE.author}
              className="w-8 h-8 rounded-full object-cover"
            />
            <span className="text-foreground font-medium">{DUMMY_RECIPE.author}</span>
          </div>
          <span className="hidden sm:inline">&bull;</span>
          <span>{DUMMY_RECIPE.date}</span>
          <span className="hidden sm:inline">&bull;</span>
          <div className="flex gap-4">
            <button className="flex items-center gap-1.5 hover:text-primary transition-colors">
              <Printer size={16} /> In công thức
            </button>
            <button className="flex items-center gap-1.5 hover:text-primary transition-colors">
              <Share2 size={16} /> Chia sẻ
            </button>
            <button className="flex items-center gap-1.5 hover:text-primary transition-colors">
              <BookmarkPlus size={16} /> Lưu món
            </button>
          </div>
        </div>
      </header>

      {/* Hero Image */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 mb-16">
        <div className="aspect-[16/9] w-full overflow-hidden bg-muted">
          <img src={DUMMY_RECIPE.image} alt={DUMMY_RECIPE.title} className="w-full h-full object-cover" />
        </div>
      </div>

      {/* Recipe Content */}
      <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-16">
          {/* Sidebar - Ingredients & Meta */}
          <div className="lg:col-span-4">
            {/* Meta Info */}
            <div className="bg-secondary p-8 mb-10">
              <div className="space-y-6">
                <div className="flex justify-between items-center border-b border-border/50 pb-4">
                  <span className="text-sm font-medium uppercase tracking-wider text-muted-foreground flex items-center gap-2">
                    <Clock size={16} /> Chuẩn bị
                  </span>
                  <span className="font-serif text-lg">{DUMMY_RECIPE.prepTime} phút</span>
                </div>
                <div className="flex justify-between items-center border-b border-border/50 pb-4">
                  <span className="text-sm font-medium uppercase tracking-wider text-muted-foreground flex items-center gap-2">
                    <ChefHat size={16} /> Thời gian nấu
                  </span>
                  <span className="font-serif text-lg">{DUMMY_RECIPE.cookTime} phút</span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-sm font-medium uppercase tracking-wider text-muted-foreground flex items-center gap-2">
                    <Users size={16} /> Khẩu phần
                  </span>
                  <span className="font-serif text-lg">{DUMMY_RECIPE.servings} phần ăn</span>
                </div>
              </div>
            </div>

            {/* Ingredients */}
            <div>
              <h3 className="text-2xl font-serif text-foreground mb-6 pb-2 border-b border-foreground">
                Nguyên liệu
              </h3>
              <ul className="space-y-4">
                {DUMMY_RECIPE.ingredients.map((item, idx) => (
                  <li key={idx} className="flex gap-4 group">
                    <div className="flex-none w-16 text-right">
                      <span className="font-serif text-lg text-primary">
                        {item.qty} {item.unit}
                      </span>
                    </div>
                    <div className="flex-1">
                      <span className="text-foreground">{item.name}</span>
                      {item.notes && (
                        <span className="block text-sm text-muted-foreground mt-0.5">{item.notes}</span>
                      )}
                    </div>
                  </li>
                ))}
              </ul>
            </div>
          </div>

          {/* Main Content - Instructions */}
          <div className="lg:col-span-8">
            <h3 className="text-3xl font-serif text-foreground mb-8 pb-2 border-b border-border">
              Các bước thực hiện
            </h3>
            <div className="space-y-12">
              {DUMMY_RECIPE.steps.map((step) => (
                <div key={step.step} className="flex gap-6 lg:gap-8">
                  <div className="flex-none pt-1">
                    <div className="w-10 h-10 border border-border flex items-center justify-center font-serif text-xl text-primary bg-background">
                      {step.step}
                    </div>
                  </div>
                  <div className="flex-1">
                    <h4 className="text-xl font-serif mb-3 text-foreground">{step.title}</h4>
                    <p className="text-lg leading-relaxed text-muted-foreground/90 text-foreground">
                      {step.description}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </article>
  )
}
