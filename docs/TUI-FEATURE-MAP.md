# TUI feature map: WinForms → `mkv`

## Ánh xạ chức năng

| UI WinForms hiện tại | TUI `mkv` | Ghi chú |
|---|---|---|
| Tạo MASTER_KEY, QR Authenticator và chốt bằng TOTP | `Create vault` | QR Authenticator được in dạng ASCII; DPAPI và vault chỉ ghi sau khi mã TOTP hợp lệ. |
| Tự mở vault bằng DPAPI lúc khởi động | Tự mở khi `mkv` chạy | Nếu DPAPI không mở được thì TUI vào trạng thái khóa. |
| Bảng key/value, ẩn value, tự lưu | `Browse keys`, `Add / edit key` | Danh sách chỉ hiện tên và mask value; sửa/xóa lưu ngay vào `vault.enc`. |
| Hiện value | `Reveal value` | Chỉ hiện value sau thao tác rõ ràng. |
| Xóa dòng | `Delete key` | Yêu cầu gõ `DELETE`, sau đó lưu vault. |
| Nhập MASTER_KEY thủ công để khôi phục | `Restore with master key` | Xác minh giải mã thành công trước khi lưu DPAPI. |
| Quét webcam hoặc chọn ảnh QR để khôi phục | Bỏ khỏi flow TUI | Thay bằng QR ghép đôi WinHello qua HTTPS pin certificate. |
| Mở thư mục và xem đường dẫn file | `Vault details` và đường dẫn trên header | Đường dẫn dựa trên Documents/OneDrive như trước. |
| Ctrl+S | Lưu ngay sau từng thay đổi | Không cần thao tác lưu riêng. |
| Import mật khẩu từ browser CSV | `Browser passwords` → `Import CSV file` hoặc `mkv --import-browser-csv <file.csv>` | Lưu trong một trường nội bộ được mã hóa cùng `vault.enc`, tách khỏi danh sách key thường. |
| Export mật khẩu ra CSV | `Browser passwords` → `Export CSV file` hoặc `mkv --export-browser-csv <file.csv>` | CSV chứa password plaintext; hỏi trước khi ghi đè. |

Với từng cặp website + username trùng, TUI cho giữ bản trong vault, thay bằng CSV, giữ cả hai, bỏ dòng CSV hoặc hủy toàn bộ lượt nhập. Màn hình xung đột chỉ hiện website và username, không in mật khẩu. CSV hỗ trợ dấu phẩy, chấm phẩy, tab, dấu quote và trường nhiều dòng.

## Giao thức QR mở khóa WinHello

TUI chọn IPv4 LAN, mở TLS server tạm trên cổng động, tạo certificate tự ký dùng trong 3 phút và QR có dạng:

```text
winhello://vault-unlock?endpoint=<HTTPS_URL>&pin=<SHA256_CERT_HEX>&token=<ONE_TIME_TOKEN>
```

Phiên có hạn 120 giây. Endpoint nhận `POST /v1/unlock`, header `X-MKV-Token`, body JSON `{ "masterKey": "..." }`. Server kiểm tra token, thử giải mã `vault.enc`, rồi mới lưu DPAPI và trả `{ "unlocked": true }`. Tối đa 5 request/key sai; token và server hết hiệu lực khi phiên kết thúc.

Client WinHello cần thêm xử lý scheme `vault-unlock`, xác minh SHA-256 certificate pin trong QR trước khi gửi secret, yêu cầu người dùng xác nhận, gửi POST trên LAN và xóa dữ liệu key khỏi bộ nhớ sau khi hoàn tất. Bản app hiện tại chưa hỗ trợ scheme này; thay đổi app nằm ngoài repository `MASTER_KEY`.
