import { Outlet, NavLink, Link, useNavigate } from 'react-router'
import { LayoutDashboard, BookOpen, PlusCircle, Tag, User, LogOut, ChevronRight } from 'lucide-react'
import { useAuth } from '../../contexts/AuthContext'

const NAV_ITEMS = [
  { to: '/dashboard', label: 'Overview', icon: LayoutDashboard, end: true },
  { to: '/dashboard/recipes', label: 'My Recipes', icon: BookOpen, end: false },
  { to: '/dashboard/recipes/new', label: 'New Recipe', icon: PlusCircle, end: true },
]

const SECONDARY_ITEMS = [
  { to: '/categories', label: 'Categories', icon: Tag },
  { to: '/profile', label: 'My Profile', icon: User },
]

export function DashboardLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  const handleLogout = () => {
    logout()
    navigate('/')
  }

  const linkClass = ({ isActive }: { isActive: boolean }) =>
    `flex items-center gap-3 px-4 py-3 text-sm font-medium transition-colors ${
      isActive
        ? 'bg-primary/10 text-primary border-l-2 border-primary'
        : 'text-foreground hover:bg-secondary hover:text-primary border-l-2 border-transparent'
    }`

  return (
    <div className="flex min-h-[calc(100vh-80px)]">
      {/* Sidebar */}
      <aside className="hidden md:flex flex-col w-60 border-r border-border bg-background shrink-0">
        {/* User card */}
        {user && (
          <div className="px-4 py-6 border-b border-border">
            <div className="flex items-center gap-3">
              {user.avatar ? (
                <img
                  src={user.avatar}
                  alt={user.name}
                  className="w-10 h-10 rounded-full object-cover border border-border"
                />
              ) : (
                <div className="w-10 h-10 rounded-full bg-secondary flex items-center justify-center font-serif text-foreground">
                  {user.name[0]}
                </div>
              )}
              <div className="min-w-0">
                <p className="text-sm font-medium truncate">{user.name}</p>
                <p className="text-xs text-muted-foreground capitalize">{user.role}</p>
              </div>
            </div>
          </div>
        )}

        {/* Primary nav */}
        <nav className="flex-1 py-4">
          <ul className="space-y-0.5">
            {NAV_ITEMS.map(({ to, label, icon: Icon, end }) => (
              <li key={to}>
                <NavLink to={to} end={end} className={linkClass}>
                  <Icon size={18} strokeWidth={1.5} />
                  <span>{label}</span>
                </NavLink>
              </li>
            ))}
          </ul>

          <div className="mt-8 pt-4 border-t border-border">
            <p className="px-4 mb-2 text-xs uppercase tracking-widest text-muted-foreground">Site</p>
            <ul className="space-y-0.5">
              {SECONDARY_ITEMS.map(({ to, label, icon: Icon }) => (
                <li key={to}>
                  <Link
                    to={to}
                    className="flex items-center gap-3 px-4 py-3 text-sm font-medium text-foreground hover:bg-secondary hover:text-primary transition-colors border-l-2 border-transparent"
                  >
                    <Icon size={18} strokeWidth={1.5} />
                    <span>{label}</span>
                  </Link>
                </li>
              ))}
            </ul>
          </div>
        </nav>

        {/* Sign out */}
        <div className="border-t border-border px-4 py-4">
          <button
            onClick={handleLogout}
            className="flex items-center gap-3 text-sm text-muted-foreground hover:text-primary transition-colors w-full py-2"
          >
            <LogOut size={16} strokeWidth={1.5} />
            Sign Out
          </button>
        </div>
      </aside>

      {/* Content */}
      <div className="flex-1 flex flex-col min-w-0">
        {/* Mobile breadcrumb bar */}
        <div className="md:hidden flex items-center gap-2 px-4 py-3 border-b border-border bg-secondary/40 text-sm text-muted-foreground">
          <Link to="/dashboard" className="hover:text-primary transition-colors">
            Dashboard
          </Link>
          <ChevronRight size={14} />
          <span className="text-foreground font-medium">Current Page</span>
        </div>

        {/* Mobile secondary nav */}
        <nav className="md:hidden overflow-x-auto border-b border-border bg-background">
          <ul className="flex px-4 py-2 gap-1 min-w-max">
            {NAV_ITEMS.map(({ to, label, end }) => (
              <li key={to}>
                <NavLink
                  to={to}
                  end={end}
                  className={({ isActive }) =>
                    `px-4 py-2 text-xs font-medium uppercase tracking-wide rounded-none transition-colors ${
                      isActive
                        ? 'text-primary border-b-2 border-primary'
                        : 'text-muted-foreground hover:text-foreground'
                    }`
                  }
                >
                  {label}
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>

        <main className="flex-1 p-6 lg:p-8">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
