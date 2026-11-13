import Image from 'next/image'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate } from 'react-router'
import { Eye, EyeOff, ChefHat, ArrowRight } from 'lucide-react'
import { ApiProblem, authErrorMessage } from '@/lib/api/problem-details'
import { useToast } from '@/components/ui/toast'
import type { RegisterValues } from '../../forms/auth-schemas'
import { useAuth } from '../../contexts/AuthContext'
import { setServerFieldErrors } from '../../forms/server-errors'
import { formMessages } from '../../forms/messages'

export function Register() {
  const { showToast } = useToast()
  const { register: createAccount, isLoading } = useAuth()
  const navigate = useNavigate()
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState('')
  const {
    register,
    handleSubmit,
    setError: setFieldError,
    formState: { errors: fieldErrors, isSubmitting },
  } = useForm<RegisterValues>({
    resolver: async (...args) => (await import('../../forms/deferred-resolvers')).registerResolver(...args),
    defaultValues: { name: '', email: '', password: '', confirm: '' },
  })

  const submit = async ({ name, email, password }: RegisterValues) => {
    try {
      setError('')
      await createAccount(name, email, password)
      showToast('Đăng ký thành công.')
      navigate('/')
    } catch (failure) {
      showToast(authErrorMessage(failure))
      setServerFieldErrors(failure, setFieldError, {
        Email: ['email', formMessages.emailInvalid],
        DisplayName: ['name', formMessages.displayName],
        Password: ['password', formMessages.passwordStrength],
      })
      if (failure instanceof ApiProblem && failure.problem.code === 'AUTH_EMAIL_EXISTS') {
        setFieldError('email', { type: 'server', message: authErrorMessage(failure) })
      } else setError(authErrorMessage(failure))
    }
  }

  return (
    <div className="min-h-[calc(100vh-160px)] grid grid-cols-1 lg:grid-cols-2">
      {/* Left: editorial panel */}
      <div className="hidden lg:block relative overflow-hidden bg-foreground">
        <Image
          fill
          sizes="50vw"
          unoptimized
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

          <form
            noValidate
            onSubmit={handleSubmit(submit, () => setError('Vui lòng kiểm tra các trường được đánh dấu.'))}
            className="space-y-5"
          >
            {error && (
              <div role="alert" className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 text-sm">
                {error}
              </div>
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
                required
                aria-invalid={Boolean(fieldErrors.name)}
                aria-describedby={fieldErrors.name ? 'name-error' : undefined}
                type="text"
                autoComplete="name"
                {...register('name')}
                placeholder="Họ và tên của bạn"
                className="w-full border border-border bg-background px-4 py-3 text-sm focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary placeholder:text-muted-foreground transition-all"
              />
              {fieldErrors.name && (
                <p id="name-error" className="mt-2 text-sm text-red-700">
                  {fieldErrors.name.message}
                </p>
              )}
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
                required
                aria-invalid={Boolean(fieldErrors.email)}
                aria-describedby={fieldErrors.email ? 'email-error' : undefined}
                type="email"
                autoComplete="email"
                {...register('email')}
                placeholder="your@email.com"
                className="w-full border border-border bg-background px-4 py-3 text-sm focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary placeholder:text-muted-foreground transition-all"
              />
              {fieldErrors.email && (
                <p id="email-error" className="mt-2 text-sm text-red-700">
                  {fieldErrors.email.message}
                </p>
              )}
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
                  required
                  aria-invalid={Boolean(fieldErrors.password)}
                  aria-describedby={`password-help${fieldErrors.password ? ' password-error' : ''}`}
                  type={showPassword ? 'text' : 'password'}
                  autoComplete="new-password"
                  {...register('password')}
                  placeholder="Tạo mật khẩu"
                  className="w-full border border-border bg-background px-4 py-3 pr-12 text-sm focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary placeholder:text-muted-foreground transition-all"
                />
                {fieldErrors.password && (
                  <p id="password-error" className="mt-2 text-sm text-red-700">
                    {fieldErrors.password.message}
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

            <p id="password-help" className="text-sm text-muted-foreground">
              Tối thiểu 8 ký tự, có chữ hoa, chữ thường, số và ký tự đặc biệt.
            </p>

            <div>
              <label
                htmlFor="confirm"
                className="block text-xs uppercase tracking-widest text-muted-foreground mb-2"
              >
                Xác nhận mật khẩu
              </label>
              <input
                id="confirm"
                required
                aria-invalid={Boolean(fieldErrors.confirm)}
                aria-describedby={fieldErrors.confirm ? 'confirm-error' : undefined}
                type={showPassword ? 'text' : 'password'}
                autoComplete="new-password"
                {...register('confirm')}
                placeholder="Nhập lại mật khẩu"
                className="w-full border border-border bg-background px-4 py-3 text-sm focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary placeholder:text-muted-foreground transition-all"
              />
              {fieldErrors.confirm && (
                <p id="confirm-error" className="mt-2 text-sm text-red-700">
                  {fieldErrors.confirm.message}
                </p>
              )}
            </div>

            <button
              type="submit"
              disabled={isLoading || isSubmitting}
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
            <Link to="/login" className="text-primary hover:underline font-medium">
              Đăng nhập
            </Link>
          </p>
        </div>
      </div>
    </div>
  )
}
