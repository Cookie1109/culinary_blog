import { Link, useNavigate } from 'react-router'
import {
  BookOpen,
  CheckCircle,
  FileText,
  Archive,
  PlusCircle,
  ArrowRight,
  TrendingUp,
  Eye,
} from 'lucide-react'
import { useAuth } from '../../contexts/AuthContext'

const RECENT_RECIPES = [
  {
    id: '1',
    title: 'Rustic Sourdough Boule',
    category: 'Baking',
    status: 'Published',
    date: 'Sep 1, 2026',
    views: 1247,
  },
  {
    id: '2',
    title: 'Wild Mushroom Risotto',
    category: 'Dinner',
    status: 'Published',
    date: 'Aug 28, 2026',
    views: 892,
  },
  {
    id: '3',
    title: 'Heirloom Tomato Galette',
    category: 'Vegetarian',
    status: 'Published',
    date: 'Aug 15, 2026',
    views: 634,
  },
  { id: '4', title: 'Cast Iron Ribeye', category: 'Dinner', status: 'Draft', date: 'Sep 10, 2026', views: 0 },
  {
    id: '5',
    title: 'Chocolate Soufflé',
    category: 'Desserts',
    status: 'Draft',
    date: 'Sep 9, 2026',
    views: 0,
  },
]

const STATUS_BADGE: Record<string, string> = {
  Published: 'bg-green-50 text-green-700 border border-green-200',
  Draft: 'bg-amber-50 text-amber-700 border border-amber-200',
  Archived: 'bg-secondary text-muted-foreground border border-border',
}

export function Dashboard() {
  const { user } = useAuth()
  const navigate = useNavigate()

  if (!user) {
    return (
      <div className="text-center py-16">
        <p className="font-serif text-2xl mb-4">You must be signed in to view the dashboard.</p>
        <button
          onClick={() => navigate('/auth/login')}
          className="bg-primary text-primary-foreground px-8 py-3 text-sm uppercase tracking-widest hover:bg-primary/90 transition-colors"
        >
          Sign In
        </button>
      </div>
    )
  }

  return (
    <div>
      {/* Welcome */}
      <div className="mb-10">
        <h1 className="font-serif text-3xl lg:text-4xl text-foreground mb-1">
          Good morning, {user.name.split(' ')[0]}.
        </h1>
        <p className="text-muted-foreground">Here's what's happening with your recipes.</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-10">
        {[
          { label: 'Total Recipes', value: '18', icon: BookOpen, color: 'text-foreground' },
          { label: 'Published', value: '12', icon: CheckCircle, color: 'text-green-600' },
          { label: 'Drafts', value: '4', icon: FileText, color: 'text-amber-600' },
          { label: 'Archived', value: '2', icon: Archive, color: 'text-muted-foreground' },
        ].map(({ label, value, icon: Icon, color }) => (
          <div key={label} className="bg-background border border-border p-5 lg:p-6">
            <div className="flex items-start justify-between mb-3">
              <span className="text-xs uppercase tracking-widest text-muted-foreground">{label}</span>
              <Icon size={18} strokeWidth={1.5} className={color} />
            </div>
            <p className="font-serif text-3xl text-foreground">{value}</p>
          </div>
        ))}
      </div>

      {/* Quick actions */}
      <div className="flex flex-wrap gap-3 mb-10">
        <Link
          to="/dashboard/recipes/new"
          className="flex items-center gap-2 bg-primary text-primary-foreground px-5 py-2.5 text-sm uppercase tracking-widest hover:bg-primary/90 transition-colors"
        >
          <PlusCircle size={16} /> New Recipe
        </Link>
        <Link
          to="/dashboard/recipes"
          className="flex items-center gap-2 border border-border px-5 py-2.5 text-sm uppercase tracking-widest hover:bg-secondary transition-colors"
        >
          <BookOpen size={16} /> Manage Recipes
        </Link>
        <Link
          to="/categories"
          className="flex items-center gap-2 border border-border px-5 py-2.5 text-sm uppercase tracking-widest hover:bg-secondary transition-colors"
        >
          <TrendingUp size={16} /> Categories
        </Link>
      </div>

      {/* Recent recipes */}
      <div className="bg-background border border-border">
        <div className="flex items-center justify-between px-6 py-4 border-b border-border">
          <h2 className="font-serif text-xl">Recent Recipes</h2>
          <Link
            to="/dashboard/recipes"
            className="flex items-center gap-1.5 text-xs uppercase tracking-widest text-primary hover:text-primary/80 transition-colors"
          >
            View All <ArrowRight size={13} />
          </Link>
        </div>

        {/* Table */}
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border">
                <th className="text-left px-6 py-3 text-xs uppercase tracking-widest text-muted-foreground font-medium">
                  Title
                </th>
                <th className="text-left px-6 py-3 text-xs uppercase tracking-widest text-muted-foreground font-medium hidden sm:table-cell">
                  Category
                </th>
                <th className="text-left px-6 py-3 text-xs uppercase tracking-widest text-muted-foreground font-medium">
                  Status
                </th>
                <th className="text-right px-6 py-3 text-xs uppercase tracking-widest text-muted-foreground font-medium hidden lg:table-cell">
                  <Eye size={14} className="inline" />
                </th>
                <th className="text-right px-6 py-3 text-xs uppercase tracking-widest text-muted-foreground font-medium hidden md:table-cell">
                  Date
                </th>
                <th className="px-6 py-3" />
              </tr>
            </thead>
            <tbody>
              {RECENT_RECIPES.map((recipe) => (
                <tr
                  key={recipe.id}
                  className="border-b border-border last:border-0 hover:bg-secondary/40 transition-colors"
                >
                  <td className="px-6 py-4">
                    <span className="font-medium text-foreground">{recipe.title}</span>
                  </td>
                  <td className="px-6 py-4 text-muted-foreground hidden sm:table-cell">{recipe.category}</td>
                  <td className="px-6 py-4">
                    <span
                      className={`inline-block px-2.5 py-1 text-xs rounded-none ${STATUS_BADGE[recipe.status]}`}
                    >
                      {recipe.status}
                    </span>
                  </td>
                  <td className="px-6 py-4 text-right text-muted-foreground hidden lg:table-cell">
                    {recipe.views > 0 ? recipe.views.toLocaleString() : '—'}
                  </td>
                  <td className="px-6 py-4 text-right text-muted-foreground hidden md:table-cell">
                    {recipe.date}
                  </td>
                  <td className="px-6 py-4 text-right">
                    <Link
                      to={`/dashboard/recipes/${recipe.id}/edit`}
                      className="text-xs uppercase tracking-widest text-muted-foreground hover:text-primary transition-colors"
                    >
                      Edit
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
