# Báo cáo kiểm thử WebBanHang

Ngày lập: **15/10/2026**. Các mục chưa chạy được ghi rõ, không coi ảnh local là minh chứng triển khai IIS.

## Môi trường thực hiện

- Windows 11 Home Single Language, phiên PowerShell không có quyền Administrator.
- .NET SDK 8.0.426 cài riêng tại `.tools/dotnet` (không thay SDK hệ thống).
- Runtime mục tiêu .NET 8, Entity Framework Core SQLite 8.0.28.
- Git và GitHub CLI có sẵn; repository riêng tư theo URL trong README.
- Chưa có dịch vụ IIS/Jenkins, chưa xác minh Hosting Bundle/Java và webhook Internet trên máy này.

## Kết quả thực thi

Kết quả cuối cùng và ảnh trình duyệt được cập nhật sau khi chạy các kiểm tra. Nhật ký/TRX sinh tại `artifacts/` (không đưa output build vào Git).

| Kiểm tra | Kết quả |
|---|---|
| Restore package lock | Chờ cập nhật kết quả |
| Build Release | Chờ cập nhật kết quả |
| Unit/integration .NET | Chờ cập nhật kết quả |
| Publish | Chờ cập nhật kết quả |
| Relay webhook | 9/9 kiểm tra Node pass, gồm cả test cha |
| PowerShell syntax | Các script parse thành công |
| CRUD bằng trình duyệt / desktop & mobile | Chờ cập nhật kết quả |
| Nhánh lỗi cú pháp | Chờ cập nhật kết quả |
| Nhánh sai tính tiền | Chờ cập nhật kết quả |
| Jenkins Job thực tế | Chưa chạy; cần Java/Jenkins |
| IIS thực tế | Chưa chạy; cần máy hỗ trợ IIS + quyền admin |
| Webhook GitHub qua Internet | Chưa chạy; cần Jenkins, ngrok và secret |

## Ba tình huống phải chụp trên Jenkins/IIS

| Tình huống | Thao tác | Kết quả mong đợi | Kết quả thực tế trên IIS |
|---|---|---|---|
| Thành công | Merge thay đổi Store:Announcement vào main | Tự chạy 7 stage, IIS có thông báo/version mới, dữ liệu cũ còn | Điền sau khi chạy trên máy IIS |
| Build lỗi | Build nhánh demo/build-failure | Dừng tại Build; website/version IIS giữ nguyên | Điền sau khi chạy trên máy IIS |
| Test lỗi | Build nhánh demo/test-failure | Build pass, Test fail; không Publish/Deploy; website/version IIS giữ nguyên | Điền sau khi chạy trên máy IIS |

Trong nhánh test lỗi: `100000 × 2 × (1 − 10/100) = 180000`; mã sai dùng dấu cộng trả `220000`.

## Danh sách minh chứng cần nộp

- URL repository, các nhánh và PR; danh sách phân công ba sinh viên thật.
- Jenkins Job `WebBanHang-CICD`: SCM/main/Jenkinsfile, trigger và parameters.
- Stage View/lịch sử: thành công, Build lỗi, Test lỗi; console/TRX của hai lỗi.
- IIS: Default Web Site/webbanhang, WebBanHangPool/No Managed Code, physical path.
- Trình duyệt `http://localhost/webbanhang`, health/version trước và sau từng tình huống.
- GitHub webhook Recent Deliveries thành công và build tự khởi chạy sau merge.

Repo/nhánh/PR do một tài khoản tạo chỉ là minh họa quy trình. Không dùng chúng làm bằng chứng có ba người phát triển.
