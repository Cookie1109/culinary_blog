# Gửi email bằng Gmail

Địa chỉ gửi là hộp thư Gmail mà Culinary Blog dùng để gửi thư cho người dùng. Có thể dùng một Gmail riêng cho dự án. Email nhận liên kết là địa chỉ người dùng nhập ở trang **Quên mật khẩu**.

## Thiết lập

1. Đăng nhập Gmail sẽ dùng làm địa chỉ gửi và bật **Xác minh 2 bước** trong tài khoản Google.
2. Mở [Mật khẩu ứng dụng](https://myaccount.google.com/apppasswords), tạo mật khẩu cho `Culinary Blog`.
3. Mở `.env` ở thư mục gốc dự án. Điền `SMTP_USERNAME` và `SMTP_FROM_ADDRESS` bằng cùng địa chỉ Gmail. Điền `SMTP_PASSWORD` bằng mật khẩu ứng dụng vừa tạo, bỏ khoảng trắng phân nhóm khi sao chép. Đặt `EMAIL_ENABLED=true` sau khi điền đủ.

```dotenv
EMAIL_ENABLED=true
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_ENABLE_SSL=true
SMTP_USERNAME=your-sender@gmail.com
SMTP_PASSWORD=your-google-app-password
SMTP_FROM_ADDRESS=your-sender@gmail.com
SMTP_FROM_NAME=Culinary Blog
```

Giữ mật khẩu trong `.env` trên máy. Tệp này đã nằm trong `.gitignore`.

4. Áp dụng cấu hình mới từ thư mục gốc dự án.

```powershell
docker compose up --detach --no-deps --wait api
```

Sau đó yêu cầu liên kết từ trang **Quên mật khẩu**. Thư có tiêu đề **Đặt lại mật khẩu Culinary Blog**, chứa liên kết có hiệu lực 30 phút. Kiểm tra cả mục Thư rác/Spam nếu chưa thấy thư trong Hộp thư đến. Tài khoản chỉ đăng nhập bằng Google khôi phục quyền truy cập qua Google.

## Khi chưa nhận được thư

- `EMAIL_ENABLED=false` làm chức năng báo dịch vụ email chưa sẵn sàng. Điền đủ thông tin và bật lại như trên.
- `SMTP_HOST=mailhog` gửi vào hộp thư nội bộ tại `http://localhost:8025`. Gmail thật cần `smtp.gmail.com`, cổng `587` và `SMTP_ENABLE_SSL=true`.
- Xem nhật ký API khi Gmail từ chối gửi. Nhật ký ghi trạng thái SMTP, không ghi mật khẩu, token hoặc liên kết khôi phục.

```powershell
docker compose logs --since 10m api | Select-String 'Password reset email'
```

`Password reset email accepted by SMTP server` cho biết SMTP đã nhận thư. Việc thư xuất hiện ở Hộp thư đến còn phụ thuộc nhà cung cấp nhận thư. `Password reset email delivery failed` cho biết gửi thất bại; kiểm tra địa chỉ gửi, mật khẩu ứng dụng và kết nối mạng. API giữ phản hồi chung cho các email để bảo vệ thông tin tài khoản.

Hướng dẫn của Google về [mật khẩu ứng dụng](https://support.google.com/mail/answer/185833?hl=vi) và [SMTP Gmail](https://developers.google.com/workspace/gmail/imap/imap-smtp).
