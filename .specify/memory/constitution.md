# Hiến Pháp Dự Án BookingHotel

## Các Nguyên Tắc Cốt Lõi (Core Principles)

### I. Kiến Trúc Phân Lớp Domain-Driven Design (DDD) & ABP Framework
- Tuân thủ nghiêm ngặt sự phân tách trách nhiệm giữa các tầng theo chuẩn ABP Framework:
  - **Domain / Domain.Shared**: Chứa Entities, Aggregate Roots, Domain Services, Enums, Hằng số và Localization. Nghiệp vụ cốt lõi nằm ở đây. Tầng Domain tuyệt đối không phụ thuộc vào tầng Application hay Infrastructure.
  - **Application.Contracts**: Chứa interfaces (`I*AppService`), DTOs, định nghĩa phân quyền (Permissions) và quy tắc kiểm tra tính hợp lệ dữ liệu (Validation).
  - **Application**: Triển khai các AppService, điều phối nghiệp vụ, ánh xạ dữ liệu (ObjectMapper / AutoMapper). Không chứa mã xử lý giao diện hay truy vấn CSDL trực tiếp.
  - **EntityFrameworkCore**: Cấu hình DbContext, Entity mapping và triển khai Repository cụ thể.
  - **HttpApi / HttpApi.Host**: Bộ điều khiển API (API Controllers), cấu hình routing, Swagger và middleware.

### II. Thiết Kế Theo Hợp Đồng (Contract-First) & Bảo Mật Mặc Định
- Mọi API endpoint phải có DTO đầu vào/đầu ra rõ ràng kèm các thuộc tính validation dữ liệu (`[Required]`, `[StringLength]`, `[Range]`,...).
- Bắt buộc kiểm tra quyền: Tất cả AppServices hoặc Actions phải khai báo quyền hạn thông qua thuộc tính phân quyền của ABP (ví dụ: `[Authorize(BookingHotelPermissions.Rooms.Create)]`).
- Tận dụng triệt để các cơ chế có sẵn của ABP: Soft Delete, Audit Logging (kế thừa từ `FullAuditedAggregateRoot` hoặc `AuditedEntity`).

### III. Quản Lý Cơ Sở Dữ Liệu Qua EF Core Migrations
- Tuyệt đối không can thiệp hoặc thay đổi cấu trúc bảng CSDL bằng các câu lệnh thủ công trực tiếp trên DBMS.
- Mọi thay đổi lược đồ dữ liệu (schema) phải được tạo qua EF Core Code-First Migrations và áp dụng vào DB thông qua project `BookingHotel.DbMigrator`.
- Cấu hình quan hệ bảng (Fluent API) phải đặt đúng vị trí tại `BookingHotelDbContextModelCreatingExtensions`.

### IV. Chuẩn Hóa Frontend (Angular) & Phân Tách Giao Diện
- Mã nguồn Angular tuân thủ chế độ Strict TypeScript và chia module rõ ràng.
- Toàn bộ việc gọi API Backend phải thông qua các Service/Proxy được định kiểu mạnh (strong-typed) khớp với DTO từ Backend.
- Áp dụng mô hình phản ứng (RxJS / Angular Signals) và Reactive Forms có validate phía client đồng bộ với quy tắc validation của Backend.
- **Frontend thuần túy hiển thị (Thin Presentation Layer)**: Frontend chỉ đảm nhận thu thập tương tác từ người dùng và trình bày dữ liệu; tuyệt đối không tự tính toán tiền tệ hay tự chuyển đổi trạng thái dữ liệu để lưu vào hệ thống.

### V. Đa Ngôn Ngữ (i18n) & Tính Toàn Vẹn Nghiệp Vụ
- Tuyệt đối không hardcode chuỗi hiển thị, nhãn, thông báo lỗi trong code C# hay HTML. Tất cả phải định nghĩa thông qua file localization (`BookingHotelResource` ở backend và `vi.json` / `en.json` ở frontend).
- Các quy tắc nghiệp vụ cốt lõi (ví dụ: logic tính tiền phòng, chuyển đổi trạng thái phòng, kiểm tra phòng trống) phải được viết unit test tại `BookingHotel.Domain.Tests`.

### VI. Toàn Quyền Thay Đổi Dữ Liệu & Tính Toán Nghiệp Vụ Thuộc Về Backend (Backend-Authoritative)
- **Mọi thao tác thay đổi dữ liệu (Create, Update, Delete) và chuyển đổi trạng thái (State Transition)**: Bắt buộc phải được kiểm soát và thực thi tại Backend (qua Domain Services / Aggregate Roots / AppServices). Frontend không được phép tự quyết định trạng thái dữ liệu.
- **Tính toán nghiệp vụ quan trọng (Tài chính, Giá cả, Tổng tiền)**:
  - Mọi phép tính trọng yếu như: **tính tổng tiền đặt phòng, đơn giá theo đêm/hạng phòng, phụ thu nhận phòng sớm/trả phòng trễ, cước dịch vụ minibar, giảm giá/khuyến mại, thuế VAT, tiền phạt hủy và thanh toán hóa đơn (Folio)** bắt buộc phải do Backend thực hiện dựa trên quy tắc nghiệp vụ và bảng giá lưu trong CSDL.
  - **Nguyên tắc không tin cậy dữ liệu tính từ Client (Zero Trust on Client-Calculated Data)**: Client chỉ gửi các tham số đầu vào (ví dụ: `RoomId`, `CheckInDate`, `CheckOutDate`, `ServiceId`, `Quantity`). Backend tuyệt đối không chấp nhận các trường tài chính tự tính từ Client (như `TotalPrice`, `UnitPrice`) để lưu thẳng vào CSDL.
  - **Tách biệt giữa hiển thị tạm tính và lưu trữ chính thức**: Nếu Frontend hiển thị tạm tính để tối ưu trải nghiệm (UX), giá trị đó chỉ mang tính tham khảo hoặc phải lấy từ API ước tính (`EstimatePriceAsync`) của Backend. Giá trị chốt tạo đơn và ghi nhận doanh thu luôn được Backend tính toán lại một cách độc lập và bảo mật.

### VII. Quy Chuẩn Concept Thiết Kế & Hệ Màu Theo Phân Hệ (Role-Based UI/UX Concepts)
Mọi giao diện Angular và stylesheet (SCSS) khi thiết kế hoặc sinh mã nguồn phải tuân thủ nghiêm ngặt bảng màu và concept nhận diện đặc thù cho từng đối tượng sử dụng:
1. **Trang Đặt phòng Khách hàng & Khách vãng lai (Customer & Guest Booking Portal)**:
   - **Màu chủ đạo (Primary)**: **Đỏ mận/đỏ sẫm (`#990000`)** kết hợp với **Đen (`#000000` / Dark neutral)**.
   - **Định hướng phong cách**: Sang trọng, ấn tượng, tạo sự tin cậy và ấm cúng cho trải nghiệm tìm kiếm, chọn phòng và đặt dịch vụ.
2. **Phân hệ Quản lý (Management / Admin Portal)**:
   - **Nền chính (Background)**: **Trắng (`#FFFFFF`)**.
   - **Thanh điều hướng bên (Sidebar)**: **Đen (`#000000`)**.
   - **Điểm nhấn thao tác & Highlight (Action / Focus)**: **Xanh Cyan (`#00FFFF`)** (dùng cho các nút hành động, trạng thái active, hover, biểu đồ chỉ số chính).
   - **Định hướng phong cách**: Chuyên nghiệp, tương phản cao, tối ưu hiển thị số liệu Dashboard, báo cáo và quản trị hệ thống.
3. **Phân hệ Lễ tân (Receptionist / Front Desk Portal)**:
   - **Màu nhận diện & thao tác chính (Action / Highlight)**: **Xanh lá tươi nhạt (`#99FF99`)**.
   - **Màu phối hợp & cấu trúc phụ trợ (Secondary / Neutral)**: **Xám (`#808080`)**.
   - **Định hướng phong cách**: Trực quan, dịu mắt, giảm căng thẳng thị giác cho nhân viên trong ca làm việc; làm nổi bật trạng thái sơ đồ phòng (Room Rack Matrix), hỗ trợ thao tác Check-in / Check-out / Gán dịch vụ nhanh chóng.

### VIII. Quy Chuẩn Tên Thương Hiệu & Hiển Thị (Brand Naming & Typography Standard)
- **Tên Logo & Thương Hiệu Mặc Định**:
  - Tên logo mặc định của toàn bộ hệ thống là **`Booking Hotel`**, chữ luôn luôn phải viết cách rời (`Booking Hotel`), tuyệt đối không được viết dính liền `BookingHotel`.
  - Trên toàn bộ giao diện người dùng (UI, Header, Navbar, Brand Logo, Tiêu đề trang, Thông báo hệ thống, Email, và Bản địa hóa i18n), tên thương hiệu **bắt buộc** phải được viết tách rời có dấu cách: **`Booking Hotel`**.
  - Trong các thành phần Logo HTML/SCSS hai tông màu (đỏ `#990000` và đen `#111111`), thẻ chứa từ `Booking` (`.logo-red`) và thẻ chứa từ `Hotel` (`.logo-black`) phải luôn có khoảng cách rõ ràng (đặt ký tự khoảng trắng giữa các thẻ và thiết lập `margin-right: 0.35rem;` hoặc gap tối thiểu tương đương) để đảm bảo không bị dính chữ trên bất kỳ trình duyệt nào.
- **Tên kỹ thuật trong mã nguồn (Code Identifiers & Namespaces)**:
  - Tên Solution, Project, Root Namespace, CSDL, C# Classes và Angular package/routes kỹ thuật giữ nguyên định dạng PascalCase theo quy ước kỹ thuật: `BookingHotel` (ví dụ: namespace `BookingHotel.Domain`, database `BookingHotel`, clientId `BookingHotel_App`).

### IX. Quy Chuẩn Bảo Mật Đăng Nhập & Quản Lý Phiên (Authentication & Session Security Standard)
- **Tuyệt đối không hiển thị sẵn thông tin đăng nhập (Zero Credential Pre-filling)**:
  - Ở phần đăng nhập (Login Form), **tuyệt đối không hiển thị thông tin đăng nhập của bất kỳ tài khoản nào, kể cả tài khoản Quản trị viên (Admin)**, tài khoản kiểm thử hay tài khoản demo.
  - Tuyệt đối cấm hardcode, gán giá trị mặc định hoặc gợi ý sẵn thông tin tài khoản trên giao diện đăng nhập. Khi người dùng truy cập trang, các trường Email/Tên đăng nhập và Mật khẩu bắt buộc phải để trống hoàn toàn.
- **Quy tắc Ghi Nhớ Đăng Nhập (Remember Me Rule)**:
  - Chỉ được phép lưu trữ và hiển thị lại thông tin đăng nhập (chỉ lưu địa chỉ Email đã nhớ, tuyệt đối **không bao giờ** lưu hay điền sẵn Mật khẩu) **khi và chỉ khi người dùng chủ động nhấn chọn "Remember Me" (Ghi nhớ đăng nhập)** trong lần đăng nhập thành công trước đó.
- **Giới hạn thời hạn phiên đăng nhập (Maximum Session Duration: Tối đa 15 ngày)**:
  - Khi người dùng nhấn chọn Remember Me, phiên đăng nhập (Session / Refresh Token / Token Expiration) có thời hạn **tối đa là 15 ngày, tuyệt đối không được hơn**.
  - Quá thời hạn 15 ngày, toàn bộ phiên đăng nhập và dữ liệu ghi nhớ bắt buộc phải tự động hết hiệu lực, yêu cầu người dùng xác thực lại từ đầu.
  - Trường hợp người dùng **không** nhấn Remember Me, phiên đăng nhập chỉ duy trì trong thời gian ngắn hạn (short-lived session, tối đa 2 giờ hoặc kết thúc khi đóng trình duyệt).

## Ràng Buộc Công Nghệ & Hạ Tầng

- **Backend**: .NET 10, ABP Framework (v10.7+), Entity Framework Core.
- **Frontend**: Angular, TypeScript, ESLint, Prettier.
- **Cơ sở dữ liệu**: Quan hệ (SQL Server / PostgreSQL) quản lý thông qua EF Core Migrations.
- **Xác thực & Bảo mật**: OpenIddict / JWT Bearer Tokens tích hợp trong ABP Identity.

## Quy Trình Triển Khai Tính Năng (Development Workflow)

1. **Đặc tả tính năng (Spec)**: Xác định rõ tác nhân (Actor), luồng nghiệp vụ, danh mục quyền (Permissions) và các điều kiện bất biến (Invariants).
2. **Thứ tự lập trình chuẩn DDD**:
   - `Domain` (Tạo Entity, Enums, Domain Service) 
   $\rightarrow$ `Contracts` (Tạo DTOs, Permissions, Interfaces)
   $\rightarrow$ `Application` (Viết AppService logic)
   $\rightarrow$ `EFCore` (Cấu hình Model Creating, tạo & chạy Migration)
   $\rightarrow$ `Angular` (Xây dựng UI Component, Form, kết nối API).
3. **Cổng kiểm tra chất lượng (Quality Gates)**:
   - Build thành công toàn bộ solution không có lỗi cú pháp.
   - Chạy `DbMigrator` thành công nếu có thay đổi lược đồ database.
   - Đảm bảo tuân thủ cấu hình định dạng code trong `.editorconfig` và `.eslintrc.json`.

## Điều Hành & Giám Sát (Governance)

- Bản Hiến pháp này có giá trị cao nhất trong toàn bộ quy chuẩn kỹ thuật của dự án `BookingHotel`.
- Mọi trợ lý AI và lập trình viên khi đề xuất giải pháp, lập kế hoạch hay sinh code đều bắt buộc tuân thủ các điều khoản trên.
- Mọi thay đổi hay bổ sung Hiến pháp cần có sự phê duyệt của Tech Lead, có lý do rõ ràng và phải nâng số phiên bản của Hiến pháp (theo chuẩn SemVer).

**Phiên bản**: 1.4.0 | **Phê duyệt**: 2026-11-11 | **Cập nhật lần cuối**: 2026-11-11
