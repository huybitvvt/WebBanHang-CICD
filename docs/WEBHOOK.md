# GitHub Webhook qua ngrok

## Khởi động nhanh

Trên máy hiện tại có Node.js, ngrok và GitHub CLI: sau khi cấu hình ngrok authtoken tại terminal riêng, nhấp đúp `CHAY-WEBHOOK.cmd` hoặc chạy:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Start-WebhookDemo.ps1
```

Script khởi động relay/tunnel, dùng tài khoản GitHub CLI để tạo/cập nhật webhook của repo và gửi ping. Secret được giữ tại `.local/webhook-secret.txt`, chỉ tài khoản Windows hiện tại và SYSTEM được cấp quyền. Log riêng tư nằm tại `.local/webhook/ngrok.log`. Không mở thêm relay/ngrok khác cùng cổng 8787/4040.

Giữ cửa sổ mở; Ctrl+C dừng tunnel và relay do script tạo. Mỗi lần khởi động, URL webhook được cập nhật theo tunnel hiện tại. Chỉ dòng `Webhook ready` chưa chứng minh build đã chạy: kiểm tra Recent Deliveries và merge một PR vào main.

Ngrok hiện cần người dùng tự hoàn tất đăng nhập; lần thử ban đầu báo ERR_NGROK_4018. Không gửi authtoken vào chat hoặc Git.

## Cấu hình thủ công

Không tunnel cổng Jenkins 8080 trực tiếp. Jenkins WAR trong bộ này chỉ nghe loopback. Relay Node.js xác thực HMAC SHA-256 trên raw body, kiểm tra repo, chỉ chuyển push lên main, giới hạn 1 MiB và bỏ qua delivery trùng trong bộ nhớ một giờ. Relay không cung cấp giao diện quản trị Jenkins.

Cần Node.js 22+, ngrok CLI đã đăng nhập, Jenkins đã chạy, job đã chạy build đầu để đăng ký SCM/trigger.

Terminal 1 (nhập secret bí mật ít nhất 32 ký tự, không dùng secret ví dụ):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Start-WebhookRelay.ps1 -Repository 'huybitvvt/WebBanHang-CICD'
```

Terminal 2:

```powershell
ngrok http http://127.0.0.1:8787
```

GitHub → repo Settings → Webhooks → Add webhook:

- Payload URL `https://<domain-ngrok>/github-webhook/`.
- Content type `application/json`.
- Secret giống Terminal 1, SSL verification bật, Just the push event.
- Ping thành công trả `Webhook authenticated`; push main hợp lệ trả `Forwarded to Jenkins`.

Ngrok và relay phải còn chạy khi demo. Relay chuyển request tới `http://127.0.0.1:8080/github-webhook/`. Nếu Jenkins dùng cổng khác, chỉnh cấu hình upstream trong relay. Secret không nằm trên command line hoặc file trong repo. Không chụp secret/token vào báo cáo.

Kiểm tra relay:

```powershell
node --test tests\webhook-relay.test.mjs
```

Webhook qua Internet và giao diện Jenkins chưa được xác minh chỉ bằng các test này. Phải kiểm tra Recent Deliveries và build tự chạy khi merge main để có minh chứng thật.
