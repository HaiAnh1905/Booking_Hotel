# Hướng Dẫn Nghiệm Thu & Kiểm Thử Nhanh (Quickstart Verification): RM-002

**Feature Branch**: `002-replace-default-auth`  
**Date**: 2026-11-11  
**Status**: Ready  

---

## 1. Kịch Bản Kiểm Thử Thủ Công (Manual End-to-End Scenarios)

### Kịch bản 1: Bấm nút "Login" trên Header LeptonX Theme
1. Khởi động ứng dụng Angular: `npm start` (tại thư mục `angular/`).
2. Mở trình duyệt tại `http://localhost:4200`.
3. Khi chưa đăng nhập, nhìn vào góc phải trên cùng của thanh Header LeptonX, nhấp chuột vào nút **"Login"** (hoặc biểu tượng Đăng nhập).
4. **Kết quả mong đợi**:
   - Trình duyệt điều hướng nội bộ SPA đến `http://localhost:4200/auth/login`.
   - Trang không bị reload hoặc chớp giật.
   - Hoàn toàn không bị đẩy sang trang Razor Page backend `https://localhost:44386/Account/Login`.

---

### Kịch bản 2: Gõ trực tiếp các URL cũ của ABP Account
1. Trên thanh địa chỉ trình duyệt, nhập: `http://localhost:4200/account/login`.
   - **Kết quả**: Tự động chuyển hướng sang `http://localhost:4200/auth/login`.
2. Nhập: `http://localhost:4200/account/register`.
   - **Kết quả**: Tự động chuyển hướng sang `http://localhost:4200/auth/register`.
3. Nhập: `http://localhost:4200/account/forgot-password`.
   - **Kết quả**: Tự động chuyển hướng sang `http://localhost:4200/auth/forgot-password`.

---

### Kịch bản 3: Đăng nhập thành công và đồng bộ phiên (Zero-Reload)
1. Tại `http://localhost:4200/auth/login`, nhập tài khoản (ví dụ `admin@hotel.com` / `admin`).
2. Bấm nút **"Đăng nhập"**.
3. **Kết quả mong đợi**:
   - API xác thực thành công và trả về JWT `accessToken`.
   - `OAuthStorage` (`localStorage`) ghi nhận `access_token` và `expires_at`.
   - Hệ thống tự động chuyển hướng về trang chủ `http://localhost:4200/` mà **không reload lại toàn trang**.
   - Thanh Header LeptonX hiển thị ngay lập tức tên người dùng, avatar và các menu quản trị mà người dùng có quyền (Identity, Settings).

---

### Kịch bản 4: Đăng xuất (Logout) an toàn
1. Khi đang trong trạng thái đăng nhập, nhấp vào Avatar/Tên người dùng trên Header LeptonX.
2. Chọn **"Logout"** (hoặc "Đăng xuất").
3. **Kết quả mong đợi**:
   - Token trong `localStorage` bị xóa sạch (`access_token = null`).
   - Ứng dụng đưa người dùng về `http://localhost:4200/` dưới vai trò khách vãng lai.
   - Nút "Login" trên Header xuất hiện trở lại.

---

### Kịch bản 5: Chuyển hướng trực tiếp từ Backend
1. Trên trình duyệt, truy cập trực tiếp: `https://localhost:44386/Account/Login`.
2. **Kết quả mong đợi**:
   - Backend trả về mã HTTP 302 Found.
   - Trình duyệt tự động mở `http://localhost:4200/auth/login`.

---

### Kịch bản 6: Kiểm tra Quy chuẩn Độ phức tạp Mật khẩu
1. Mở trang Đăng ký: `http://localhost:4200/auth/register` (hoặc Đặt lại mật khẩu).
2. Tại ô "Mật khẩu", nhập thử các trường hợp không đạt chuẩn:
   - Nhập `12345` (< 8 ký tự) $\rightarrow$ Báo lỗi độ dài.
   - Nhập `abcdefgh` (thiếu hoa, số, ký tự đặc biệt) $\rightarrow$ Báo lỗi.
   - Nhập `Abcdefgh` (thiếu số và ký tự đặc biệt) $\rightarrow$ Báo lỗi.
   - Nhập `Abcdef12` (thiếu ký tự đặc biệt) $\rightarrow$ Báo lỗi.
3. Nhập mật khẩu đạt chuẩn: `Booking@2026` (đủ 8 ký tự, có 1 hoa `B`, chữ thường, số `2026`, ký tự đặc biệt `@`).
4. **Kết quả mong đợi**:
   - Tất cả thông báo lỗi biến mất, form hợp lệ, cho phép gửi dữ liệu lên server.
   - Server Backend tiếp nhận và mã hóa mật khẩu an toàn.

---

### Kịch bản 7: Hoàn tất Đăng ký và Yêu cầu Đăng nhập Thủ công
1. Mở trang Đăng ký: `http://localhost:4200/auth/register`.
2. Hoàn tất tuần tự:
   - Bước 1: Nhập email hợp lệ, mật khẩu đạt chuẩn phức tạp, xác nhận mật khẩu khớp $\rightarrow$ Bấm "Continue (Bước 2)".
   - Bước 2: Nhập Họ và Tên $\rightarrow$ Bấm "Continue (Bước 3)".
   - Bước 3: Nhập Số điện thoại Việt Nam $\rightarrow$ Bấm "Hoàn tất Đăng ký".
3. **Kết quả mong đợi**:
   - Giao diện chuyển sang Bước 4 (Success Screen): hiển thị thông báo "Đăng Ký Thành Công!" và nút bấm **"Quay lại Đăng nhập"**.
   - Kiểm tra `localStorage`: Không có access token nào được lưu tự động; thanh Header vẫn hiển thị nút "Login" (chưa đăng nhập).
   - Nhấp vào nút **"Quay lại Đăng nhập"**: Trình duyệt điều hướng về `http://localhost:4200/auth/login`.
   - Người dùng tự nhập mật khẩu vừa tạo để chính thức đăng nhập vào hệ thống.

---

### Kịch bản 8: Ngăn chặn người dùng đã đăng nhập truy cập lại trang đăng nhập
1. Đăng nhập thành công vào hệ thống bằng tài khoản bất kỳ.
2. Xác nhận thanh Header LeptonX hiển thị trạng thái đã đăng nhập (tên người dùng/avatar).
3. Người dùng cố ý gõ URL `http://localhost:4200/auth/login` trên thanh địa chỉ trình duyệt hoặc bấm nút Back.
4. **Kết quả mong đợi**:
   - `guestGuard` chặn yêu cầu điều hướng.
   - Trình duyệt tự động đưa người dùng trở lại Trang chủ `http://localhost:4200/`.
   - Phiên làm việc và các dữ liệu hiện thời vẫn được giữ nguyên vẹn 100%.

---

## 2. Kiểm Tra Biên Dịch & Kiểm Thử Tự Động (Automated Build & Test Checks)

### 2.1. Kiểm tra Biên dịch Angular (Production Build)
```powershell
Set-Location angular
npm run build
```
- **Tiêu chuẩn**: Build thành công với **0 lỗi, 0 cảnh báo**.
- Chunk `abp-ng-account` đã được loại bỏ hoàn toàn khỏi danh sách lazy chunks.

### 2.2. Kiểm tra Test Suite Toàn Diện Backend .NET
```powershell
dotnet test test/BookingHotel.EntityFrameworkCore.Tests/BookingHotel.EntityFrameworkCore.Tests.csproj --no-build
```
- **Tiêu chuẩn**: 32/32 tests passing (100%).

