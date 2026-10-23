'use client'

import { useState, type FormEvent } from 'react'
import { signIn } from 'next-auth/react'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { Eye, EyeOff, ChefHat, ArrowRight } from 'lucide-react'
import loginBackground from '@/assets/login-cooking-background.webp'
import { useAuth } from '../../contexts/AuthContext'

export function Login({ googleEnabled }: { googleEnabled: boolean }) {
  const { login, isLoading } = useAuth()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState('')
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    const errors: Record<string, string> = {}
    if (!email.trim()) errors.email = 'Vui lòng nhập email.'
    else if ((document.getElementById('email') as HTMLInputElement).validity.typeMismatch)
      errors.email = 'Địa chỉ email chưa hợp lệ.'
    if (!password) errors.password = 'Vui lòng nhập mật khẩu.'
    setFieldErrors(errors)
    if (Object.keys(errors).length) {
      setError('Vui lòng kiểm tra các trường được đánh dấu.')
      document.getElementById(Object.keys(errors)[0])?.focus()
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
    <div className="grid min-h-svh grid-cols-1 lg:grid-cols-2">
      {/* Left: editorial panel */}
      <div className="hidden lg:block relative overflow-hidden bg-background">
        <img
          src={loginBackground.src}
          alt="Rau thơm, cà chua và dụng cụ nấu ăn trên mặt bàn sáng dưới ánh sáng tự nhiên"
          className="absolute inset-0 w-full h-full object-cover"
        />
        <div className="absolute inset-0 bg-gradient-to-t from-background/95 via-background/20 to-transparent" />
        <div className="relative flex h-full min-h-svh flex-col justify-between gap-16 p-12">
          <Link to="/" className="flex items-center gap-2 text-foreground">
            <ChefHat className="text-primary" size={28} strokeWidth={1.5} />
            <span className="font-serif text-2xl">Culinary Blog</span>
          </Link>
          <blockquote className="max-w-2xl text-foreground">
            <p className="mb-4 font-serif text-xl leading-relaxed xl:text-2xl">
              "Nấu ăn vừa là trò chơi trẻ thơ, vừa là niềm vui của người trưởng thành. Và nấu ăn bằng cả tâm
              huyết chính là một hành động của tình yêu."
            </p>
            <footer className="text-sm uppercase tracking-widest text-muted-foreground">
              Craig Claiborne
            </footer>
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

          <form noValidate onSubmit={handleSubmit} className="space-y-5">
            {searchParams.get('passwordChanged') === '1' && (
              <p role="status" className="border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-700">
                Mật khẩu đã được cập nhật. Vui lòng đăng nhập lại bằng mật khẩu mới.
              </p>
            )}
            {error && (
              <div role="alert" className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 text-sm">
                {error}
              </div>
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
                required
                aria-invalid={Boolean(fieldErrors.email)}
                aria-describedby={fieldErrors.email ? 'email-error' : undefined}
                type="email"
                autoComplete="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="your@email.com"
                className="w-full border border-border bg-background px-4 py-3 text-sm focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary placeholder:text-muted-foreground transition-all"
              />
              {fieldErrors.email && (
                <p id="email-error" className="mt-2 text-sm text-red-700">
                  {fieldErrors.email}
                </p>
              )}
            </div>

            <div>
              <div className="flex justify-between items-center mb-2">
                <label
                  htmlFor="password"
                  className="block text-xs uppercase tracking-widest text-muted-foreground"
                >
                  Mật khẩu
                </label>
                <Link to="/forgot-password" className="text-xs text-primary hover:underline">
                  Quên mật khẩu?
                </Link>
              </div>
              <div className="relative">
                <input
                  id="password"
                  required
                  aria-invalid={Boolean(fieldErrors.password)}
                  aria-describedby={fieldErrors.password ? 'password-error' : undefined}
                  type={showPassword ? 'text' : 'password'}
                  autoComplete="current-password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="••••••••"
                  className="w-full border border-border bg-background px-4 py-3 pr-12 text-sm focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary placeholder:text-muted-foreground transition-all"
                />
                {fieldErrors.password && (
                  <p id="password-error" className="mt-2 text-sm text-red-700">
                    {fieldErrors.password}
                  </p>
                )}
                <button
                  type="button"
                  onClick={() => setShowPassword((v) => !v)}
                  className="absolute right-4 top-6 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors"
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

          {googleEnabled && (
            <button
              type="button"
              onClick={() => {
                const returnTo = searchParams.get('returnTo')
                const callbackUrl =
                  returnTo?.startsWith('/') && !returnTo.startsWith('//') ? returnTo : '/dashboard'
                void signIn('google', { callbackUrl })
              }}
              className="mt-4 flex w-full items-center justify-center gap-3 border border-border px-4 py-3 text-sm font-medium hover:bg-secondary"
            >
              <svg
                width="20"
                height="20"
                viewBox="0 0 24 24"
                aria-hidden="true"
                focusable="false"
                className="shrink-0"
              >
                <path
                  fill="#4285F4"
                  d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92a5.06 5.06 0 0 1-2.2 3.32v2.77h3.56c2.08-1.92 3.28-4.74 3.28-8.1Z"
                />
                <path
                  fill="#34A853"
                  d="M12 23c2.97 0 5.46-.98 7.28-2.65l-3.56-2.77c-.98.66-2.24 1.06-3.72 1.06-2.86 0-5.29-1.93-6.16-4.53H2.16v2.84A11 11 0 0 0 12 23Z"
                />
                <path
                  fill="#FBBC05"
                  d="M5.84 14.09A6.6 6.6 0 0 1 5.5 12c0-.73.12-1.43.34-2.09V7.07H2.16A11 11 0 0 0 1 12c0 1.78.43 3.46 1.16 4.93l3.68-2.84Z"
                />
                <path
                  fill="#EA4335"
                  d="M12 5.36c1.62 0 3.07.56 4.21 1.64l3.16-3.16A10.6 10.6 0 0 0 12 1a11 11 0 0 0-9.84 6.07l3.68 2.84c.87-2.6 3.3-4.55 6.16-4.55Z"
                />
              </svg>
              Tiếp tục với Google
            </button>
          )}

          <p className="mt-8 text-sm text-center text-muted-foreground">
            Chưa có tài khoản?{' '}
            <Link to="/register" className="text-primary hover:underline font-medium">
              Tạo tài khoản mới
            </Link>
          </p>
        </div>
      </div>
    </div>
  )
}
