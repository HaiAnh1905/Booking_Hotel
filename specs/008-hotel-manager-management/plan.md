# Implementation Plan: RM-008 - Thiết Kế Trang Quản Lý Cho Role Hotel Manager (Hotel Manager Operations & Property Console)

**Branch**: `008-hotel-manager-management` | **Date**: 2026-11-19 | **Spec**: [specs/008-hotel-manager-management/spec.md](spec.md)  
**Input**: Feature specification từ `spec.md`, các kết quả nghiên cứu Phase 0 (`research.md`), mô hình dữ liệu Phase 1 (`data-model.md`), các hợp đồng API (`contracts/`), và hướng dẫn kiểm thử (`quickstart.md`).

---

## 1. Summary

Xây dựng và hoàn thiện toàn diện phân hệ điều hành và quản lý cơ sở khách sạn dành cho vai trò **Quản lý Khách sạn (`HotelManager`)** trên nền tảng ABP Framework 10.7 (.NET 10 / EF Core / Angular 19+ Standalone) tuân thủ nghiêm ngặt Hiến pháp dự án `Booking Hotel`:
1. **Bộ Chọn Cơ Sở & Xác Thực Độc Lập Ngữ Cảnh (Property Context & Scoping)**:
   - Backend xác thực bắt buộc 100% quyền quản lý của nhân sự đối với cơ sở qua `UserHotelAssignment`; từ chối mọi can thiệp trái phép với HTTP 403 Forbidden.
   - Hỗ trợ làm việc đa tab độc lập qua Angular URL State & Session Context, không bị ghi đè chéo dữ liệu giữa các tab.
2. **Hồ Sơ Cơ Sở & Quản Trị Tiện Ích (Hotel Profile & Amenities)**:
   - Bảo vệ mã định danh cơ sở `HotelCode` và Tên cơ sở gốc ở trạng thái bất biến (Read-Only).
   - Cho phép quản lý cập nhật mô tả, hình ảnh tiện ích, tiện nghi (Amenities), quy định lưu trú nội bộ và giờ Check-in/Check-out.
3. **Quản Lý Buồng Phòng & Bảng Giá Đa Tầng (Rooms & Rates)**:
   - Tách biệt 2 chiều trạng thái độc lập: `RoomOperationalStatus` (Vận hành) và `HousekeepingStatus` (Vệ sinh). Áp dụng quy tắc phòng sẵn sàng đón khách: $\text{Sellable} \iff (\text{Available}) \land (\text{Clean} \lor \text{Inspected})$.
   - Quản lý đưa phòng vào `Maintenance` hoặc `OutOfService` kèm Safety Guardrails: Bắt buộc lý do, ngày kết thúc và tự động chặn nếu phòng đang có khách hoặc có booking tương lai xung đột; lưu vết `AuditLog`.
   - Quản lý gói giá: Kế thừa danh mục chuẩn `RatePlan` của Tenant Admin (bật/tắt tại cơ sở), cấu hình `BasePrice` và thiết lập bảng ghi đè giá theo ngày `DailyRateOverride`.
4. **Vòng Đời Đặt Phòng & Điều Phối Tác Nghiệp (Booking Operations)**:
   - Tách biệt 3 trường trạng thái: `BookingStatus`, `PaymentStatus`, `RoomAssignmentStatus`.
   - Cấp quyền `Bookings.Create` cho Hotel Manager để tạo đơn đặt phòng trực tiếp (`Direct Booking`) cho khách đoàn, khách VIP và hợp đồng doanh nghiệp (Corporate).
   - Thao tác đánh dấu No-Show: Chỉ giải phóng phân bổ phòng theo khoảng thời gian của đơn; áp dụng mức phạt Tenant tự động kèm quyền điều chỉnh giảm/miễn phạt có lý do và ghi `AuditLog`.
5. **Bảo Vệ Dữ Liệu Khách Hàng Cơ Sở & Ghi Chú Có Audit**:
   - Chỉ cho phép xem thông tin khách từng lưu trú tại cơ sở; che bảo mật số CCCD/Passport (`***-***-XXXX`).
   - Quản lý Ghi chú nội bộ (`InternalGuestNotes`) có Audit Log đầy đủ; cấm hiển thị ra Customer Portal.
6. **Báo Cáo Hiệu Suất Chuẩn Hóa Quốc Tế (USALI Analytics)**:
   - Tính toán tự động các chỉ số Occupancy Rate, ADR, RevPAR theo công thức chuẩn; phân rã doanh thu phòng thuần, doanh thu dịch vụ phụ trợ và tiền thực thu.
7. **Đánh Giá & Xử Lý Khiếu Nại Kèm Cam Kết SLA 48h**:
   - Theo dõi điểm đánh giá theo 4 tiêu chí; đăng phản hồi chính thức của khách sạn.
   - Luồng khiếu nại `Open` $\rightarrow$ `InInvestigation` $\rightarrow$ `Resolved`; tự động gắn nhãn `Overdue SLA` và leo thang (`Escalated`) lên Tenant Admin khi trễ hạn 48 giờ.

---

## 2. Technical Context

- **Ngôn ngữ / Phiên bản**: C# .NET 10, TypeScript 5.x / Angular 19+ (Standalone Components).
- **Phụ thuộc chính**:
  - ABP Framework 10.7 (`Volo.Abp.Identity`, `Volo.Abp.Authorization`, `Volo.Abp.MultiTenancy`, `Volo.Abp.PermissionManagement`).
  - Entity Framework Core 10.x (SQL Server / LocalDB).
  - Angular Router, Signals, Reactive Forms, `@abp/ng.core`.
  - Design Tokens: Concept Quản lý (Nền Trắng `#FFFFFF`, Sidebar Đen sâu `#000000`, Điểm nhấn Cyan `#00FFFF`, Huy hiệu vai trò Xanh dương `#2563EB`).
- **Lưu trữ CSDL**: SQL Server 2022+ / LocalDB, EF Core Code-First Migrations qua `BookingHotel.DbMigrator`.
- **Testing Framework**:
  - Backend: xUnit, Shouldly, ABP TestBase (`BookingHotel.Application.Tests`, `BookingHotel.Domain.Tests`).
  - Frontend: TypeScript Compile Check (`tsc --noEmit`), Jasmine/Karma.
- **Mục tiêu hiệu năng**: P95 Latency: Dashboard < 2.0s, Danh sách buồng phòng < 1.5s, Danh sách đặt phòng < 2.0s, Tác nghiệp < 1.0s.

---

## 3. Constitution Check (Tuân Thủ Hiến Pháp Dự Án)

*GATE: Kiểm tra 11 Điều khoản của Hiến pháp dự án tại `.specify/memory/constitution.md`*

| Điều khoản Hiến pháp | Tiêu chí đánh giá | Trạng thái | Giải pháp kỹ thuật tương ứng |
| :--- | :--- | :---: | :--- |
| **I. Phân lớp DDD & ABP** | Tách biệt Domain, Contracts, Application, EF Core, HttpApi | **PASS** | Thực thể `RoomType`, `Room`, `RatePlan`, `Booking`, `Complaint` tại Domain; DTOs/Interfaces tại Contracts; AppServices tại Application; Controllers tại HttpApi. |
| **II. Contract-First & Security** | Kiểm tra quyền ABP, Data Annotations, Audit Logging | **PASS** | Khai báo quyền `BookingHotelPermissions.Rooms.*`, `Bookings.*`, `Guests.*`, `Reviews.*`. DTOs validation đầy đủ; kế thừa `FullAuditedAggregateRoot` / `FullAuditedEntity`. |
| **III. EF Core Migrations** | Quản lý schema bằng Code-First Migrations qua DbMigrator | **PASS** | Tạo migration `AddHotelManagerOperationsEntities` và cấu hình Fluent API trong `BookingHotelDbContextModelCreatingExtensions`. |
| **IV. Chuẩn hóa Frontend** | Thin presentation, Reactive Forms, Angular Signals, Strict TS | **PASS** | Xây dựng các module standalone trong `angular/src/app/admin/manager/`: dashboard, hotel-profile, rooms, rates, bookings, guests, reports, feedback. |
| **V. Đa ngôn ngữ (Zero Fixed Text)** | Cấm tuyệt đối văn bản cứng; đồng bộ `vi.json` và `en.json` | **PASS** | 100% nhãn form, trạng thái enum, toastr khai báo đầy đủ song ngữ tại `vi.json` và `en.json` với tiền tố `Manager:*`. |
| **VI. Backend-Authoritative** | Tính toán giá cả, chuyển trạng thái thuộc về Backend | **PASS** | Backend tính toán TotalAmount, kiểm tra trùng lặp phòng, tính Occupancy, ADR, RevPAR. Frontend chỉ thu thập input và hiển thị. |
| **VII. UI Concept Hệ Màu** | Nền Trắng, Sidebar Đen, Điểm nhấn Cyan, WCAG AA $\ge 4.5:1$ | **PASS** | Sử dụng theme *Operational Manager Console* với CSS variables chuẩn từ `styles.scss`. |
| **VIII. Brand Naming** | Tên thương hiệu `Booking Hotel` có dấu cách | **PASS** | Header và layout đảm bảo hiển thị thương hiệu `Booking Hotel` có dấu cách. |
| **IX. Bảo mật Phiên & Login** | Zero credential pre-filling | **PASS** | Không ảnh hưởng đến login; tuân thủ hoàn toàn. |
| **X. Cấm Dữ Liệu Ảo (Zero Mock)** | 100% dữ liệu thực từ CSDL; dùng `bh-empty-state` khi rỗng | **PASS** | Mọi API truy vấn trực tiếp CSDL qua EF Core; khi rỗng hiển thị `bh-empty-state`. |
| **XI. Dấu Sao Đỏ Bắt Buộc Nhập** | Dấu sao `*` màu đỏ `#dc2626`, nhãn không in đậm | **PASS** | Cấu trúc form tuân thủ: `<label class="form-label">{{ 'Key' | abpLocalization }} <span class="text-danger">*</span></label>`. |

---

## 4. Project Structure & Code Mapping

### 4.1. Tài Liệu Đặc Tả & Thiết Kế (Documentation Artifacts)
```text
BookingHotel/specs/008-hotel-manager-management/
├── spec.md                          # Đặc tả yêu cầu hoàn chỉnh kèm 5 quyết định nghiệp vụ
├── plan.md                          # Kế hoạch kiến trúc kỹ thuật (Tài liệu này)
├── research.md                      # Báo cáo nghiên cứu Phase 0
├── data-model.md                    # Thiết kế mô hình dữ liệu CSDL Phase 1
├── quickstart.md                    # Kịch bản kiểm thử End-to-End Phase 1
└── contracts/
    ├── manager-dashboard.md         # API bảng điều khiển KPIs thời gian thực
    ├── manager-rooms-rates.md       # API buồng phòng, gói giá RatePlan, giá ngày
    ├── manager-bookings.md          # API đặt phòng, Direct Booking, No-Show
    ├── manager-guests.md            # API khách hàng cơ sở, CCCD che masked, ghi chú
    ├── manager-reports.md           # API báo cáo hiệu suất Occupancy, ADR, RevPAR
    └── manager-reviews-complaints.md # API đánh giá & luồng khiếu nại SLA 48h
```

### 4.2. Mã Nguồn Backend (.NET 10 / ABP Framework)
```text
BookingHotel/src/
├── BookingHotel.Domain.Shared/
│   ├── Rooms/
│   │   ├── RoomOperationalStatus.cs         # Available, Occupied, Reserved, Maintenance, OutOfService
│   │   └── HousekeepingStatus.cs            # Clean, Dirty, Cleaning, Inspected
│   ├── Bookings/
│   │   ├── BookingStatus.cs                 # PendingConfirmation, Confirmed, CheckedIn, Completed, Rejected, Cancelled, NoShow
│   │   ├── PaymentStatus.cs                 # Unpaid, PartiallyPaid, Paid, Refunded
│   │   └── RoomAssignmentStatus.cs          # Unassigned, Assigned
│   ├── Complaints/
│   │   └── ComplaintStatus.cs               # Open, InInvestigation, Resolved, Escalated
│   └── Localization/BookingHotel/
│       ├── vi.json                          # Bản địa hóa tiếng Việt (Manager:*)
│       └── en.json                          # Bản địa hóa tiếng Anh (Manager:*)
├── BookingHotel.Domain/
│   ├── Rooms/
│   │   ├── RoomType.cs                      # AggregateRoot Loại phòng
│   │   ├── Room.cs                          # AggregateRoot Phòng thực tế
│   │   ├── RatePlan.cs                      # AggregateRoot Gói giá Tenant
│   │   ├── HotelRatePlan.cs                 # Entity Kích hoạt gói giá chi nhánh
│   │   └── DailyRateOverride.cs             # Entity Ghi đè giá theo ngày
│   ├── Bookings/
│   │   ├── Booking.cs                       # AggregateRoot Đơn đặt phòng
│   │   └── RoomAssignment.cs                # Entity Phân bổ phòng theo khoảng ngày
│   ├── Guests/
│   │   └── InternalGuestNote.cs             # Entity Ghi chú nội bộ có Audit
│   ├── Complaints/
│   │   └── Complaint.cs                     # AggregateRoot Khiếu nại khách hàng
│   └── Reviews/
│       └── Review.cs                        # AggregateRoot Đánh giá & Phản hồi
├── BookingHotel.Application.Contracts/
│   ├── Permissions/
│   │   ├── BookingHotelPermissions.cs       # Bổ sung Rooms.*, Bookings.Create, Guests.*, Reviews.*
│   │   └── BookingHotelPermissionDefinitionProvider.cs
│   └── Manager/
│       ├── Dashboard/                       # IManagerDashboardAppService & DTOs
│       ├── Rooms/                           # IManagerRoomAppService, IRatePlanAppService & DTOs
│       ├── Bookings/                        # IManagerBookingAppService & DTOs
│       ├── Guests/                          # IManagerGuestAppService & DTOs
│       ├── Reports/                         # IManagerReportAppService & DTOs
│       └── Feedback/                        # IManagerFeedbackAppService & DTOs
├── BookingHotel.Application/
│   └── Manager/                             # Triển khai các AppServices + Centralized Hotel Access Check
├── BookingHotel.EntityFrameworkCore/
│   ├── EntityFrameworkCore/
│   │   └── BookingHotelDbContextModelCreatingExtensions.cs # Fluent API mapping & Indexes
│   └── Migrations/                          # EF Core Code-First Migration
└── BookingHotel.HttpApi/
    └── Controllers/Manager/                 # API Controllers cho từng phân hệ
```

### 4.3. Mã Nguồn Frontend (Angular 19+ Standalone)
```text
BookingHotel/angular/src/app/admin/manager/
├── hotel-manager-layout/                    # Layout chung kèm Property Selector đa tab
├── manager-dashboard/                       # Realtime KPIs, Today Arrivals, Action Items
├── hotel-profile/                           # Hồ sơ cơ sở (Mã bất biến, tiện ích, giờ check-in/out)
├── rooms/
│   ├── room-list/                           # Danh sách phòng, thẻ trạng thái 2 chiều, lọc tầng
│   ├── room-modal/                          # Modal tạo/sửa phòng, chuyển Maintenance/OOS
│   └── room-type-modal/                     # Modal quản lý loại phòng
├── rates/                                   # Kích hoạt RatePlan Tenant & bảng DailyRateOverride
├── bookings/
│   ├── booking-list/                        # Tra cứu đa tiêu chí, 3 trường trạng thái
│   ├── booking-detail-modal/                # Chi tiết đơn, xem dòng tiền
│   ├── direct-booking-modal/                # Tạo đặt phòng trực tiếp cho khách đoàn
│   └── no-show-modal/                       # Modal xác nhận No-Show kèm điều chỉnh miễn phạt
├── guests/
│   ├── guest-list/                          # Danh sách khách cơ sở, CCCD che masked
│   └── guest-notes-modal/                   # Ghi chú nội bộ có lưu Audit Log
├── reports/                                 # Báo cáo hiệu suất Occupancy, ADR, RevPAR, phân rã dòng tiền
├── feedback/
│   ├── review-list/                         # Đánh giá 4 tiêu chí & modal phản hồi chính thức
│   └── complaint-list/                      # Danh sách khiếu nại, đồng hồ SLA 48h, cảnh báo Escalated
└── manager.routes.ts                        # Cấu hình định tuyến hoàn chỉnh
```

---

## 5. Các Giai Đoạn Triển Khai (Phased Rollout Plan)

### Giai Đoạn 1: Lược Đồ Dữ Liệu & Entity Framework Core
- Tạo Enums tại `BookingHotel.Domain.Shared`.
- Tạo các thực thể Entity/AggregateRoot tại `BookingHotel.Domain`.
- Cấu hình Fluent API, Indexes và Foreign Keys tại `BookingHotel.EntityFrameworkCore`.
- Tạo Migration EF Core và nạp dữ liệu mẫu khởi tạo qua `DbMigrator`.

### Giai Đoạn 2: Hợp Đồng Ứng Dụng (Contracts, Permissions & DTOs)
- Khai báo bổ sung các quyền `BookingHotelPermissions` (`Rooms.Create`, `Rooms.Edit`, `Rooms.SetRates`, `Bookings.Create`, `Bookings.MarkNoShow`, `Guests.ManageNotes`, `Reviews.Reply`).
- Xây dựng các DTOs đầu vào/đầu ra với đầy đủ thuộc tính Data Annotations validation.

### Giai Đoạn 3: Triển Khai Nghiệp Vụ Tầng Ứng Dụng (AppServices)
- Xây dựng phương thức bảo vệ ngữ cảnh `CheckHotelManagerAccessAsync(Guid hotelId)` kiểm tra quyền trong `UserHotelAssignment`.
- Triển khai logic tính toán giá phòng và ghi đè giá ngày.
- Triển khai logic kiểm tra xung đột buồng phòng trước khi chuyển sang `Maintenance` / `OutOfService`.
- Triển khai luồng No-Show giải phóng phòng và tính phạt/hoàn cọc minh bạch.
- Triển khai công thức báo cáo USALI (Occupancy, ADR, RevPAR).
- Cài đặt Background Job kiểm tra SLA khiếu nại 48 giờ để tự động kích hoạt `Escalated`.

### Giai Đoạn 4: Bộ Điều Khiển API (HttpApi Controllers)
- Xây dựng các API Controllers chuẩn RESTful trong `BookingHotel.HttpApi`.
- Kiểm thử tích hợp qua Swagger UI.

### Giai Đoạn 5: Giao Diện Người Dùng Angular Standalone
- Cập nhật định tuyến `manager.routes.ts` kết nối đến các module chức năng.
- Hoàn thiện giao diện các màn hình: Dashboard, Hồ sơ khách sạn, Buồng phòng, Bảng giá, Đặt phòng, Khách hàng, Báo cáo, Đánh giá & Khiếu nại.
- Tích hợp 100% bản địa hóa vào `vi.json` và `en.json` (Zero Hardcoded Text).
- Áp dụng chuẩn dấu sao đỏ `*` cho các trường bắt buộc.

### Giai Đoạn 6: Kiểm Thử Toàn Diện & Đóng Gói
- Thực hiện toàn bộ 9 kịch bản kiểm thử tích hợp trong `quickstart.md`.
- Kiểm tra tính độc lập đa tab và khả năng phòng thủ chống can thiệp trái phép `HotelId`.
- Biên dịch Angular và .NET xác nhận 0 Warning / 0 Error.