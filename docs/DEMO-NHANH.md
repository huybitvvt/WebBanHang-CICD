# Demo nhanh trên máy hiện tại

Đã làm sẵn mã nguồn, GitHub private, Jenkins portable và các nhánh lỗi. Máy hiện tại chưa có IIS: các bước dưới đây chứng minh ứng dụng và CI. Muốn trình bày đầy đủ CD lên IIS theo đề, làm phần 3–6 trong [hướng dẫn đầy đủ](HUONG-DAN-DEMO.md) trên máy có IIS.

## Trước giờ học

1. Nhấp đúp `CHAY-DEMO.cmd`, giữ cửa sổ mở → http://localhost:5080/webbanhang.
2. Jenkins hiện đang chạy → http://localhost:8080/job/WebBanHang-CICD/. Nếu đã tắt, nhấp đúp `CHAY-JENKINS.cmd`.
3. Mở `.local/jenkins-login.json` bằng Notepad để lấy username/password. Không chụp hoặc chia sẻ file này trong bài nộp.
4. Mở [GitHub](https://github.com/huybitvvt/WebBanHang-CICD), Jenkins và website thành ba tab.

## Thao tác và lời nói

| Phần | Thao tác | Nói ngắn |
|---|---|---|
| 1. Ứng dụng | Tạo danh mục; tạo sản phẩm giá 100000, tồn 10; tạo đơn số lượng 2, giảm 10%; kiểm tra tổng 180000 và tồn 8; hủy đơn, tồn về 10 | “MVC quản lý danh mục, sản phẩm, đơn hàng; SQLite lưu dữ liệu; tính tiền và kiểm kho ở server.” |
| 2. CI thành công | Jenkins → Build with Parameters → SOURCE_BRANCH=main, DEPLOY_TO_IIS=false → Build → xem Checkout/Restore/Build/Test/Publish xanh | “Jenkins lấy mã thật từ GitHub, build Release, chạy 30 test và publish.” |
| 3. Lỗi biên dịch | Chọn demo/build-failure → Build → mở Console Output, trỏ lỗi CS0116/CS1022 | “Compiler trả lỗi nên pipeline dừng trước Test và Publish.” |
| 4. Lỗi nghiệp vụ | Chọn demo/test-failure → Build → mở Console Output, trỏ Expected 180000 / Actual 220000 | “Build vẫn đúng cú pháp, nhưng unit test chặn công thức giảm giá sai.” |

Stage View có thể tô đỏ cả các stage sau lỗi. Console Output ghi rõ các stage bị skip do lỗi trước đó; kiểm tra dòng `Stage "Publish" skipped due to earlier failure(s)`.

## Webhook sau khi đã cấu hình ngrok

Nhấp đúp `CHAY-WEBHOOK.cmd`, giữ cửa sổ mở. Script tự tạo/cập nhật webhook của repo và gửi ping, không công khai giao diện Jenkins.

Tạo nhánh, sửa `Store:Announcement` trong `src/WebBanHang/appsettings.json`, push và tạo PR vào main. Merge PR → Jenkins phải tự sinh build. Kiểm tra GitHub → Settings → Webhooks → Recent Deliveries.

Ngrok ban đầu chưa đăng nhập (ERR_NGROK_4018). Người dùng tự cấu hình authtoken bằng terminal riêng. Bản web Kestrel từ CHAY-DEMO không tự cập nhật sau build CI; cập nhật web tự động trong đề phải chứng minh trên IIS.

## Tệp để trình chiếu/nộp

- [Jenkinsfile](../Jenkinsfile).
- [Báo cáo kiểm thử](BAO-CAO-KIEM-THU.md).
- [Ảnh ứng dụng/Jenkins thật](evidence/README.md).
- [Hướng dẫn IIS, webhook và toàn bộ kịch bản](HUONG-DAN-DEMO.md).

Repo private đã có ba nhánh feature/PR minh họa và hai nhánh lỗi. Ba sinh viên cần đóng góp bằng tài khoản thật; các PR hiện có đều từ một tài khoản.
