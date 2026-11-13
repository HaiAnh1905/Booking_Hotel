# Giao Diện Lập Trình Thành Phần (Contracts: UI Components API): RM-004

**Feature Branch**: `004-shared-ui-layout`  
**Date**: 2026-11-13  
**Status**: Completed  
**Spec Reference**: [spec.md](../spec.md)

---

## 1. Thành Phần Bảng Dữ Liệu (`BhDataTableComponent`)

- **Selector**: `<bh-data-table>`
- **Export As**: `BhDataTableComponent` (Standalone)
- **Mục đích**: Bảng dữ liệu chuẩn cho các màn hình quản trị, hỗ trợ phân trang, sắp xếp và responsive.

### Inputs:
| Tên Thuộc Tính | Kiểu Dữ Liệu | Mặc Định | Mô Tả |
| :--- | :--- | :---: | :--- |
| `columns` | `BhDataTableColumn[]` | `[]` | Cấu hình danh sách các cột |
| `items` | `any[]` | `[]` | Mảng dữ liệu nguồn cần hiển thị |
| `loading` | `boolean` | `false` | Hiển thị lớp phủ đang tải dữ liệu |
| `pagination` | `BhPaginationState` | `null` | Cấu hình phân trang (nếu có) |
| `emptyTextKey` | `string` | `'::Common:NoData'` | Khóa dịch khi mảng items rỗng |
| `striped` | `boolean` | `true` | Bật/tắt hiệu ứng dòng xen kẽ zebra |
| `bordered` | `boolean` | `false` | Bật viền ô |

### Outputs:
| Tên Sự Kiện | Kiểu Dữ Liệu | Mô Tả |
| :--- | :--- | :--- |
| `pageChange` | `EventEmitter<BhPageChangeEvent>` | Phát ra khi người dùng chuyển trang hoặc đổi pageSize |
| `sortChange` | `EventEmitter<{ field: string; direction: 'asc' \| 'desc' }>` | Phát ra khi nhấp vào cột có thể sắp xếp |

### Ví Dụ Sử Dụng:
```html
<bh-data-table
  [columns]="columns"
  [items]="users"
  [loading]="isLoading"
  [pagination]="paginationState"
  (pageChange)="onPageChanged($event)"
  (sortChange)="onSortChanged($event)">
  
  <!-- Custom Slot Template cho cột Actions -->
  <ng-template #cellTemplate let-item let-column="column">
    @if(column.field === 'actions') {
      <button class="btn btn-sm btn-outline-primary" (click)="editUser(item)">
        <i class="fa fa-pencil"></i>
      </button>
    }
  </ng-template>
</bh-data-table>
```

---

## 2. Thành Phần Hộp Thoại Xác Nhận (`BhModalComponent`)

- **Selector**: `<bh-modal>`
- **Export As**: `BhModalComponent` (Standalone)
- **Mục đích**: Hộp thoại popup xác nhận hành động nguy hiểm hoặc hiển thị form phụ trợ với backdrop làm mờ.

### Inputs:
| Tên Thuộc Tính | Kiểu Dữ Liệu | Mặc Định | Mô Tả |
| :--- | :--- | :---: | :--- |
| `isOpen` | `boolean` | `false` | Điều khiển hiển thị modal |
| `config` | `BhModalConfig` | `{ ... }` | Cấu hình tiêu đề, icon, nhãn nút, variant |
| `busy` | `boolean` | `false` | Trạng thái nút xác nhận đang xử lý (loading spinner) |

### Outputs:
| Tên Sự Kiện | Kiểu Dữ Liệu | Mô Tả |
| :--- | :--- | :--- |
| `confirm` | `EventEmitter<void>` | Phát ra khi người dùng nhấn nút Xác nhận |
| `cancel` | `EventEmitter<void>` | Phát ra khi người dùng nhấn nút Hủy, bấm phím ESC hoặc click backdrop |
| `isOpenChange` | `EventEmitter<boolean>` | Two-way binding cho trạng thái mở modal |

### Ví Dụ Sử Dụng:
```html
<bh-modal
  [(isOpen)]="isConfirmOpen"
  [config]="deleteModalConfig"
  [busy]="isDeleting"
  (confirm)="handleDeleteUser()"
  (cancel)="isConfirmOpen = false">
  
  <p>{{ '::Admin:Users:DeleteWarning' | abpLocalization }}</p>
</bh-modal>
```

---

## 3. Thành Phần Huy Hiệu Trạng Thái (`BhStatusBadgeComponent`)

- **Selector**: `<bh-status-badge>`
- **Export As**: `BhStatusBadgeComponent` (Standalone)
- **Mục đích**: Hiển thị nhãn trạng thái đồng nhất màu sắc (đặc biệt là 4 trạng thái phòng của Lễ tân).

### Inputs:
| Tên Thuộc Tính | Kiểu Dữ Liệu | Mặc Định | Mô Tả |
| :--- | :--- | :---: | :--- |
| `variant` | `StatusBadgeVariant` | `'active'` | Biến thể màu sắc (`available`, `dirty`, `occupied`, `maintenance`,...) |
| `textKey` | `string` | `''` | Khóa dịch nội dung hiển thị |
| `iconClass` | `string` | `''` | Icon fontawesome (tùy chọn) |
| `isDot` | `boolean` | `false` | Hiển thị chấm tròn chỉ báo thay vì khối |

### Bảng Mã Màu & Kiểu Dáng Biến Thể:
| Variant | Nền (Background) | Màu Chữ (Text) | Ý Nghĩa Nghiệp Vụ |
| :--- | :---: | :---: | :--- |
| `available` | `#99FF99` | `#000000` | Phòng Sạch / Sẵn sàng đón khách |
| `dirty` | `#808080` | `#FFFFFF` | Phòng Bẩn / Chờ nhân viên buồng dọn dẹp |
| `occupied` | `#990000` | `#FFFFFF` | Phòng Đang có khách lưu trú |
| `maintenance` | `#FFA500` | `#000000` | Phòng Đang sửa chữa / Tạm khóa |
| `active` | `#d1e7dd` | `#0f5132` | Tài khoản / Bản ghi đang hoạt động |
| `locked` | `#f8d7da` | `#842029` | Tài khoản bị tạm khóa / vô hiệu hóa |

---

## 4. Thành Phần Chuyển Đổi Ngôn Ngữ (`BhLanguageSwitcherComponent`)

- **Selector**: `<bh-language-switcher>`
- **Export As**: `BhLanguageSwitcherComponent` (Standalone)
- **Mục đích**: Dropdown chuyển đổi ngôn ngữ song ngữ tức thì không reload trang.

### Inputs & Cấu Hình:
- Tự động kết nối với `SessionStateService` và `ConfigStateService` của ABP Core.
- Hỗ trợ tùy chọn chế độ hiển thị: `compact` (chỉ hiển thị mã `VI`/`EN`) hoặc `full` (hiển thị cờ và tên đầy đủ).

---

## 5. Dịch Vụ Thông Báo Nổi (`BhToastrService`)

- **Injectable**: `{ providedIn: 'root' }`
- **Mục đích**: Hiển thị Toast notifications với tone nhận diện hệ thống.

### Các Phương Thức Công Khai:
```typescript
class BhToastrService {
  success(messageKey: string, titleKey?: string, durationMs?: number): void;
  error(messageKey: string, titleKey?: string, durationMs?: number): void;
  warning(messageKey: string, titleKey?: string, durationMs?: number): void;
  info(messageKey: string, titleKey?: string, durationMs?: number): void;
  clearAll(): void;
}
```
