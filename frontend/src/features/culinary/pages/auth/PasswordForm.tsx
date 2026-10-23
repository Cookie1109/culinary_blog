'use client'

import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { changePassword, forgotPassword, resetPassword } from '@/lib/api/auth-client'
import { ApiProblem } from '@/lib/api/problem-details'
import { useAuth } from '../../contexts/AuthContext'

export function PasswordForm({ mode }: { mode: 'change' | 'forgot' | 'reset' }) {
  const { logout } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [token, setToken] = useState('')
  const [ready, setReady] = useState(mode !== 'reset')
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [busy, setBusy] = useState(false)
  const [success, setSuccess] = useState(false)
  const [error, setError] = useState('')
  const title = mode === 'change' ? 'Đổi mật khẩu' : mode === 'forgot' ? 'Quên mật khẩu' : 'Đặt lại mật khẩu'
  const invalidLink = ready && mode === 'reset' && (!email || !token)
  const inputClass =
    'w-full border border-border bg-background px-4 py-3 text-sm focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary'

  useEffect(() => {
    if (mode !== 'reset') return
    const parameters = new URLSearchParams(window.location.hash.slice(1))
    setEmail(parameters.get('email') ?? '')
    setToken(parameters.get('token') ?? '')
    setReady(true)
  }, [mode])

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (busy || !ready || invalidLink) return
    setError('')
    if (mode !== 'forgot') {
      if (
        newPassword.length < 8 ||
        newPassword.length > 128 ||
        !/[a-z]/.test(newPassword) ||
        !/[A-Z]/.test(newPassword) ||
        !/[0-9]/.test(newPassword) ||
        !/[^a-zA-Z0-9]/.test(newPassword)
      ) {
        setError('Mật khẩu cần từ 8 đến 128 ký tự, có chữ hoa, chữ thường, số và ký tự đặc biệt.')
        document.getElementById(`${mode}-new-password`)?.focus()
        return
      }
      if (newPassword !== confirmPassword) {
        setError('Mật khẩu xác nhận không khớp.')
        document.getElementById(`${mode}-confirm-password`)?.focus()
        return
      }
    }
    setBusy(true)
    try {
      if (mode === 'forgot') await forgotPassword(email.trim())
      else if (mode === 'change') await changePassword(currentPassword, newPassword)
      else await resetPassword(email, token, newPassword)
      setSuccess(true)
      setCurrentPassword('')
      setNewPassword('')
      setConfirmPassword('')
      if (mode === 'reset') window.history.replaceState(null, '', window.location.pathname)
      if (mode !== 'forgot') await logout().catch(() => {})
      if (mode === 'change') navigate('/login?passwordChanged=1')
    } catch (failure) {
      const code = failure instanceof ApiProblem ? failure.problem.code : ''
      setError(
        code === 'AUTH_CURRENT_PASSWORD_INVALID'
          ? 'Mật khẩu hiện tại không chính xác.'
          : code === 'AUTH_RESET_TOKEN_INVALID'
            ? 'Liên kết không hợp lệ, đã hết hạn hoặc đã được sử dụng. Vui lòng yêu cầu liên kết mới.'
            : code === 'AUTH_EMAIL_UNAVAILABLE'
              ? 'Dịch vụ gửi email chưa sẵn sàng. Vui lòng thử lại sau.'
              : failure instanceof ApiProblem && failure.problem.status === 429
                ? 'Bạn thao tác quá nhiều lần. Vui lòng chờ một phút rồi thử lại.'
                : 'Không thể thực hiện yêu cầu. Vui lòng thử lại.',
      )
    } finally {
      setBusy(false)
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
      {success ? (
        <div>
          <p role="status" className="mb-6 text-sm text-green-700">
            {mode === 'forgot'
              ? 'Nếu email thuộc tài khoản có mật khẩu đang hoạt động, bạn sẽ nhận được liên kết đặt lại mật khẩu. Vui lòng kiểm tra cả thư rác. Liên kết có hiệu lực trong 30 phút.'
              : 'Mật khẩu đã được cập nhật. Vui lòng đăng nhập lại bằng mật khẩu mới.'}
          </p>
          <Link to="/login" className="text-primary hover:underline">
            Quay lại đăng nhập
          </Link>
          {mode === 'forgot' && (
            <button
              type="button"
              onClick={() => setSuccess(false)}
              className="ml-6 text-primary hover:underline"
            >
              Gửi lại yêu cầu
            </button>
          )}
        </div>
      ) : (
        <>
          <p className="mb-6 text-sm text-muted-foreground">
            {mode === 'forgot'
              ? 'Nhập email đăng ký để nhận liên kết đặt lại mật khẩu.'
              : mode === 'change'
                ? 'Nhập mật khẩu hiện tại và mật khẩu mới. Sau khi lưu, bạn cần đăng nhập lại.'
                : 'Nhập mật khẩu mới cho tài khoản của bạn.'}
          </p>
          {(error || invalidLink) && (
            <p
              id={`${mode}-error`}
              role="alert"
              className="mb-5 border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"
            >
              {error || 'Liên kết đặt lại mật khẩu không hợp lệ. Vui lòng yêu cầu liên kết mới.'}
            </p>
          )}
          {ready && !invalidLink && (
            <form onSubmit={handleSubmit} aria-describedby={error ? `${mode}-error` : undefined}>
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
                      value={email}
                      onChange={(event) => setEmail(event.target.value)}
                      className={inputClass}
                    />
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
                      value={currentPassword}
                      onChange={(event) => setCurrentPassword(event.target.value)}
                      className={inputClass}
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
                        aria-describedby={`${mode}-password-help`}
                        value={newPassword}
                        onChange={(event) => setNewPassword(event.target.value)}
                        className={inputClass}
                      />
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
                        value={confirmPassword}
                        onChange={(event) => setConfirmPassword(event.target.value)}
                        className={inputClass}
                      />
                    </div>
                  </>
                )}
                <button
                  type="submit"
                  className="w-full bg-primary px-6 py-3 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-60"
                >
                  {busy ? 'Đang xử lý…' : mode === 'forgot' ? 'Gửi liên kết đặt lại mật khẩu' : title}
                </button>
              </fieldset>
            </form>
          )}
          {mode !== 'forgot' && (
            <Link to="/forgot-password" className="mt-5 inline-block text-sm text-primary hover:underline">
              Yêu cầu liên kết đặt lại mật khẩu
            </Link>
          )}
          {mode === 'forgot' && (
            <Link to="/login" className="mt-5 inline-block text-sm text-primary hover:underline">
              Quay lại đăng nhập
            </Link>
          )}
        </>
      )}
    </section>
  )
}
