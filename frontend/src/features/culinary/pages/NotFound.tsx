import { Link } from 'react-router'
import { ChefHat } from 'lucide-react'

export function NotFound() {
  return (
    <div className="min-h-[70vh] flex flex-col items-center justify-center text-center px-4">
      <ChefHat size={48} className="text-primary mb-6" strokeWidth={1.5} />
      <h1 className="text-5xl font-serif mb-4">Không tìm thấy trang</h1>
      <p className="text-muted-foreground text-lg mb-8 max-w-md">
        Không tìm thấy công thức hoặc trang bạn đang tìm kiếm. Nội dung này có thể đã được chuyển đi hoặc
        không còn tồn tại.
      </p>
      <Link
        to="/"
        className="bg-foreground text-background px-8 py-4 uppercase tracking-widest text-sm font-medium hover:bg-primary transition-colors"
      >
        Quay về trang chủ
      </Link>
    </div>
  )
}
