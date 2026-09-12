import { useState, type FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { Eye, EyeOff, ChefHat, ArrowRight } from 'lucide-react'
import { useAuth } from '../../contexts/AuthContext'

export function Login() {
  const { login, isLoading } = useAuth()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState('')

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!email || !password) {
      setError('Vui lòng điền đầy đủ các trường.')
      return
    }
    try {
      setError('')
      await login(email, password)
      const returnTo = searchParams.get('returnTo')
      navigate(returnTo?.startsWith('/') && !returnTo.startsWith('//') ? returnTo : '/dashboard')
    } catch {
      setError('Email hoặc mật khẩu không chính xác.')
    }
  }

  return (
    <div className="min-h-[calc(100vh-160px)] grid grid-cols-1 lg:grid-cols-2">
      {/* Left: editorial panel */}
      <div className="hidden lg:block relative overflow-hidden bg-foreground">
        <img
          src="https://images.unsplash.com/photo-1495195134817-aeb325a55b65?w=900&h=1200&fit=crop"
          alt="Món ăn được trình bày đẹp mắt"
          className="absolute inset-0 w-full h-full object-cover opacity-60"
        />
        <div className="absolute inset-0 bg-gradient-to-t from-foreground/80 via-foreground/20 to-transparent" />
        <div className="relative h-full flex flex-col justify-between p-12">
          <Link to="/" className="flex items-center gap-2 text-white">
            <ChefHat size={28} strokeWidth={1.5} />
            <span className="font-serif text-2xl">Culinary Blog</span>
          </Link>
          <blockquote className="text-white">
            <p className="font-serif text-3xl leading-snug mb-4">
              "Nấu ăn vừa là trò chơi trẻ thơ, vừa là niềm vui của người trưởng thành. Và nấu ăn bằng cả tâm
              huyết chính là một hành động của tình yêu."
            </p>
            <footer className="text-white/60 text-sm uppercase tracking-widest">Craig Claiborne</footer>
          </blockquote>
        </div>
      </div>

      {/* Right: form */}
      <div className="flex items-center justify-center p-8 lg:p-16">
        <div className="w-full max-w-md">
          <div className="mb-10">
            <Link to="/" className="flex items-center gap-2 lg:hidden mb-8 text-foreground">
              <ChefHat className="text-primary" size={24} strokeWidth={1.5} />
              <span className="font-serif text-xl">Culinary Blog</span>
            </Link>
            <h1 className="font-serif text-4xl text-foreground mb-2">Chào mừng trở lại</h1>
            <p className="text-muted-foreground">Đăng nhập để quản lý công thức và hồ sơ của bạn.</p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-5">
            {error && (
              <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 text-sm">{error}</div>
            )}

            <div>
              <label
                htmlFor="email"
                className="block text-xs uppercase tracking-widest text-muted-foreground mb-2"
              >
                Địa chỉ email
              </label>
              <input
                id="email"
                type="email"
                autoComplete="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="your@email.com"
                className="w-full border border-border bg-background px-4 py-3 text-sm focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary placeholder:text-muted-foreground transition-all"
              />
            </div>

            <div>
              <div className="flex justify-between items-center mb-2">
                <label
                  htmlFor="password"
                  className="block text-xs uppercase tracking-widest text-muted-foreground"
                >
                  Mật khẩu
                </label>
                <a href="#" className="text-xs text-muted-foreground hover:text-primary transition-colors">
                  Quên mật khẩu?
                </a>
              </div>
              <div className="relative">
                <input
                  id="password"
                  type={showPassword ? 'text' : 'password'}
                  autoComplete="current-password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="••••••••"
                  className="w-full border border-border bg-background px-4 py-3 pr-12 text-sm focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary placeholder:text-muted-foreground transition-all"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((v) => !v)}
                  className="absolute right-4 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors"
                  aria-label={showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
                >
                  {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
                </button>
              </div>
            </div>

            <button
              type="submit"
              disabled={isLoading}
              className="w-full flex items-center justify-center gap-2 bg-primary text-primary-foreground py-3 text-sm font-medium uppercase tracking-widest hover:bg-primary/90 transition-colors disabled:opacity-60"
            >
              {isLoading ? (
                'Đang đăng nhập…'
              ) : (
                <>
                  Đăng nhập <ArrowRight size={16} />
                </>
              )}
            </button>
          </form>

          <p className="mt-8 text-sm text-center text-muted-foreground">
            Chưa có tài khoản?{' '}
            <Link to="/auth/register" className="text-primary hover:underline font-medium">
              Tạo tài khoản mới
            </Link>
          </p>
        </div>
      </div>
    </div>
  )
}
