import Image from 'next/image'
import { useState, type MouseEvent } from 'react'
import { useForm } from 'react-hook-form'
import { useNavigate } from 'react-router'
import { Pencil, Check, X } from 'lucide-react'
import { FieldError } from '@/components/ui/field-error'
import { useToast } from '@/components/ui/toast'
import { apiErrorMessage } from '@/lib/api/problem-details'
import { useAuth } from '../contexts/AuthContext'
import { PasswordForm } from './auth/PasswordForm'
import type { ProfileValues } from '../forms/auth-schemas'
import { setServerFieldErrors } from '../forms/server-errors'
import { formMessages } from '../forms/messages'

export function Profile() {
  const { user, updateProfile, logout } = useAuth()
  const navigate = useNavigate()
  const { showToast } = useToast()
  const [editing, setEditing] = useState(false)
  const {
    register,
    handleSubmit,
    reset,
    setError: setFieldError,
    formState: { errors, isSubmitting: saving },
  } = useForm<ProfileValues>({
    resolver: async (...args) => (await import('../forms/deferred-resolvers')).profileResolver(...args),
    defaultValues: { name: '', bio: '', avatar: '' },
  })
  const [saved, setSaved] = useState(false)
  const [error, setError] = useState('')

  if (!user) {
    return (
      <div className="max-w-2xl mx-auto px-4 py-24 text-center">
        <h1 className="font-serif text-3xl mb-4">Đăng nhập để xem hồ sơ của bạn</h1>
        <button
          type="button"
          onClick={() => navigate('/login')}
          className="bg-primary text-primary-foreground px-8 py-3 text-sm uppercase tracking-widest hover:bg-primary/90 transition-colors"
        >
          Đăng nhập
        </button>
      </div>
    )
  }

  const handleSave = async (values: ProfileValues) => {
    try {
      setError('')
      await updateProfile(values)
      setEditing(false)
      setSaved(true)
      showToast('Cập nhật hồ sơ thành công.')
      setTimeout(() => setSaved(false), 2500)
    } catch (failure) {
      setServerFieldErrors(failure, setFieldError, {
        DisplayName: ['name', formMessages.displayName],
        AvatarUrl: ['avatar', formMessages.imageUrlInvalid],
        Bio: ['bio', formMessages.bioLength],
      })
      const message = apiErrorMessage(failure, 'PROFILE_UPDATE_FAILED')
      setError(message)
      showToast(message)
    }
  }

  const handleCancel = () => {
    reset({ name: user.name, bio: user.bio ?? '', avatar: user.avatar ?? '' })
    setEditing(false)
    setError('')
  }

  const beginEdit = (event: MouseEvent<HTMLButtonElement>) => {
    event.preventDefault()
    reset({ name: user.name, bio: user.bio ?? '', avatar: user.avatar ?? '' })
    setError('')
    setEditing(true)
  }

  return (
    <div className="max-w-3xl mx-auto px-4 sm:px-6 py-16 lg:py-24">
      {/* Header */}
      <div className="mb-12">
        <h1 className="font-serif text-4xl lg:text-5xl text-foreground mb-2">Hồ sơ cá nhân</h1>
        <p className="text-muted-foreground">Quản lý thông tin cá nhân và hồ sơ tác giả của bạn.</p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-12">
        {/* Avatar column */}
        <div className="flex flex-col items-center lg:items-start gap-4">
          <div className="relative">
            {user.avatar ? (
              <Image
                width={112}
                height={112}
                unoptimized
                src={user.avatar}
                alt={user.name}
                className="w-28 h-28 rounded-full object-cover border-2 border-border"
              />
            ) : (
              <div className="w-28 h-28 rounded-full bg-secondary flex items-center justify-center text-4xl font-serif text-foreground border-2 border-border">
                {user.name[0]}
              </div>
            )}
          </div>
          <div className="text-center lg:text-left">
            <p className="font-medium">{user.name}</p>
            <p className="text-sm text-muted-foreground">
              {user.role === 'admin' ? 'Quản trị viên' : 'Tác giả'}
            </p>
          </div>
        </div>

        {/* Info column */}
        <div className="lg:col-span-2">
          {saved && (
            <div className="flex items-center gap-2 bg-green-50 border border-green-200 text-green-700 px-4 py-3 text-sm mb-6">
              <Check size={16} />
              Cập nhật hồ sơ thành công.
            </div>
          )}
          {error && (
            <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 text-sm mb-6" role="alert">
              {error}
            </div>
          )}

          <form noValidate onSubmit={handleSubmit(handleSave)}>
            {/* Name */}
            <div className="mb-6 pb-6 border-b border-border">
              <div className="flex items-center justify-between mb-4">
                <span className="text-xs uppercase tracking-widest text-muted-foreground font-medium">
                  Họ và tên
                </span>
                {!editing && (
                  <button
                    type="button"
                    onClick={beginEdit}
                    className="flex items-center gap-1.5 text-xs text-primary hover:text-primary/80 transition-colors"
                  >
                    <Pencil size={12} /> Chỉnh sửa
                  </button>
                )}
              </div>
              {editing ? (
                <input
                  aria-label="Họ và tên"
                  disabled={saving}
                  type="text"
                  {...register('name')}
                  aria-invalid={Boolean(errors.name)}
                  aria-describedby={errors.name ? 'profile-name-error' : undefined}
                  className="w-full border border-border bg-background px-4 py-3 text-sm focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary transition-all"
                />
              ) : (
                <p className="text-foreground">{user.name}</p>
              )}
              <FieldError id="profile-name-error" message={errors.name?.message} />
            </div>

            {editing && (
              <div className="mb-6 pb-6 border-b border-border">
                <label
                  htmlFor="profile-avatar"
                  className="mb-3 block text-xs uppercase tracking-widest text-muted-foreground font-medium"
                >
                  URL ảnh đại diện
                </label>
                <input
                  id="profile-avatar"
                  type="url"
                  disabled={saving}
                  {...register('avatar')}
                  aria-invalid={Boolean(errors.avatar)}
                  aria-describedby={`profile-avatar-help${errors.avatar ? ' profile-avatar-error' : ''}`}
                  placeholder="https://example.com/avatar.jpg"
                  className="w-full border border-border bg-background px-4 py-3 text-sm focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary"
                />
                <p id="profile-avatar-help" className="mt-2 text-xs text-muted-foreground">
                  Nhập đường dẫn ảnh HTTP hoặc HTTPS. Để trống để xóa ảnh đại diện.
                </p>
                <FieldError id="profile-avatar-error" message={errors.avatar?.message} />
              </div>
            )}

            {/* Email (readonly) */}
            <div className="mb-6 pb-6 border-b border-border">
              <span className="block text-xs uppercase tracking-widest text-muted-foreground font-medium mb-3">
                Địa chỉ email
              </span>
              <p className="text-foreground">{user.email}</p>
              <p className="text-xs text-muted-foreground mt-1">
                Email không thể thay đổi tại đây. Vui lòng liên hệ hỗ trợ nếu cần.
              </p>
            </div>

            {/* Bio */}
            <div className="mb-6 pb-6 border-b border-border">
              <span className="block text-xs uppercase tracking-widest text-muted-foreground font-medium mb-3">
                Tiểu sử giới thiệu
              </span>
              {editing ? (
                <textarea
                  aria-label="Tiểu sử giới thiệu"
                  disabled={saving}
                  {...register('bio')}
                  aria-invalid={Boolean(errors.bio)}
                  aria-describedby={errors.bio ? 'profile-bio-error' : undefined}
                  rows={4}
                  placeholder="Chia sẻ đôi nét về bạn với độc giả..."
                  className="w-full border border-border bg-background px-4 py-3 text-sm focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary placeholder:text-muted-foreground transition-all resize-none"
                />
              ) : (
                <p className="text-foreground leading-relaxed">
                  {user.bio ?? (
                    <span className="text-muted-foreground italic">Chưa có thông tin giới thiệu.</span>
                  )}
                </p>
              )}
              <FieldError id="profile-bio-error" message={errors.bio?.message} />
            </div>

            {/* Role */}
            <div className="mb-8 pb-6 border-b border-border">
              <span className="block text-xs uppercase tracking-widest text-muted-foreground font-medium mb-3">
                Vai trò tài khoản
              </span>
              <span className="inline-block px-3 py-1 bg-secondary text-foreground text-sm border border-border">
                {user.role === 'admin' ? 'Quản trị viên' : 'Tác giả'}
              </span>
            </div>

            {/* Actions */}
            {editing ? (
              <div className="flex items-center gap-4">
                <button
                  type="submit"
                  disabled={saving}
                  className="flex items-center gap-2 bg-primary text-primary-foreground px-6 py-3 text-sm uppercase tracking-widest hover:bg-primary/90 transition-colors"
                >
                  <Check size={16} /> {saving ? 'Đang lưu…' : 'Lưu thay đổi'}
                </button>
                <button
                  type="button"
                  onClick={handleCancel}
                  disabled={saving}
                  className="flex items-center gap-2 border border-border px-6 py-3 text-sm uppercase tracking-widest hover:bg-secondary transition-colors"
                >
                  <X size={16} /> Hủy
                </button>
              </div>
            ) : (
              <div className="flex items-center gap-4">
                <button
                  type="button"
                  onClick={beginEdit}
                  className="bg-foreground text-background px-6 py-3 text-sm uppercase tracking-widest hover:bg-primary transition-colors"
                >
                  Cập nhật
                </button>
                <button
                  type="button"
                  onClick={async () => {
                    await logout()
                    navigate('/')
                  }}
                  className="text-sm text-muted-foreground hover:text-primary transition-colors uppercase tracking-widest"
                >
                  Đăng xuất
                </button>
              </div>
            )}
          </form>
          <PasswordForm mode="change" />
        </div>
      </div>
    </div>
  )
}
