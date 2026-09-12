import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router'
import { Camera, Pencil, Check, X } from 'lucide-react'
import { useAuth } from '../contexts/AuthContext'

export function Profile() {
  const { user, updateProfile, logout } = useAuth()
  const navigate = useNavigate()
  const [editing, setEditing] = useState(false)
  const [name, setName] = useState(user?.name ?? '')
  const [bio, setBio] = useState(user?.bio ?? '')
  const [saved, setSaved] = useState(false)
  const [error, setError] = useState('')

  if (!user) {
    return (
      <div className="max-w-2xl mx-auto px-4 py-24 text-center">
        <h1 className="font-serif text-3xl mb-4">Đăng nhập để xem hồ sơ của bạn</h1>
        <button
          onClick={() => navigate('/auth/login')}
          className="bg-primary text-primary-foreground px-8 py-3 text-sm uppercase tracking-widest hover:bg-primary/90 transition-colors"
        >
          Đăng nhập
        </button>
      </div>
    )
  }

  const handleSave = async (e: FormEvent) => {
    e.preventDefault()
    if (name.trim().length < 2 || name.trim().length > 100) {
      setError('Tên hiển thị phải có từ 2 đến 100 ký tự.')
      return
    }
    if (bio.length > 2000) {
      setError('Tiểu sử không được vượt quá 2.000 ký tự.')
      return
    }
    try {
      setError('')
      await updateProfile({ name: name.trim(), bio })
      setEditing(false)
      setSaved(true)
      setTimeout(() => setSaved(false), 2500)
    } catch {
      setError('Cập nhật hồ sơ thất bại. Vui lòng thử lại.')
    }
  }

  const handleCancel = () => {
    setName(user.name)
    setBio(user.bio ?? '')
    setEditing(false)
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
              <img
                src={user.avatar}
                alt={user.name}
                className="w-28 h-28 rounded-full object-cover border-2 border-border"
              />
            ) : (
              <div className="w-28 h-28 rounded-full bg-secondary flex items-center justify-center text-4xl font-serif text-foreground border-2 border-border">
                {user.name[0]}
              </div>
            )}
            <button
              className="absolute bottom-1 right-1 w-8 h-8 rounded-full bg-foreground text-background flex items-center justify-center hover:bg-primary transition-colors shadow"
              aria-label="Đổi ảnh đại diện"
            >
              <Camera size={14} />
            </button>
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

          <form onSubmit={handleSave}>
            {/* Name */}
            <div className="mb-6 pb-6 border-b border-border">
              <div className="flex items-center justify-between mb-4">
                <label className="text-xs uppercase tracking-widest text-muted-foreground font-medium">
                  Họ và tên
                </label>
                {!editing && (
                  <button
                    type="button"
                    onClick={() => setEditing(true)}
                    className="flex items-center gap-1.5 text-xs text-primary hover:text-primary/80 transition-colors"
                  >
                    <Pencil size={12} /> Chỉnh sửa
                  </button>
                )}
              </div>
              {editing ? (
                <input
                  type="text"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  className="w-full border border-border bg-background px-4 py-3 text-sm focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary transition-all"
                />
              ) : (
                <p className="text-foreground">{user.name}</p>
              )}
            </div>

            {/* Email (readonly) */}
            <div className="mb-6 pb-6 border-b border-border">
              <label className="block text-xs uppercase tracking-widest text-muted-foreground font-medium mb-3">
                Địa chỉ email
              </label>
              <p className="text-foreground">{user.email}</p>
              <p className="text-xs text-muted-foreground mt-1">
                Email không thể thay đổi tại đây. Vui lòng liên hệ hỗ trợ nếu cần.
              </p>
            </div>

            {/* Bio */}
            <div className="mb-6 pb-6 border-b border-border">
              <label className="block text-xs uppercase tracking-widest text-muted-foreground font-medium mb-3">
                Tiểu sử giới thiệu
              </label>
              {editing ? (
                <textarea
                  value={bio}
                  onChange={(e) => setBio(e.target.value)}
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
            </div>

            {/* Role */}
            <div className="mb-8 pb-6 border-b border-border">
              <label className="block text-xs uppercase tracking-widest text-muted-foreground font-medium mb-3">
                Vai trò tài khoản
              </label>
              <span className="inline-block px-3 py-1 bg-secondary text-foreground text-sm border border-border">
                {user.role === 'admin' ? 'Quản trị viên' : 'Tác giả'}
              </span>
            </div>

            {/* Actions */}
            {editing ? (
              <div className="flex items-center gap-4">
                <button
                  type="submit"
                  className="flex items-center gap-2 bg-primary text-primary-foreground px-6 py-3 text-sm uppercase tracking-widest hover:bg-primary/90 transition-colors"
                >
                  <Check size={16} /> Lưu thay đổi
                </button>
                <button
                  type="button"
                  onClick={handleCancel}
                  className="flex items-center gap-2 border border-border px-6 py-3 text-sm uppercase tracking-widest hover:bg-secondary transition-colors"
                >
                  <X size={16} /> Hủy
                </button>
              </div>
            ) : (
              <div className="flex items-center gap-4">
                <button
                  type="button"
                  onClick={() => setEditing(true)}
                  className="bg-foreground text-background px-6 py-3 text-sm uppercase tracking-widest hover:bg-primary transition-colors"
                >
                  Chỉnh sửa hồ sơ
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
        </div>
      </div>
    </div>
  )
}
