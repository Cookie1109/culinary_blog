import { useState, useRef, useEffect } from 'react'
import { Outlet, Link, useNavigate, NavLink } from 'react-router'
import { ChefHat, Search, Menu, X, LayoutDashboard, LogOut, User, ChevronDown } from 'lucide-react'
import { useAuth } from '../../contexts/AuthContext'
import { NetworkStatusBanner } from '@/features/culinary/components/NetworkStatusBanner'
import { MaintenanceBanner } from '@/features/culinary/components/MaintenanceBanner'
import { useUIStore } from '@/store/useUIStore'

const FOCUSABLE_SELECTOR =
  'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])'

export function AppLayout() {
  const { user, logout } = useAuth()
  const mobileOpen = useUIStore((state) => state.sidebarOpen)
  const setMobileOpen = useUIStore((state) => state.setSidebarOpen)
  const [userMenuOpen, setUserMenuOpen] = useState(false)
  const navigate = useNavigate()
  const userMenuRef = useRef<HTMLDivElement>(null)
  const userMenuButtonRef = useRef<HTMLButtonElement>(null)
  const hamburgerButtonRef = useRef<HTMLButtonElement>(null)
  const mobileDrawerRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    function onClickOutside(e: MouseEvent) {
      if (userMenuRef.current && !userMenuRef.current.contains(e.target as Node)) {
        setUserMenuOpen(false)
      }
    }
    document.addEventListener('mousedown', onClickOutside)
    return () => document.removeEventListener('mousedown', onClickOutside)
  }, [])

  useEffect(() => {
    if (!userMenuOpen) return undefined
    const handleUserMenuKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        setUserMenuOpen(false)
        userMenuButtonRef.current?.focus()
      }
    }
    document.addEventListener('keydown', handleUserMenuKeyDown)
    return () => document.removeEventListener('keydown', handleUserMenuKeyDown)
  }, [userMenuOpen])

  useEffect(() => {
    if (mobileOpen) {
      document.body.style.overflow = 'hidden'
      window.requestAnimationFrame(() => {
        const focusable = mobileDrawerRef.current?.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR)
        if (focusable && focusable.length > 0) {
          focusable[0].focus()
        }
      })
    } else {
      document.body.style.overflow = ''
    }

    const handleKeyDown = (e: KeyboardEvent) => {
      if (!mobileOpen) return

      if (e.key === 'Escape') {
        e.preventDefault()
        setMobileOpen(false)
        hamburgerButtonRef.current?.focus()
        return
      }

      if (e.key === 'Tab' && mobileDrawerRef.current) {
        const elements = Array.from(
          mobileDrawerRef.current.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR),
        ).filter((el) => !el.hasAttribute('disabled'))

        if (elements.length === 0) {
          e.preventDefault()
          return
        }

        const first = elements[0]
        const last = elements[elements.length - 1]

        if (e.shiftKey) {
          if (document.activeElement === first || !mobileDrawerRef.current.contains(document.activeElement)) {
            e.preventDefault()
            last.focus()
          }
        } else {
          if (document.activeElement === last || !mobileDrawerRef.current.contains(document.activeElement)) {
            e.preventDefault()
            first.focus()
          }
        }
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => {
      document.body.style.overflow = ''
      document.removeEventListener('keydown', handleKeyDown)
    }
  }, [mobileOpen, setMobileOpen])

  const handleLogout = async () => {
    await logout()
    setUserMenuOpen(false)
    navigate('/')
  }

  const navLinkClass = ({ isActive }: { isActive: boolean }) =>
    `text-sm font-medium tracking-wide uppercase transition-colors ${
      isActive ? 'text-primary' : 'hover:text-primary'
    }`

  return (
    <div className="min-h-screen flex flex-col" style={{ colorScheme: 'light' }}>
      <a
        href="#main-content"
        className="sr-only focus:not-sr-only focus:fixed focus:top-4 focus:left-4 focus:z-50 focus:bg-primary focus:text-primary-foreground focus:px-4 focus:py-2 focus:text-sm focus:font-medium focus:shadow-lg focus:outline-none"
      >
        Chuyển đến nội dung chính
      </a>

      <header className="border-b border-border bg-background sticky top-0 z-30">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-20">
            {/* Mobile: hamburger */}
            <div className="flex items-center gap-3 lg:hidden">
              <button
                ref={hamburgerButtonRef}
                onClick={() => setMobileOpen(true)}
                className="p-2 text-foreground hover:text-primary transition-colors"
                aria-label="Mở menu điều hướng"
                aria-expanded={mobileOpen}
                aria-controls="mobile-nav-drawer"
              >
                <Menu size={24} strokeWidth={1.5} aria-hidden="true" />
              </button>
            </div>

            {/* Desktop nav left */}
            <nav className="hidden lg:flex items-center gap-8" aria-label="Điều hướng chính">
              <NavLink to="/recipes" className={navLinkClass}>
                Công thức
              </NavLink>
              <NavLink to="/categories" className={navLinkClass}>
                Danh mục
              </NavLink>
              <NavLink to="/search" className={navLinkClass}>
                Tìm kiếm
              </NavLink>
            </nav>

            {/* Center: logo */}
            <div className="flex-1 flex justify-center lg:flex-none">
              <Link to="/" className="flex items-center gap-2.5 group">
                <ChefHat
                  className="text-primary group-hover:rotate-12 transition-transform duration-300"
                  size={30}
                  strokeWidth={1.5}
                  aria-hidden="true"
                />
                <span className="font-serif text-2xl tracking-tight">Culinary Blog</span>
              </Link>
            </div>

            {/* Right: actions */}
            <div className="flex items-center gap-3">
              <Link
                to="/search"
                className="hidden lg:flex p-2 text-foreground hover:text-primary transition-colors"
                aria-label="Tìm kiếm"
              >
                <Search size={20} strokeWidth={1.5} aria-hidden="true" />
              </Link>

              {user ? (
                <div className="relative" ref={userMenuRef}>
                  <button
                    ref={userMenuButtonRef}
                    onClick={() => setUserMenuOpen((v) => !v)}
                    className="flex items-center gap-2 hover:text-primary transition-colors"
                    aria-expanded={userMenuOpen}
                    aria-haspopup="true"
                    aria-label={`Menu người dùng: ${user.name}`}
                  >
                    {user.avatar ? (
                      <img
                        src={user.avatar}
                        alt={`Ảnh đại diện của ${user.name}`}
                        className="w-8 h-8 rounded-full object-cover border border-border"
                      />
                    ) : (
                      <div className="w-8 h-8 rounded-full bg-secondary flex items-center justify-center text-sm font-serif text-foreground">
                        {user.name[0]}
                      </div>
                    )}
                    <span className="hidden sm:inline text-sm font-medium">{user.name.split(' ')[0]}</span>
                    <ChevronDown
                      size={16}
                      strokeWidth={1.5}
                      aria-hidden="true"
                      className={`transition-transform ${userMenuOpen ? 'rotate-180' : ''}`}
                    />
                  </button>

                  {userMenuOpen && (
                    <div
                      role="menu"
                      aria-label="Menu tài khoản"
                      className="absolute right-0 mt-2 w-52 bg-card border border-border shadow-md py-1 z-50"
                    >
                      <div className="px-4 py-3 border-b border-border">
                        <p className="font-medium text-foreground text-sm">{user.name}</p>
                        <p className="text-muted-foreground text-xs mt-0.5">{user.email}</p>
                      </div>
                      <Link
                        role="menuitem"
                        to="/profile"
                        onClick={() => setUserMenuOpen(false)}
                        className="flex items-center gap-3 px-4 py-3 text-sm hover:bg-secondary transition-colors"
                      >
                        <User size={16} strokeWidth={1.5} aria-hidden="true" />
                        Hồ sơ của tôi
                      </Link>
                      <Link
                        role="menuitem"
                        to="/dashboard"
                        onClick={() => setUserMenuOpen(false)}
                        className="flex items-center gap-3 px-4 py-3 text-sm hover:bg-secondary transition-colors"
                      >
                        <LayoutDashboard size={16} strokeWidth={1.5} aria-hidden="true" />
                        Bảng điều khiển
                      </Link>
                      <div className="border-t border-border mt-1 pt-1">
                        <button
                          role="menuitem"
                          onClick={handleLogout}
                          className="flex items-center gap-3 px-4 py-3 text-sm text-muted-foreground hover:text-primary hover:bg-secondary transition-colors w-full text-left"
                        >
                          <LogOut size={16} strokeWidth={1.5} aria-hidden="true" />
                          Đăng xuất
                        </button>
                      </div>
                    </div>
                  )}
                </div>
              ) : (
                <Link
                  to="/login"
                  className="flex items-center gap-2 text-sm font-medium tracking-wide uppercase hover:text-primary transition-colors"
                >
                  <span className="hidden sm:inline">Đăng nhập</span>
                  <User size={20} strokeWidth={1.5} aria-hidden="true" />
                </Link>
              )}
            </div>
          </div>
        </div>
      </header>

      <NetworkStatusBanner />
      <MaintenanceBanner />

      {/* Mobile menu overlay */}
      {mobileOpen && (
        <div
          id="mobile-nav-drawer"
          role="dialog"
          aria-modal="true"
          aria-label="Menu điều hướng di động"
          className="fixed inset-0 z-40 lg:hidden"
        >
          <div
            className="absolute inset-0 bg-foreground/30"
            onClick={() => {
              setMobileOpen(false)
              hamburgerButtonRef.current?.focus()
            }}
            aria-hidden="true"
          />
          <div
            ref={mobileDrawerRef}
            className="absolute left-0 top-0 bottom-0 w-72 max-w-[85vw] bg-background flex flex-col shadow-xl"
          >
            <div className="flex items-center justify-between px-6 h-20 border-b border-border">
              <Link
                to="/"
                onClick={() => {
                  setMobileOpen(false)
                  hamburgerButtonRef.current?.focus()
                }}
                className="flex items-center gap-2"
              >
                <ChefHat className="text-primary" size={26} strokeWidth={1.5} aria-hidden="true" />
                <span className="font-serif text-xl">Culinary Blog</span>
              </Link>
              <button
                type="button"
                onClick={() => {
                  setMobileOpen(false)
                  hamburgerButtonRef.current?.focus()
                }}
                className="p-2 text-foreground hover:text-primary transition-colors"
                aria-label="Đóng menu điều hướng"
              >
                <X size={22} strokeWidth={1.5} aria-hidden="true" />
              </button>
            </div>
            <nav className="flex-1 overflow-y-auto px-6 py-8" aria-label="Điều hướng di động">
              <ul className="space-y-1">
                {[
                  { to: '/', label: 'Trang chủ' },
                  { to: '/recipes', label: 'Công thức' },
                  { to: '/categories', label: 'Danh mục' },
                  { to: '/search', label: 'Tìm kiếm' },
                ].map(({ to, label }) => (
                  <li key={to}>
                    <NavLink
                      to={to}
                      end={to === '/'}
                      onClick={() => {
                        setMobileOpen(false)
                        hamburgerButtonRef.current?.focus()
                      }}
                      className={({ isActive }) =>
                        `block px-4 py-3 text-sm font-medium tracking-wide uppercase transition-colors ${
                          isActive
                            ? 'text-primary bg-primary/5'
                            : 'text-foreground hover:text-primary hover:bg-secondary'
                        }`
                      }
                    >
                      {label}
                    </NavLink>
                  </li>
                ))}
                {user && (
                  <>
                    <li className="pt-4 pb-2">
                      <span className="text-xs uppercase tracking-widest text-muted-foreground px-4">
                        Tài khoản
                      </span>
                    </li>
                    <li>
                      <NavLink
                        to="/profile"
                        onClick={() => {
                          setMobileOpen(false)
                          hamburgerButtonRef.current?.focus()
                        }}
                        className={({ isActive }) =>
                          `block px-4 py-3 text-sm font-medium uppercase tracking-wide transition-colors ${isActive ? 'text-primary bg-primary/5' : 'hover:text-primary hover:bg-secondary'}`
                        }
                      >
                        Hồ sơ của tôi
                      </NavLink>
                    </li>
                    <li>
                      <NavLink
                        to="/dashboard"
                        onClick={() => {
                          setMobileOpen(false)
                          hamburgerButtonRef.current?.focus()
                        }}
                        className={({ isActive }) =>
                          `block px-4 py-3 text-sm font-medium uppercase tracking-wide transition-colors ${isActive ? 'text-primary bg-primary/5' : 'hover:text-primary hover:bg-secondary'}`
                        }
                      >
                        Bảng điều khiển
                      </NavLink>
                    </li>
                  </>
                )}
              </ul>
            </nav>
            <div className="border-t border-border px-6 py-6">
              {user ? (
                <button
                  type="button"
                  onClick={() => {
                    handleLogout()
                    setMobileOpen(false)
                    hamburgerButtonRef.current?.focus()
                  }}
                  className="flex items-center gap-3 text-sm text-muted-foreground hover:text-primary transition-colors"
                >
                  <LogOut size={16} aria-hidden="true" />
                  Đăng xuất
                </button>
              ) : (
                <Link
                  to="/login"
                  onClick={() => {
                    setMobileOpen(false)
                    hamburgerButtonRef.current?.focus()
                  }}
                  className="block w-full text-center bg-foreground text-background px-6 py-3 text-sm uppercase tracking-widest font-medium hover:bg-primary transition-colors"
                >
                  Đăng nhập
                </Link>
              )}
            </div>
          </div>
        </div>
      )}

      <main id="main-content" tabIndex={-1} className="flex-1 outline-none">
        <Outlet />
      </main>

      <footer className="bg-secondary text-secondary-foreground border-t border-border pt-16 pb-8">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid grid-cols-1 md:grid-cols-4 gap-12 mb-12">
            <div className="md:col-span-2">
              <Link to="/" className="flex items-center gap-2 mb-6">
                <ChefHat className="text-primary" size={28} strokeWidth={1.5} aria-hidden="true" />
                <span className="font-serif text-2xl tracking-tight text-foreground">Culinary Blog</span>
              </Link>
              <p className="text-muted-foreground leading-relaxed max-w-sm">
                Bộ sưu tập công thức nấu ăn và ghi chú ẩm thực được tuyển chọn kỹ lưỡng. Chúng tôi tin vào
                nguyên liệu tươi ngon, nấu nướng chân thành và niềm vui sẻ chia bữa ăn.
              </p>
            </div>
            <div>
              <nav aria-label="Khám phá">
                <h3 className="font-serif text-lg mb-6 text-foreground">Khám phá</h3>
                <ul className="space-y-4">
                  <li>
                    <Link
                      to="/recipes"
                      className="text-muted-foreground hover:text-primary transition-colors"
                    >
                      Tất cả công thức
                    </Link>
                  </li>
                  <li>
                    <Link
                      to="/categories"
                      className="text-muted-foreground hover:text-primary transition-colors"
                    >
                      Danh mục
                    </Link>
                  </li>
                  <li>
                    <Link to="/search" className="text-muted-foreground hover:text-primary transition-colors">
                      Tìm kiếm
                    </Link>
                  </li>
                </ul>
              </nav>
            </div>
            <div>
              <nav aria-label="Kết nối">
                <h3 className="font-serif text-lg mb-6 text-foreground">Kết nối</h3>
                <ul className="space-y-4">
                  <li>
                    <a
                      href="#main-content"
                      className="text-muted-foreground hover:text-primary transition-colors"
                    >
                      Về chúng tôi
                    </a>
                  </li>
                  <li>
                    <a
                      href="#main-content"
                      className="text-muted-foreground hover:text-primary transition-colors"
                    >
                      Liên hệ
                    </a>
                  </li>
                  <li>
                    <a
                      href="#main-content"
                      className="text-muted-foreground hover:text-primary transition-colors"
                    >
                      Bản tin
                    </a>
                  </li>
                </ul>
              </nav>
            </div>
          </div>
          <div className="pt-8 border-t border-border/50 text-sm text-muted-foreground flex flex-col sm:flex-row justify-between items-center gap-4">
            <p>&copy; {new Date().getFullYear()} Culinary Blog. Đã đăng ký bản quyền.</p>
            <div className="flex gap-4">
              <a href="#main-content" className="hover:text-foreground transition-colors">
                Chính sách bảo mật
              </a>
              <a href="#main-content" className="hover:text-foreground transition-colors">
                Điều khoản dịch vụ
              </a>
            </div>
          </div>
        </div>
      </footer>
    </div>
  )
}
