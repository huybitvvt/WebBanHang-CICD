# Minh chứng local

Các ảnh PNG là ảnh chụp trình duyệt Edge thật của ứng dụng chạy bằng **Kestrel tại cổng 5080**, không phải Jenkins hoặc IIS.

- `01-tong-quan-desktop.png`: dashboard 1440px.
- `02-san-pham.png`: danh sách sản phẩm, có sản phẩm kiểm tra.
- `03-don-hang-180000.png`: đơn hai sản phẩm giá 100.000, giảm 10%, tổng 180.000.
- `04-tong-quan-mobile.png`: dashboard 375px.
- `05-tao-don-mobile.png`: form tạo đơn 375px.

Kết quả âm tính local:

- Nhánh `demo/build-failure`: compiler trả CS0116/CS1022 tại OrderCalculator.cs, dừng trước Test/Publish.
- Nhánh `demo/test-failure`: Build thành công, Test thất bại 4/28; `AppliesTenPercentDiscount` kỳ vọng 180000, thực tế 220000.
- Sau mỗi tình huống, health của bản Kestrel đang chạy vẫn Healthy, version `1.0.0` tại thời điểm kiểm tra.

Để nộp đủ yêu cầu, bổ sung ảnh Jenkins Stage View/Job/Webhook, IIS Manager và URL `http://localhost/webbanhang` sau khi chạy trên máy IIS.
