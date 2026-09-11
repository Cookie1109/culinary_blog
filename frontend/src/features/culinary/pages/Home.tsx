import { Link } from 'react-router'
import { Clock, ArrowRight } from 'lucide-react'

const FEATURED_RECIPES = [
  {
    id: '1',
    title: 'Rustic Sourdough Boule',
    slug: 'rustic-sourdough-boule',
    description:
      'A deeply flavorful, crusty sourdough bread with an airy crumb. Perfect for morning toast or pairing with hearty soups.',
    image: 'https://images.unsplash.com/photo-1585478259715-876acc5be8eb?w=800&h=1000&fit=crop',
    prepTime: 30,
    cookTime: 45,
    category: 'Baking',
  },
  {
    id: '2',
    title: 'Wild Mushroom Risotto',
    slug: 'wild-mushroom-risotto',
    description:
      'Creamy Arborio rice simmered slowly with a medley of wild mushrooms, finished with aged Parmesan and white truffle oil.',
    image: 'https://images.unsplash.com/photo-1626844131082-256783844137?w=800&h=1000&fit=crop',
    prepTime: 15,
    cookTime: 40,
    category: 'Dinner',
  },
  {
    id: '3',
    title: 'Heirloom Tomato Galette',
    slug: 'heirloom-tomato-galette',
    description:
      'A free-form rustic tart showcasing the best of summer tomatoes, layered with herbed ricotta on a flaky butter crust.',
    image: 'https://images.unsplash.com/photo-1595854341625-f33ee10dbf94?w=800&h=1000&fit=crop',
    prepTime: 20,
    cookTime: 35,
    category: 'Vegetarian',
  },
]

export function Home() {
  return (
    <div className="pb-24">
      {/* Hero Section */}
      <section className="relative">
        <div className="absolute inset-0 bg-secondary/30" />
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-24 lg:py-32 relative">
          <div className="max-w-3xl">
            <span className="text-primary font-medium tracking-widest uppercase text-sm mb-4 block">
              New This Week
            </span>
            <h1 className="text-5xl lg:text-7xl font-serif text-foreground leading-[1.1] mb-6">
              The Art of Slow Roasting Tomatoes
            </h1>
            <p className="text-xl text-muted-foreground leading-relaxed mb-10 max-w-2xl">
              Discover how low heat and patience transform humble summer tomatoes into intensely sweet, deeply
              savory flavor bombs.
            </p>
            <Link
              to="/recipes/slow-roasted-tomatoes"
              className="inline-flex items-center gap-2 bg-primary text-primary-foreground px-8 py-4 uppercase tracking-widest text-sm font-medium hover:bg-primary/90 transition-colors"
            >
              Read the Guide
              <ArrowRight size={18} strokeWidth={2} />
            </Link>
          </div>
        </div>
      </section>

      {/* Featured Recipes - Asymmetric Grid */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pt-24">
        <div className="flex justify-between items-end mb-12 border-b border-border pb-6">
          <h2 className="text-3xl font-serif text-foreground">Featured Collection</h2>
          <Link
            to="/recipes"
            className="text-sm font-medium uppercase tracking-wide text-primary hover:text-foreground transition-colors"
          >
            View All
          </Link>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-12 gap-y-16 gap-x-8">
          {/* Large Feature */}
          <div className="md:col-span-7 group cursor-pointer">
            <Link to={`/recipes/${FEATURED_RECIPES[0].slug}`} className="block overflow-hidden bg-muted mb-6">
              <img
                src={FEATURED_RECIPES[0].image}
                alt={FEATURED_RECIPES[0].title}
                className="w-full h-[600px] object-cover group-hover:scale-105 transition-transform duration-700 ease-out"
              />
            </Link>
            <span className="text-muted-foreground text-xs uppercase tracking-widest mb-3 block">
              {FEATURED_RECIPES[0].category}
            </span>
            <h3 className="text-3xl font-serif mb-4 group-hover:text-primary transition-colors">
              <Link to={`/recipes/${FEATURED_RECIPES[0].slug}`}>{FEATURED_RECIPES[0].title}</Link>
            </h3>
            <p className="text-muted-foreground leading-relaxed mb-4 max-w-xl">
              {FEATURED_RECIPES[0].description}
            </p>
            <div className="flex items-center gap-6 text-sm text-muted-foreground">
              <div className="flex items-center gap-2">
                <Clock size={16} strokeWidth={1.5} />
                <span>{FEATURED_RECIPES[0].prepTime + FEATURED_RECIPES[0].cookTime} mins</span>
              </div>
            </div>
          </div>

          {/* Smaller Features */}
          <div className="md:col-span-4 md:col-start-9 flex flex-col gap-16">
            {FEATURED_RECIPES.slice(1).map((recipe) => (
              <div key={recipe.id} className="group cursor-pointer">
                <Link to={`/recipes/${recipe.slug}`} className="block overflow-hidden bg-muted mb-6">
                  <img
                    src={recipe.image}
                    alt={recipe.title}
                    className="w-full h-[300px] object-cover group-hover:scale-105 transition-transform duration-700 ease-out"
                  />
                </Link>
                <span className="text-muted-foreground text-xs uppercase tracking-widest mb-3 block">
                  {recipe.category}
                </span>
                <h3 className="text-2xl font-serif mb-3 group-hover:text-primary transition-colors">
                  <Link to={`/recipes/${recipe.slug}`}>{recipe.title}</Link>
                </h3>
                <div className="flex items-center gap-6 text-sm text-muted-foreground">
                  <div className="flex items-center gap-2">
                    <Clock size={16} strokeWidth={1.5} />
                    <span>{recipe.prepTime + recipe.cookTime} mins</span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Newsletter */}
      <section className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 pt-32 text-center">
        <div className="bg-secondary p-12 lg:p-20 border border-border">
          <h2 className="text-3xl font-serif text-foreground mb-4">The Weekly Digest</h2>
          <p className="text-muted-foreground mb-8 max-w-lg mx-auto">
            Join our community to receive seasonal recipes, cooking techniques, and kitchen stories delivered
            to your inbox every Sunday.
          </p>
          <form className="flex flex-col sm:flex-row gap-4 max-w-md mx-auto">
            <input
              type="email"
              placeholder="Your email address"
              className="flex-1 bg-background border border-border px-4 py-3 focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary transition-all placeholder:text-muted-foreground"
            />
            <button
              type="submit"
              className="bg-foreground text-background px-8 py-3 uppercase tracking-widest text-sm font-medium hover:bg-primary transition-colors whitespace-nowrap"
            >
              Subscribe
            </button>
          </form>
        </div>
      </section>
    </div>
  )
}
