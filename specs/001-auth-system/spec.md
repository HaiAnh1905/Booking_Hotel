# Feature Specification: RM-001 - Xác Thực & Đăng Nhập/Đăng Ký Hệ Thống (Authentication System)

**Feature Branch**: `001-auth-system`  
**Created**: 2026-11-11  
**Last Updated**: 2026-11-11  
**Status**: Clarified & Ready for Task Breakdown  
**Input**: User description: "RM-001 phải có đầy đủ phần đăng nhập và đăng ký. Ở phần đăng nhập, phải có các lựa chọn đăng nhập bằng form nhập thông thường, người dùng phải sử dụng email để đăng nhập. Ngoài ra còn có chức năng đăng nhập bằng tài khoản google. Ở chức năng đăng ký thì cũng có thể đăng ký thông qua form nhập thông thường hoặc bằng tài khoản google. Sau khi nhập mail hoặc chọn tài khoản google, hệ thống đưa người dùng tới trang nhập Tên, nhập xong thì đưa tới trang nhập SDT. Hệ thống chống brute force: sai 5 lần mới khóa và tăng lũy tiến 5 phút với mỗi 5 lần sai (tối đa 30 phút); từ lần sai thứ 15 trở đi thì số lượng sai buộc giảm xuống còn 3 lần, sau lần 30 phút vẫn tiếp tục sai thì ngay lập tức vô hiệu hóa tài khoản. Thêm chức năng quên mật khẩu, yêu cầu thực hiện kiểm tra captcha."

---

### Clarified Requirements & Technical Decisions (2026-11-11)

1. **Google Auth Flow trên Angular SPA**: Sử dụng **Google Identity Services (GIS) / One Tap**. Frontend Angular lấy ID Token (JWT) từ Google SDK rồi gửi về API Backend xác thực trực tiếp qua `GoogleJsonWebSignature`, mang lại trải nghiệm liền mạch không cần reload hoặc redirect toàn trang.
2. **Quy trình Phục hồi Tài khoản bị Vô hiệu hóa**: **Chỉ Quản trị viên (Admin)** mới có quyền mở khóa và kích hoạt lại tài khoản trong phân hệ Quản lý Identity (đảm bảo kiểm soát an ninh tối đa và lưu vết `IdentitySecurityLog`). Người dùng không thể tự mở khóa khi đã bị vô hiệu hóa vĩnh viễn do brute-force.
3. **Giải pháp Captcha cho Quên mật khẩu**: Chuẩn hóa sử dụng **Cloudflare Turnstile** (hiện đại, thân thiện người dùng, bảo mật cao, không bắt chọn hình ảnh gây ức chế, hoàn toàn miễn phí).
4. **Cơ chế Email trong Môi trường Development**: Tích hợp ghi log nội dung email ra Console / lưu file HTML cục bộ trong Development để hỗ trợ lập trình viên kiểm thử luồng Quên mật khẩu ngay lập tức; sẵn sàng chuyển đổi cấu hình SMTP thật cho Staging/Production.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Đăng nhập bằng Email với Hệ thống Chống Brute-Force Lũy Tiến Phân Tầng (Priority: P1)

Là một người dùng (Khách hàng hoặc Nhân viên khách sạn), tôi muốn đăng nhập vào hệ thống bằng địa chỉ Email và Mật khẩu, đồng thời hệ thống phải tự động bảo vệ tài khoản trước các cuộc tấn công dò mật khẩu (Brute-Force) bằng cơ chế phân tầng thông minh:
- Trước mốc 15 lần sai: Cho phép sai tối đa 5 lần mỗi chu kỳ trước khi tạm khóa (thời gian khóa tăng lũy tiến 5 phút mỗi chu kỳ).
- Từ lần sai thứ 15 trở đi: Thắt chặt an ninh, số lượng sai cho phép buộc giảm xuống chỉ còn 3 lần mỗi chu kỳ (tiếp tục tăng thời gian khóa thêm 5 phút, tối đa 30 phút).
- Khi đã ở mức phạt 30 phút mà tiếp tục sai 3 lần ở chu kỳ sau: Ngay lập tức vô hiệu hóa tài khoản.

**Why this priority**: Cánh cổng xác thực chính và cơ chế an ninh mạng cốt lõi của hệ thống PMS khách sạn để bảo vệ dữ liệu tài khoản, vừa tránh khóa nhầm người dùng hợp lệ ở những lần đầu, vừa siết chặt phòng thủ tối đa khi phát hiện dấu hiệu tấn công dai dẳng.

**Independent Test**: Có thể kiểm thử độc lập:
- Giai đoạn 1 (Lần 1 - 15): Sai 5 lần đầu khóa 5m $\rightarrow$ sai tiếp 5 lần khóa 10m $\rightarrow$ sai tiếp 5 lần khóa 15m.
- Giai đoạn 2 (Từ lần 15 trở đi): Sai tiếp 3 lần khóa 20m $\rightarrow$ sai tiếp 3 lần khóa 25m $\rightarrow$ sai tiếp 3 lần khóa 30m.
- Sau khi hết đợt 30m: Sai tiếp 3 lần $\rightarrow$ tài khoản lập tức bị vô hiệu hóa (`IsActive = false`).

**Acceptance Scenarios**:
1. **Đăng nhập thành công**:
   - **Given** Người dùng có tài khoản đang kích hoạt (`IsActive = true`) và không trong thời gian bị khóa.
   - **When** Nhập đúng Email và Mật khẩu, bấm "Đăng nhập".
   - **Then** Hệ thống cấp JWT token / phiên đăng nhập, đặt lại toàn bộ bộ đếm (`FailedLoginCount = 0`, `FailedAttemptsInCurrentCycle = 0`, `CurrentLockoutMinutes = 0`, `LockoutEnd = null`) và chuyển hướng đến trang chủ/portal tương ứng.
2. **Giai đoạn 1 (Lần sai 1 đến 15) - Cho phép 5 lần sai mỗi chu kỳ**:
   - **Given** Tổng số lần sai từ trước đến nay nhỏ hơn 15 (`TotalFailedAttempts < 15`).
   - **When** Người dùng nhập sai mật khẩu lần thứ $n$ ($n \in [1..4]$) trong chu kỳ hiện tại.
   - **Then** Hệ thống không khóa tài khoản, hiển thị thông báo: *"Mật khẩu không chính xác. Bạn còn $(5 - n)$ lần thử trước khi tài khoản bị tạm khóa."*
   - **When** Người dùng nhập sai mật khẩu ở lần thứ 5 của chu kỳ:
     - Lần sai thứ 5: Khóa tạm thời **5 phút** (`LockoutEnd = UtcNow + 5m`).
     - Lần sai thứ 10 (hết đợt 5m $\rightarrow$ sai tiếp 5 lần): Khóa **10 phút** (`LockoutEnd = UtcNow + 10m`).
     - Lần sai thứ 15 (hết đợt 10m $\rightarrow$ sai tiếp 5 lần): Khóa **15 phút** (`LockoutEnd = UtcNow + 15m`).
3. **Giai đoạn 2 (Từ lần sai thứ 15 trở đi) - Giảm ngưỡng xuống 3 lần sai mỗi chu kỳ**:
   - **Given** Tài khoản đã có từ 15 lần sai trở lên (`TotalFailedAttempts >= 15`) và vừa hết hạn đợt khóa trước đó.
   - **When** Người dùng nhập sai mật khẩu lần thứ $m$ ($m \in [1..2]$) trong chu kỳ mới này.
   - **Then** Hệ thống phát cảnh báo siết chặt an ninh: *"Cảnh báo bảo mật: Tài khoản có dấu hiệu bị dò quét. Mật khẩu không chính xác. Bạn chỉ còn $(3 - m)$ lần thử."*
   - **When** Người dùng nhập sai đủ **3 lần** trong chu kỳ:
     - Lần sai thứ 18 (hết đợt 15m $\rightarrow$ sai tiếp 3 lần): Khóa **20 phút** (`LockoutEnd = UtcNow + 20m`).
     - Lần sai thứ 21 (hết đợt 20m $\rightarrow$ sai tiếp 3 lần): Khóa **25 phút** (`LockoutEnd = UtcNow + 25m`).
     - Lần sai thứ 24 (hết đợt 25m $\rightarrow$ sai tiếp 3 lần): Khóa tối đa **30 phút** (`LockoutEnd = UtcNow + 30m`, `CurrentLockoutMinutes = 30`).
4. **Vô hiệu hóa tài khoản khi tiếp tục sai sau mốc 30 phút**:
   - **Given** Tài khoản đã chịu mức phạt tối đa 30 phút và thời gian khóa tạm thời đã kết thúc.
   - **When** Người dùng tiếp tục nhập sai mật khẩu đủ **3 lần liên tiếp** trong chu kỳ tiếp theo này (lần sai thứ 27).
   - **Then** Hệ thống **ngay lập tức vô hiệu hóa tài khoản** (`IsActive = false`), đặt `DeactivatedReason = "BruteForceLimitExceeded"`, hủy mọi phiên đăng nhập trên tất cả thiết bị, và hiển thị thông báo: *"Tài khoản của bạn đã bị vô hiệu hóa vĩnh viễn do đăng nhập sai vượt quá giới hạn an toàn. Vui lòng liên hệ Quản trị viên để được hỗ trợ mở khóa."*

---

### User Story 2 - Đăng ký Tài khoản Khách hàng theo Quy trình 3 Bước (Priority: P1)

Là một khách hàng mới, tôi muốn đăng ký tài khoản thông qua biểu mẫu 3 bước trực quan (Email & Mật khẩu $\rightarrow$ Họ tên $\rightarrow$ Số điện thoại) để tạo hồ sơ lưu trú hoàn chỉnh.

**Why this priority**: Cung cấp luồng gia nhập (onboarding) tối ưu, thu thập đủ thông tin cốt lõi (Họ tên, SĐT) bắt buộc cho nghiệp vụ đặt phòng và liên hệ xác nhận tại khách sạn mà không làm người dùng bị quá tải thông tin trên một form duy nhất.

**Independent Test**: Hoàn tất Bước 1 $\rightarrow$ Bước 2 $\rightarrow$ Bước 3, tài khoản được tạo mới trong CSDL với đầy đủ Email, Name, PhoneNumber và tự động đăng nhập vào hệ thống.

**Acceptance Scenarios**:
1. **Bước 1 (Email & Mật khẩu)**:
   - **Given** Khách hàng ở màn hình Đăng ký.
   - **When** Khách hàng nhập Email hợp lệ, Mật khẩu (đáp ứng tiêu chuẩn bảo mật) và Xác nhận mật khẩu, sau đó bấm "Tiếp tục".
   - **Then** Hệ thống kiểm tra Email chưa tồn tại trong CSDL, tạo phiên Onboarding tạm thời (`OnboardingToken` hạn 15 phút) và chuyển hướng sang màn hình Bước 2 (Nhập Tên).
2. **Bước 2 (Họ và Tên)**:
   - **Given** Khách hàng có `OnboardingToken` hợp lệ từ Bước 1.
   - **When** Khách hàng nhập Họ và Tên đầy đủ (tối thiểu 2 ký tự) và bấm "Tiếp tục".
   - **Then** Hệ thống cập nhật Họ tên vào phiên Onboarding và chuyển hướng sang Bước 3 (Nhập SĐT).
3. **Bước 3 (Số điện thoại) & Kích hoạt**:
   - **Given** Khách hàng đã hoàn thành Bước 2.
   - **When** Khách hàng nhập Số điện thoại hợp lệ (định dạng số di động Việt Nam hoặc quốc tế chuẩn E.164) và bấm "Hoàn tất đăng ký".
   - **Then** Hệ thống tạo bản ghi `AppUser` hoàn chỉnh trong CSDL với `IsActive = true`, gán vai trò Khách hàng (`Customer`), tự động đăng nhập và điều hướng vào trang chủ Booking.

---

### User Story 3 - Đăng nhập & Đăng ký một chạm bằng Google (Priority: P2)

Là một người dùng, tôi muốn sử dụng tài khoản Google để đăng nhập nhanh hoặc đăng ký tài khoản mới mà không cần ghi nhớ thêm một mật khẩu riêng.

**Why this priority**: Tăng tỷ lệ chuyển đổi đặt phòng của khách hàng lên hơn 40%, giảm thiểu tỷ lệ quên mật khẩu và tăng tính tiện lợi cho khách quốc tế và khách công tác.

**Independent Test**: Nhấp chọn "Đăng nhập với Google" / "Đăng ký với Google", hoàn tất xác thực Google GIS / One Tap, hệ thống xử lý đăng nhập hoặc đưa vào luồng thu thập Họ tên và SĐT.

**Acceptance Scenarios**:
1. **Đăng nhập bằng Google cho tài khoản đã liên kết**:
   - **Given** Người dùng đã có tài khoản và đã liên kết Google Account (hoặc trùng Email Google).
   - **When** Nhấn nút "Đăng nhập bằng Google" và chọn tài khoản tương ứng qua Google GIS One Tap.
   - **Then** Hệ thống xác thực Google ID Token, kiểm tra tài khoản đang hoạt động (`IsActive = true`) và cấp quyền truy cập ngay lập tức.
2. **Đăng ký mới thông qua Google (Onboarding)**:
   - **Given** Người dùng chưa từng có tài khoản trên hệ thống.
   - **When** Nhấn nút "Đăng ký bằng Google" và hoàn tất ủy quyền qua Google GIS SDK.
   - **Then** Hệ thống nhận Email và Tên hiển thị từ Google, khởi tạo phiên Onboarding, tự động điền Tên ở Bước 2 cho người dùng xác nhận/chỉnh sửa $\rightarrow$ Bấm "Tiếp tục" đưa đến Bước 3 để thu thập Số điện thoại bắt buộc $\rightarrow$ Tạo tài khoản thành công.

---

### User Story 4 - Quên Mật Khẩu và Đặt Lại Mật Khẩu với Xác Thực Cloudflare Turnstile (Priority: P2)

Là một người dùng bị quên mật khẩu, tôi muốn gửi yêu cầu đặt lại mật khẩu qua email của mình và hoàn thành bước kiểm tra Captcha bảo mật (Cloudflare Turnstile) để chứng minh tôi là người dùng thực sự (chống bot spam gửi email hàng loạt), sau đó nhận email chứa liên kết an toàn để đổi mật khẩu mới.

**Why this priority**: Bảo vệ cổng email của khách sạn khỏi các cuộc tấn công DDoS/Spam Email (email bombing) bằng bot tự động, đồng thời cho phép người dùng tự khôi phục mật khẩu mà không cần liên hệ tổng đài.

**Independent Test**:
- Không giải Turnstile hoặc Token sai: Hệ thống chặn ngay lập tức, không gửi email.
- Giải Turnstile thành công: Nhận được email chứa Reset Token trong vòng 1 phút; nhấp vào link đổi mật khẩu thành công và đăng nhập lại bằng mật khẩu mới.

**Acceptance Scenarios**:
1. **Yêu cầu Đặt lại Mật khẩu với Turnstile Captcha hợp lệ**:
   - **Given** Người dùng ở màn hình "Quên mật khẩu".
   - **When** Người dùng nhập địa chỉ Email, xác thực thành công Turnstile widget (sinh `CaptchaToken` hợp lệ) và bấm "Gửi yêu cầu".
   - **Then** Backend gửi `CaptchaToken` sang Cloudflare API để kiểm tra. Nếu hợp lệ:
     - Nếu email tồn tại trong hệ thống: Sinh `PasswordResetToken` an toàn (thời hạn 15 phút), gửi email chứa link đặt lại mật khẩu (hoặc ghi log console trong môi trường Dev).
     - Hiển thị thông báo an toàn (không làm lộ việc email có tồn tại hay không): *"Nếu địa chỉ email của bạn khớp với tài khoản trong hệ thống, chúng tôi đã gửi hướng dẫn đặt lại mật khẩu đến hộp thư của bạn."*
2. **Yêu cầu thất bại do Captcha không hợp lệ hoặc bị bỏ qua**:
   - **Given** Người dùng ở màn hình "Quên mật khẩu".
   - **When** Người dùng bấm gửi khi chưa xác thực Turnstile, hoặc `CaptchaToken` hết hạn / bị làm giả.
   - **Then** Backend từ chối xử lý, không sinh token và không gửi email; trả về thông báo lỗi: *"Xác minh bảo mật Captcha không thành công. Vui lòng thử lại."*
3. **Thực hiện đổi mật khẩu mới qua liên kết trong Email**:
   - **Given** Người dùng nhận được email và nhấp vào link chứa `PasswordResetToken` trong vòng 15 phút.
   - **When** Giao diện "Đặt lại mật khẩu" hiển thị, người dùng nhập Mật khẩu mới (đáp ứng tiêu chuẩn độ phức tạp) và Xác nhận mật khẩu mới, bấm "Cập nhật".
   - **Then** Backend xác thực token hợp lệ, cập nhật mật khẩu mới, hủy token đã dùng, đặt lại toàn bộ bộ đếm đăng nhập sai về 0, vô hiệu hóa các phiên đăng nhập cũ và điều hướng người dùng về màn hình Đăng nhập kèm thông báo thành công.

---

### User Story 5 - Quản trị viên Mở khóa và Phục hồi Tài khoản (Priority: P3)

Là một Quản trị viên hệ thống (Admin), tôi muốn xem danh sách các tài khoản đang bị khóa tạm thời hoặc bị vô hiệu hóa do vi phạm brute-force, và là người DUY NHẤT có thẩm quyền đặt lại bộ đếm hoặc mở khóa tài khoản sau khi đã xác minh người dùng chính chủ.

**Why this priority**: Đảm bảo an ninh tuyệt đối cho hệ thống, ngăn chặn kẻ tấn công tự mở khóa qua email giả mạo hoặc spam sau khi đã bị vô hiệu hóa ở cấp độ cao nhất.

**Independent Test**: Đăng nhập bằng tài khoản Admin, vào phân hệ quản trị Identity, chọn tài khoản bị vô hiệu hóa, nhấn "Mở khóa & Kích hoạt lại", sau đó người dùng có thể đăng nhập bình thường.

**Acceptance Scenarios**:
1. **Admin mở khóa tài khoản**:
   - **Given** Tài khoản đang ở trạng thái `IsActive = false` do brute-force.
   - **When** Admin chọn "Kích hoạt lại tài khoản" (Reactivate & Reset Lockout).
   - **Then** Hệ thống cập nhật `IsActive = true`, đặt `LockoutEnd = null`, `FailedLoginCount = 0`, `FailedAttemptsInCurrentCycle = 0`, `CurrentLockoutMinutes = 0` và ghi lại Audit Log với định danh của Admin thực hiện.

---

### Edge Cases & Xử Lý Ngoại Lệ

1. **Email đã được đăng ký**: Khi người dùng nhập email ở Bước 1 của Đăng ký thường hoặc chọn tài khoản Google đã tồn tại, hệ thống báo lỗi rõ ràng: *"Email này đã được sử dụng. Vui lòng đăng nhập hoặc dùng chức năng Quên mật khẩu."*
2. **Bỏ qua bước trong luồng Đăng ký (Step Skipping)**: Nếu người dùng cố tình nhập URL trực tiếp vào trang Bước 2 hoặc Bước 3 mà không có Onboarding Token hợp lệ từ Bước 1, hệ thống từ chối và tự động điều hướng về lại Bước 1.
3. **Mạng gián đoạn giữa các bước đăng ký**: `OnboardingToken` có thời gian sống (TTL) 15 phút. Nếu người dùng quay lại trước 15 phút với token còn hạn, họ có thể tiếp tục bước dang dở; nếu quá hạn, họ phải bắt đầu lại từ Bước 1 để đảm bảo bảo mật.
4. **Cố tình gửi request đăng nhập khi tài khoản đang trong thời gian bị khóa**: Hệ thống trả về mã lỗi HTTP `423 Locked` kèm số giây còn lại, tuyệt đối không tính thêm số lần sai và không kích hoạt vô hiệu hóa cho đến khi hết hạn thời gian khóa hiện tại.
5. **Định dạng số điện thoại**: Kiểm tra tính hợp lệ bằng Regex chuẩn (E.164 hoặc số di động Việt Nam 10 chữ số bắt đầu bằng `03, 05, 07, 08, 09`).
6. **Token Quên mật khẩu hết hạn hoặc đã được sử dụng**: Khi người dùng nhấp vào link reset password quá thời hạn 15 phút hoặc link đã từng dùng một lần trước đó, hệ thống báo lỗi: *"Liên kết đặt lại mật khẩu đã hết hạn hoặc không còn hiệu lực. Vui lòng gửi yêu cầu mới."*
7. **Tấn công Bot vào trang Quên mật khẩu**: Toàn bộ lượt gọi API quên mật khẩu không có `CaptchaToken` hợp lệ đều bị chặn ngay tại tầng API Gateway / Middleware, bảo vệ máy chủ gửi mail (SMTP) khỏi bị cạn kiệt tài nguyên hoặc rơi vào blacklist spam.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Hệ thống BẮT BUỘC hỗ trợ đăng nhập bằng **Email** kết hợp Mật khẩu (không sử dụng username thô cho màn hình khách hàng).
- **FR-002**: Hệ thống BẮT BUỘC hỗ trợ Đăng nhập và Đăng ký liên kết qua **Google Account** (Google Identity Services / One Tap) xác thực ID Token trực tiếp tại Backend.
- **FR-003**: Quy trình Đăng ký BẮT BUỘC chia thành **3 bước tuần tự**:
  - *Bước 1*: Nhập Email & Mật khẩu (hoặc xác thực qua Google).
  - *Bước 2*: Nhập Họ và Tên (nếu từ Google thì điền sẵn để người dùng xác nhận/sửa).
  - *Bước 3*: Nhập Số điện thoại liên hệ (bắt buộc để phục vụ lưu trú).
- **FR-004**: Hệ thống BẮT BUỘC tích hợp thuật toán **Brute-Force Lockout lũy tiến phân tầng**:
  - *Giai đoạn 1 (Lần sai 1 đến 15)*: Ngưỡng kích hoạt khóa là **5 lần sai** trong mỗi chu kỳ. Thời gian khóa tăng lũy tiến:
    - 5 lần sai đầu $\rightarrow$ Tạm khóa **5 phút**.
    - 10 lần sai $\rightarrow$ Tạm khóa **10 phút**.
    - 15 lần sai $\rightarrow$ Tạm khóa **15 phút**.
  - *Giai đoạn 2 (Từ lần sai thứ 15 trở đi)*: Số lượng sai cho phép BẮT BUỘC **giảm xuống còn 3 lần** trong mỗi chu kỳ tiếp theo, thời gian khóa tiếp tục tăng lũy tiến thêm +5 phút:
    - Sai tiếp 3 lần (lần 18) $\rightarrow$ Tạm khóa **20 phút**.
    - Sai tiếp 3 lần (lần 21) $\rightarrow$ Tạm khóa **25 phút**.
    - Sai tiếp 3 lần (lần 24) $\rightarrow$ Tạm khóa tối đa **30 phút** (`CurrentLockoutMinutes = 30`).
- **FR-005**: Nếu tài khoản đã chịu mức phạt tối đa 30 phút mà ở chu kỳ sau khi mở khóa lại tiếp tục nhập sai đủ **3 lần nữa** (lần sai thứ 27), hệ thống BẮT BUỘC **ngay lập tức vô hiệu hóa tài khoản (`IsActive = false`)**. Chỉ Quản trị viên mới có thẩm quyền mở khóa.
- **FR-006 (Hiến pháp - Backend-Authoritative)**: Toàn bộ việc kiểm tra số lần sai, tính toán thời gian khóa (`LockoutEnd`), kiểm tra trạng thái vô hiệu hóa, xác thực Captcha và kiểm tra mật khẩu BẮT BUỘC phải thực hiện 100% tại Backend. Frontend chỉ nhận thông báo và đếm ngược hiển thị.
- **FR-007 (Hiến pháp - UI Concept Khách hàng)**: Giao diện Đăng nhập / Đăng ký / Quên mật khẩu dành cho Portal Khách hàng BẮT BUỘC tuân thủ bảng màu: Màu chủ đạo **Đỏ sẫm (`#990000`)** phối cùng **Đen (`#000000`)** trên nền sáng ấm sang trọng.
- **FR-008 (Hiến pháp - i18n Song ngữ)**: Tất cả thông báo bảo mật, nhãn form, thông báo lỗi lockout, thông báo Captcha BẮT BUỘC phải cấu hình đầy đủ trong tài nguyên bản địa hóa `vi.json` và `en.json`.
- **FR-009**: Hệ thống BẮT BUỘC ghi nhận Security Audit Log cho mỗi lần đăng nhập thất bại và sự kiện khóa/vô hiệu hóa tài khoản (ghi rõ IP nguồn, thời điểm, Email, mức phạt).
- **FR-010**: Hệ thống BẮT BUỘC sinh `OnboardingToken` mã hóa (JWT có ký số, hạn 15 phút) để quản lý luồng đăng ký 3 bước, ngăn chặn giả mạo dữ liệu giữa các bước.
- **FR-011**: Chức năng **Quên mật khẩu** BẮT BUỘC tích hợp kiểm tra **Cloudflare Turnstile** tại Backend trước khi thực hiện quy trình sinh mã và gửi email.
- **FR-012**: Token đặt lại mật khẩu (`PasswordResetToken`) phải là chuỗi ngẫu nhiên bảo mật cao (cryptographically secure), chỉ sử dụng một lần (single-use) và có thời hạn hiệu lực tối đa **15 phút**.
- **FR-013**: Trong môi trường Development, hệ thống hỗ trợ ghi log nội dung email ra Console / File HTML để lập trình viên kiểm thử mà không bắt buộc cấu hình SMTP thật.

---

### Key Entities & Data Schema

- **AppUser (mở rộng từ ABP IdentityUser)**:
  - `FailedAttemptsInCurrentCycle` (*int*): Số lần đăng nhập sai tích lũy trong chu kỳ hiện tại (0 đến 5 trước mốc 15 lần sai; 0 đến 3 từ mốc 15 lần sai trở đi).
  - `TotalFailedAttempts` (*int*): Tổng số lần sai liên tiếp từ trước đến nay.
  - `CurrentLockoutMinutes` (*int*): Mức phạt khóa hiện tại (0, 5, 10, 15, 20, 25, 30).
  - `LockoutEnd` (*DateTimeOffset?*): Thời điểm kết thúc khóa tạm thời.
  - `IsActive` (*bool*): Trạng thái hoạt động (`false` khi bị vô hiệu hóa do brute-force).
  - `DeactivatedReason` (*string?*): Lý do vô hiệu hóa (ví dụ: `BruteForceLimitExceeded`).
  - `GoogleId` (*string?*): Mã định danh Google nếu đã liên kết.
- **UserRegistrationState (Cache / In-memory Session)**:
  - `RegistrationId` (*Guid / Token*): Khóa phiên Onboarding.
  - `Email` (*string*): Email đã xác minh ở Bước 1.
  - `PasswordHash` (*string?*): Mật khẩu tạm thời nếu đăng ký thường.
  - `GoogleProfileData` (*json?*): Dữ liệu từ Google nếu đăng ký qua Google.
  - `FullName` (*string?*): Họ tên thu thập ở Bước 2.
  - `PhoneNumber` (*string?*): Số điện thoại thu thập ở Bước 3.
  - `CurrentStep` (*enum: Step1, Step2, Step3*): Trạng thái bước hiện tại.
  - `ExpiresAt` (*DateTime*): Hạn phiên đăng ký (15 phút).
- **PasswordResetRecord (CSDL hoặc ABP Identity Token Provider)**:
  - `UserId` (*Guid*): Định danh người dùng yêu cầu.
  - `ResetTokenHash` (*string*): Mã băm của token đặt lại mật khẩu.
  - `ExpiresAt` (*DateTime*): Thời điểm hết hạn (15 phút).
  - `IsUsed` (*bool*): Cờ đánh dấu đã sử dụng.
  - `RequestedIp` (*string*): IP gửi yêu cầu.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Người dùng hoàn thành quá trình đăng nhập hợp lệ trong thời gian **dưới 2 giây**.
- **SC-002**: Người dùng mới hoàn thành toàn bộ 3 bước đăng ký và vào được hệ thống trong **dưới 60 giây**.
- **SC-003**: 100% các cuộc tấn công brute force giả lập bị chặn chính xác theo cơ chế phân tầng: ngưỡng 5 lần sai ở các đợt khóa đầu (5m, 10m, 15m) và tự động siết chặt xuống ngưỡng 3 lần sai từ lần thứ 15 trở đi (20m, 25m, 30m).
- **SC-004**: 100% trường hợp tài khoản đã ở mức khóa 30 phút mà tiếp tục sai 3 lần ở chu kỳ sau sẽ bị chuyển trạng thái `IsActive = false` (vô hiệu hóa ngay lập tức), và chỉ Admin mới có quyền phục hồi.
- **SC-005**: 100% các yêu cầu Quên mật khẩu không có token Turnstile hợp lệ đều bị Backend từ chối và chặn đứng hoàn toàn việc gửi email.
- **SC-006**: Tỷ lệ đăng ký / đăng nhập thành công qua Google GIS One Tap đạt **$\ge 98\%$** trong điều kiện mạng bình thường.
- **SC-007**: 100% các chuỗi thông báo, cảnh báo lỗi lockout, số lần thử còn lại và Captcha hiển thị chính xác theo ngôn ngữ người dùng lựa chọn (Tiếng Việt và Tiếng Anh).

---

## Assumptions & Technical Dependencies

1. **ABP Framework Identity & OpenIddict**: Tận dụng cơ chế `IdentitySecurityLog`, `SignInManager` và `UserManager` của ASP.NET Core Identity & ABP Framework v10.7, mở rộng cấu hình lockout tùy biến theo chu kỳ 5 lần và 3 lần sai.
2. **Google Identity Services (GIS)**: Cấu hình `ClientId` của Google Console trong `appsettings.json` tại backend và cấu hình script GIS trên frontend Angular.
3. **Dịch vụ Cloudflare Turnstile**: Cấu hình `Turnstile:SecretKey` (phía backend) và `Turnstile:SiteKey` (phía frontend Angular).
4. **Dịch vụ Email (IEmailSender)**: Tích hợp thông qua `IEmailSender` chuẩn của ABP Framework; hỗ trợ mock/console log cho Dev và SMTP cho Production.
