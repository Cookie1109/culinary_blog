import { Outlet, Link, useNavigate, useSearchParams } from 'react-router'
import { usePathname } from 'next/navigation'
import {
  LayoutDashboard,
  BookOpen,
  Tag,
  LogOut,
  ChevronDown,
  Activity,
  ExternalLink,
  Shield,
} from 'lucide-react'
import { useAuth } from '../../contexts/AuthContext'

const NAV_ITEMS = [
  { to: '/dashboard', label: 'Tổng quan', icon: LayoutDashboard, end: true },
  { to: '/dashboard/recipes', label: 'Công thức của tôi', icon: BookOpen, end: true },
]

export function DashboardLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const pathname = usePathname()
  const [searchParams] = useSearchParams()
  const systemRecipe = user?.role === 'admin' && searchParams.get('from') === 'system'

  const handleLogout = async () => {
    await logout()
    navigate('/')
  }

  const linkClass = ({ isActive }: { isActive: boolean }) =>
    `flex items-center gap-3 px-4 py-3 text-sm font-medium transition-colors ${
      isActive
        ? 'bg-primary/10 text-primary border-l-2 border-primary'
        : 'text-foreground hover:bg-secondary hover:text-primary border-l-2 border-transparent'
    }`

  const navigationGroups = [
    { label: 'Cá nhân', items: NAV_ITEMS },
    ...(user?.role === 'admin'
      ? [
          {
            label: 'Quản trị hệ thống',
            items: [
              { to: '/dashboard/system', label: 'Tổng quan hệ thống', icon: Shield, end: true },
              { to: '/dashboard/categories', label: 'Quản lý danh mục', icon: Tag, end: true },
            ],
          },
        ]
      : []),
  ]
  const navigationItems = navigationGroups.flatMap((group) => group.items)

  const isActive = (to: string, end: boolean) => {
    const target = to.split('?')[0]
    const pathMatches = end ? pathname === target : pathname === target || pathname.startsWith(`${target}/`)
    if (to === '/dashboard/recipes') return pathname.startsWith('/dashboard/recipes') && !systemRecipe
    return (
      pathMatches ||
      (to === '/dashboard/system' &&
        (pathname === '/dashboard/audit' || (pathname.startsWith('/dashboard/recipes') && systemRecipe)))
    )
  }
  const currentPage =
    pathname === '/dashboard/audit'
      ? 'Lịch sử hệ thống'
      : pathname.endsWith('/new')
        ? 'Tạo công thức mới'
        : pathname.endsWith('/preview')
          ? 'Xem trước công thức'
          : pathname.endsWith('/edit')
            ? 'Chỉnh sửa công thức'
            : (navigationItems.find((item) => isActive(item.to, item.end))?.label ?? 'Bảng điều khiển')

  return (
    <div className="flex min-h-[calc(100vh-80px)]">
      {/* Sidebar */}
      <aside className="hidden md:flex flex-col w-60 border-r border-border bg-background shrink-0">
        {/* User card */}
        {user && (
          <div className="px-4 py-6 border-b border-border">
            <Link
              to="/profile"
              aria-label="Đi đến hồ sơ của tôi"
              className="flex items-center gap-3 hover:text-primary transition-colors"
            >
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
                <p className="text-xs text-muted-foreground">
                  {user.role === 'admin' ? 'Quản trị viên' : 'Tác giả'}
                </p>
              </div>
            </Link>
          </div>
        )}

        {/* Primary nav */}
        <nav aria-label="Điều hướng khu vực quản lý" className="flex-1 py-4">
          {navigationGroups.map((group) => (
            <section key={group.label} className="mb-5 last:mb-0">
              <h2 className="mb-2 px-4 text-xs uppercase tracking-widest text-muted-foreground">
                {group.label}
              </h2>
              <ul className="space-y-0.5">
                {group.items.map(({ to, label, icon: Icon, end }) => (
                  <li key={to}>
                    <Link
                      to={to}
                      aria-current={isActive(to, end) ? 'page' : undefined}
                      className={linkClass({ isActive: isActive(to, end) })}
                    >
                      <Icon size={18} strokeWidth={1.5} aria-hidden="true" />
                      <span>{label}</span>
                    </Link>
                  </li>
                ))}
              </ul>
            </section>
          ))}

          {user?.role === 'admin' && (
            <div className="mt-6 pt-4 border-t border-border">
              <p className="px-4 mb-2 text-xs uppercase tracking-widest text-muted-foreground">
                Vận hành (Admin)
              </p>
              <ul className="space-y-0.5">
                <li>
                  <a
                    href="/jobs"
                    aria-label="Tác vụ nền Hangfire, mở trong tab mới"
                    target="_blank"
                    rel="noreferrer"
                    className="flex items-center justify-between px-4 py-3 text-sm font-medium text-foreground hover:bg-secondary hover:text-primary transition-colors border-l-2 border-transparent"
                  >
                    <div className="flex items-center gap-3">
                      <Activity size={18} strokeWidth={1.5} />
                      <span>Tác vụ nền (Hangfire)</span>
                    </div>
                    <ExternalLink size={14} className="text-muted-foreground" />
                  </a>
                </li>
              </ul>
            </div>
          )}
        </nav>

        {/* Sign out */}
        <div className="border-t border-border px-4 py-4">
          <button
            onClick={handleLogout}
            className="flex items-center gap-3 text-sm text-muted-foreground hover:text-primary transition-colors w-full py-2"
          >
            <LogOut size={16} strokeWidth={1.5} />
            Đăng xuất
          </button>
        </div>
      </aside>

      {/* Content */}
      <div className="flex-1 flex flex-col min-w-0">
        <details
          className="border-b border-border bg-background md:hidden"
          onClick={(event) => {
            if ((event.target as HTMLElement).closest('a')) event.currentTarget.open = false
          }}
        >
          <summary className="flex cursor-pointer list-none items-center justify-between gap-3 px-4 py-3 text-sm font-medium [&::-webkit-details-marker]:hidden">
            <span>{currentPage}</span>
            <ChevronDown size={16} aria-hidden="true" />
          </summary>
          <nav aria-label="Điều hướng quản lý trên điện thoại" className="space-y-4 px-4 pb-4">
            {navigationGroups.map((group) => (
              <section key={group.label}>
                <h2 className="mb-2 text-xs uppercase tracking-widest text-muted-foreground">
                  {group.label}
                </h2>
                <ul className="grid grid-cols-2 gap-2">
                  {group.items.map(({ to, label, end }) => (
                    <li key={to}>
                      <Link
                        to={to}
                        aria-current={isActive(to, end) ? 'page' : undefined}
                        className={`block border px-3 py-2 text-sm ${isActive(to, end) ? 'border-primary text-primary bg-primary/5' : 'border-border hover:bg-secondary'}`}
                      >
                        {label}
                      </Link>
                    </li>
                  ))}
                </ul>
              </section>
            ))}
            {user?.role === 'admin' && (
              <a
                href="/jobs"
                target="_blank"
                rel="noreferrer"
                className="inline-flex items-center gap-2 text-sm text-muted-foreground"
              >
                Tác vụ nền (Hangfire)
                <ExternalLink size={14} aria-label="Mở trong tab mới" />
              </a>
            )}
          </nav>
        </details>

        <div className="flex-1 p-4 sm:p-6 lg:p-8">
          <div className="mx-auto w-full max-w-7xl">
            <Outlet />
          </div>
        </div>
      </div>
    </div>
  )
}
