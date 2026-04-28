_bundles được tạo để gom bộ file chạy theo từng ứng dụng cho dễ quản lý.

- MailSender.Manager/: bộ file runtime của MailSender Manager
- PlayAPP/: bộ file runtime của PlayAPP

Launcher:
- Chạy Manager: E:\Project\Automation\Run-MailSender-Manager.bat
- Chạy PlayAPP: E:\Project\Automation\Run-PlayAPP.bat

Đồng bộ lại _bundles sau khi build:
- powershell -ExecutionPolicy Bypass -File "E:\Project\Automation\scripts\sync-bundles.ps1"

Lưu ý:
- Các file gốc ở thư mục root vẫn được giữ nguyên để đảm bảo tương thích hiện tại.
- Có thể chạy trực tiếp từ launcher để làm quen dần với cấu trúc _bundles mà không phá setup cũ.
