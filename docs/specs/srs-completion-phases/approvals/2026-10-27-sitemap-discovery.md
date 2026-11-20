# Thay thế endpoint ping sitemap Google đã ngừng hỗ trợ

Ngày27/10/2026, Asia/Bangkok. Phạm vi: DEC-02, D6-09, phần thông báo sitemap
trong FR-JOB-003/NFR-SEO-003. Không thay PDF gốc hoặc đính chính A/B/C.

## Chấp thuận của người dùng

Agent đã trình phương án: sinh sitemap theo lịch, công khai URL sitemap và khai
báo URL đó trong robots.txt; Search Console là cách gửi bổ sung tùy chọn.
Người dùng trả lời nguyên văn:

> Chấp thuận phương án thay thế qua robots.txt

Nguồn: câu trả lời trong hội thoại hiện tại cho câu hỏi
`call_e75c52ccad524f5786f84b48b5940591`. Đây là chấp thuận riêng cho yêu cầu
provider đã ngừng, không phải agent tự miễn nghiệm thu.

## Tiêu chí thay thế cụ thể

1. Giữ job sinh sitemap02:00UTC, retry2, singleton lock và các URL public,
   lastmod/changefreq/priority theo hợp đồng. Upload lỗi giữ artifact cũ.
2. Sitemap được phục vụ qua URL công khai; robots.txt khai báo URL đó.
3. Không gọi endpoint ping cũ hoặc ghi log/kết quả giả rằng Google đã nhận ping.
4. Search Console là lựa chọn vận hành bổ sung, không bắt buộc đăng nhập/gửi
   thay người dùng để nghiệm thu phương án robots.txt.
5. Kiểm chứng job/XML/HTTP/robots thực tế theo môi trường đã chạy. Không suy
   việc khai báo sitemap thành Google đã lập chỉ mục hoặc hiển thị rich results.

Google xác nhận endpoint cũ trả404 và hỗ trợ robots.txt/Search Console:
[thông báo ngừng ping](https://developers.google.com/search/blog/2023/06/sitemaps-lastmod-ping),
[cách công bố sitemap](https://developers.google.com/search/docs/crawling-indexing/sitemaps/build-sitemap).

Thay đổi này không cho phép sửa cấu hình cloud, chạy migration/seed, gửi email,
publish sitemap trên tài khoản khác hoặc mở phase10/28/36. Các quyền đó vẫn
theo phạm vi người dùng đã giao riêng.
