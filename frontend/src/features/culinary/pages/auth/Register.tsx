import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { Eye, EyeOff, ChefHat, ArrowRight } from 'lucide-react'
import { useAuth } from '../../contexts/AuthContext'

export function Register() {
  const { register, isLoading } = useAuth()
  const navigate = useNavigate()
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirm, setConfirm] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState('')

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!name || !email || !password || !confirm) {
      setError('Vui lòng điền đầy đủ các trường.')
      return
    }
    if (password !== confirm) {
      setError('Mật khẩu xác nhận không khớp.')
      return
    }
    if (
      password.length < 8 ||
      !/[a-z]/.test(password) ||
      !/[A-Z]/.test(password) ||
      !/[0-9]/.test(password) ||
      !/[^a-zA-Z0-9]/.test(password)
    ) {
      setError('Mật khẩu cần tối thiểu 8 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.')
      return
    }
    try {
      setError('')
      await register(name, email, password)
      navigate('/dashboard')
    } catch {
      setError('Đăng ký thất bại. Vui lòng thử lại.')
    }
  }

  return (
    <div className="min-h-[calc(100vh-160px)] grid grid-cols-1 lg:grid-cols-2">
      {/* Left: editorial panel */}
      <div className="hidden lg:block relative overflow-hidden bg-foreground">
        <img
          src="https://images.unsplash.com/photo-1466637574441-749b8f19452f?w=900&h=1200&fit=crop"
          alt="Nguyên liệu và dụng cụ nấu nướng trên mặt bàn gỗ"
          className="absolute inset-0 w-full h-full object-cover opacity-55"
        />
        <div className="absolute inset-0 bg-gradient-to-t from-foreground/80 via-foreground/20 to-transparent" />
        <div className="relative h-full flex flex-col justify-between p-12">
          <Link to="/" className="flex items-center gap-2 text-white">
            <ChefHat size={28} strokeWidth={1.5} />
            <span className="font-serif text-2xl">Culinary Blog</span>
          </Link>
          <div className="text-white">
            <p className="font-serif text-3xl leading-snug mb-6">
              Chia sẻ niềm đam mê ẩm thực cùng cộng đồng trân quý từng nguyên liệu.
            </p>
            <ul className="space-y-3 text-white/70 text-sm">
              {[
                'Đăng tải và quản lý không giới hạn công thức',
                'Tải ảnh và xây dựng hồ sơ ẩm thực cá nhân',
                'Tìm kiếm toàn diện các món ăn Việt Nam & quốc tế',
              ].map((item) => (
                <li key={item} className="flex items-start gap-2">
                  <span className="text-primary mt-0.5">—</span>
                  {item}
                </li>
              ))}
            </ul>
          </div>
        </div>
      </div>

      {/* Right: form */}
      <div className="flex items-center justify-center p-8 lg:p-16 overflow-y-auto">
        <div className="w-full max-w-md">
          <div className="mb-10">
            <Link to="/" className="flex items-center gap-2 lg:hidden mb-8 text-foreground">
              <ChefHat className="text-primary" size={24} strokeWidth={1.5} />
              <span className="font-serif text-xl">Culinary Blog</span>
            </Link>
            <h1 className="font-serif text-4xl text-foreground mb-2">Tạo tài khoản</h1>
            <p className="text-muted-foreground">Bắt đầu chia sẻ công thức nấu ăn của bạn ngay hôm nay.</p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-5">
            {error && (
              <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 text-sm">{error}</div>
            )}

            <div>
              <label
                htmlFor="name"
                className="block text-xs uppercase tracking-widest text-muted-foreground mb-2"
              >
                Họ và tên
              </label>
              <input
                id="name"
                type="text"
                autoComplete="name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Họ và tên của bạn"
                className="w-full border border-border bg-background px-4 py-3 text-sm focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary placeholder:text-muted-foreground transition-all"
              />
            </div>

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
              <label
                htmlFor="password"
                className="block text-xs uppercase tracking-widest text-muted-foreground mb-2"
              >
                Mật khẩu
              </label>
              <div className="relative">
                <input
                  id="password"
                  type={showPassword ? 'text' : 'password'}
                  autoComplete="new-password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="Tối thiểu 8 ký tự"
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

            <div>
              <label
                htmlFor="confirm"
                className="block text-xs uppercase tracking-widest text-muted-foreground mb-2"
              >
                Xác nhận mật khẩu
              </label>
              <input
                id="confirm"
                type={showPassword ? 'text' : 'password'}
                autoComplete="new-password"
                value={confirm}
                onChange={(e) => setConfirm(e.target.value)}
                placeholder="Nhập lại mật khẩu"
                className="w-full border border-border bg-background px-4 py-3 text-sm focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary placeholder:text-muted-foreground transition-all"
              />
            </div>

            <p className="text-xs text-muted-foreground">
              Bằng việc tạo tài khoản, bạn đồng ý với{' '}
              <a href="#" className="underline hover:text-primary">
                Điều khoản dịch vụ
              </a>{' '}
              và{' '}
              <a href="#" className="underline hover:text-primary">
                Chính sách bảo mật
              </a>{' '}
              của chúng tôi.
            </p>

            <button
              type="submit"
              disabled={isLoading}
              className="w-full flex items-center justify-center gap-2 bg-primary text-primary-foreground py-3 text-sm font-medium uppercase tracking-widest hover:bg-primary/90 transition-colors disabled:opacity-60"
            >
              {isLoading ? (
                'Đang tạo tài khoản…'
              ) : (
                <>
                  Đăng ký tài khoản <ArrowRight size={16} />
                </>
              )}
            </button>
          </form>

          <p className="mt-8 text-sm text-center text-muted-foreground">
            Đã có tài khoản?{' '}
            <Link to="/auth/login" className="text-primary hover:underline font-medium">
              Đăng nhập
            </Link>
          </p>
        </div>
      </div>
    </div>
  )
}
