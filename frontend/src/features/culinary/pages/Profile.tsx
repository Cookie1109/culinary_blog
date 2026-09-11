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

  if (!user) {
    return (
      <div className="max-w-2xl mx-auto px-4 py-24 text-center">
        <h1 className="font-serif text-3xl mb-4">Sign in to view your profile</h1>
        <button
          onClick={() => navigate('/auth/login')}
          className="bg-primary text-primary-foreground px-8 py-3 text-sm uppercase tracking-widest hover:bg-primary/90 transition-colors"
        >
          Sign In
        </button>
      </div>
    )
  }

  const handleSave = (e: FormEvent) => {
    e.preventDefault()
    updateProfile({ name, bio })
    setEditing(false)
    setSaved(true)
    setTimeout(() => setSaved(false), 2500)
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
        <h1 className="font-serif text-4xl lg:text-5xl text-foreground mb-2">My Profile</h1>
        <p className="text-muted-foreground">Manage your personal information and public author profile.</p>
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
              aria-label="Change avatar"
            >
              <Camera size={14} />
            </button>
          </div>
          <div className="text-center lg:text-left">
            <p className="font-medium">{user.name}</p>
            <p className="text-sm text-muted-foreground capitalize">{user.role}</p>
          </div>
        </div>

        {/* Info column */}
        <div className="lg:col-span-2">
          {saved && (
            <div className="flex items-center gap-2 bg-green-50 border border-green-200 text-green-700 px-4 py-3 text-sm mb-6">
              <Check size={16} />
              Profile updated successfully.
            </div>
          )}

          <form onSubmit={handleSave}>
            {/* Name */}
            <div className="mb-6 pb-6 border-b border-border">
              <div className="flex items-center justify-between mb-4">
                <label className="text-xs uppercase tracking-widest text-muted-foreground font-medium">
                  Full Name
                </label>
                {!editing && (
                  <button
                    type="button"
                    onClick={() => setEditing(true)}
                    className="flex items-center gap-1.5 text-xs text-primary hover:text-primary/80 transition-colors"
                  >
                    <Pencil size={12} /> Edit
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
                Email Address
              </label>
              <p className="text-foreground">{user.email}</p>
              <p className="text-xs text-muted-foreground mt-1">
                Email cannot be changed here. Contact support if needed.
              </p>
            </div>

            {/* Bio */}
            <div className="mb-6 pb-6 border-b border-border">
              <label className="block text-xs uppercase tracking-widest text-muted-foreground font-medium mb-3">
                Bio
              </label>
              {editing ? (
                <textarea
                  value={bio}
                  onChange={(e) => setBio(e.target.value)}
                  rows={4}
                  placeholder="Tell readers about yourself..."
                  className="w-full border border-border bg-background px-4 py-3 text-sm focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary placeholder:text-muted-foreground transition-all resize-none"
                />
              ) : (
                <p className="text-foreground leading-relaxed">
                  {user.bio ?? <span className="text-muted-foreground italic">No bio yet.</span>}
                </p>
              )}
            </div>

            {/* Role */}
            <div className="mb-8 pb-6 border-b border-border">
              <label className="block text-xs uppercase tracking-widest text-muted-foreground font-medium mb-3">
                Account Role
              </label>
              <span className="inline-block px-3 py-1 bg-secondary text-foreground text-sm capitalize border border-border">
                {user.role}
              </span>
            </div>

            {/* Actions */}
            {editing ? (
              <div className="flex items-center gap-4">
                <button
                  type="submit"
                  className="flex items-center gap-2 bg-primary text-primary-foreground px-6 py-3 text-sm uppercase tracking-widest hover:bg-primary/90 transition-colors"
                >
                  <Check size={16} /> Save Changes
                </button>
                <button
                  type="button"
                  onClick={handleCancel}
                  className="flex items-center gap-2 border border-border px-6 py-3 text-sm uppercase tracking-widest hover:bg-secondary transition-colors"
                >
                  <X size={16} /> Cancel
                </button>
              </div>
            ) : (
              <div className="flex items-center gap-4">
                <button
                  type="button"
                  onClick={() => setEditing(true)}
                  className="bg-foreground text-background px-6 py-3 text-sm uppercase tracking-widest hover:bg-primary transition-colors"
                >
                  Edit Profile
                </button>
                <button
                  type="button"
                  onClick={() => {
                    logout()
                    navigate('/')
                  }}
                  className="text-sm text-muted-foreground hover:text-primary transition-colors uppercase tracking-widest"
                >
                  Sign Out
                </button>
              </div>
            )}
          </form>
        </div>
      </div>
    </div>
  )
}
