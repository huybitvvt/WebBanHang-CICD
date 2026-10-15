# Báo cáo kiểm thử WebBanHang

Ngày lập: **15/10/2026**. Các mục chưa chạy được ghi rõ, không coi ảnh local là minh chứng triển khai IIS.

## Môi trường thực hiện

- Windows 11 Home Single Language, phiên PowerShell không có quyền Administrator.
- .NET SDK 8.0.426 cài riêng tại `.tools/dotnet` (không thay SDK hệ thống).
- Runtime mục tiêu .NET 8, Entity Framework Core SQLite 8.0.28.
- Git và GitHub CLI có sẵn; repository riêng tư theo URL trong README.
- Chưa có dịch vụ IIS/Jenkins, chưa xác minh Hosting Bundle/Java và webhook Internet trên máy này.

## Kết quả thực thi

Nhật ký/TRX sinh tại `artifacts/` (không đưa output build vào Git). Ảnh trình duyệt thật nằm trong [evidence](evidence/README.md).

| Kiểm tra | Kết quả |
|---|---|
| Restore package lock | PASS với --locked-mode |
| Build Release | PASS, 0 warning / 0 error |
| Unit/integration .NET | 30/30 PASS trên main |
| Publish | PASS, artifacts/publish có DLL, web.config và static assets |
| Relay webhook | 9/9 kiểm tra Node pass, gồm cả test cha |
| PowerShell syntax | Các script parse thành công |
| CRUD bằng trình duyệt / desktop & mobile | PASS trên Edge thật, desktop 1440 và mobile 375, không có lỗi JavaScript; giá trị đơn 180000, tồn kho 10 → 8 → 10 |
| Nhánh lỗi cú pháp | Build FAIL đúng dự kiến với CS0116/CS1022; bản Kestrel nền vẫn Healthy, version không đổi |
| Nhánh sai tính tiền | Build PASS, Test FAIL 4/28 đúng dự kiến; kỳ vọng 180000, thực tế 220000; bản Kestrel nền giữ nguyên |
| Đồng bộ file và rollback deploy | 3/3 kiểm tra Node PASS (gồm test cha), dùng robocopy thật và HTTP endpoint giả lập; giữ App_Data, logs, config Production, thay code và khôi phục code khi version sai |
| Verify trên bản publish Kestrel | PASS: health/database, homepage=200, CSS=200 dưới /webbanhang |
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

## GitHub đã tạo

- Repository private: https://github.com/huybitvvt/WebBanHang-CICD
- `feature/categories`: https://github.com/huybitvvt/WebBanHang-CICD/pull/1 (merged).
- `feature/products`: https://github.com/huybitvvt/WebBanHang-CICD/pull/2 (merged).
- `feature/orders`: https://github.com/huybitvvt/WebBanHang-CICD/pull/3 (merged).
- Hai nhánh demo lỗi đã push riêng: `demo/build-failure`, `demo/test-failure`; không merge vào main.
- Khi nộp repo private, cấp quyền cho giảng viên và thành viên nhóm; chưa thay đổi quyền truy cập thay người dùng.

## Giới hạn kiểm chứng

Chưa chạy Jenkins thực tế, IIS, hay webhook qua Internet. Test file deployment dùng endpoint giả lập chỉ chứng minh logic đồng bộ/giữ dữ liệu/rollback, không chứng minh Hosting Bundle hoặc IIS đã hoạt động. Cần hoàn thành phần 3–6 của hướng dẫn trên máy IIS và bổ sung ảnh đúng URL đề.
