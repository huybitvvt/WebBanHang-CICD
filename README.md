# WebBanHang · CI/CD với Jenkins, GitHub và IIS

Ứng dụng ASP.NET Core MVC **.NET 8**, quản lý danh mục/sản phẩm/đơn hàng, có SQLite lưu dữ liệu và quy trình CI/CD theo bài thực hành trong `Bài 4-5 CI-CD.pdf`.

Repository: https://github.com/huybitvvt/WebBanHang-CICD (private).

**Bắt đầu đọc [demo nhanh trên máy này](docs/DEMO-NHANH.md).** [Hướng dẫn đầy đủ](docs/HUONG-DAN-DEMO.md) có chuẩn bị IIS, lời thuyết trình, lệnh thao tác và ba tình huống thành công/thất bại.

Trên máy hiện tại có thể nhấp đúp **CHAY-DEMO.cmd** để chạy. Giữ cửa sổ mở và truy cập URL bên dưới.

Jenkins portable đã được cấu hình trên máy này: nhấp đúp **CHAY-JENKINS.cmd**, mở **http://localhost:8080/job/WebBanHang-CICD/**. Tài khoản ở file riêng tư `.local/jenkins-login.json` (mở bằng Notepad, không đưa lên Git/ảnh báo cáo). Job đã có ba lần chạy thật: main thành công, lỗi build, lỗi test. Trên máy chưa có IIS, `DEPLOY_TO_IIS` mặc định tắt.

Sau khi bạn cấu hình ngrok authtoken trên máy, nhấp đúp **CHAY-WEBHOOK.cmd**. Script khởi động relay có chữ ký, tunnel ngrok và tạo/cập nhật webhook của repo qua GitHub CLI đang đăng nhập; giữ cửa sổ mở. Secret nằm trong `.local/webhook-secret.txt`, không đưa lên Git.

## Chạy ngay trên máy hiện tại

```powershell
cd E:\btnewtkpm
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Start-Demo.ps1
```

Mở **http://localhost:5080/webbanhang**. Đây là bản tập demo bằng Kestrel. URL IIS đúng đề sau khi cấu hình là **http://localhost/webbanhang**.

Nếu clone sang máy thiếu .NET 8 SDK:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Install-DotNet.ps1
```

## Kiểm thử

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-Local.ps1
node --test tests\webhook-relay.test.mjs
node --test tests\deployment.test.mjs
```

Lệnh đầu chạy Restore → Build Release → Test → Publish, dừng ngay khi lệnh lỗi. TRX nằm ở `artifacts/TestResults`, publish ở `artifacts/publish`.

Test deploy dùng robocopy thật trên thư mục thử nghiệm và HTTP endpoint giả lập để kiểm tra giữ config/data, thay code và rollback; không thay thế kiểm tra IIS.

Sau khi có Git repository/main, tạo và kiểm tra hai nhánh lỗi độc lập:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\New-DemoBranches.ps1 -Push
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-FailureScenarios.ps1 -BaselineUrl 'http://localhost:5080/webbanhang/health'
```

Không merge `demo/build-failure` hoặc `demo/test-failure` vào main. Jenkins lấy Jenkinsfile từ main; parameter `SOURCE_BRANCH` chọn mã nguồn của lần kiểm thử, chỉ main được deploy.

## Cấu trúc

| Đường dẫn | Nội dung |
|---|---|
| `src/WebBanHang` | MVC, CRUD danh mục/sản phẩm, lọc sản phẩm, đơn hàng nhiều dòng, giảm giá, kiểm kho, hoàn kho khi hủy |
| `tests/WebBanHang.Tests` | Unit test công thức, transaction/tồn kho và HTTP/PathBase/CSRF |
| `Jenkinsfile` | Checkout, Restore, Build, Test, Publish, Deploy, Verify |
| `scripts/Setup-IIS.ps1` | Pool No Managed Code, IIS sub-application, thư mục dữ liệu và quyền |
| `scripts/Deploy-IIS.ps1` | Backup, app_offline, đồng bộ code, giữ config/data, xác minh, rollback code khi lỗi |
| `scripts/Verify-Deployment.ps1` | Health/database, đúng version, homepage và CSS |
| `scripts/Start-Jenkins.ps1` | Chạy Jenkins WAR ở loopback, giữ cơ chế đăng nhập/setup wizard |
| `scripts/Start-WebhookDemo.ps1` | Khởi động relay/ngrok và cấu hình webhook qua GitHub CLI |
| `scripts/webhook-relay.mjs` | Endpoint webhook xác thực chữ ký; không công khai Jenkins UI |
| `docs/HUONG-DAN-DEMO.md` | Kịch bản trình bày 10–12 phút và khắc phục lỗi |
| `docs/BAO-CAO-KIEM-THU.md` | Kết quả đã kiểm tra và phần minh chứng cần bổ sung trên IIS |

## Lưu dữ liệu và cấu hình

- Local: `src/WebBanHang/App_Data/store.db`; seed 3 danh mục, 6 sản phẩm khi database rỗng. Đơn hàng ban đầu rỗng.
- IIS: script Setup đặt dữ liệu tại `C:\ProgramData\WebBanHang`, ngoài thư mục deploy. `appsettings.Production.json` được tạo ở server và giữ nguyên khi cập nhật.
- `PathBase` do IIS cung cấp; bản local dùng `/webbanhang` qua biến môi trường. Razor Tag Helpers tạo link/CSS đúng prefix.
- Tổng tiền dùng decimal và làm tròn VNĐ. Giá chốt theo database khi tạo đơn; transaction bảo vệ tồn kho, cộng dồn các dòng trùng sản phẩm trước khi kiểm tra.
- Không có đăng nhập/phân quyền người dùng: đây là ứng dụng quản trị cho demo localhost. Không công khai website quản trị bán hàng lên Internet.
- Bài dùng `EnsureCreated`, phù hợp schema cố định. Nếu phát triển schema, cần bổ sung EF migrations và kế hoạch backup/restore dữ liệu; rollback hiện tại chỉ khôi phục code.

## Đối chiếu đề

| Yêu cầu | Cách đáp ứng |
|---|---|
| MVC .NET 8, quản lý ba chức năng | Source MVC và SQLite |
| GitHub, nhánh riêng, PR | Repo có nhánh/PR; ba sinh viên phải tự phân công và commit bằng tài khoản thật |
| Jenkins Windows, job/Webhook | Jenkinsfile + hướng dẫn cấu hình `WebBanHang-CICD` và relay |
| Dừng khi Build/Test lỗi | Exit code được kiểm tra; hai nhánh cố tình sai để chứng minh |
| IIS đúng pool/path/URL | Setup-IIS và Deploy-IIS; cần chạy trên máy hỗ trợ IIS |
| Giữ cấu hình/dữ liệu, tránh khóa DLL | Dữ liệu ngoài code, loại trừ file/thư mục riêng, app_offline, backup |
| Ba tình huống và minh chứng | Test local + kịch bản Jenkins/IIS và mẫu báo cáo |

Tình trạng kiểm chứng và giới hạn môi trường được ghi trong [báo cáo](docs/BAO-CAO-KIEM-THU.md). Ảnh local không thay thế ảnh Jenkins/IIS thật.
