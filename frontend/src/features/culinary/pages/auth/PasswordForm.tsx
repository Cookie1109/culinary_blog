'use client'

import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { FieldError } from '@/components/ui/field-error'
import { Link, useNavigate } from 'react-router'
import { changePassword, forgotPassword, resetPassword } from '@/lib/api/auth-client'
import { ApiProblem, apiErrorMessage } from '@/lib/api/problem-details'
import { useToast } from '@/components/ui/toast'
import type { PasswordValues } from '../../forms/auth-schemas'
import { setServerFieldErrors } from '../../forms/server-errors'
import { formMessages } from '../../forms/messages'
import { useAuth } from '../../contexts/AuthContext'

export function PasswordForm({ mode }: { mode: 'change' | 'forgot' | 'reset' }) {
  const { showToast } = useToast()
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const [token, setToken] = useState('')
  const [ready, setReady] = useState(mode !== 'reset')
  const {
    register,
    handleSubmit,
    resetField,
    setValue,
    setError: setFieldError,
    formState: { errors, isSubmitting: busy },
  } = useForm<PasswordValues>({
    resolver: async (...args) =>
      (await import('../../forms/deferred-resolvers')).passwordResolver(mode)(...args),
    defaultValues: { email: '', currentPassword: '', newPassword: '', confirmPassword: '' },
  })
  const [linkEmail, setLinkEmail] = useState('')
  const [success, setSuccess] = useState(false)
  const [sentEmail, setSentEmail] = useState('')
  const [resendAfter, setResendAfter] = useState(0)
  const [error, setError] = useState('')
  const { title, instructions, submitLabel } = {
    change: {
      title: 'Đổi mật khẩu',
      instructions: 'Nhập mật khẩu hiện tại và mật khẩu mới. Sau khi lưu, bạn cần đăng nhập lại.',
      submitLabel: 'Đổi mật khẩu',
    },
    forgot: {
      title: 'Quên mật khẩu',
      instructions: 'Nhập email đã dùng để đăng ký. Mở liên kết trong email để chọn mật khẩu mới.',
      submitLabel: 'Gửi liên kết đặt lại mật khẩu',
    },
    reset: {
      title: 'Đặt lại mật khẩu',
      instructions: 'Nhập mật khẩu mới cho tài khoản của bạn.',
      submitLabel: 'Đặt lại mật khẩu',
    },
  }[mode]
  const resendLabel = resendAfter > 0 ? `Gửi lại sau ${resendAfter} giây` : 'Gửi lại liên kết'
  const requestLabel = mode === 'forgot' && resendAfter > 0 ? resendLabel : submitLabel
  const invalidLink = ready && mode === 'reset' && (!linkEmail || !token)
  const inputClass =
    'w-full border border-border bg-background px-4 py-3 text-sm focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary'

  useEffect(() => {
    if (mode === 'forgot' && user?.email) setValue('email', user.email)
  }, [mode, user?.email, setValue])

  useEffect(() => {
    if (resendAfter <= 0) return undefined
    const timeout = window.setTimeout(() => setResendAfter((seconds) => seconds - 1), 1000)
    return () => window.clearTimeout(timeout)
  }, [resendAfter])

  useEffect(() => {
    if (mode !== 'reset') return
    const parameters = new URLSearchParams(window.location.hash.slice(1))
    const email = parameters.get('email') ?? ''
    setLinkEmail(email)
    setValue('email', email)
    setToken(parameters.get('token') ?? '')
    setReady(true)
  }, [mode, setValue])

  const submit = async ({ email, currentPassword, newPassword }: PasswordValues) => {
    if (!ready || invalidLink) return
    setError('')
    try {
      if (mode === 'forgot') {
        if (resendAfter > 0) return
        await forgotPassword(email.trim())
        setSentEmail(email.trim())
        setResendAfter(60)
      } else if (mode === 'change') await changePassword(currentPassword, newPassword)
      else await resetPassword(email, token, newPassword)
      setSuccess(true)
      showToast(mode === 'forgot' ? 'Đã tiếp nhận yêu cầu đặt lại mật khẩu.' : 'Mật khẩu đã được cập nhật.')
      resetField('currentPassword')
      resetField('newPassword')
      resetField('confirmPassword')
      if (mode === 'reset') window.history.replaceState(null, '', window.location.pathname)
      if (mode !== 'forgot') await logout().catch(() => {})
      if (mode === 'change') navigate('/login?passwordChanged=1')
    } catch (failure) {
      if (failure instanceof ApiProblem && failure.problem.code === 'AUTH_RESET_TOKEN_INVALID') setToken('')
      if (failure instanceof ApiProblem && failure.problem.code === 'AUTH_CURRENT_PASSWORD_INVALID')
        setFieldError('currentPassword', { type: 'server', message: apiErrorMessage(failure) })
      setServerFieldErrors(failure, setFieldError, {
        Email: ['email', formMessages.emailInvalid],
        CurrentPassword: ['currentPassword', formMessages.passwordRequired],
        NewPassword: ['newPassword', formMessages.passwordStrength],
      })
      setError(apiErrorMessage(failure, 'PASSWORD_REQUEST_FAILED'))
      showToast(apiErrorMessage(failure, 'PASSWORD_REQUEST_FAILED'))
    }
  }

  return (
    <section
      className={
        mode === 'change' ? 'mt-12 border-t border-border pt-8' : 'mx-auto w-full max-w-md px-4 py-20'
      }
      aria-labelledby={`${mode}-title`}
    >
      {mode === 'change' ? (
        <h2 id={`${mode}-title`} className="mb-4 font-serif text-2xl">
          {title}
        </h2>
      ) : (
        <h1 id={`${mode}-title`} className="mb-4 font-serif text-3xl">
          {title}
        </h1>
      )}
      {(error || invalidLink) && (
        <p
          id={`${mode}-error`}
          role="alert"
          className="mb-5 border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"
        >
          {error || 'Liên kết đặt lại mật khẩu không hợp lệ. Vui lòng yêu cầu liên kết mới.'}
        </p>
      )}
      {success ? (
        <div>
          {mode === 'forgot' ? (
            <>
              <div role="status" className="mb-6 space-y-3 text-sm">
                <h2 className="font-serif text-xl">Kiểm tra hộp thư của bạn</h2>
                <p>
                  Đã tiếp nhận yêu cầu cho <strong className="wrap-anywhere">{sentEmail}</strong>. Nếu email
                  thuộc tài khoản có mật khẩu đang hoạt động, bạn sẽ nhận được thư đặt lại mật khẩu.
                </p>
                <p className="text-muted-foreground">
                  Mở thư “Đặt lại mật khẩu Culinary Blog” và bấm liên kết trong thư. Liên kết có hiệu lực
                  trong 30 phút và chỉ dùng được một lần.
                </p>
                <p className="text-muted-foreground">
                  Chưa thấy thư? Kiểm tra mục Thư rác hoặc Spam và địa chỉ email đã nhập.
                </p>
              </div>
              <div className="flex flex-wrap items-center gap-4 text-sm">
                <button
                  type="button"
                  disabled={busy || resendAfter > 0}
                  onClick={handleSubmit(submit)}
                  className="bg-primary px-4 py-3 font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-60"
                >
                  {busy ? 'Đang gửi…' : resendLabel}
                </button>
                <button
                  type="button"
                  disabled={busy}
                  onClick={() => {
                    setSuccess(false)
                    setError('')
                  }}
                  className="text-primary hover:underline disabled:opacity-60"
                >
                  Đổi địa chỉ email
                </button>
              </div>
            </>
          ) : (
            <p role="status" className="mb-6 text-sm text-green-700">
              Mật khẩu đã được cập nhật. Vui lòng đăng nhập lại bằng mật khẩu mới.
            </p>
          )}
          <Link to="/login" className="mt-5 inline-block text-sm text-primary hover:underline">
            Quay lại đăng nhập
          </Link>
        </div>
      ) : (
        <>
          <p className="mb-6 text-sm text-muted-foreground">{instructions}</p>
          {mode === 'reset' && linkEmail && (
            <p className="mb-6 text-sm">
              Tài khoản <strong className="wrap-anywhere">{linkEmail}</strong>
            </p>
          )}
          {ready && !invalidLink && (
            <form
              noValidate
              onSubmit={handleSubmit(submit)}
              aria-describedby={error ? `${mode}-error` : undefined}
            >
              <fieldset disabled={busy} className="space-y-5">
                {mode === 'forgot' && (
                  <div>
                    <label htmlFor="forgot-email" className="mb-2 block text-sm">
                      Địa chỉ email
                    </label>
                    <input
                      id="forgot-email"
                      type="email"
                      autoComplete="email"
                      required
                      maxLength={256}
                      {...register('email')}
                      aria-invalid={Boolean(errors.email)}
                      aria-describedby={errors.email ? 'forgot-email-error' : undefined}
                      className={inputClass}
                    />
                    <FieldError id="forgot-email-error" message={errors.email?.message} />
                  </div>
                )}
                {mode === 'change' && (
                  <div>
                    <label htmlFor="change-current-password" className="mb-2 block text-sm">
                      Mật khẩu hiện tại
                    </label>
                    <input
                      id="change-current-password"
                      type="password"
                      autoComplete="current-password"
                      required
                      maxLength={128}
                      {...register('currentPassword')}
                      aria-invalid={Boolean(errors.currentPassword)}
                      aria-describedby={errors.currentPassword ? 'change-current-password-error' : undefined}
                      className={inputClass}
                    />
                    <FieldError
                      id="change-current-password-error"
                      message={errors.currentPassword?.message}
                    />
                  </div>
                )}
                {mode !== 'forgot' && (
                  <>
                    <div>
                      <label htmlFor={`${mode}-new-password`} className="mb-2 block text-sm">
                        Mật khẩu mới
                      </label>
                      <input
                        id={`${mode}-new-password`}
                        type="password"
                        autoComplete="new-password"
                        required
                        maxLength={128}
                        aria-describedby={`${mode}-password-help${errors.newPassword ? ` ${mode}-new-password-error` : ''}`}
                        {...register('newPassword')}
                        aria-invalid={Boolean(errors.newPassword)}
                        className={inputClass}
                      />
                      <FieldError id={`${mode}-new-password-error`} message={errors.newPassword?.message} />
                      <p id={`${mode}-password-help`} className="mt-2 text-sm text-muted-foreground">
                        Từ 8 đến 128 ký tự, có chữ hoa, chữ thường, số và ký tự đặc biệt.
                      </p>
                    </div>
                    <div>
                      <label htmlFor={`${mode}-confirm-password`} className="mb-2 block text-sm">
                        Xác nhận mật khẩu mới
                      </label>
                      <input
                        id={`${mode}-confirm-password`}
                        type="password"
                        autoComplete="new-password"
                        required
                        maxLength={128}
                        {...register('confirmPassword')}
                        aria-invalid={Boolean(errors.confirmPassword)}
                        aria-describedby={
                          errors.confirmPassword ? `${mode}-confirm-password-error` : undefined
                        }
                        className={inputClass}
                      />
                      <FieldError
                        id={`${mode}-confirm-password-error`}
                        message={errors.confirmPassword?.message}
                      />
                    </div>
                  </>
                )}
                <button
                  type="submit"
                  disabled={mode === 'forgot' && resendAfter > 0}
                  className="w-full bg-primary px-6 py-3 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-60"
                >
                  {busy ? 'Đang xử lý…' : requestLabel}
                </button>
              </fieldset>
            </form>
          )}
          {mode !== 'forgot' && (
            <Link to="/forgot-password" className="mt-5 inline-block text-sm text-primary hover:underline">
              {mode === 'change' ? 'Quên mật khẩu hiện tại?' : 'Gửi liên kết đặt lại mật khẩu mới'}
            </Link>
          )}
          {mode === 'forgot' && (
            <Link to="/login" className="mt-5 inline-block text-sm text-primary hover:underline">
              Quay lại đăng nhập
            </Link>
          )}
        </>
      )}
      {mode === 'forgot' && (
        <p className="mt-6 border-t border-border pt-5 text-sm text-muted-foreground">
          Nếu bạn đăng nhập bằng Google và chưa có mật khẩu riêng cho Culinary Blog, hãy chọn “Tiếp tục với
          Google” ở trang đăng nhập. Khi quên mật khẩu Google, hãy khôi phục tài khoản Google.
        </p>
      )}
    </section>
  )
}
