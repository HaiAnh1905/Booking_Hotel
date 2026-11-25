# Implementation Plan: RM-010 – ROOM MANAGEMENT (Quản Lý Phòng Khách Sạn)

**Branch**: `010-room-management`  
**Created**: 2026-11-20  
**Status**: Ready for Implementation  
**Spec Reference**: `specs/010-room-management/spec.md`  

---

## 1. Kiến Trúc & Chiến Lược Triển Khai (Architecture & Strategy)

Hệ thống tuân thủ nghiêm ngặt mô hình DDD (Domain-Driven Design) của ABP Framework kết hợp Frontend Angular 19 Standalone Components:
- **Domain Layer (`BookingHotel.Domain` & `Domain.Shared`)**:
  - Mở rộng enum `RoomOccupancyStatus` (`Vacant`, `Reserved`, `Occupied`, `Blocked`), `RoomOperationalStatus` (`Active`, `UnderMaintenance`, `OutOfService`), `RoomClosureType` (`Planned`, `Emergency`), `RoomClosureStatus`, `AffectedBookingResolutionType`.
  - Cập nhật Aggregate Root `Room`: thêm `OccupancyStatus`, `IsActive`, `Building`, `Description`.
  - Tạo mới Aggregate Root `RoomClosure` (kế thừa `FullAuditedAggregateRoot<Guid>, IMultiTenant`).
  - Tạo mới Entity / Aggregate `AffectedBookingResolution` (kế thừa `FullAuditedEntity<Guid>, IMultiTenant`).
- **Data Access Layer (`BookingHotel.EntityFrameworkCore`)**:
  - Khai báo `DbSet<RoomClosure>`, `DbSet<AffectedBookingResolution>`.
  - Ánh xạ Fluent API trong `BookingHotelDbContextModelSnapshot` và sinh Migration EF Core (`AddRoomManagementAndClosures`).
- **Application Contracts & DTOs (`BookingHotel.Application.Contracts`)**:
  - DTOs: `RoomDashboardDto`, `PhysicalRoomDto`, `CreatePhysicalRoomDto`, `UpdatePhysicalRoomDto`, `DeactivateRoomDto`, `CheckAffectedBookingsInputDto`, `AffectedBookingDto`, `CreatePlannedClosureDto`, `CreateEmergencyClosureDto`, `RoomClosureDto`, `ResolveAffectedBookingDto`, `ConfirmRoomRecoveryDto`.
- **Application Layer (`BookingHotel.Application`)**:
  - Mở rộng `ManagerRoomAppService`:
    - `GetDashboardOverviewAsync(Guid hotelId)`: Tính toán KPI thời gian thực.
    - `GetPhysicalRoomsAsync(...)`: Lọc theo Tầng, Hạng phòng, Occupancy, Operational, Search.
    - `CreatePhysicalRoomAsync(...)`, `UpdatePhysicalRoomAsync(...)`.
    - `DeactivateRoomAsync(...)`: Kiểm tra `Occupied` và booking tương lai trước khi khóa.
    - `DeletePhysicalRoomAsync(...)`: Kiểm tra chưa từng phát sinh booking trước khi xóa.
    - `CheckAffectedBookingsAsync(...)`: Quét xung đột thời gian $[T_{start}, T_{end}]$.
    - `CreatePlannedClosureAsync(...)`: Lưu lịch và đánh dấu xung đột.
    - `CreateEmergencyClosureAsync(...)`: Đổi phòng thành `OutOfService`, quét booking, bắn Email cảnh báo cho khách và notification cho Tenant Admin & Lễ tân.
    - `GetClosureHistoryAsync(...)`: Trích xuất lịch sử và vết xử lý.
    - `ResolveAffectedBookingAsync(...)`: Ghi nhận phương án đổi phòng, nâng hạng, hoàn tiền.
    - `ConfirmRoomRecoveryAsync(...)`: Nghiệm thu khôi phục phòng về `Active`.
  - Tích hợp `IBackgroundJobManager` đẩy tác vụ gửi email thông báo sự cố cho khách hàng chạy nền bất đồng bộ, cập nhật trường `IsCustomerNotified`.
  - Tích hợp API tự động gợi ý phòng thay thế khi Đổi phòng (cùng hạng) hoặc Nâng hạng (hạng cao hơn) còn trống trong cùng khách sạn.
- **Frontend Layer (`BookingHotel/angular`)**:
  - Mở rộng `/admin/manager/rooms`:
    - Component `RoomDashboardComponent` (KPI cards: Total, Occupied, Vacant, Reserved, Unavailable, Occupancy breakdown, Operational breakdown).
    - Mở rộng `RoomListComponent`: Chuyên biệt dạng **Lưới Thẻ theo Tầng (Room Cards grouped by Floor)** trực quan, hiển thị huy hiệu 2 chiều Occupancy/Operational và thời gian đóng phòng nếu có.
    - Component `RoomClosureModalComponent` (Tab Planned Closure & Tab Emergency Closure kèm xác nhận cảnh báo).
    - Component `AffectedBookingsModalComponent` (Danh sách booking sự cố kèm dropdown gợi ý 1-click phòng thay thế khi Đổi phòng / Nâng hạng, Hoàn tiền).
    - Component `ClosureHistoryComponent` (Tra cứu lịch sử đóng phòng và vết kiểm toán).
    - Cập nhật đa ngôn ngữ `vi.json` và `en.json`.

---

## 2. Các Giai Đoạn Triển Khai (Phases of Work)

### Phase 1: Backend Domain & Data Modeling
1. Khai báo các enum và mở rộng Entity `Room`.
2. Tạo mới Entity `RoomClosure` và `AffectedBookingResolution`.
3. Đăng ký DbContext, tạo và áp dụng migration EF Core `AddRoomManagementAndClosures`.

### Phase 2: Application Services & Business Logic (BR-01 đến BR-22)
1. Xây dựng DTOs và Interface trong `Application.Contracts`.
2. Hiện thực hóa các phương thức trong `ManagerRoomAppService`.
3. Tích hợp cơ chế quét booking bị ảnh hưởng giao cắt $[T_{start}, T_{end}]$.
4. Tích hợp gửi email thông báo tự động cho khách hàng khi Emergency Closure kích hoạt.

### Phase 3: Angular Service & UI Components
1. Mở rộng `ManagerRoomService` trên Angular.
2. Thiết kế và tích hợp Dashboard KPI buồng phòng.
3. Bổ sung bộ lọc đa chiều (Tầng, Hạng phòng, Trạng thái sử dụng, Trạng thái vận hành) và hành động Deactivate / Delete an toàn.
4. Xây dựng Modal Lập lịch Planned Closure & Modal Kích hoạt Emergency Closure.
5. Xây dựng giao diện Điều phối & Xử lý Booking Sự cố (Room Change, Room Upgrade, Refund).
6. Tích hợp đồng bộ ngữ cảnh khách sạn (`PropertyContextService`) và bản địa hóa song ngữ.

### Phase 4: Verification & Acceptance Testing
1. Kiểm tra build backend (`dotnet build`).
2. Kiểm tra build frontend (`ng build`).
3. Kiểm tra tính toàn vẹn kiểm toán (Audit Trail) và ranh giới bảo mật Multi-Tenant Isolation.
