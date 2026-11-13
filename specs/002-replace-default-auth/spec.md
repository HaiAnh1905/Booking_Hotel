# Feature Specification: RM-002 - Thay Thế Form Auth Mặc Định ABP Bằng Form Tùy Chỉnh RM-001

**Feature Branch**: `002-replace-default-auth`  
**Created**: 2026-11-11  
**Last Updated**: 2026-11-11  
**Status**: Clarified & Ready for Planning  
**Input**: 
- "thay thế form đăng nhập/đăng ký mặc định của ABP và thay bằng UI form đã làm ở RM-001 (nếu chưa có hãy thông báo)"
- "Bắt buộc mật khẩu có ít nhất 8 ký tự, ít nhất 1 chữ viết Hoa, viết thường, ít nhất 1 chữ số và ký tự đặc biệt"
- "sau khi đăng ký thành công, người dùng ấn vào nút quay lại đăng nhập để đăng nhập lần nữa mới chính thức đăng nhập vào hệ thống"

---

### Khảo sát Hiện trạng & Xác nhận Sẵn sàng (RM-001 Check)
> [!NOTE] **Xác nhận UI Form RM-001**:
> Các giao diện và logic form xác thực tùy chỉnh của RM-001 đã được hoàn thiện 100% tại `angular/src/app/auth/`:
> - `/auth/login`: Form đăng nhập Email & Google One Tap, chống Brute-force phân tầng, đếm ngược thời gian, theme Đỏ (`#990000`) / Đen (`#000000`).
> - `/auth/register`: Wizard Onboarding 3 bước (Email/Password $\rightarrow$ Họ tên $\rightarrow$ SĐT) chống nhảy bước.
> - `/auth/forgot-password` & `/auth/reset-password`: Bảo vệ bằng Cloudflare Turnstile CAPTCHA.
> - Backend REST APIs: `AuthAppService` đầy đủ 9 methods, 32/32 tests passing, Angular build 0 errors/0 warnings.

---

### Clarified Requirements & Technical Decisions (2026-11-11)

1. **Phương án ghi đè điều hướng Đăng nhập**: Xây dựng `CustomAuthService` kế thừa `AuthService` của ABP và ghi đè phương thức `navigateToLogin(queryParams)`. Cơ chế này chặn tập trung tất cả các nguồn kích hoạt đăng nhập (nút Login trên Header LeptonX Theme, Route Guards bảo vệ trang, interceptor mã lỗi 401) để tự động điều hướng sang `/auth/login` trong Angular SPA mượt mà, không reload trình duyệt.
2. **Cơ chế đồng bộ phiên người dùng (Session & Quyền hạn)**: Khi đăng nhập thành công tại `/auth/login`, token được lưu vào `OAuthStorage` (`localStorage`), đồng thời gọi trực tiếp `ConfigStateService.refreshAppState()` để cập nhật tức thời thông tin người dùng (`currentUser`), avatar và danh sách quyền hạn (`grantedPolicies`) ngay trong phiên làm việc SPA mà không cần tải lại toàn bộ trang (no full-page reload).
3. **Hành vi Đăng xuất (Logout)**: Khi người dùng bấm "Logout" trên User Menu của LeptonX, hệ thống sẽ xóa sạch JWT token trong `OAuthStorage`, reset app state, và chuyển hướng người dùng về **Trang chủ (`/`) dưới vai trò khách vãng lai**, cho phép người dùng tiếp tục xem thông tin phòng, tiện ích và đặt dịch vụ mà không bị ép về màn hình đăng nhập.
4. **Backend Redirect**: Cấu hình backend middleware / trang `Account/Login` tự động chuyển hướng HTTP 302 về frontend `http://localhost:4200/auth/login` nếu người dùng hoặc bên thứ ba truy cập trực tiếp vào backend Razor Pages.
5. **Quy chuẩn độ phức tạp của mật khẩu (Strong Password Policy)**: Bắt buộc áp dụng thống nhất ở cả Frontend và Backend cho mọi thao tác thiết lập mật khẩu (Đăng ký mới, Đặt lại mật khẩu):
   - Tối thiểu 8 ký tự (`Length >= 8`).
   - Ít nhất 1 chữ cái in hoa (`[A-Z]`).
   - Ít nhất 1 chữ cái in thường (`[a-z]`).
   - Ít nhất 1 chữ số (`[0-9]`).
   - Ít nhất 1 ký tự đặc biệt (`[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]`).
   - Biểu thức chính quy (Regex): `^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$`
6. **Trải nghiệm Checklist Trực quan Thời Gian Thực (Password Checklist UX)**: Trên form Đăng ký (`/auth/register`) và Đặt lại mật khẩu (`/auth/reset-password`), hệ thống hiển thị khối checklist gồm 5 tiêu chuẩn ngay dưới ô nhập mật khẩu. Khi người dùng nhập từng ký tự, các tiêu chuẩn đạt sẽ sáng xanh (`✓`) và các tiêu chuẩn chưa đạt sẽ mờ/đỏ (`✗`), giúp người dùng tạo mật khẩu nhanh chóng và không gây ức chế.
7. **Đồng bộ Mật khẩu Quản trị viên Khởi tạo (Admin Seed Password Update)**: Nâng cấp mật khẩu admin seed mặc định trong `BookingHotelConsts.AdminPasswordDefaultValue` lên `1q2w3E*#` (8 ký tự, có 1 hoa `E`, thường `q,w`, số `1,2,3`, ký tự đặc biệt `*` và `#`) để tài khoản hệ thống ban đầu tuân thủ 100% chính sách độ phức tạp mới.
8. **Phạm vi kiểm tra Form Đăng nhập (/auth/login)**: Form Đăng nhập **không** áp dụng Regex độ phức tạp, chỉ kiểm tra không được để trống (`Validators.required`), đảm bảo tương thích cho các tài khoản đăng nhập bình thường.
9. **Quy trình Xác nhận Hoàn tất Đăng ký & Yêu cầu Đăng nhập lại (Post-Registration Explicit Login)**: Sau khi hoàn tất đăng ký Bước 3 thành công, hệ thống **tuyệt đối KHÔNG tự động lưu Access Token vào phiên làm việc (`OAuthStorage`)** và **KHÔNG tự động cấp quyền truy cập**. Giao diện hoàn tất thành công (Bước 4) sẽ hiển thị thông báo "Đăng ký thành công" kèm nút hành động "Quay lại Đăng nhập" (`/auth/login`). Người dùng phải bấm vào nút này và tự nhập mật khẩu vừa tạo để chính thức đăng nhập vào hệ thống, đảm bảo người dùng ghi nhớ thông tin tài khoản và tuân thủ Hiến pháp Điều IX.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Chuyển Hướng Nút Đăng Nhập & Header LeptonX Vào Form Mới (Priority: P1) 🎯 MVP

Là một người dùng (Khách hàng hoặc Nhân viên), khi tôi truy cập hệ thống và bấm vào nút "Login" (hoặc "Đăng nhập") trên thanh điều hướng Header/Navbar của LeptonX Theme hay trên trang chủ, tôi muốn được chuyển hướng ngay lập tức đến giao diện đăng nhập hiện đại tại `/auth/login` (thay vì bị chuyển hướng sang trang Razor Page mặc định của OpenIddict backend).

**Why this priority**: Đây là điểm tiếp xúc đầu tiên (entry point) của người dùng. Nếu nút đăng nhập mặc định vẫn mở trang cũ của ABP, người dùng sẽ không thể tiếp cận được hệ thống bảo mật phân tầng và theme nhận diện thương hiệu đã xây dựng ở RM-001.

**Independent Test**:
- Khi chưa đăng nhập, nhấp vào nút "Login" ở góc trên bên phải thanh Header của LeptonX Theme.
- Hệ thống điều hướng trong Angular SPA tới `/auth/login` với đầy đủ giao diện Đỏ sẫm `#990000` / Đen `#000000`.
- Hoàn toàn không có hiện tượng reload toàn trang hoặc chuyển hướng sang cổng `https://localhost:44386/Account/Login`.

**Acceptance Scenarios**:
1. **Bấm nút Login trên Top Navbar**:
   - **Given** Người dùng là khách vãng lai (chưa đăng nhập) đang xem bất kỳ trang nào trên SPA (ví dụ `/`).
   - **When** Người dùng bấm nút "Login" trên thanh công cụ Header của LeptonX.
   - **Then** `CustomAuthService.navigateToLogin()` được gọi và Angular Router điều hướng tới URL `/auth/login` kèm theo tham số `returnUrl` (nếu có).
2. **Bấm nút Login trong Home Hero Banner**:
   - **Given** Người dùng đang ở màn hình trang chủ (`/`).
   - **When** Bấm vào nút `Login` trong banner chào mừng.
   - **Then** Router Angular chuyển sang `/auth/login`.

---

### User Story 2 - Đồng Bộ Phiên Đăng Nhập Với ABP Session & LeptonX (Priority: P1) 🎯 MVP

Là một người dùng đã đăng nhập thành công qua form mới (`/auth/login`), tôi muốn hệ thống ABP Angular và LeptonX Theme nhận diện đầy đủ phiên làm việc của tôi (trạng thái `isAuthenticated = true`, hiển thị tên tài khoản, avatar, menu phân quyền) và hỗ trợ đăng xuất trở về trang chủ an toàn.

**Why this priority**: Sau khi đăng nhập thành công tại form mới, JWT token cần phải được liên kết đồng bộ vào cơ chế quản lý token của ABP (`OAuthStorage` / `ConfigStateService`) để các module ABP khác (Identity, Permissions, Tenant Management) tiếp tục hoạt động bình thường.

**Independent Test**:
- Thực hiện đăng nhập tại `/auth/login` với tài khoản hợp lệ.
- Sau khi đăng nhập thành công, điều hướng về `/`.
- Thanh Header LeptonX hiển thị menu hồ sơ người dùng (User Profile Menu), tên người dùng, quyền hạn tương ứng, và nút Đăng xuất chuyển về `/` dưới vai trò khách vãng lai.

**Acceptance Scenarios**:
1. **Lưu trữ và kích hoạt Token không reload**:
   - **Given** Người dùng submit form tại `/auth/login` và API trả về `accessToken` hợp lệ.
   - **When** Component xử lý kết quả đăng nhập.
   - **Then** Token được lưu đồng bộ vào storage tiêu chuẩn của ABP (`OAuthStorage`), gọi cập nhật `ConfigStateService.refreshAppState()` để nạp thông tin quyền hạn (`currentUser`, `grantedPolicies`) mà không reload lại trình duyệt.
   - **And** Trình duyệt chuyển hướng người dùng về trang đích (`returnUrl` hoặc `/`).
2. **Đăng xuất (Logout) an toàn**:
   - **Given** Người dùng đang trong trạng thái đăng nhập.
   - **When** Người dùng bấm "Logout" trên User Menu của LeptonX.
   - **Then** Hệ thống xóa sạch access token khỏi storage, reset session state và đưa người dùng về **Trang chủ (`/`) dưới vai trò khách vãng lai**.

---

### User Story 3 - Gỡ Bỏ Module `@abp/ng.account` & Chặn Các Tuyến Đường Cũ (Priority: P2)

Là một kỹ sư hệ thống / nhà phát triển, tôi muốn loại bỏ hoàn toàn các route mặc định của ABP Account (`@abp/ng.account`) trong Angular SPA và thiết lập cơ chế chuyển hướng (Redirect) để bất kỳ truy cập nào vào các URL cũ đều được điều hướng về URL mới tương ứng:
- `/account/login` $\rightarrow$ chuyển hướng về `/auth/login`
- `/account/register` $\rightarrow$ chuyển hướng về `/auth/register`
- `/account/forgot-password` $\rightarrow$ chuyển hướng về `/auth/forgot-password`

**Why this priority**: Đảm bảo không tồn tại liên kết chết (dead link), không cho phép người dùng vô tình truy cập vào các giao diện cũ chưa qua tinh chỉnh của ABP, đồng thời tối ưu kích thước bundle của Angular bằng cách loại bỏ các component cũ không dùng.

**Independent Test**:
- Nhập trực tiếp vào thanh địa chỉ trình duyệt: `http://localhost:4200/account/login`.
- Trình duyệt tự động chuyển hướng (Redirect HTTP 301/Router navigate) sang `http://localhost:4200/auth/login`.

**Acceptance Scenarios**:
1. **Truy cập route cũ của Login**:
   - **Given** Người dùng gõ URL `/account/login`.
   - **When** Angular router xử lý định tuyến.
   - **Then** Tự động chuyển hướng ngay sang `/auth/login` giữ nguyên query parameters.
2. **Truy cập route cũ của Register**:
   - **Given** Người dùng gõ URL `/account/register`.
   - **When** Angular router xử lý định tuyến.
   - **Then** Tự động chuyển hướng ngay sang `/auth/register`.

---

### User Story 4 - Tự Động Chuyển Hướng Từ Backend Razor Pages Về Angular (Priority: P2)

Là một quản trị viên an ninh, tôi muốn nếu ai đó cố tình gõ trực tiếp URL backend `https://localhost:44386/Account/Login` hoặc các ứng dụng bên thứ ba gửi request xác thực về endpoint này, server backend sẽ tự động chuyển hướng (HTTP Redirect) về giao diện Angular SPA tại `http://localhost:4200/auth/login`.

**Why this priority**: Chặn đứng hoàn toàn việc bypass giao diện frontend để đăng nhập thẳng qua form MVC Razor Pages của backend, đảm bảo mọi luồng xác thực đều tuân thủ Brute-force phân tầng và Captcha.

**Independent Test**:
- Mở tab trình duyệt mới và truy cập thẳng vào `https://localhost:44386/Account/Login`.
- Backend trả về mã chuyển hướng (302 Redirect) đưa trình duyệt về `http://localhost:4200/auth/login`.

**Acceptance Scenarios**:
1. **Truy cập trực tiếp URL backend**:
   - **Given** Trình duyệt gửi GET request tới `https://localhost:44386/Account/Login`.
   - **When** Middleware hoặc Razor Page xử lý request.
   - **Then** Trả về HTTP Redirect 302 chuyển hướng tới `http://localhost:4200/auth/login`.

---

### User Story 5 - Quy Chuẩn Độ Phức Tạp Mật Khẩu & Kiểm Tra Tính Hợp Lệ (Priority: P1) 🎯

Là một người dùng đăng ký tài khoản mới hoặc thực hiện đặt lại mật khẩu trong hệ thống, tôi muốn hệ thống yêu cầu và hướng dẫn tôi tạo mật khẩu có độ bảo mật cao (tối thiểu 8 ký tự, có ít nhất 1 chữ in hoa, 1 chữ in thường, 1 chữ số và 1 ký tự đặc biệt), để bảo vệ an toàn cho tài khoản cá nhân, thông tin đặt phòng và ngăn chặn các cuộc tấn công dò quét/đoán mật khẩu.

**Why this priority**: Bảo mật mật khẩu là phòng tuyến cơ bản nhất của hệ thống xác thực. Việc chặn mật khẩu yếu ngay từ khâu nhập liệu phía client và cưỡng chế kiểm tra chặt chẽ ở backend đảm bảo không có tài khoản nào được tạo ra với mật khẩu dễ bị tấn công.

**Independent Test**:
- Tại màn hình Đăng ký Bước 1 (`/auth/register`) hoặc Đặt lại mật khẩu (`/auth/reset-password`), nhập lần lượt các mật khẩu không đạt chuẩn (ví dụ `< 8` ký tự, thiếu chữ hoa, thiếu chữ số, thiếu ký tự đặc biệt).
- Form phía Frontend hiển thị thông báo lỗi tức thì và vô hiệu hóa nút submit.
- Nếu cố tình gửi request trực tiếp lên API backend với mật khẩu không đạt chuẩn, API lập tức từ chối và trả về HTTP 400 Bad Request kèm thông điệp lỗi chi tiết.

**Acceptance Scenarios**:
1. **Kiểm tra độ dài mật khẩu (< 8 ký tự)**:
   - **Given** Người dùng nhập mật khẩu ít hơn 8 ký tự (ví dụ `Abc@1`).
   - **When** Người dùng rời ô nhập liệu hoặc submit form.
   - **Then** Hệ thống báo lỗi yêu cầu mật khẩu phải có ít nhất 8 ký tự.
2. **Kiểm tra chữ viết hoa (Thiếu Uppercase)**:
   - **Given** Người dùng nhập `abcdef@1` (đủ 8 ký tự nhưng không có chữ hoa).
   - **When** Hệ thống validate.
   - **Then** Báo lỗi yêu cầu có ít nhất 1 chữ cái in hoa (A-Z).
3. **Kiểm tra chữ viết thường (Thiếu Lowercase)**:
   - **Given** Người dùng nhập `ABCDEF@1` (đủ 8 ký tự nhưng không có chữ thường).
   - **When** Hệ thống validate.
   - **Then** Báo lỗi yêu cầu có ít nhất 1 chữ cái in thường (a-z).
4. **Kiểm tra chữ số (Thiếu Digit)**:
   - **Given** Người dùng nhập `Abcdefgh@` (đủ 8 ký tự, có hoa, thường, ký tự đặc biệt nhưng thiếu số).
   - **When** Hệ thống validate.
   - **Then** Báo lỗi yêu cầu có ít nhất 1 chữ số (0-9).
5. **Kiểm tra ký tự đặc biệt (Thiếu Non-alphanumeric)**:
   - **Given** Người dùng nhập `Abcdef12` (đủ 8 ký tự, có hoa, thường, số nhưng không có ký tự đặc biệt).
   - **When** Hệ thống validate.
   - **Then** Báo lỗi yêu cầu có ít nhất 1 ký tự đặc biệt (!@#$%^&*...).
6. **Mật khẩu hợp lệ**:
   - **Given** Người dùng nhập `Booking@2026` hoặc `Hotel#999` (thỏa mãn cả 5 điều kiện).
   - **When** Hệ thống validate.
   - **Then** Mật khẩu được chấp nhận, trạng thái form hợp lệ (`isValid = true`).

---

### User Story 6 - Yêu Cầu Đăng Nhập Thủ Công Sau Khi Đăng Ký Thành Công (Priority: P1) 🎯

Là một người dùng vừa hoàn tất quá trình đăng ký tài khoản thành viên trong hệ thống, tôi muốn nhìn thấy màn hình thông báo "Đăng ký thành công" kèm theo nút hành động "Quay lại Đăng nhập", để tôi có thể chủ động chuyển về form đăng nhập (`/auth/login`) và tự nhập tài khoản/mật khẩu vừa tạo để chính thức đăng nhập vào hệ thống.

**Why this priority**:
1. Đảm bảo người dùng ghi nhớ chính xác mật khẩu mình vừa tạo thông qua một lần đăng nhập thực tế ngay sau khi đăng ký.
2. Ngăn ngừa việc tự động cấp quyền truy cập khi người dùng có thể đang sử dụng thiết bị công cộng hoặc thiết bị dùng chung.
3. Tuân thủ tuyệt đối quy tắc của Hiến pháp Điều IX: Không tự động lưu phiên ngoài ý muốn, không prefill mật khẩu, người dùng chủ động đăng nhập.

**Independent Test**:
- Tại Bước 3 của `/auth/register`, nhập số điện thoại hợp lệ và bấm "Hoàn tất Đăng ký".
- Hệ thống hiển thị Màn hình Thành công (Bước 4) kèm thông điệp chúc mừng và nút "Quay lại Đăng nhập".
- Kiểm tra `OAuthStorage` (`localStorage`): Không có access token nào được lưu tự động, Header chưa hiển thị user profile menu.
- Bấm nút "Quay lại Đăng nhập": Hệ thống điều hướng về `/auth/login`. Người dùng nhập email và mật khẩu vừa tạo để đăng nhập chính thức.

**Acceptance Scenarios**:
1. **Không tự động đăng nhập khi đăng ký xong**:
   - **Given** Người dùng submit thành công Bước 3 (`registerStep3`).
   - **When** API trả về kết quả thành công.
   - **Then** Frontend KHÔNG lưu `access_token` vào `OAuthStorage` và KHÔNG gọi `refreshAppState()`.
   - **And** Trạng thái người dùng trên Header vẫn là khách vãng lai (hiển thị nút Login).
2. **Hiển thị nút "Quay lại Đăng nhập" trên Màn hình Hoàn tất**:
   - **Given** Người dùng vừa hoàn tất Bước 3.
   - **When** Giao diện chuyển sang Bước 4 (Success Screen).
   - **Then** Hiển thị icon chúc mừng `🎉`, thông báo "Đăng Ký Thành Công!", thông điệp hướng dẫn đăng nhập và nút bấm nổi bật "Quay lại Đăng nhập" (thay thế cho nút "Truy cập Trang chủ" trước đây).
3. **Điều hướng về Form Đăng nhập**:
   - **Given** Người dùng đang ở màn hình Bước 4.
   - **When** Người dùng click vào nút "Quay lại Đăng nhập".
   - **Then** Angular router điều hướng về `/auth/login`.

---

### User Story 7 - Ngăn Chặn Người Dùng Đã Đăng Nhập Truy Cập Lại Trang Đăng Nhập (Guest Protection) (Priority: P1) 🎯

Là một người dùng đã đăng nhập thành công vào hệ thống Booking Hotel,  
Tôi muốn khi tôi cố ý hoặc vô tình truy cập vào URL trang đăng nhập (`/auth/login`), hệ thống tự động đưa tôi về Trang chủ (`/`),  
Để tôi không bị phân tâm, không lặp lại form đăng nhập vô nghĩa và giữ vững tính toàn vẹn phiên làm việc hiện tại.

**Acceptance Scenarios**:
1. **Truy cập /auth/login khi đã có phiên làm việc**:
   - **Given** Người dùng đã đăng nhập thành công (có token hợp lệ trong `OAuthStorage`).
   - **When** Người dùng gõ trực tiếp URL `http://localhost:4200/auth/login` trên thanh địa chỉ hoặc click link/bookmark.
   - **Then** `guestGuard` chặn truy cập và chuyển hướng tức thời về `http://localhost:4200/`.
   - **And** Giao diện Trang chủ hiển thị ngay lập tức với phiên làm việc hiện hữu (avatar, tên người dùng trên Header).
2. **Khách vãng lai truy cập /auth/login**:
   - **Given** Khách vãng lai chưa đăng nhập (hoặc vừa bấm Đăng xuất).
   - **When** Người dùng truy cập `http://localhost:4200/auth/login`.
   - **Then** `guestGuard` cho phép truy cập bình thường và hiển thị form đăng nhập.

---

## Edge Cases

- **Token hết hạn (Token Expiration / 401 Unauthorized)**: Khi gọi API trả về lỗi 401, guard của ABP hoặc HTTP interceptor tự động kích hoạt `CustomAuthService.navigateToLogin()` điều hướng người dùng về `/auth/login` kèm theo `returnUrl` trang hiện tại thay vì mở form OpenIddict.
- **Deep Link với ReturnUrl**: Khi người dùng chưa đăng nhập cố tình truy cập vào trang bảo mật (ví dụ `/identity/users`), Guard bắt giữ và chuyển hướng về `/auth/login?returnUrl=/identity/users`. Sau khi đăng nhập thành công, hệ thống phải trả người dùng về đúng trang đó.
- **Tài khoản đang bị khóa tạm thời hoặc vô hiệu hóa**: Form mới đã có sẵn UI xử lý đếm ngược thời gian và thông báo vô hiệu hóa, đảm bảo khi bị redirect về `/auth/login` người dùng nhận được cảnh báo chính xác.
- **Khoảng trắng trong mật khẩu (Whitespace Handling)**: Mật khẩu không được chỉ chứa khoảng trắng; các khoảng trắng đầu/cuối phải được xử lý hoặc không tính vào độ dài hợp lệ.
- **Ký tự đặc biệt Unicode / Mở rộng**: Hệ thống hỗ trợ đầy đủ các ký tự đặc biệt ASCII chuẩn (`!@#$%^&*()_+-=[]{};':"|,.<>/?`) và các ký tự đặc biệt mở rộng.

---

## Requirements *(mandatory)*

### Functional Requirements
- **FR-001**: Hệ thống MUST triển khai `CustomAuthService` kế thừa `AuthService` và đăng ký thay thế trong `app.config.ts`, ghi đè `navigateToLogin()` để chuyển hướng tới `/auth/login`.
- **FR-002**: Hệ thống MUST đồng bộ JWT Access Token nhận được từ `AuthAppService` vào hệ thống lưu trữ session của ABP (`OAuthStorage`) để kích hoạt phiên làm việc toàn cục cho Angular SPA.
- **FR-003**: Hệ thống MUST nạp lại thông tin cấu hình ứng dụng (`ConfigStateService.refreshAppState()`) ngay sau khi đăng nhập thành công để cập nhật danh sách quyền (`GrantedPolicies`), thông tin định danh (`CurrentUser`) và các menu tương ứng mà không reload toàn bộ trang.
- **FR-004**: Hệ thống MUST cấu hình chuyển hướng (Redirect) các đường dẫn cũ `/account/login`, `/account/register`, `/account/forgot-password` sang các đường dẫn mới `/auth/*`.
- **FR-005**: Hệ thống MUST loại bỏ việc tải module `@abp/ng.account` trong `app.routes.ts` để tối ưu kích thước bundle và tránh xung đột định tuyến.
- **FR-006**: Backend MUST cấu hình trang hoặc middleware của `Account/Login` để tự động chuyển hướng về Frontend URL `http://localhost:4200/auth/login` khi có truy cập trực tiếp.
- **FR-007**: Hệ thống MUST xử lý thao tác Đăng xuất (Logout) bằng cách xóa sạch token trong storage, reset session state và điều hướng về Trang chủ (`/`) dưới vai trò khách vãng lai.
- **FR-008**: Hệ thống MUST áp dụng quy chuẩn độ phức tạp cho tất cả các luồng tạo mới hoặc đặt lại mật khẩu:
  - Độ dài tối thiểu 8 ký tự (`Length >= 8`).
  - Có ít nhất 1 chữ cái in hoa (`[A-Z]`).
  - Có ít nhất 1 chữ cái in thường (`[a-z]`).
  - Có ít nhất 1 chữ số (`[0-9]`).
  - Có ít nhất 1 ký tự đặc biệt (`[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]`).
- **FR-009**: Phía Frontend (Angular) MUST kiểm tra quy chuẩn độ phức tạp mật khẩu tức thời (real-time validation qua Reactive Forms Validator) trên form Đăng ký (`/auth/register`) và Đặt lại mật khẩu (`/auth/reset-password`).
- **FR-010**: Phía Backend (.NET) MUST xác thực độ phức tạp mật khẩu thông qua ASP.NET Core Identity Password Options và DataAnnotation validation (`[RegularExpression]`) trên các DTO `RegisterStep1InputDto`, `ResetPasswordInputDto`, từ chối ngay lập tức với HTTP 400 nếu dữ liệu gửi lên không đạt chuẩn.
- **FR-011**: Hệ thống MUST cung cấp thông điệp lỗi và hướng dẫn tạo mật khẩu song ngữ hoàn chỉnh trong file tài nguyên `vi.json` và `en.json`.
- **FR-012**: Phía Frontend MUST hiển thị Checklist trực quan thời gian thực (5 tiêu chí) ngay dưới ô mật khẩu trên form Đăng ký (`/auth/register`) và Đặt lại mật khẩu (`/auth/reset-password`), tự động đổi trạng thái đạt (`✓`) hoặc chưa đạt (`✗`) theo từng ký tự người dùng nhập.
- **FR-013**: Mật khẩu admin mặc định khởi tạo trong `BookingHotelConsts.AdminPasswordDefaultValue` MUST được nâng cấp thành `1q2w3E*#` (8 ký tự) để đồng bộ 100% với chính sách bảo mật mới.
- **FR-014**: Sau khi hoàn tất đăng ký tài khoản thành công tại Bước 3 (`registerStep3`), hệ thống MUST KHÔNG tự động lưu Access Token vào `OAuthStorage` và MUST KHÔNG tự động kích hoạt phiên làm việc (không gọi `refreshAppState()`).
- **FR-015**: Màn hình Hoàn tất Thành công (Bước 4) MUST hiển thị thông báo "Đăng Ký Thành Công!" kèm nút hành động "Quay lại Đăng nhập" liên kết tới route `/auth/login`.
- **FR-016**: Nút "Quay lại Đăng nhập" MUST chuyển hướng an toàn về `/auth/login`, tuân thủ Hiến pháp Điều IX (không tự động điền sẵn mật khẩu, người dùng chủ động nhập mật khẩu vừa tạo để xác thực chính thức).
- **FR-017**: Hệ thống MUST triển khai `guestGuard` (Unauthenticated Route Guard) và kiểm tra phòng vệ tại `LoginComponent.ngOnInit()`. Nếu người dùng đã đăng nhập thành công mà cố ý truy cập vào `/auth/login` (hoặc các route auth dành riêng cho khách), hệ thống MUST tự động chuyển hướng người dùng về Trang chủ (`/`).

### Non-Functional Requirements
- **NFR-001 (Constitution Compliance)**: Mọi thao tác xác thực và phân quyền tiếp tục diễn ra tại Backend thông qua `AuthAppService` đã kiểm thử ở RM-001.
- **NFR-002 (UI/UX Theme)**: Không làm mất giao diện màu sắc chuẩn Khách hàng Đỏ sẫm (`#990000`) và Đen (`#000000`) trên các trang auth.
- **NFR-003 (Performance)**: Thao tác điều hướng sang form đăng nhập phải diễn ra tức thì trong nội bộ SPA Client-side router, không gây hiện tượng chớp nháy hoặc reload trình duyệt.
- **NFR-004 (Password Security Standards)**: Tuân thủ quy chuẩn an toàn mật khẩu OWASP / NIST SP 800-63B và cấu hình bảo mật mật khẩu mặc định của ABP Framework.
