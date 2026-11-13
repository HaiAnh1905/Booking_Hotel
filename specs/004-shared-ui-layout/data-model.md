# Mô Hình Dữ Liệu & Hợp Đồng Giao Diện (Phase 1: Data Model): RM-004

**Feature Branch**: `004-shared-ui-layout`  
**Date**: 2026-11-13  
**Status**: Completed  
**Spec Reference**: [spec.md](spec.md)

---

## 1. Giới Thiệu & Phạm Vi (Overview)

Tài liệu này định nghĩa cấu trúc dữ liệu, các kiểu dữ liệu TypeScript (TypeScript Interfaces & Types) và các hợp đồng trạng thái giao diện (UI State Contracts) dùng chung cho toàn bộ phân hệ **RM-004 - Shared UI & Layout**.

Các mô hình được thiết kế theo nguyên tắc:
- **Strict TypeScript**: Đảm bảo an toàn kiểu dữ liệu thời gian biên dịch (Compile-time Type Safety).
- **Phân tách trách nhiệm (Separation of Concerns)**: Tách bạch giữa trạng thái hiển thị (Presentation State) và dữ liệu nghiệp vụ (Business Data).
- **Đa ngôn ngữ sẵn sàng (i18n-Ready)**: Mọi chuỗi nhãn hiển thị đều gắn với khóa bản địa hóa (Localization Key) thay vì chuỗi cứng.

---

## 2. Mô Hình Khung Điều Hướng & Trạng Thái Bố Cục (Navigation & Layout Models)

### 2.1 Mục Menu Điều Hướng (Navigation Item)
Đại diện cho một liên kết hoặc nhánh menu hiển thị trên Header (Customer) hoặc Sidebar (Management).

```typescript
export interface NavigationItem {
  /** Đường dẫn tuyến đường Angular */
  route: string;
  /** Khóa bản địa hóa hiển thị nhãn menu (ví dụ: '::Menu:Home') */
  labelKey: string;
  /** Lớp icon FontAwesome (ví dụ: 'fa-solid fa-hotel') */
  icon?: string;
  /** Quyền hạn ABP bắt buộc để hiển thị mục này (từ BookingHotelPermissions) */
  requiredPermission?: string;
  /** Danh sách vai trò được phép truy cập (Customer, HostAdmin, TenantAdmin, HotelManager, Receptionist) */
  allowedRoles?: string[];
  /** Đánh dấu mục có danh sách con hay không */
  children?: NavigationItem[];
  /** Huy hiệu số lượng thông báo (nếu có) */
  badgeCount?: number;
  /** Loại biến thể màu của huy hiệu */
  badgeVariant?: 'accent' | 'danger' | 'warning' | 'info';
  /** Trạng thái mở/đóng danh mục con trên Sidebar */
  isExpanded?: boolean;
}
```

### 2.2 Ngữ Cảnh Người Dùng Trên Header / Topbar (User Header Context)
Lưu trữ thông tin hiển thị của người dùng đang đăng nhập trên thanh công cụ.

```typescript
export interface UserHeaderContext {
  /** Người dùng đã xác thực hay chưa */
  isAuthenticated: boolean;
  /** Họ và tên hiển thị */
  fullName: string;
  /** Địa chỉ Email */
  email: string;
  /** Tên đăng nhập */
  userName: string;
  /** Đường dẫn ảnh đại diện (avatar) nếu có */
  avatarUrl?: string;
  /** Danh sách tên vai trò hiện tại (Roles) */
  roles: string[];
  /** Tên Tenant hiện tại (null nếu là Host) */
  tenantName?: string | null;
  /** Mã định danh Tenant */
  tenantId?: string | null;
}
```

### 2.3 Cấu Hình Chuyển Đổi Ngôn Ngữ (Language Option)
Đại diện cho một ngôn ngữ được hệ thống hỗ trợ.

```typescript
export interface LanguageOption {
  /** Mã văn hóa ABP / ISO (ví dụ: 'vi', 'en') */
  cultureName: string;
  /** Tên hiển thị đầy đủ (ví dụ: 'Tiếng Việt', 'English') */
  displayName: string;
  /** Mã viết tắt hiển thị trên nút thanh công cụ (ví dụ: 'VI', 'EN') */
  shortCode: string;
  /** Biểu tượng cờ quốc gia (SVG hoặc Unicode / CSS class) */
  flagIcon: string;
}
```

---

## 3. Mô Hình Thư Viện Thành Phần Dùng Chung (Shared UI Component Models)

### 3.1 Bảng Dữ Liệu Dùng Chung (BhDataTable Models)

```typescript
export type ColumnAlignment = 'left' | 'center' | 'right';

export interface BhDataTableColumn<T = any> {
  /** Khóa thuộc tính trong đối tượng dữ liệu */
  field: keyof T | string;
  /** Khóa bản địa hóa tiêu đề cột (ví dụ: '::Admin:Table:FullName') */
  headerKey: string;
  /** Cột có cho phép sắp xếp hay không */
  sortable?: boolean;
  /** Độ rộng cột (ví dụ: '150px', '20%') */
  width?: string;
  /** Căn lề nội dung */
  align?: ColumnAlignment;
  /** Tên slot template tùy biến (ng-template) nếu muốn tự render ô */
  cellTemplate?: string;
}

export interface BhPaginationState {
  /** Trang hiện tại (bắt đầu từ 1) */
  currentPage: number;
  /** Số bản ghi trên một trang */
  pageSize: number;
  /** Tổng số bản ghi thực tế */
  totalCount: number;
  /** Tên trường đang sắp xếp */
  sortField?: string;
  /** Chiều sắp xếp ('asc' | 'desc') */
  sortDirection?: 'asc' | 'desc';
}

export interface BhPageChangeEvent {
  page: number;
  pageSize: number;
}
```

### 3.2 Hộp Thoại Xác Nhận (BhModal Models)

```typescript
export type ModalSize = 'sm' | 'md' | 'lg' | 'xl';
export type ModalButtonVariant = 'primary' | 'danger' | 'warning' | 'secondary' | 'accent';

export interface BhModalConfig {
  /** Tiêu đề hộp thoại (hoặc khóa dịch) */
  titleKey: string;
  /** Nội dung thông điệp chi tiết (hoặc khóa dịch) */
  messageKey?: string;
  /** Kích thước hộp thoại */
  size?: ModalSize;
  /** Icon minh họa tiêu đề (ví dụ: 'fa-solid fa-triangle-exclamation') */
  iconClass?: string;
  /** Nhãn nút Xác nhận (mặc định: '::Common:Confirm') */
  confirmTextKey?: string;
  /** Nhãn nút Hủy bỏ (mặc định: '::Common:Cancel') */
  cancelTextKey?: string;
  /** Kiểu màu của nút Xác nhận */
  confirmVariant?: ModalButtonVariant;
  /** Ẩn nút Hủy (chỉ hiển thị thông báo đóng) */
  hideCancelButton?: boolean;
}
```

### 3.3 Huy Hiệu Trạng Thái (BhStatusBadge Models)

```typescript
export type StatusBadgeVariant = 
  | 'available'    // Phòng Sạch/Sẵn sàng: #99FF99, text #000000
  | 'dirty'        // Phòng Bẩn/Chờ dọn: #808080, text #FFFFFF
  | 'occupied'     // Phòng Có khách: #990000, text #FFFFFF
  | 'maintenance'  // Phòng Bảo trì: #FFA500, text #000000
  | 'active'       // Đang hoạt động: bg-success
  | 'inactive'     // Tạm dừng: bg-secondary
  | 'locked'       // Bị khóa: bg-danger
  | 'pending';     // Chờ duyệt: bg-warning

export interface BhStatusBadgeConfig {
  /** Biến thể kiểu dáng */
  variant: StatusBadgeVariant;
  /** Khóa bản địa hóa nội dung chữ trên huy hiệu */
  textKey: string;
  /** Icon biểu tượng kèm theo */
  iconClass?: string;
  /** Dạng hiển thị tròn chấm điểm (dot indicator) */
  isDot?: boolean;
}
```

### 3.4 Dịch Vụ Thông Báo Nổi (BhToastr Models)

```typescript
export type ToastType = 'success' | 'error' | 'warning' | 'info';

export interface BhToastMessage {
  /** Định danh duy nhất của thông báo */
  id: string;
  /** Phân loại thông báo */
  type: ToastType;
  /** Tiêu đề thông báo */
  titleKey?: string;
  /** Nội dung thông báo */
  messageKey: string;
  /** Các tham số nội suy nếu có */
  messageParams?: any[];
  /** Thời gian tự biến mất tính bằng mili-giây (mặc định: 4000) */
  durationMs: number;
  /** Thời điểm tạo */
  timestamp: Date;
}
```

---

## 4. Mô Hình Nghiệp Vụ Trực Quan Cho Các Màn Hình Mẫu (Showcase & Preview Models)

### 4.1 Thẻ Buồng Phòng Sơ Đồ Phòng Mẫu (Room Rack Card Model)

```typescript
export type RoomRackStatus = 'Available' | 'Dirty' | 'Occupied' | 'Maintenance';

export interface RoomRackCardItem {
  /** Mã định danh phòng */
  roomId: string;
  /** Số phòng hiển thị (ví dụ: '101', '205', 'VIP-301') */
  roomNumber: string;
  /** Tên hạng phòng (ví dụ: 'Deluxe Ocean View') */
  roomTypeNameKey: string;
  /** Số tầng */
  floor: number;
  /** Trạng thái hiện tại */
  status: RoomRackStatus;
  /** Giá niêm yết theo đêm (VND) */
  pricePerNight: number;
  /** Tên khách đang lưu trú (nếu trạng thái là Occupied) */
  guestName?: string;
  /** Ngày dự kiến trả phòng (nếu trạng thái là Occupied) */
  checkOutDate?: string;
  /** Số lượng khách tối đa */
  capacity: number;
}
```

### 4.2 Tiêu Chí Tìm Kiếm Phòng Trang Chủ (Search Room Criteria)

```typescript
export interface SearchRoomCriteria {
  /** Địa điểm hoặc Cơ sở khách sạn được chọn */
  destinationOrHotelId: string;
  /** Ngày nhận phòng (ISO format: YYYY-MM-DD) */
  checkInDate: string;
  /** Ngày trả phòng (ISO format: YYYY-MM-DD) */
  checkOutDate: string;
  /** Số lượng người lớn */
  adults: number;
  /** Số lượng trẻ em */
  children: number;
  /** Số lượng phòng cần đặt */
  rooms: number;
}
```

---

## 5. Quy Chuẩn Tài Nguyên Bản Địa Hóa (Localization Schema)

Tất cả các khóa giao diện dùng chung tuân thủ cấu trúc tiền tố phân cấp:
- `::Common:*` (Các nhãn dùng chung: Xác nhận, Hủy, Lưu, Đóng, Tìm kiếm, v.v.)
- `::Menu:*` (Các mục điều hướng: Trang chủ, Khách sạn, Ưu đãi, Quản trị, v.v.)
- `::Auth:*` (Các nhãn xác thực đã chuẩn bị từ RM-001/RM-002)
- `::Admin:*` (Các nhãn quản trị: Dashboard, Quản lý tài khoản, Báo cáo, v.v.)
- `::RoomRack:*` (Các trạng thái phòng: Phòng sạch, Phòng bẩn, Có khách, Bảo trì)
- `::Landing:*` (Các nhãn cổng đặt phòng khách hàng)

Hai tệp `vi.json` và `en.json` phải có 100% khóa đối ứng khớp nhau chính xác.
