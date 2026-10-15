# Minh chứng local

Các ảnh PNG đều là ảnh chụp trình duyệt Edge thật. Ảnh 01–05 là ứng dụng chạy bằng **Kestrel tại cổng 5080**; ảnh 06–09 là **Jenkins thật tại cổng 8080**. Chưa có ảnh IIS.

- `01-tong-quan-desktop.png`: dashboard 1440px.
- `02-san-pham.png`: danh sách sản phẩm, có sản phẩm kiểm tra.
- `03-don-hang-180000.png`: đơn hai sản phẩm giá 100.000, giảm 10%, tổng 180.000.
- `04-tong-quan-mobile.png`: dashboard 375px.
- `05-tao-don-mobile.png`: form tạo đơn 375px.
- `06-jenkins-stage-view.png`: lịch sử ba lần chạy thật (#1 SUCCESS, #2/#3 FAILURE); Deploy/Verify ở build #1 bị tắt vì máy chưa có IIS.
- `07-jenkins-job-config.png`: Job WebBanHang-CICD, GitHub SCM, main và Jenkinsfile.
- `08-jenkins-build-failure.png`: compiler phát hiện lỗi cú pháp trên nhánh demo/build-failure.
- `09-jenkins-test-failure.png`: Build thành công nhưng test tính giảm giá thất bại.

Kết quả âm tính local:

- Nhánh `demo/build-failure`: compiler trả CS0116/CS1022 tại OrderCalculator.cs, dừng trước Test/Publish.
- Nhánh `demo/test-failure`: Build thành công, Test thất bại 4/28; `AppliesTenPercentDiscount` kỳ vọng 180000, thực tế 220000.
- Sau mỗi tình huống, health của bản Kestrel đang chạy vẫn Healthy, version `1.0.0` tại thời điểm kiểm tra.

Để nộp đủ yêu cầu, bổ sung ảnh webhook nhận sự kiện GitHub thật, IIS Manager, URL `http://localhost/webbanhang` và một build thành công có Deploy/Verify thật trên IIS.
