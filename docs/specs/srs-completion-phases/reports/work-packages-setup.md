# Chuyển cách giao việc sang 7 gói — 23/10/2026

- Yêu cầu: người dùng đồng ý đề xuất 7 gói và yêu cầu “Bạn sửa giúp tôi”. Đây là sửa cách điều phối, chưa thực hiện gói G01–G07 hoặc thêm kết quả nghiệm thu ứng dụng.
- HEAD trước sửa: `5bee44f137e56c29da25e07597f0d494103f125f`, nhánh main, index trống.136 file thay đổi/chưa tracked có sẵn được snapshot tại `tmp/srs-work-packages/before.json`, SHA256 `8df5ef7e5ca8dd3fc882f20ec6c4b20b71ff1125146bfa3b52a7757cea099603`.
- Cwd `D:\Culinary_Blog`; PowerShell/Python3.13.7. Chỉ đọc/sửa tài liệu điều phối; không đổi PDF, acceptance, decision, backend/frontend/OpenAPI, migration hoặc môi trường chạy.

## Thay đổi

- Thêm [work-packages.md](../work-packages.md):7 gói bao phủ 35 phase/81 task, hàng chờ10/28/36 bao phủ3 phase/4 task. Giữ đủ 38 phase/85 task và thứ tự phụ thuộc; nêu phạm vi, checkpoint, prompt giao/tiếp tục và điều kiện nghiệm thu cuối.
- Sửa [README điều phối](../README.md) và phần0 [checklist](../../srs-completion-checklist.md) để mặc định giao gói. Khi giao gói, câu “chỉ phase này” trong file chi tiết không buộc agent dừng giữa các phase thuộc gói. Khi giao riêng phase, prompt cũ vẫn dùng được.
- Sửa [mẫu report](README.md): một report gói có mapping phase/task→evidence dùng chung và checkpoint tiếp tục; giữ report cũ, không tạo nhiều report phase trùng nội dung. Mẫu không phải kết quả chạy.
- Thêm bảng7 gói vào [progress](../progress.md), giữ nguyên38 dòng trạng thái phase và 85 dòng owner. G01 phản ánh01 đã có kết quả,02 còn quyết định,03 chưa bắt đầu; các gói khác chưa bắt đầu. Không tạo report thực thi gói giả.
- Các phase03–38 đã có sẵn nhưng chưa tracked: đưa nguyên 36 file chỉ dẫn vào Git để người nhận trên GitHub có đủ tài liệu phụ thuộc. Không đổi bytes hoặc coi việc thêm vào Git là thực thi phase. Các source/PDF/Postman thay đổi sẵn ngoài phạm vi giữ nguyên và không đưa vào commit điều phối.

## Kiểm chứng thực tế

| Kiểm tra/lệnh từ root | Expected / actual | Kết quả |
|---|---|---|
| `python tmp/srs-work-packages/verify.py` | Bảng gói và hàng chờ phủ38 phase/85 task đúng một lần; số task từng gói khớp file phase và checklist; thứ tự dependency không đảo; progress mapping khớp |0/PASS |
| Cùng phép kiểm trên |85 checkbox task,38 dòng trạng thái phase và 85 owner giữ nguyên so với HEAD;136 file có sẵn giữ bytes, kể cả36 file chỉ dẫn được thêm vào Git |0/PASS |
| Cùng phép kiểm trên | Markdown tables và local links trong6 tài liệu điều phối hợp lệ |0/PASS |
| `python docs/specs/srs-completion-phases/verify-decisions.py` |33 GAP/25 DEC/12 mappings;11 quyết định A/B/C cũ được thay hiệu lực,5 vấn đề nguồn còn mở,3 hash baseline giữ nguyên |0/PASS |
| `python docs/specs/srs-completion-phases/verify-inventory.py` |34FR/30NFR/10CONS,347 acceptance,207 CT,45 operations; PDF hash không đổi |0/PASS |
| `git diff --check` theo phạm vi và staged diff check trước commit | Không whitespace errors; chỉ điều phối và 36 file chỉ dẫn phụ thuộc có sẵn |0/PASS |

Script kiểm mapping/snapshot là phép kiểm một lần trong tmp, không thêm framework/test suite ứng dụng. Không chạy lại unit/integration/browser/benchmark vì không sửa runtime. Commit/push theo ủy quyền trong phiên; mô tả tiếng Việt không dấu, không tuyên bố hoàn thành phase.

## Cách dùng tiếp

Dùng prompt G01 ở [work-packages.md](../work-packages.md); agent tái dùng report01, đọc các quyết định hiện tại và làm phần môi trường03 độc lập khi đủ điều kiện. Trong mỗi gói, kiểm phần hiện có trước sửa và chạy kiểm tra liên quan; regression chung chỉ tái dùng khi đúng artifact/config/phạm vi. Các yêu cầu bắt buộc chưa kiểm vẫn mở, không đánh toàn gói Đạt từ phần local hoặc bỏ hàng chờ để kết luận đạt SRS.

Số gói giao việc giảm, không cam kết đúng 7 phiên hoặc giảm quota theo tỷ lệ cố định. Nếu cần đổi phiên, tiếp tục từ checkpoint của cùng report thay vì bắt đầu lại gói.
