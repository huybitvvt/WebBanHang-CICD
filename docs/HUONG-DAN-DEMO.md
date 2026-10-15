# Kịch bản demo WebBanHang CI/CD

## 1. Chọn đúng môi trường

Đề bắt buộc GitHub → Jenkins → IIS tại `http://localhost/webbanhang`.
Máy thực hiện hiện là Windows 11 Home, chưa có IIS/Jenkins và phiên làm việc không có quyền Administrator. **Bản chạy tại cổng 5080 là Kestrel để tập demo, không phải minh chứng IIS.** Muốn demo trọn đề, chuẩn bị Windows 11 Pro/Education/Enterprise hoặc Windows Server có IIS và quyền admin.

Trước buổi học, chạy:

```powershell
cd E:\btnewtkpm
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Check-Environment.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-Local.ps1
```

SDK .NET 8 đã nằm ở `.tools/dotnet` trên máy này. Khi clone sang máy khác thiếu SDK 8:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Install-DotNet.ps1
```

## 2. Tập thao tác ứng dụng ngay trên máy hiện tại

Cách nhanh: nhấp đúp `CHAY-DEMO.cmd` trong thư mục dự án. Hoặc chạy lệnh sau:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Start-Demo.ps1
```

Mở `http://localhost:5080/webbanhang`. Giữ terminal mở, dừng bằng Ctrl+C.

1. Tổng quan: giới thiệu số danh mục, sản phẩm, đơn hàng, sản phẩm sắp hết.
2. Danh mục → Thêm danh mục → nhập `Thiết bị demo` → Lưu.
3. Sản phẩm → Thêm sản phẩm → `Bàn phím demo`, danh mục vừa tạo, giá `100000`, tồn kho `10`.
4. Đơn hàng → Tạo đơn → khách `Nguyễn Văn An`, điện thoại `0901234567`, sản phẩm demo, số lượng `2`, giảm giá `10`.
5. Chi tiết phải ra **180.000 đ**. Quay lại Sản phẩm: tồn kho còn **8**.
6. Hủy đơn: tồn kho trở về **10**. Tạo đơn mới rồi bấm Hoàn tất để minh họa trạng thái khác.

Lời nói ngắn: “Ứng dụng dùng MVC, SQLite lưu bền vững. Giá được lấy từ server; đơn hàng và thay đổi tồn kho được lưu trong cùng transaction.”

## 3. Cấu hình IIS một lần, trước khi lên lớp

Trên máy hỗ trợ IIS:

1. Cài Git for Windows, .NET 8 SDK và Java 21+ (Temurin).
2. Mở **Windows PowerShell 5.1 → Run as administrator**. Bật IIS:

```powershell
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole,IIS-WebServer,IIS-ManagementConsole,IIS-ManagementScriptingTools -All
```

3. Cài **ASP.NET Core Hosting Bundle .NET 8** từ https://dotnet.microsoft.com/download/dotnet/8.0 sau khi bật IIS. Nếu cài trước IIS thì Repair Hosting Bundle. Khởi động lại máy nếu được yêu cầu.
4. Từ thư mục repo, chạy (thay `TENMAY\taikhoan` bằng tài khoản thực sự chạy Jenkins):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Setup-IIS.ps1 -JenkinsAccount 'TENMAY\taikhoan'
```

Script tạo `C:\wwwroot\webbanhang`, Application Pool `WebBanHangPool` với **No Managed Code**, ứng dụng `webbanhang` dưới **Default Web Site**; dữ liệu/khóa nằm ở `C:\ProgramData\WebBanHang`.

Không cần cấp Jenkins quyền admin cho từng lần deploy: Jenkins có Modify trên thư mục ứng dụng và backup, dùng `app_offline.htm` để giải phóng DLL. IIS AppPool chỉ có Read & Execute ở thư mục code và Modify ở thư mục dữ liệu.

Kiểm tra Default Web Site có HTTP binding cổng 80. Nếu cổng bị chiếm, xử lý xung đột để giữ đúng URL đề. Ứng dụng là **Application**, không phải Virtual Directory. Mỗi ứng dụng ASP.NET Core in-process dùng pool riêng.

## 4. Tạo Jenkins Job

Có thể dùng Jenkins Windows Installer từ https://www.jenkins.io/download/ hoặc bản WAR không cần cài service:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Start-Jenkins.ps1
```

1. Mở `http://localhost:8080`, lấy mật khẩu từ đường dẫn script in ra. Không đưa mật khẩu vào Git/ảnh báo cáo.
2. Install suggested plugins; đảm bảo có **Pipeline**, **Git**, **GitHub**, **Pipeline: Stage View**, **Timestamper**. Khởi động lại nếu yêu cầu.
3. Manage Jenkins → Nodes → Built-In Node → Configure → Labels: `windows`; Executors: `1`. Phòng lab một máy dùng built-in node; môi trường thật nên tách agent.
4. Nếu SDK 8/Git được cài hệ thống sau khi Jenkins đã chạy, khởi động lại Jenkins để nhận PATH. SDK cài trong repo gốc không tự xuất hiện ở workspace clone của Jenkins; cài SDK 8 hệ thống trên máy agent hoặc chạy `Install-DotNet.ps1` trong workspace đó trước lần build đầu.
5. Repo riêng tư: Manage Jenkins → Credentials → System → Global → Add Credentials → **Username with password**. Username: tài khoản GitHub, password: fine-grained PAT chỉ có quyền Contents: Read trên repo, ID: `github-webbanhang`. PAT để trong Jenkins Credentials, không ghi Jenkinsfile. Repo public có thể để Credentials trống.
6. New Item → tên **WebBanHang-CICD** → **Pipeline**.
7. Pipeline → **Pipeline script from SCM** → SCM **Git** → Repository URL lấy trong README → chọn Credentials → Branch Specifier `*/main` → Script Path `Jenkinsfile`.
8. Save → **Build Now lần đầu**. Sau lần đầu Jenkins đăng ký parameters và trigger; từ đó có **Build with Parameters**.
9. `SOURCE_BRANCH=main`, `DEPLOY_TO_IIS=true`. Pipeline phải xanh cả Checkout, Restore, Build, Test, Publish, Deploy, Verify.
10. Mở `http://localhost/webbanhang` và `/webbanhang/health`; version phải trùng mã commit/build trong Jenkins. Chụp ảnh.

`DEPLOY_TO_IIS=false` chỉ kiểm tra CI/Publish, hai bước cuối bị skip. Không dùng kết quả đó để tuyên bố đã triển khai IIS.

## 5. GitHub Webhook

Máy Jenkins localhost cần URL HTTPS GitHub truy cập được. Có thể dùng ngrok, nhưng chỉ công khai endpoint webhook, giữ giao diện quản trị ở loopback.

1. Tạo ngrok account/cấu hình authtoken bằng trình hướng dẫn chính thức, không đưa token vào repo.
2. Chạy relay an toàn có trong repo theo `docs/WEBHOOK.md` rồi tunnel **cổng relay 8787**. Relay chỉ chấp nhận `POST /github-webhook/` có chữ ký HMAC SHA-256 hợp lệ, không proxy Jenkins UI.
3. GitHub repo → Settings → Webhooks → Add webhook:
   - Payload URL: `https://<ngrok-domain>/github-webhook/` (có dấu `/` cuối).
   - Content type: `application/json`.
   - Secret: cùng secret đã nhập lúc chạy relay.
   - Events: Just the push event; Active bật; SSL verification bật.
4. Trong Jenkins job, xác nhận **GitHub hook trigger for GITScm polling** đã được Jenkinsfile bật sau build đầu.
5. Recent Deliveries phải có phản hồi 2xx; push/merge lên `main` phải tự sinh build. Relay bỏ qua push nhánh khác.

Nếu ngrok đổi domain, cập nhật Payload URL trước buổi demo. Không có Internet thì thao tác Build with Parameters vẫn chạy CI/CD nhưng **không chứng minh webhook tự động**.

## 6. Demo trên lớp, khoảng 10–12 phút

Mở sẵn bốn tab: GitHub, Jenkins, IIS Manager, website IIS. Không cài công cụ hoặc tải NuGet trực tiếp khi đang trình bày.

### Phần A — Giới thiệu và chức năng (2 phút)

Nói: “Mỗi lần merge main, GitHub gửi webhook; Jenkins lấy đúng commit, restore, build Release, test, publish, deploy và kiểm tra lại ứng dụng.” Sau đó tạo đơn 180.000 đ theo phần 2.

### Phần B — Triển khai thành công (3 phút)

1. Ghi lại `/webbanhang/health` của bản hiện tại và sản phẩm demo để kiểm tra dữ liệu còn sau cập nhật.
2. Tạo nhánh mới và sửa **Store:Announcement** trong `src/WebBanHang/appsettings.json`, ví dụ `Phiên bản demo trên lớp đã cập nhật thành công.`

```powershell
git switch main
git pull --ff-only
git switch -c demo/home-update-01
# Sửa Store:Announcement bằng editor, rồi:
git add src/WebBanHang/appsettings.json
git commit -m "demo: update homepage announcement"
git push -u origin demo/home-update-01
gh pr create --base main --head demo/home-update-01 --title "demo: update homepage" --body "Cập nhật thông báo để minh họa triển khai tự động."
```

3. Mở PR trên GitHub và **Merge pull request**. Có thể dùng `gh pr merge --merge` nếu repo cho phép. Không push trực tiếp main cho tình huống này.
4. Chuyển Jenkins: build phải tự chạy. Giải thích từng stage khi đang chạy.
5. Sau Verify xanh, F5 website IIS: thông báo mới, version mới; sản phẩm/đơn demo vẫn còn.

### Phần C — Build thất bại (2 phút)

Nhánh `demo/build-failure` đã có lỗi cú pháp C# thật. Trong Jenkins → Build with Parameters → chọn nhánh đó, giữ tùy chọn deploy bật → Build.

Kỳ vọng: Checkout/Restore xanh, **Build đỏ**, Test/Publish/Deploy/Verify không chạy. Mở Console Output để chỉ lỗi C#. F5 website IIS và health: bản thành công ở phần B vẫn hoạt động, version không đổi.

Nói: “Pipeline dừng theo exit code của compiler nên mã lỗi chưa thể tới bước deploy.”

Lưu ý: hai nhánh lỗi được tạo từ bản main có 28 test. Main hiện có thêm hai kiểm tra bộ lọc và chạy 30 test; chênh lệch này là do thời điểm tạo nhánh.

### Phần D — Test thất bại (2 phút)

Nhánh `demo/test-failure` cố tình đổi công thức giảm giá từ dấu trừ sang dấu cộng. Jenkins → Build with Parameters → chọn nhánh đó → Build.

Kỳ vọng: **Build xanh**, **Test đỏ**, Publish/Deploy/Verify không chạy. Console Output/Artifacts TRX cho thấy `AppliesTenPercentDiscount`: kỳ vọng 180.000, thực tế 220.000. Website IIS vẫn giữ phiên bản phần B.

Nói: “Code hợp lệ cú pháp vẫn có thể sai nghiệp vụ. Unit test chặn bản tính tiền sai trước Publish và Deploy.”

### Phần E — Kết thúc (1 phút)

Mở lịch sử Jenkins gồm một lần thành công và hai lần thất bại. Mở Jenkinsfile và trỏ vào thứ tự các stage. Chỉ thư mục dữ liệu ngoài thư mục code, cơ chế `app_offline.htm`, backup và kiểm tra version sau deploy.

## 7. Câu hỏi thường gặp

| Câu hỏi | Câu trả lời ngắn |
|---|---|
| CI là gì? | Tự động lấy mã, restore, build và test sau thay đổi mã nguồn. |
| CD trong bài là gì? | Publish và tự triển khai bản đã qua kiểm thử lên IIS. |
| Vì sao cần test nếu build đã xanh? | Build kiểm tra biên dịch; test phát hiện sai nghiệp vụ như công thức giảm giá. |
| Vì sao No Managed Code? | ASP.NET Core chạy qua ASP.NET Core Module, không dùng CLR .NET Framework của IIS pool. |
| Deploy có mất dữ liệu không? | SQLite/keys ở thư mục ngoài code; config Production và App_Data được loại khỏi đồng bộ/xóa. |
| Nếu deploy lỗi? | Script thử khôi phục code cũ và health check; vẫn báo pipeline thất bại. Không tự rollback thay đổi schema dữ liệu. |
| Vì sao CSS phải dùng `~/`? | Tag Helper sinh URL theo PathBase `/webbanhang`, hoạt động khi app là IIS sub-application. |
| Ba nhánh có chứng minh ba sinh viên? | Không. Nhánh/PR chứng minh quy trình; nhóm phải phân công và commit bằng tài khoản của từng người thật. |

## 8. Ảnh cần nộp và xử lý lỗi

Chụp đúng kết quả thật: GitHub nhánh/PR, Jenkins Job SCM/trigger, Stage View ba lần chạy, Console Output lỗi, IIS application và pool, thư mục publish, trình duyệt đúng URL IIS, health version trước/sau lỗi. Điền `docs/BAO-CAO-KIEM-THU.md` sau khi chạy trên IIS.

| Lỗi | Xử lý |
|---|---|
| `dotnet` không tìm thấy SDK 8 | Cài .NET 8 SDK hoặc chạy Install-DotNet trong đúng workspace. Runtime 8 không thay thế SDK. |
| Jenkins chờ agent | Gán label `windows` cho node Windows đang online. |
| Job chưa có parameters | Chạy Build Now một lần để Jenkins đọc Jenkinsfile. |
| Checkout private repo thất bại | Kiểm tra Credentials PAT, quyền đọc repo và branch main. |
| IIS 500.19 / 500.30 / 500.31 | Kiểm tra Hosting Bundle, pool No Managed Code, quyền DataDirectory, Windows Event Viewer. |
| CSS 404 | Dùng URL `/webbanhang`, kiểm tra ứng dụng IIS và URL `~/css/site.css` được render có prefix. |
| DLL đang bị khóa | Kiểm tra quyền ghi app_offline.htm, chờ app tắt; xem log robocopy. Không xóa database. |
| Webhook không kích hoạt | Kiểm tra ngrok/relay đang chạy, URL mới, secret, delivery 2xx, job đã chạy lần đầu, nhánh main. |
| Deploy giữ trang bảo trì | Xem lỗi copy/rollback và bản backup; chỉ xóa app_offline.htm sau khi xác nhận code đầy đủ. |

## 9. Tài liệu chính thức

- Jenkins WAR/Java: https://www.jenkins.io/doc/book/installing/war-file/
- GitHub trigger: https://plugins.jenkins.io/github/
- IIS/Hosting Bundle/sub-application: https://learn.microsoft.com/aspnet/core/host-and-deploy/iis/?view=aspnetcore-8.0
- Non-admin SDK installation: https://learn.microsoft.com/dotnet/core/tools/dotnet-install-script
