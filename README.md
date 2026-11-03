# MASTER_KEY / mkv

`mkv` là giao diện dòng lệnh và TUI để quản lý `vault.enc` trên Windows. Chạy `mkv` hoặc `mkv --tui` để mở menu; các lệnh như `mkv --list`, `mkv KEY_NAME` vẫn giữ nguyên cho script.

## Phím tắt trong TUI

- `↑` / `↓` rồi `Enter`, hoặc nhấn số `1`–`9` để chọn.
- `Browse keys` liệt kê tên key và che value.
- `Add / edit key`, `Delete key` sửa vault và lưu ngay.
- `Reveal value` hiện value theo yêu cầu.
- `Browser passwords` là mục riêng để xem, thêm, xóa hoặc nhập CSV; mật khẩu được che và lưu bên trong `vault.enc` đã mã hóa.
- `Create vault` sinh MASTER_KEY, in QR Authenticator, yêu cầu TOTP trước khi ghi DPAPI và tạo vault.
- `Restore with master key` mở vault trên máy mới và lưu lại DPAPI.
- `Unlock with WinHello QR` mở phiên TLS tạm để điện thoại gửi key; QR hết hạn sau 120 giây và chỉ nhận một key hợp lệ.

Vault giữ nguyên đường dẫn Documents (dùng OneDrive\Documents nếu Documents được chuyển hướng), định dạng AES-256-CBC có IV 16 byte ở đầu file, JSON key-value và Credential Manager/DPAPI theo user Windows hiện tại.

## Build và chạy

```powershell
dotnet run --project .\mkv\mkv.csproj -- --tui
dotnet build .\mkv\mkv.csproj -c Release
```

`make run` mở TUI; `make run-cli KEY=NAME` gọi tra key cũ. Installer và shortcut khởi chạy `mkv --tui`.

Nhập CSV bằng `mkv --import-browser-csv "C:\Users\MiniPC\Downloads\fake_password_import.csv"`, hoặc vào `Browser passwords` → `Import CSV file`. Hỗ trợ các tên cột phổ biến như `name/title`, `url/website/site`, `username/login`, `password` và `note`. Khi website + username trùng, chọn giữ bản cũ, thay bằng CSV, giữ cả hai, bỏ dòng hoặc hủy lượt nhập. Danh sách và màn hình xung đột không hiện mật khẩu.

Xuất bằng `mkv --export-browser-csv "C:\Users\MiniPC\Downloads\browser_passwords.csv"`, hoặc `Browser passwords` → `Export CSV file`. CSV xuất có password dạng plaintext; TUI cảnh báo và hỏi xác nhận trước khi ghi đè file có sẵn.

## Kết nối app WinHello

TUI tạo QR dạng `winhello://vault-unlock` chứa HTTPS endpoint LAN, SHA-256 certificate pin và token một lần. Client gửi `POST /v1/unlock` qua HTTPS với header `X-MKV-Token: <token>` và JSON `{"masterKey":"<BASE32>"}`. Server chỉ lưu key vào DPAPI sau khi dùng key đó giải mã được vault. Xem [TUI feature map và giao thức](docs/TUI-FEATURE-MAP.md).

App WinHello hiện có cần được cập nhật để nhận QR scheme này, xác minh certificate pin, hỏi xác nhận người dùng rồi gửi POST trên cùng mạng. Mã app WinHello không nằm trong repo này và chưa được thay đổi ở nhánh này.
