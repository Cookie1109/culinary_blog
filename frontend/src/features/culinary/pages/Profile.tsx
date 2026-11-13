import Image from 'next/image'
import { useEffect, useRef, useState, type MouseEvent } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useNavigate } from 'react-router'
import { Camera, Pencil, Check, X } from 'lucide-react'
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
  const avatarInputRef = useRef<HTMLInputElement>(null)
  const [avatarFile, setAvatarFile] = useState<File | null>(null)
  const [avatarPreview, setAvatarPreview] = useState<string | null>(null)
  const {
    register,
    control,
    handleSubmit,
    reset,
    setValue,
    clearErrors,
    setError: setFieldError,
    formState: { errors, isSubmitting: saving },
  } = useForm<ProfileValues>({
    resolver: async (...args) => (await import('../forms/deferred-resolvers')).profileResolver(...args),
    defaultValues: { name: '', bio: '', avatar: '' },
  })
  const [saved, setSaved] = useState(false)
  const [error, setError] = useState('')
  const avatarValue = useWatch({ control, name: 'avatar' })

  useEffect(() => {
    if (!avatarFile) {
      setAvatarPreview(null)
      return undefined
    }
    const url = URL.createObjectURL(avatarFile)
    setAvatarPreview(url)
    return () => URL.revokeObjectURL(url)
  }, [avatarFile])

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
      await updateProfile(values, avatarFile ?? undefined)
      setAvatarFile(null)
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
    setAvatarFile(null)
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

  const avatarSource = editing ? (avatarPreview ?? avatarValue) : user.avatar

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
            {avatarSource ? (
              <Image
                width={112}
                height={112}
                unoptimized
                src={avatarSource}
                alt={user.name}
                className="w-28 h-28 rounded-full object-cover border-2 border-border"
              />
            ) : (
              <div className="w-28 h-28 rounded-full bg-secondary flex items-center justify-center text-4xl font-serif text-foreground border-2 border-border">
                {user.name[0]}
              </div>
            )}
            <button
              type="button"
              aria-label="Thay đổi ảnh đại diện"
              title="Thay đổi ảnh đại diện"
              disabled={saving}
              onClick={(event) => {
                if (!editing) beginEdit(event)
                avatarInputRef.current?.click()
              }}
              className="absolute -bottom-1 -right-1 grid h-9 w-9 place-items-center rounded-full border-2 border-background bg-primary text-primary-foreground shadow-sm transition-colors hover:bg-primary/90 disabled:opacity-60"
            >
              <Camera size={18} aria-hidden="true" />
            </button>
            <input
              id="profile-avatar-file"
              ref={avatarInputRef}
              type="file"
              aria-label="Chọn ảnh đại diện từ máy"
              accept="image/jpeg,image/png,image/webp,image/avif"
              disabled={saving}
              className="hidden"
              onChange={(event) => {
                const input = event.currentTarget
                const file = input.files?.[0]
                input.value = ''
                if (!file) return
                if (!['image/jpeg', 'image/png', 'image/webp', 'image/avif'].includes(file.type)) {
                  setFieldError('avatar', { type: 'manual', message: formMessages.uploadType })
                  return
                }
                if (file.size > 5 * 1024 * 1024) {
                  setFieldError('avatar', { type: 'manual', message: formMessages.uploadSize })
                  return
                }
                clearErrors('avatar')
                setError('')
                setAvatarFile(file)
              }}
            />
          </div>
          {editing && (
            <div className="space-y-2 text-center lg:text-left">
              <p className="text-xs text-muted-foreground">JPEG, PNG, WebP hoặc AVIF, tối đa 5 MB.</p>
              {avatarFile && (
                <p className="text-xs text-muted-foreground">Bấm Lưu thay đổi để dùng ảnh này.</p>
              )}
              {avatarSource && (
                <button
                  type="button"
                  disabled={saving}
                  onClick={() => {
                    setAvatarFile(null)
                    setValue('avatar', '', { shouldDirty: true })
                    clearErrors('avatar')
                  }}
                  className="text-xs text-muted-foreground underline hover:text-primary disabled:opacity-60"
                >
                  Xóa ảnh đại diện
                </button>
              )}
              <FieldError id="profile-avatar-error" message={errors.avatar?.message} />
            </div>
          )}
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
          {user.hasPassword === true && <PasswordForm mode="change" />}
          {user.hasPassword === false && (
            <section className="mt-12 border-t border-border pt-8" aria-labelledby="profile-sign-in-title">
              <h2 id="profile-sign-in-title" className="mb-4 font-serif text-2xl">
                Đăng nhập bằng Google
              </h2>
              <p className="text-sm text-muted-foreground">
                Thông tin bảo mật đăng nhập được quản lý trong tài khoản Google của bạn.
              </p>
            </section>
          )}
        </div>
      </div>
    </div>
  )
}
