import type { ProblemDetails } from './problem-details-schema'

export type { ProblemDetails } from './problem-details-schema'

export class ApiProblem extends Error {
  constructor(public readonly problem: ProblemDetails) {
    super(problem.detail ?? problem.title)
    this.name = 'ApiProblem'
  }
}

const errorMessages = {
  PROFILE_UPDATE_FAILED: 'Cập nhật hồ sơ thất bại. Vui lòng thử lại.',
  RECIPE_DELETE_FAILED: 'Không thể xóa công thức.',
  RECIPE_PUBLISH_FAILED: 'Không thể xuất bản công thức.',
  RECIPE_UNPUBLISH_FAILED: 'Không thể gỡ xuất bản công thức.',
  RECIPE_ARCHIVE_FAILED: 'Không thể lưu trữ công thức.',
  RECIPE_UNARCHIVE_FAILED: 'Không thể khôi phục công thức.',
  RECIPE_LIST_FAILED: 'Không thể tải danh sách công thức.',
  CONTENT_SAVE_FAILED: 'Không thể lưu thay đổi. Vui lòng thử lại.',
  COMPOSITION_SAVE_FAILED: 'Không thể lưu thay đổi.',
  RECIPE_SAVE_FAILED: 'Không thể lưu công thức. Vui lòng thử lại.',
  CATEGORY_SAVE_FAILED: 'Không thể lưu danh mục. Vui lòng thử lại.',
  CATEGORY_DELETE_FAILED: 'Không thể xóa danh mục.',
  PASSWORD_REQUEST_FAILED: 'Không thể thực hiện yêu cầu. Vui lòng thử lại.',
  AUTH_EMAIL_EXISTS: 'Email này đã có tài khoản. Hãy đăng nhập hoặc dùng email khác.',
  AUTH_INVALID_CREDENTIALS: 'Email hoặc mật khẩu không chính xác. Hãy kiểm tra và thử lại.',
  AUTH_ACCOUNT_LOCKED: 'Tài khoản đang tạm khóa. Vui lòng chờ rồi thử lại hoặc đặt lại mật khẩu.',
  AUTH_REQUEST_FAILED: 'Chưa thực hiện được yêu cầu. Vui lòng thử lại sau.',
  AUTH_CURRENT_PASSWORD_INVALID: 'Mật khẩu hiện tại không chính xác.',
  AUTH_RESET_TOKEN_INVALID:
    'Liên kết không hợp lệ, đã hết hạn hoặc đã được sử dụng. Vui lòng yêu cầu liên kết mới.',
  AUTH_EMAIL_UNAVAILABLE: 'Dịch vụ gửi email chưa sẵn sàng. Vui lòng thử lại sau.',
  AUTH_ACCOUNT_DISABLED: 'Tài khoản đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên.',
  AUTH_EXTERNAL_ACCOUNT_CONFLICT:
    'Email đã liên kết với tài khoản khác. Vui lòng dùng phương thức đăng nhập ban đầu.',
  AUTH_GOOGLE_EMAIL_UNVERIFIED: 'Vui lòng xác minh email Google trước khi đăng nhập.',
  AUTH_GOOGLE_TOKEN_INVALID: 'Phiên Google không hợp lệ. Vui lòng đăng nhập lại.',
  AUTH_GOOGLE_UNAVAILABLE: 'Chưa kết nối được Google. Vui lòng thử lại sau.',
  CATEGORY_DELETE_HAS_RECIPES: 'Xung đột dữ liệu: Danh mục này đang chứa công thức nên không thể xóa.',
  CATEGORY_NAME_EXISTS: 'Tên danh mục đã tồn tại. Vui lòng chọn tên khác.',
  CATEGORY_SLUG_EXISTS: 'Đường dẫn danh mục đã tồn tại. Vui lòng chọn tên khác.',
  CATEGORY_NOT_FOUND: 'Danh mục không còn tồn tại. Vui lòng tải lại danh sách.',
  CATEGORY_NAME_INVALID: 'Tên danh mục cần từ 2 đến 50 ký tự và không chứa HTML.',
  CATEGORY_ORDER_INVALID: 'Thứ tự danh mục phải là số nguyên không âm.',
  CATEGORY_DATA_INVALID: 'Vui lòng kiểm tra mô tả và URL ảnh của danh mục.',
  RECIPE_NOT_FOUND: 'Công thức không còn tồn tại hoặc không còn được chia sẻ. Vui lòng tải lại danh sách.',
  RECIPE_FORBIDDEN: 'Bạn không có quyền thay đổi công thức này.',
  FORBIDDEN: 'Bạn không có quyền thực hiện thao tác này.',
  RECIPE_CONCURRENCY_CONFLICT:
    'Công thức đã thay đổi ở phiên khác. Giữ nội dung đang nhập và tải lại phiên bản mới trước khi lưu.',
  RECIPE_PUBLISH_INCOMPLETE: 'Công thức cần ít nhất 1 nguyên liệu và 1 bước thực hiện để xuất bản.',
  RECIPE_INVALID_TRANSITION: 'Trạng thái công thức đã thay đổi. Vui lòng tải lại trước khi tiếp tục.',
  RECIPE_STATE_CONFLICT: 'Trạng thái công thức đã thay đổi. Vui lòng tải lại trước khi tiếp tục.',
  RECIPE_SLUG_EXISTS: 'Đường dẫn công thức đã tồn tại. Vui lòng chọn tên khác.',
  RECIPE_DATA_INVALID: 'Nội dung công thức chưa hợp lệ. Vui lòng kiểm tra các trường và thử lại.',
  IMAGE_NOT_FOUND: 'Ảnh không còn tồn tại. Vui lòng tải lại công thức.',
  IMAGE_PRIMARY_CONFLICT: 'Ảnh đại diện đã thay đổi. Vui lòng tải lại và chọn lại ảnh.',
  VALIDATION_ERROR: 'Vui lòng kiểm tra các trường được đánh dấu và thử lại.',
  RATE_LIMIT_EXCEEDED: 'Bạn thao tác quá nhiều lần. Vui lòng chờ một phút rồi thử lại.',
  AUTH_REQUIRED: 'Phiên đăng nhập không còn hợp lệ. Vui lòng đăng nhập lại.',
} as const

type ErrorMessageCode = keyof typeof errorMessages

export function apiErrorMessage(
  error: unknown,
  fallbackCode: ErrorMessageCode = 'AUTH_REQUEST_FAILED',
): string {
  const fallback = errorMessages[fallbackCode]
  if (!(error instanceof ApiProblem)) return fallback
  const message = Object.hasOwn(errorMessages, error.problem.code)
    ? errorMessages[error.problem.code as ErrorMessageCode]
    : undefined
  if (message) return message
  if (error.problem.status >= 500) return fallback
  if (error.problem.status === 401) return errorMessages.AUTH_REQUIRED
  if (error.problem.status === 403) return errorMessages.FORBIDDEN
  if (error.problem.status === 429) return errorMessages.RATE_LIMIT_EXCEEDED
  return fallback
}

export const authErrorMessage = apiErrorMessage

export async function readProblemDetails(response: Response): Promise<ApiProblem> {
  const fallback: ProblemDetails = {
    title: response.statusText || 'Request failed',
    status: response.status,
    code: 'HTTP_ERROR',
  }

  try {
    const { problemDetailsSchema } = await import('./problem-details-schema')
    const parsed = problemDetailsSchema.safeParse(await response.json())
    return new ApiProblem(parsed.success ? parsed.data : fallback)
  } catch {
    return new ApiProblem(fallback)
  }
}
