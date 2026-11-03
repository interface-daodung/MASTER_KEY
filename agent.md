# AGENT DIRECTIVE: QUẢN LÝ GIT & VERSION LIÊN TỤC

> **BẮT BUỘC DÀNH CHO MỌI AI AGENT / ASSISTANT:**
> Tài liệu này thiết lập các quy tắc quản trị bắt buộc khi làm việc trong repository `MASTER_KEY`. AI Agent phải **chủ động thực hiện tự động** mọi quy trình Git và quản lý Version trong mỗi lượt làm việc mà **KHÔNG ĐƯỢC CHỜ NGƯỜI DÙNG PHẢI NHẮC LẠI**.

---

## 1. QUẢN LÝ VERSION LIÊN TỤC (CONTINUOUS VERSIONING)

### 1.1. Nguyên tắc Semantic Versioning (`MAJOR.MINOR.PATCH`)
Mỗi khi có thay đổi trong mã nguồn `mkv` CLI / TUI:
- **Tăng `PATCH` (x.y.Z+1)**: Sửa lỗi (bugfix), sửa câu thông báo, tinh chỉnh hiển thị TUI, cập nhật installer script.
- **Tăng `MINOR` (x.Y+1.0)**: Thêm cờ lệnh CLI mới, thêm chức năng mới trong TUI (như cơ chế import/export mới, giao thức ghép đôi mới).
- **Tăng `MAJOR` (X+1.0.0)**: Thay đổi định dạng mã hóa `vault.enc`, cấu trúc DPAPI hoặc thay đổi lớn phá vỡ tính tương thích.

### 1.2. Đồng bộ tệp Version
Đồng bộ version xuyên suốt các tệp cấu hình:
1. `Makefile`: Biến `APP_VERSION ?= X.Y.Z`.
2. `installer/MasterKeyVault.iss`: `#define MyAppVersion "X.Y.Z"`.
3. `installer/build-installer.ps1`: `param([string]$Version = "X.Y.Z")`.

---

## 2. QUẢN LÝ GIT LIÊN TỤC (CONTINUOUS GIT MANAGEMENT)

### 2.1. Trạng thái Working Tree
- Trước khi thực hiện thay đổi: Luôn chạy `git status` để nắm các tệp đang chỉnh sửa.
- Không để tồn tại các tệp rác, tệp biên dịch tạm thời không được theo dõi. Thường xuyên cập nhật `.gitignore` cho các artifact sinh ra (`dist/`, `mkv/publish/`, `bin/`, `obj/`, v.v.).

### 2.2. Tự động Commit sau mỗi Task
Sau khi hoàn thành một nhiệm vụ, tính năng hoặc sửa lỗi và đã xác minh bản build chạy thành công:
- **Bắt buộc tự động thực hiện commit** vào Git với thông điệp theo chuẩn Conventional Commits:
  - `feat: <mô tả>` cho tính năng mới.
  - `fix: <mô tả>` cho sửa lỗi.
  - `refactor: <mô tả>` khi tối ưu/cấu trúc lại code.
  - `chore: <mô tả>` cho việc dọn dẹp, cập nhật cấu hình/tài liệu.
- Định dạng commit nên kèm phiên bản mới (ví dụ: `feat(cli): loai bo winforms cu, toi uu mkv tui doc lap (v1.0.0)`).

---

## 3. QUY TRÌNH KIỂM TRA & XÁC MINH (VERIFICATION)

Trước khi coi một tác vụ là hoàn thành:
1. **Kiểm tra biên dịch và Selftest**:
   - Chạy `dotnet build mkv/mkv.csproj -c Release`.
   - Chạy `dotnet mkv/bin/Release/net8.0-windows/mkv.dll --selftest` để xác nhận logic mã hóa/giải mã toàn vẹn.
2. **Kiểm tra Installer**:
   - Nếu có thay đổi liên quan đến đóng gói: chạy `make publish` hoặc `powershell -ExecutionPolicy Bypass -File installer/build-installer.ps1`.
3. **Báo cáo kết quả**:
   - Nêu rõ số version, commit ID, đường dẫn tệp build hoàn chỉnh và hướng dẫn sử dụng cho người dùng.
