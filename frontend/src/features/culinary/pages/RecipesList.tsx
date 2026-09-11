import { Link } from 'react-router'
import { Clock } from 'lucide-react'

const DUMMY_LIST = [
  {
    id: '1',
    title: 'Rustic Sourdough Boule',
    slug: 'rustic-sourdough-boule',
    category: 'Baking',
    image: 'https://images.unsplash.com/photo-1585478259715-876acc5be8eb?w=800&h=800&fit=crop',
    time: 75,
  },
  {
    id: '2',
    title: 'Wild Mushroom Risotto',
    slug: 'wild-mushroom-risotto',
    category: 'Dinner',
    image: 'https://images.unsplash.com/photo-1626844131082-256783844137?w=800&h=800&fit=crop',
    time: 55,
  },
  {
    id: '3',
    title: 'Heirloom Tomato Galette',
    slug: 'heirloom-tomato-galette',
    category: 'Vegetarian',
    image: 'https://images.unsplash.com/photo-1595854341625-f33ee10dbf94?w=800&h=800&fit=crop',
    time: 55,
  },
  {
    id: '4',
    title: 'Cast Iron Ribeye',
    slug: 'cast-iron-ribeye',
    category: 'Dinner',
    image: 'https://images.unsplash.com/photo-1544025162-d76694265947?w=800&h=800&fit=crop',
    time: 25,
  },
  {
    id: '5',
    title: 'Classic French Omelette',
    slug: 'classic-french-omelette',
    category: 'Breakfast',
    image: 'https://images.unsplash.com/photo-1510693206972-df098062cb71?w=800&h=800&fit=crop',
    time: 15,
  },
  {
    id: '6',
    title: 'Miso Glazed Eggplant',
    slug: 'miso-glazed-eggplant',
    category: 'Vegetarian',
    image: 'https://images.unsplash.com/photo-1580476262798-bddd9f4b7369?w=800&h=800&fit=crop',
    time: 40,
  },
]

export function RecipesList() {
  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16">
      <header className="text-center mb-16">
        <h1 className="text-4xl md:text-5xl font-serif text-foreground mb-4">The Recipe Index</h1>
        <p className="text-muted-foreground text-lg max-w-2xl mx-auto">
          Browse our complete collection of seasonal recipes, from weeknight dinners to weekend baking
          projects.
        </p>
      </header>

      {/* Filters (Visual only) */}
      <div className="flex flex-wrap items-center justify-center gap-4 mb-16 border-b border-border pb-8">
        <button className="text-sm uppercase tracking-widest font-medium text-primary border-b border-primary pb-1">
          All
        </button>
        <button className="text-sm uppercase tracking-widest font-medium text-muted-foreground hover:text-foreground transition-colors pb-1">
          Baking
        </button>
        <button className="text-sm uppercase tracking-widest font-medium text-muted-foreground hover:text-foreground transition-colors pb-1">
          Dinner
        </button>
        <button className="text-sm uppercase tracking-widest font-medium text-muted-foreground hover:text-foreground transition-colors pb-1">
          Vegetarian
        </button>
        <button className="text-sm uppercase tracking-widest font-medium text-muted-foreground hover:text-foreground transition-colors pb-1">
          Breakfast
        </button>
      </div>

      {/* Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-x-8 gap-y-16">
        {DUMMY_LIST.map((recipe) => (
          <div key={recipe.id} className="group cursor-pointer">
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
            <div className="flex items-center justify-between mb-2">
              <span className="text-muted-foreground text-xs uppercase tracking-widest">
                {recipe.category}
              </span>
              <div className="flex items-center gap-1.5 text-sm text-muted-foreground">
                <Clock size={14} />
                <span>{recipe.time}m</span>
              </div>
            </div>
            <h3 className="text-2xl font-serif group-hover:text-primary transition-colors">
              <Link to={`/recipes/${recipe.slug}`}>{recipe.title}</Link>
            </h3>
          </div>
        ))}
      </div>

      {/* Pagination (Visual) */}
      <div className="mt-20 flex justify-center gap-2">
        <button className="w-10 h-10 border border-foreground bg-foreground text-background font-serif text-lg">
          1
        </button>
        <button className="w-10 h-10 border border-border text-muted-foreground hover:border-foreground hover:text-foreground transition-colors font-serif text-lg">
          2
        </button>
        <button className="w-10 h-10 border border-border text-muted-foreground hover:border-foreground hover:text-foreground transition-colors font-serif text-lg">
          3
        </button>
        <span className="w-10 h-10 flex items-center justify-center text-muted-foreground">...</span>
        <button className="w-10 h-10 border border-border text-muted-foreground hover:border-foreground hover:text-foreground transition-colors font-serif text-lg">
          12
        </button>
      </div>
    </div>
  )
}
