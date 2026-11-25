# Data Model: RM-006 - Phân Tách Không Gian & Thiết Kế Giao Diện Quản Lý Theo 4 Vai Trò

**Feature Branch**: `006-role-based-management`  
**Date**: 2026-11-13  
**Status**: Completed  
**Spec**: [specs/006-role-based-management/spec.md](spec.md)

---

## 1. Sơ Đồ Thực Thể Quan Hệ (Entity-Relationship Diagram)

```mermaid
erDiagram
    IdentityTenant ||--o{ IdentityUser : "contains"
    IdentityTenant ||--o{ Hotel : "owns"
    IdentityTenant ||--o{ OperationalApprovalTicket : "scopes"
    
    IdentityUser ||--o{ UserHotelAssignment : "assigned"
    Hotel ||--o{ UserHotelAssignment : "operated_by"
    
    Hotel ||--o{ OperationalApprovalTicket : "applies_to"
    IdentityUser ||--o{ OperationalApprovalTicket : "requests"
    IdentityUser ||--o{ OperationalApprovalTicket : "reviews"
    
    Booking ||--o{ OperationalApprovalTicket : "associated_with"

    OperationalApprovalTicket {
        Guid Id PK
        Guid TenantId FK "Nullable (Multi-Tenancy Scoped)"
        Guid HotelId FK "Khách sạn phát sinh ngoại lệ"
        int TicketType "1=DiscountOverride, 2=PenaltyWaiver, 3=EarlyCheckIn, 4=LateCheckOut, 5=RoomUpgrade"
        int Status "0=Pending, 1=Approved, 2=Rejected, 3=Cancelled"
        Guid RequestedByStaffId FK "Nhân viên lễ tân gửi yêu cầu"
        string RequestedByStaffName "Tên hiển thị nhân viên"
        Guid BookingId FK "Mã đơn đặt phòng liên quan (nullable)"
        Guid FolioId "Mã hóa đơn tạm liên quan (nullable)"
        string RoomNumber "Số phòng tác nghiệp"
        decimal Amount "Số tiền hoặc giá trị ngoại lệ"
        decimal DiscountPercent "Tỷ lệ chiết khấu (%) nếu có"
        string Reason "Lý do xin phê duyệt ngoại lệ"
        Guid ReviewedByManagerId FK "Quản lý khách sạn duyệt (nullable)"
        string ReviewedByManagerName "Tên hiển thị người duyệt"
        DateTime ReviewedAt "Thời điểm phê duyệt/từ chối"
        string RejectionReason "Lý do từ chối (bắt buộc nếu Rejected)"
        DateTime CreationTime "Audit: Thời điểm tạo"
        Guid CreatorId "Audit: Người tạo"
    }

    UserHotelAssignment {
        Guid Id PK
        Guid TenantId FK
        Guid UserId FK
        Guid HotelId FK
        bool IsPrimary
        bool IsActive
        DateTime AssignedAt
        DateTime UnassignedAt
    }

    Hotel {
        Guid Id PK
        Guid TenantId FK
        string Name
        string Code
        string Address
        bool IsActive
    }

    Booking {
        Guid Id PK
        Guid TenantId FK
        Guid HotelId FK
        string BookingCode
        int Status
        decimal TotalAmount
    }
```

---

## 2. Chi Tiết Thực Thể `OperationalApprovalTicket`

Thực thể kế thừa `FullAuditedEntity<Guid>` và triển khai giao diện `IMultiTenant`:

| Tên trường | Kiểu dữ liệu | Ràng buộc | Mô tả |
| :--- | :--- | :--- | :--- |
| `Id` | `Guid` | Primary Key | Định danh duy nhất của phiếu yêu cầu phê duyệt. |
| `TenantId` | `Guid?` | Foreign Key (Index) | Định danh Tenant (Multi-Tenancy Isolation). |
| `HotelId` | `Guid` | Foreign Key (Index) | Cơ sở khách sạn xảy ra nghiệp vụ ngoại lệ. |
| `TicketType` | `ApprovalTicketType` | Required, Enum (int) | Loại ngoại lệ: `DiscountOverride` (1), `PenaltyWaiver` (2), `EarlyCheckIn` (3), `LateCheckOut` (4), `RoomUpgrade` (5). |
| `Status` | `ApprovalTicketStatus` | Required, Enum (int) | Trạng thái phiếu: `Pending` (0), `Approved` (1), `Rejected` (2), `Cancelled` (3). |
| `RequestedByStaffId` | `Guid` | Foreign Key (Index) | ID tài khoản Lễ tân gửi yêu cầu. |
| `RequestedByStaffName` | `string` | MaxLen(256) | Họ tên Lễ tân (snapshot để hiển thị nhanh). |
| `BookingId` | `Guid?` | Nullable, Index | ID đơn đặt phòng phát sinh ngoại lệ. |
| `FolioId` | `Guid?` | Nullable | ID bảng kê chi tiêu / hóa đơn buồng phòng. |
| `RoomNumber` | `string?` | MaxLen(32) | Số phòng liên quan đến ngoại lệ. |
| `Amount` | `decimal?` | Precision(18,2) | Số tiền điều chỉnh hoặc giá trị xin miễn giảm. |
| `DiscountPercent` | `decimal?` | Precision(5,2) | Tỷ lệ chiết khấu (%) xin áp dụng. |
| `Reason` | `string` | Required, MaxLen(1000) | Giải trình chi tiết lý do cần phê duyệt ngoại lệ. |
| `ReviewedByManagerId` | `Guid?` | Nullable | ID tài khoản Hotel Manager duyệt hoặc từ chối. |
| `ReviewedByManagerName` | `string?` | MaxLen(256) | Họ tên người quản lý phê duyệt. |
| `ReviewedAt` | `DateTime?` | Nullable | Thời điểm thực hiện hành động duyệt/từ chối. |
| `RejectionReason` | `string?` | MaxLen(1000) | Lý do từ chối (bắt buộc khi trạng thái là `Rejected`). |

### Chỉ Mục & Tối Ưu Hóa (Indexes)
- `IX_OperationalApprovalTickets_TenantId_HotelId_Status`: Phục vụ truy vấn danh sách ticket chờ duyệt theo từng khách sạn của Hotel Manager.
- `IX_OperationalApprovalTickets_RequestedByStaffId_Status`: Phục vụ Lễ tân tra cứu các ticket mình vừa gửi tại quầy.
- `IX_OperationalApprovalTickets_BookingId`: Phục vụ kiểm tra lịch sử ngoại lệ của một đơn đặt phòng.

---

## 3. Máy Trạng Thái Của Phiếu Duyệt Ngoại Lệ (State Machine)

```mermaid
stateDiagram-v2
    [*] --> Pending: Lễ tân tạo ticket tại quầy (Check-in/Folio)
    
    Pending --> Approved: Hotel Manager nhấn "Chấp thuận"
    Pending --> Rejected: Hotel Manager nhấn "Từ chối" (Kèm lý do)
    Pending --> Cancelled: Lễ tân tự hủy yêu cầu (nếu khách đổi ý)
    
    Approved --> [*]: Folio tự động cập nhật chính sách mới
    Rejected --> [*]: Lễ tân nhận thông báo lý do từ chối
    Cancelled --> [*]: Phiếu đóng, không thay đổi hóa đơn
```

---

## 4. Mô Hình Quản Lý Ngữ Cảnh Cơ Sở (`PropertyContextService` State Model)

```mermaid
classDiagram
    class PropertyContextService {
        +Signal~string?~ currentHotelId
        +Signal~HotelItemDto[]~ assignedHotels
        +Signal~HotelItemDto?~ currentHotel
        +Signal~bool~ isLoading
        +initialize(): Promise~void~
        +setActiveHotel(hotelId: string): void
        +refreshAssignedHotels(): Promise~void~
    }

    class HotelItemDto {
        +string id
        +string name
        +string code
        +string address
        +bool isPrimary
    }

    PropertyContextService --> HotelItemDto : holds collection
```

- **Lưu trữ Persistent**:
  - Khóa: `bh_active_hotel_id`.
  - Cơ chế đồng bộ: Khi `setActiveHotel(id)` được gọi, Signal `currentHotelId` phát tín hiệu thay đổi đến toàn bộ component đang lắng nghe và cập nhật giá trị mới vào `localStorage`.

---

## 5. Sơ Đồ Tuần Hoàn Tác Nghiệp (Sequence Diagrams)

### 5.1. Định Tuyến Tự Động Sau Đăng Nhập (`RoleBasedRedirectGuard`)

```mermaid
sequenceDiagram
    autonumber
    actor User as Người dùng
    participant Router as Angular Router (/admin)
    participant Guard as RoleBasedRedirectGuard
    participant Auth as AuthService / TokenClaims
    participant Portal as Destination Portal

    User->>Router: Truy cập URL /admin
    Router->>Guard: canActivate()
    Guard->>Auth: Lấy danh sách Roles từ Token
    alt Là SystemAdmin
        Guard-->>Router: Redirect to /admin/system/dashboard
    else Là TenantAdmin
        Guard-->>Router: Redirect to /admin/tenant/dashboard
    else Có cả HotelManager & Receptionist
        Guard-->>Router: Redirect to /admin/manager/dashboard (Ưu tiên quyền cao hơn)
    else Chỉ là HotelManager
        Guard-->>Router: Redirect to /admin/manager/dashboard
    else Chỉ là Receptionist
        Guard-->>Router: Redirect to /admin/frontdesk/room-rack
    else Khách hàng (Customer) hoặc không có quyền
        Guard-->>Router: Redirect to / (Customer Home)
    end
    Router->>Portal: Mount Layout & Components tương ứng
```

### 5.2. Chuyển Đổi Không Gian Cho Nhân Sự Kiêm Nhiệm (Dual-Role Switcher)

```mermaid
sequenceDiagram
    autonumber
    actor Manager as Quản lý (Kiêm Lễ tân)
    participant Topbar as HotelManagerLayout Topbar
    participant Switcher as BhPortalSwitcherComponent
    participant Router as Angular Router
    participant Frontdesk as ReceptionistLayout (/admin/frontdesk/room-rack)

    Manager->>Topbar: Nhìn thấy nút "[ Quầy Lễ Tân (Frontdesk) 🡒 ]"
    Manager->>Switcher: Nhấp nút chuyển đổi
    Switcher->>Router: navigateByUrl('/admin/frontdesk/room-rack')
    Router->>Frontdesk: Hủy HotelManagerLayout, Khởi tạo ReceptionistLayout
    Note over Frontdesk: Áp dụng bảng màu Lễ tân (#99FF99, #808080)<br>Hiển thị nút "[ 🡐 Bàn Quản Lý (Manager) ]" trên Topbar
```

### 5.3. Quy Trình Tạo & Phê Duyệt Phiếu Ngoại Lệ (Approval Ticket Workflow)

```mermaid
sequenceDiagram
    autonumber
    actor Rec as Lễ tân (Quầy)
    participant RecUI as Folio / Check-in Modal
    participant AppService as OperationalApprovalAppService
    participant DB as SQL Server (ApprovalTickets)
    actor Mgr as Hotel Manager
    participant MgrUI as Manager Approvals Screen

    Rec->>RecUI: Bấm "Yêu cầu chiết khấu 10%"
    RecUI->>AppService: POST /api/app/operational-approval (CreateApprovalTicketDto)
    AppService->>DB: INSERT Ticket (Status = Pending)
    AppService-->>RecUI: Trả về TicketId (Đang chờ duyệt)
    
    Note over MgrUI: Manager mở /admin/manager/approvals
    MgrUI->>AppService: GET /api/app/operational-approval?hotelId={currentHotelId}&status=Pending
    AppService->>DB: SELECT Tickets WHERE HotelId = ... AND Status = 0
    DB-->>AppService: Danh sách Ticket chờ duyệt
    AppService-->>MgrUI: Hiển thị danh sách phiếu
    
    Mgr->>MgrUI: Nhấn "Chấp thuận" (Approve)
    MgrUI->>AppService: POST /api/app/operational-approval/{id}/approve
    AppService->>DB: UPDATE Ticket SET Status = 1, ReviewedBy = MgrId, ReviewedAt = Now
    DB-->>AppService: Thành công
    AppService-->>MgrUI: Cập nhật giao diện (Ticket đã duyệt)
    
    Note over RecUI: Lễ tân tải lại Folio -> Mức chiết khấu 10% được áp dụng tự động
```

