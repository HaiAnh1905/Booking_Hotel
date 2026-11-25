# Route & Portal Contract: RM-006 - Phân Tách Không Gian & Thiết Kế Giao Diện Quản Lý Theo 4 Vai Trò

**Feature Branch**: `006-role-based-management`  
**Date**: 2026-11-13  
**Status**: Completed  
**Spec**: [specs/006-role-based-management/spec.md](../spec.md)

---

## 1. Tổng Quan Cấu Trúc Tuyến Đường Độc Lập (Independent Root Route Namespaces)

Hệ thống phân định 4 không gian làm việc với 4 Root URL hoàn toàn độc lập, xóa bỏ tiền tố `/admin` chung cho các vai trò ngoài System Admin để đảm bảo đúng tính chất nghiệp vụ (DDD):

```text
/ (Root)
│
├── /admin/*         (SystemAdminLayoutComponent - Quản trị Nền tảng Host)
│   ├── /dashboard
│   ├── /tenants
│   ├── /users
│   └── /roles
│
├── /tenant/*        (TenantAdminLayoutComponent - Quản trị Chuỗi / Doanh nghiệp)
│   ├── /dashboard
│   ├── /hotels
│   ├── /staff
│   └── /roles
│
├── /manager/*       (HotelManagerLayoutComponent - Bàn Quản lý Cơ sở & Buồng phòng)
│   ├── /dashboard
│   ├── /rooms
│   ├── /rates
│   ├── /approvals
│   └── /staff
│
└── /frontdesk/*     (ReceptionistLayoutComponent - Quầy Lễ tân & Tác nghiệp Tốc độ cao)
    ├── /room-rack
    ├── /check-in
    ├── /check-out
    └── /folios
```

---

## 2. Đặc Tả Chi Tiết Từng Portal (Portal Specifications)

### 2.1. System Admin Portal (`/admin/*`)
- **Đối tượng**: Host System Admin (`TenantId = null`).
- **Layout Shell**: `SystemAdminLayoutComponent`.
- **Route Guard**: `systemAdminGuard` + `firstLoginPasswordChangeGuard`.
- **Màu sắc & Nhận diện**:
  - Sidebar: Đen `#000000`, Nền chính: Trắng `#FFFFFF`, Điểm nhấn: Cyan `#00FFFF`.
  - Badge định danh: Tím chàm `#7C3AED` (`System Admin - Host Control`).
- **Tuyến đường con**:

| Đường dẫn (URL) | Component nạp (Lazy Loaded) | Quyền tối thiểu (Atomic Permission) | Tiêu đề / Breadcrumb |
| :--- | :--- | :--- | :--- |
| `/admin/dashboard` | `SystemDashboardComponent` | `BookingHotel.Tenants.Default` | `Bảng Điều Khiển Nền Tảng` |
| `/admin/tenants` | `@abp/ng.tenant-management` | `BookingHotel.Tenants.Default` | `Quản Lý Chuỗi Đối Tác (Tenants)` |
| `/admin/users` | `UserListComponent` | `BookingHotel.Identity.UnlockUser` | `Tài Khoản Quản Trị Nền Tảng` |
| `/admin/roles` | `RoleListComponent` | `AbpIdentity.Roles` | `Danh Mục Vai Trò Chuẩn` |

---

### 2.2. Tenant Admin Portal (`/tenant/*`)
- **Đối tượng**: Tenant Admin (`TenantId != null`).
- **Layout Shell**: `TenantAdminLayoutComponent`.
- **Route Guard**: `tenantAdminGuard` + `firstLoginPasswordChangeGuard`.
- **Màu sắc & Nhận diện**:
  - Sidebar: Đen `#000000`, Nền chính: Trắng `#FFFFFF`, Điểm nhấn: Cyan `#00FFFF`.
  - Badge định danh: Vàng hổ phách `#F59E0B` (`Tenant Admin - Chuỗi Khách Sạn`).
- **Tuyến đường con**:

| Đường dẫn (URL) | Component nạp (Lazy Loaded) | Quyền tối thiểu (Atomic Permission) | Tiêu đề / Breadcrumb |
| :--- | :--- | :--- | :--- |
| `/tenant/dashboard` | `TenantDashboardComponent` | `BookingHotel.Reports.Consolidated` | `Tổng Quan Hoạt Động Chuỗi` |
| `/tenant/hotels` | `TenantDashboardComponent` | `BookingHotel.Hotels.Default` | `Danh Mục Khách Sạn / Chi Nhánh` |
| `/tenant/staff` | `UserListComponent` | `BookingHotel.Hotels.AssignStaff` | `Quản Lý Nhân Sự (Manager/Receptionist)` |
| `/tenant/roles` | `RoleListComponent` | `AbpIdentity.Roles` | `Cơ Cấu Quyền Hạn (Chỉ Xem)` |

---

### 2.3. Hotel Manager Portal (`/manager/*`)
- **Đối tượng**: Hotel Manager (`TenantId != null`, được phân công $\ge 1$ chi nhánh).
- **Layout Shell**: `HotelManagerLayoutComponent`.
- **Route Guard**: `hotelManagerGuard` + `firstLoginPasswordChangeGuard`.
- **Màu sắc & Nhận diện**:
  - Sidebar: Đen `#000000`, Nền chính: Trắng `#FFFFFF`, Điểm nhấn: Cyan `#00FFFF`.
  - Topbar: Tích hợp **Property Selector Dropdown** (`PropertyContextService`) và **Portal Switcher Pill** (nếu kiêm nhiệm Lễ tân).
- **Tuyến đường con**:

| Đường dẫn (URL) | Component nạp (Lazy Loaded) | Quyền tối thiểu (Atomic Permission) | Tiêu đề / Breadcrumb |
| :--- | :--- | :--- | :--- |
| `/manager/dashboard` | `ManagerDashboardComponent` | `BookingHotel.Reports.HotelPerformance` | `Bảng Điều Hành Cơ Sở` |
| `/manager/rooms` | `RoomRackPreviewComponent` | `BookingHotel.Rooms.Default` | `Quản Trị Buồng Phòng & Tình Trạng` |
| `/manager/rates` | `ManagerDashboardComponent` | `BookingHotel.Rooms.SetRates` | `Bảng Giá Phòng & Chính Sách Phạt` |
| `/manager/approvals` | `ManagerApprovalsComponent` | `BookingHotel.Bookings.ApproveException` | `Phê Duyệt Ngoại Lệ Tác Nghiệp` |
| `/manager/staff` | `UserListComponent` | `BookingHotel.Identity.Default` | `Nhân Sự Cơ Sở (Chỉ Xem)` |

---

### 2.4. Receptionist Portal (`/frontdesk/*`)
- **Đối tượng**: Receptionist (`TenantId != null`, thuộc 1 cơ sở khách sạn).
- **Layout Shell**: `ReceptionistLayoutComponent`.
- **Route Guard**: `receptionistGuard` + `firstLoginPasswordChangeGuard`.
- **Màu sắc & Nhận diện**:
  - Concept: *Frontdesk Speed Operation* (Xanh lá tươi nhạt `#99FF99`, Xám viền `#808080`, Chữ Đen `#000000`).
  - Đảm bảo độ tương phản WCAG 2.1 AA $\ge 4.5:1$.
  - Topbar: Tích hợp **Portal Switcher Pill** (nếu có vai trò Hotel Manager kiêm nhiệm).
- **Tuyến đường con**:

| Đường dẫn (URL) | Component nạp (Lazy Loaded) | Quyền tối thiểu (Atomic Permission) | Tiêu đề / Breadcrumb |
| :--- | :--- | :--- | :--- |
| `/frontdesk/room-rack` | `RoomRackComponent` | `BookingHotel.Rooms.Default` | `Sơ Đồ Buồng Phòng Trực Quan` |
| `/frontdesk/check-in` | `RoomRackComponent` | `BookingHotel.Bookings.CheckIn` | `Nhận Phòng Nhanh (Fast Check-in)` |
| `/frontdesk/check-out` | `RoomRackComponent` | `BookingHotel.Bookings.CheckOut` | `Trả Phòng & Quyết Toán (Check-out)`|
| `/frontdesk/folios` | `RoomRackComponent` | `BookingHotel.Folios.View` | `Hóa Đơn & Dịch Vụ Đang Mở` |

---

## 3. Điều Hướng Tự Động Sau Đăng Nhập & Chống Ép URL

1. **Điều hướng thông minh sau đăng nhập (`roleBasedRedirectGuard`)**:
   - Khi truy cập `/admin` (hoặc sau khi đăng nhập thành công):
     - `SystemAdmin` $\rightarrow$ `/admin/dashboard`
     - `TenantAdmin` $\rightarrow$ `/tenant/dashboard`
     - `HotelManager` $\rightarrow$ `/manager/dashboard`
     - `Receptionist` $\rightarrow$ `/frontdesk/room-rack`
     - `Customer` $\rightarrow$ `/`

2. **Chặn truy cập trái phép & Fallback an toàn**:
   - Khi người dùng cố gắng gõ URL của phân hệ khác trên thanh địa chỉ trình duyệt:
     - Route Guard kích hoạt Toastr cảnh báo vi phạm bản quyền (HTTP 403).
     - Tự động fallback về đúng Portal hợp lệ tương ứng với vai trò của tài khoản.
