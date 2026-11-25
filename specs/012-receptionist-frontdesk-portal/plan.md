# Implementation Plan: Phân hệ Tác nghiệp Lễ tân (RM-012)

**Branch**: `012-receptionist-frontdesk-portal` | **Date**: 2026-11-25 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/012-receptionist-frontdesk-portal/spec.md`

---

## Summary

Xây dựng hoàn chỉnh **Phân hệ Tác nghiệp Lễ tân (`/frontdesk/*`)** dành cho nhân viên Lễ tân tiếp nhận và xử lý các booking được tạo qua nền tảng **Booking Hotel Platform**, gồm 5 phân khu cốt lõi: **Dashboard**, **Room Rack (chỉ theo dõi)**, **Reservation Management**, **Guest Management**, và **Check-in / Check-out**.

Cách tiếp cận kỹ thuật theo chuẩn DDD & ABP Framework:
- **Backend**: Tách namespace độc lập `Frontdesk` (`FrontdeskAppServiceBase` + 4 AppServices & 4 API Controllers chuyên biệt) chỉ cấp đúng quyền Lễ tân (`Bookings.*`, `Rooms.Default`) kết hợp kiểm soát cơ sở được phân công (`UserHotelAssignment`), bổ sung `FrontdeskStayDomainService` tại tầng Domain để kiểm soát chuyển trạng thái booking/phòng, chống trùng lịch gán phòng và chuẩn hóa che số giấy tờ định danh (*masked* giữ 4 ký tự cuối), cùng 1 EF Core Migration bổ sung 6 cột theo dõi lưu trú trên `AppBookings`.
- **Frontend**: 5 trang Angular standalone (`OnPush`, Signals, song ngữ VI/EN, Zero Mock Data) kế thừa layout `ReceptionistLayoutComponent` theo Concept Lễ tân (`#99FF99` & `#808080`, chữ đen tương phản cao), loại bỏ hoàn toàn các mục ngoài phạm vi (Folios, nút Housekeeping, quản trị giá/bảo trì).

---

## Technical Context

**Language/Version**: C# / .NET 10 (Backend), TypeScript 5 / Angular 19+ (Frontend)  
**Primary Dependencies**: ABP Framework 10.7+, Entity Framework Core, xUnit / Shouldly, Angular Signals / RxJS, FontAwesome  
**Storage**: SQL Server (EF Core Code-First Migrations áp dụng qua `BookingHotel.DbMigrator`)  
**Testing**: `BookingHotel.Domain.Tests` (Domain unit tests cho `FrontdeskStayDomainService`) và `BookingHotel.EntityFrameworkCore.Tests` (Application + EF Core integration tests cho 4 Frontdesk AppServices + bảo mật cross-hotel)  
**Target Platform**: Web Application (Desktop & Tablet tại quầy Lễ tân, tương thích Chrome/Edge)  
**Project Type**: Multi-tier Web Application (ABP Layered DDD Backend + Angular SPA Frontend)  
**Performance Goals**: Dashboard tổng quan tải < 1s (đáp ứng SC-001 < 3s), tìm kiếm/lọc booking & khách < 500ms (SC-002 < 5s), thao tác Check-in/Check-out hoàn tất nguyên tử trong 1 Unit of Work  
**Constraints**:
- Cô lập tuyệt đối theo `TenantId` (`IMultiTenant`) và `AssignedHotelId` (`UserHotelAssignment`).
- Tuyệt đối không chứa dữ liệu ảo (*Zero Mock Data*), không hardcode chữ (*Zero Fixed Text*), không cho phép Lễ tân sửa giá phòng/gói giá/nhân sự/Housekeeping/bảo trì/Reject/No-Show.
- Check-out trả phòng về `Vacant` + `Clean` (sẵn sàng ngay theo Q2); số CCCD/Hộ chiếu bắt buộc khi Check-in và lưu dạng *masked* giữ 4 ký tự cuối (theo Q4).
**Scale/Scope**: 5 màn hình Angular chính, 4 Backend AppServices / Controllers, 1 Domain Service, 1 EF Core Migration (6 cột mở rộng trên `AppBookings`), ~120 khóa dịch song ngữ VI/EN.

---

## Constitution Check

*GATE: Evaluated before Phase 0 and re-checked post Phase 1 design.*

| Nguyên tắc Hiến pháp | Đánh giá Pre-Design | Đánh giá Post-Design | Biện pháp tuân thủ |
| --- | --- | --- | --- |
| **I. DDD & ABP Layered Architecture** | PASS | PASS | Logic chuyển trạng thái & mask định danh đặt ở `BookingHotel.Domain` (`FrontdeskStayDomainService`); DTO/Interface ở `Application.Contracts/Frontdesk`; AppService ở `Application/Frontdesk`; Controller ở `HttpApi/Controllers/Frontdesk`. |
| **II. Contract-First & Bảo mật Mặc định** | PASS | PASS | 100% DTO có DataAnnotations (`[Required]`, `[StringLength]`, `[EmailAddress]`); 100% AppService gắn `[Authorize(BookingHotelPermissions.*)]` + `CheckFrontdeskHotelAccessAsync(hotelId)` chặn cross-hotel và vượt quyền. |
| **III. EF Core Migrations** | PASS | PASS | Mở rộng 6 cột trên `AppBookings` qua EF Core Migration chuẩn và áp dụng qua `BookingHotel.DbMigrator`. |
| **IV. Chuẩn hóa Frontend Angular** | PASS | PASS | Standalone components (`OnPush`), strong-typed `FrontdeskService`, Signals + Reactive validation, FE chỉ trình bày và không tự quyết định trạng thái hay tính tiền. |
| **V. Song ngữ i18n & Zero Fixed Text** | PASS | PASS | Toàn bộ nhãn/trạng thái/lỗi mới đồng bộ 3 nơi: `vi.json`, `en.json`, `fallback-localizations.ts`; unit test đầy đủ tại `BookingHotel.Domain.Tests`. |
| **VI. Backend-Authoritative** | PASS | PASS | Mọi chuyển trạng thái (Confirm, AssignRoom, Cancel, CheckIn, CheckOut) và che số CCCD thực thi 100% tại Backend; DTO không chấp nhận trường tài chính từ Client. |
| **VII. Concept Màu sắc Lễ tân** | PASS | PASS | Sử dụng đúng hệ màu Lễ tân (`#99FF99` hành động/sẵn sàng, `#808080` cấu trúc/trung tính, `#000000` tương phản cao). |
| **VIII. Quy chuẩn Thương hiệu** | PASS | PASS | Giữ tên hiển thị `Booking Hotel` có dấu cách chuẩn. |
| **IX. Bảo mật Đăng nhập & Phiên** | PASS | PASS | Tái sử dụng hệ thống xác thực & JWT hiện hữu, không lưu dữ liệu nhạy cảm trên client. |
| **X. Zero Mock / Fake Data** | PASS | PASS | 100% truy xuất CSDL thật; hiển thị `<bh-empty-state>` khi cơ sở chưa có dữ liệu. |
| **XI. Đánh dấu Trường Bắt buộc (`*`)** | PASS | PASS | Mọi trường bắt buộc trên modal Check-in, Hủy booking, Cập nhật khách đều dùng `<span class="text-danger">*</span>` (chỉ dấu sao đỏ, chữ nhãn không in đậm). |

**Gate Result**: **ALL 11/11 CONSTITUTION GATES PASS** — không có ngoại lệ hay vi phạm nào cần giải trình.

---

## Project Structure

### Documentation (this feature)

```text
BookingHotel/specs/012-receptionist-frontdesk-portal/
├── spec.md                       # Feature specification (đã qua /speckit-clarify Q1-Q4)
├── plan.md                       # Implementation plan (file này)
├── research.md                   # Phase 0: Quyết định kiến trúc & phân tích phương án
├── data-model.md                 # Phase 1: Mô hình dữ liệu, mở rộng Booking & chuyển trạng thái
├── quickstart.md                 # Phase 1: Hướng dẫn kiểm chứng E2E & chạy test
├── checklists/
│   └── requirements.md           # Checklist chất lượng đặc tả (16/16 PASS)
├── contracts/
│   └── frontdesk-api.md          # Phase 1: Hợp đồng REST API & DTO cho 4 nhóm endpoint
└── tasks.md                      # Phase 2 output (/speckit-tasks — chưa tạo ở bước này)
```

### Source Code (repository root: `d:\DFT\Booking_Hotel\BookingHotel`)

```text
src/
├── BookingHotel.Domain.Shared/
│   ├── Bookings/
│   │   └── FrontdeskRoomRackStatus.cs                 # Enum trạng thái hiển thị Room Rack (Ready/Reserved/Occupied/NotReady/Unavailable)
│   └── Localization/BookingHotel/
│       ├── vi.json                                    # Khóa dịch tiếng Việt cho 5 phân khu Frontdesk & error codes
│       └── en.json                                    # Khóa dịch tiếng Anh tương ứng
├── BookingHotel.Domain/
│   └── Bookings/
│       ├── Booking.cs                                 # Bổ sung 6 thuộc tính theo dõi lưu trú (RoomTypeId, ActualCheckIn/Out, IsIdentityVerified, CancellationReason, FrontdeskNotes)
│       └── FrontdeskStayDomainService.cs              # Domain Service: MaskIdentityNumber, kiểm tra chuyển trạng thái, chống trùng lịch phòng
├── BookingHotel.EntityFrameworkCore/
│   ├── EntityFrameworkCore/
│   │   └── BookingHotelDbContext.cs                   # Fluent API mapping & index cho 6 cột mới của AppBookings
│   └── Migrations/
│       └── <timestamp>_AddFrontdeskStayTrackingToBookings.cs
├── BookingHotel.Application.Contracts/
│   └── Frontdesk/
│       ├── FrontdeskDashboardDtos.cs                  # KPI + Quick Lists DTOs
│       ├── FrontdeskRoomRackDtos.cs                   # Room Rack read-only DTOs
│       ├── FrontdeskReservationDtos.cs                # List, Detail, AssignRoom, Update, Cancel, CheckIn, CheckOut DTOs
│       ├── FrontdeskGuestDtos.cs                      # Guest List, Detail, Update DTOs
│       ├── IFrontdeskDashboardAppService.cs
│       ├── IFrontdeskRoomRackAppService.cs
│       ├── IFrontdeskReservationAppService.cs
│       └── IFrontdeskGuestAppService.cs
├── BookingHotel.Application/
│   └── Frontdesk/
│       ├── FrontdeskAppServiceBase.cs                 # CheckFrontdeskHotelAccessAsync(hotelId) cô lập cơ sở
│       ├── FrontdeskDashboardAppService.cs
│       ├── FrontdeskRoomRackAppService.cs
│       ├── FrontdeskReservationAppService.cs
│       └── FrontdeskGuestAppService.cs
└── BookingHotel.HttpApi/
    └── Controllers/Frontdesk/
        ├── FrontdeskDashboardController.cs
        ├── FrontdeskRoomRackController.cs
        ├── FrontdeskReservationController.cs
        └── FrontdeskGuestController.cs

test/
├── BookingHotel.Domain.Tests/
│   └── Bookings/
│       └── FrontdeskStayDomainServiceTests.cs         # Unit tests cho masking CCCD, chuyển trạng thái & chống trùng lịch
└── BookingHotel.EntityFrameworkCore.Tests/
    └── Frontdesk/
        └── FrontdeskAppServiceTests.cs                # Integration tests cho Dashboard, RoomRack, Reservations, Guests, Check-in/out & Cross-hotel security

angular/src/app/
├── shared/localization/
│   └── fallback-localizations.ts                      # Đồng bộ fallback VI/EN
└── admin/
    ├── guards/
    │   └── role-based-redirect.guard.ts               # Điều hướng Receptionist mặc định về /frontdesk/dashboard
    ├── services/frontdesk/
    │   └── frontdesk.service.ts                       # Strong-typed Angular REST client cho 4 Frontdesk APIs
    └── frontdesk/
        ├── frontdesk.routes.ts                        # 5 tuyến đường chính (/dashboard, /room-rack, /reservations, /guests, /check-in-out + alias)
        ├── receptionist-layout/
        │   └── receptionist-layout.component.*        # Menu 5 mục chuẩn RM-012 (gỡ bỏ Folios)
        ├── dashboard/
        │   └── frontdesk-dashboard.component.*        # US1: 4 thẻ KPI + danh sách tác nghiệp trong ngày
        ├── room-rack/
        │   └── room-rack.component.*                  # US4: Nâng cấp chế độ chỉ theo dõi + drawer chi tiết phòng/booking (gỡ bỏ Housekeeping)
        ├── reservations/
        │   └── frontdesk-reservations.component.*     # US2: Danh sách, bộ lọc, drawer chi tiết, xác nhận, gán/đổi phòng, sửa ghi chú, hủy đơn
        ├── guests/
        │   └── frontdesk-guests.component.*           # US5: Danh sách khách, tìm kiếm, drawer lịch sử lưu trú, modal cập nhật thông tin
        └── check-in-out/
            └── frontdesk-check-in-out.component.*     # US3: Quầy 2 tab Nhận phòng (xác minh CCCD + chọn phòng) & Trả phòng
```

**Structure Decision**: Kế thừa đúng cấu trúc phân lớp ABP (.NET 10) + Angular standalone module sẵn có trong `BookingHotel/`, tạo phân vùng `Frontdesk` độc lập tương đương cấu trúc `Manager` và `Tenant` hiện hành để tách biệt triệt để quyền hạn và dễ bảo trì.

---

## Complexity Tracking

Không có vi phạm Hiến pháp nào — không cần mục giải trình ngoại lệ.
