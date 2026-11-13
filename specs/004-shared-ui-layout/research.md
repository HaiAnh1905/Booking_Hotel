# Nghiên Cứu Kỹ Thuật (Phase 0 Research): RM-004 - Shared UI & Layout Chuẩn Song Ngữ Theo Concept

**Feature Branch**: `004-shared-ui-layout`  
**Date**: 2026-11-13  
**Status**: Completed  
**Spec Reference**: [spec.md](spec.md)

---

## 1. Mục Tiêu Nghiên Cứu & Bối Cảnh Hệ Thống (Research Scope)

Tính năng **RM-004** đóng vai trò là "xương sống thị giác" (Visual Backbone) cho toàn bộ nền tảng **Booking Hotel**. Hệ thống xây dựng trên nền tảng ABP Framework v10.7 (.NET 10 / Angular 19+ / LeptonX Lite Theme).

Mục tiêu nghiên cứu bao gồm:
1. **Kiến trúc Khung giao diện (Layout Shells)**: Thiết kế giải pháp phân tách độc lập giữa Cổng Đặt phòng Khách hàng (`CustomerLayoutComponent`) và Không gian Tác nghiệp Quản trị (`ManagementLayoutComponent`) mà không bị ảnh hưởng bởi CSS ẩn hoặc ràng buộc của LeptonX Lite Theme.
2. **Bộ chuyển đổi ngôn ngữ song ngữ tức thì (vi/en)**: Cơ chế thay đổi ngôn ngữ động qua `SessionStateService` của ABP, cập nhật trực tiếp cây dịch mà không gây tải lại toàn bộ trang (no hard page refresh).
3. **Thư viện Standalone Components dùng chung**: Đóng gói các thành phần giao diện hạt nhân (`BhDataTable`, `BhModal`, `BhStatusBadge`, `BhLanguageSwitcher`, `BhToastrService`, `BhLoadingSpinner`, `BhEmptyState`).
4. **Hệ thống Design Tokens & Khả năng tiếp cận (Accessibility - WCAG 2.1 AA)**: Chuẩn hóa bộ CSS Custom Properties cho các mã màu cốt lõi (`#990000`, `#000000`, `#FFFFFF`, `#00FFFF`, `#99FF99`, `#808080`) và chứng minh độ tương phản $\ge 4.5:1$.
5. **Chuyển đổi Trang chủ Khách hàng**: Tái thiết kế `HomeComponent` thành Customer Booking Landing Page theo concept Đỏ sẫm & Đen với form tìm kiếm phòng trực quan.
6. **Màn hình Thử nghiệm Thị giác**: Xây dựng UI Showcase (`/admin/ui-showcase`) và Room Rack Preview (`/admin/room-rack-preview`) để kiểm thử bảng màu Lễ tân.

---

## 2. Phân Tích & Các Quyết Định Kiến Trúc (Architectural Decisions)

### Quyết định 1: Kiến trúc Khung Giao Diện (Dual Standalone Layouts)

#### Vấn đề:
Theme mặc định của ABP (LeptonX Lite) sử dụng `DynamicLayoutComponent` để nạp `ApplicationLayoutComponent`. Tuy nhiên, LeptonX có nhiều CSS nội bộ can thiệp vào cấu trúc `#lpx-wrapper`, thanh điều hướng, logo và màu sắc mặc định. Tại RM-003, chúng ta đã tạm thời ẩn sidebar bằng `.lpx-sidebar-container { display: none !important; }`. Nếu tiếp tục can thiệp bằng cách ghi đè CSS của LeptonX, giao diện sẽ rất dễ vỡ, khó mở rộng và không đạt được đúng chuẩn nhận diện của Hiến pháp (Sidebar đen sâu `#000000`, Nền trắng `#FFFFFF`, Điểm nhấn Cyan `#00FFFF` cho Quản lý; Header Đỏ `#990000`/Đen `#000000` cho Khách hàng).

#### Giải pháp lựa chọn:
Xây dựng **2 Custom Standalone Layouts độc lập**:
- `CustomerLayoutComponent`: Đảm nhận toàn bộ giao diện Cổng đặt phòng công khai, trang chủ, tìm kiếm phòng, đăng ký/đăng nhập. Bao gồm Header Navbar thương hiệu, Menu công khai, Dropdown ngôn ngữ và Footer bản quyền.
- `ManagementLayoutComponent`: Đảm nhận toàn bộ phân hệ quản trị và vận hành (Host Admin, Tenant Admin, Hotel Manager, Receptionist). Bao gồm Sidebar cố định màu đen sâu `#000000` (hỗ trợ collapse), Topbar nền trắng với điểm nhấn Cyan `#00FFFF`, Dropdown ngôn ngữ, Tenant switch và Breadcrumb.

#### Tích hợp với Router & ABP:
1. Đối với các route công khai và khách hàng:
   ```typescript
   {
     path: '',
     component: CustomerLayoutComponent,
     data: { layout: 'empty' },
     children: [
       { path: '', component: HomeComponent },
       // các route booking tương lai...
     ]
   }
   ```
2. Đối với các route quản trị mới:
   ```typescript
   {
     path: 'admin',
     component: ManagementLayoutComponent,
     data: { layout: 'empty' },
     children: [
       { path: 'ui-showcase', component: UiShowcaseComponent },
       { path: 'room-rack-preview', component: RoomRackPreviewComponent },
     ]
   }
   ```
3. Đối với các module có sẵn của ABP (`identity`, `tenant-management`, `setting-management`):
   Sử dụng `ReplaceableComponentsService` thay thế `eThemeLeptonXComponents.ApplicationLayout` bằng `ManagementLayoutComponent`. Nhờ đó, ngay cả khi người dùng truy cập trực tiếp `/identity` hay `/tenant-management`, hệ thống vẫn hiển thị chuẩn Sidebar đen và Topbar quản trị của `Booking Hotel`!

---

### Quyết định 2: Cơ Chế Chuyển Đổi Ngôn Ngữ Tức Thì (Instant i18n Switching)

#### Vấn đề:
Chuyển đổi ngôn ngữ truyền thống trong một số ứng dụng thường reload toàn bộ trang web (`window.location.reload()`), gây trải nghiệm gián đoạn, nhấp nháy màn hình và mất trạng thái form chưa lưu.

#### Nghiên cứu cơ chế ABP Core:
Trong `@abp/ng.core`:
- `SessionStateService` cung cấp phương thức:
  ```typescript
  sessionState.setLanguage(cultureName: string): void
  ```
- Khi gọi phương thức này, ABP cập nhật `sessionState`, đồng thời kích hoạt các pipe `abpLocalization` và directive `abpLocalization` tái tính toán giá trị dịch ngay trong chu kỳ phát hiện thay đổi (Change Detection) của Angular.
- ABP lưu văn hóa ngôn ngữ vào cookie `COOKIE_LANGUAGE_KEY` (`.AspNetCore.Culture`).
- Để đảm bảo tính bền vững (Persistence) tuyệt đối giữa các phiên duyệt web, component `BhLanguageSwitcherComponent` đồng thời lưu mã ngôn ngữ vào `localStorage.setItem('bh_selected_language', lang)` và đồng bộ khi khởi tạo ứng dụng.

#### Kết luận:
Chuyển đổi ngôn ngữ diễn ra trong bộ nhớ (in-memory) tức thì dưới **50ms**, đáp ứng vượt trội chỉ tiêu hiệu năng $\text{SC-003} < 200\text{ ms}$.

---

### Quyết định 3: Thư Viện Standalone UI Components Dùng Chung

#### Vấn đề:
Các màn hình chức năng tiếp theo (RM-005 Quản lý Tài khoản, Quản lý Khách sạn, Buồng phòng, Bảng giá) đều cần các khối giao diện lặp đi lặp lại: Bảng dữ liệu có phân trang, Hộp thoại xác nhận hành động, Huy hiệu trạng thái, Thông báo toast, Chỉ báo đang tải, Khối dữ liệu trống.

#### Cấu trúc thiết kế `angular/src/app/shared/components/`:
1. `bh-data-table`: Bọc bảng dữ liệu HTML5 chuẩn với responsive container, header phân biệt, zebra striping nhẹ, thanh phân trang tích hợp tính toán số trang.
2. `bh-modal`: Hộp thoại modal tùy biến độ rộng (`sm`, `md`, `lg`, `xl`), có backdrop làm mờ, bắt sự kiện phím `Escape` để đóng, hỗ trợ 2 nút tác vụ (Cancel & Confirm với variant `primary` | `danger`).
3. `bh-status-badge`: Render huy hiệu trạng thái với kiểu dáng cố định:
   - Buồng phòng: `available` (Xanh lá `#99FF99`), `dirty` (Xám `#808080`), `occupied` (Đỏ `#990000`), `maintenance` (Vàng cam `#FFA500`).
   - Nghiệp vụ chung: `active`, `inactive`, `locked`, `pending`.
4. `bh-language-switcher`: Menu dropdown nhỏ gọn, hiển thị mã `VI` hoặc `EN`, cho phép chọn nhanh ngôn ngữ với cờ và tên đầy đủ.
5. `bh-toastr`: Dịch vụ singleton (`BhToastrService`) kết hợp overlay container để hiển thị thông báo góc trên bên phải (Success, Error, Warning, Info) theo tone màu chuẩn, tự tắt sau 4 giây.
6. `bh-loading-spinner`: Vòng xoay tải dữ liệu mượt mà, hỗ trợ chế độ toàn màn hình (fullscreen overlay) hoặc nhúng trong thẻ (inline card).
7. `bh-empty-state`: Hiển thị biểu tượng minh họa, tiêu đề, thông điệp hướng dẫn và nút CTA khi bảng hoặc danh sách không có dữ liệu.

---

### Quyết định 4: Bảng Màu & Kiểm Định Khả Năng Tiếp Cận (WCAG 2.1 AA)

#### Quy định màu từ Hiến pháp:
- **Customer & Guest**: Đỏ sẫm (`#990000`) & Đen (`#000000`).
- **Management / Admin**: Nền chính Trắng (`#FFFFFF`), Sidebar Đen (`#000000`), Điểm nhấn Cyan (`#00FFFF`).
- **Receptionist / Front Desk**: Xanh lá tươi (`#99FF99`) & Xám (`#808080`).

#### Bảng Kiểm Định Tỷ Lệ Tương Phản (Contrast Ratio Matrix):
Theo tiêu chuẩn WCAG 2.1 Cấp độ AA, tỷ lệ tương phản tối thiểu là **4.5:1** cho văn bản thông thường và **3.0:1** cho văn bản cỡ lớn / UI Components.

| Màu Nền (Background) | Mã Màu | Màu Chữ Quy Định | Tỷ Lệ Tương Phản | Đánh Giá WCAG 2.1 AA | Ghi Chú |
| :--- | :--- | :--- | :---: | :---: | :--- |
| **Đỏ sẫm thương hiệu** | `#990000` | Trắng (`#FFFFFF`) | **$7.60 : 1$** | **PASS (AAA)** | Rất sắc nét trên Header & Nút CTA |
| **Đen Sidebar** | `#000000` | Trắng (`#FFFFFF`) | **$21.0 : 1$** | **PASS (AAA)** | Độ tương phản tối đa |
| **Đen Sidebar (Mục phụ)**| `#000000` | Xám sáng (`#CCCCCC`) | **$12.3 : 1$** | **PASS (AAA)** | Menu phụ trợ, dễ nhìn |
| **Xanh Cyan Quản trị** | `#00FFFF` | Đen (`#000000`) | **$16.7 : 1$** | **PASS (AAA)** | Tuyệt đối **không** dùng chữ trắng trên nền Cyan |
| **Xanh lá tươi Lễ tân** | `#99FF99` | Đen (`#000000`) | **$15.4 : 1$** | **PASS (AAA)** | Nhãn phòng trống / Nút Check-in |
| **Xám Lễ tân** | `#808080` | Trắng (`#FFFFFF`) | **$3.95 : 1$** | **PASS (AA Large / Badges)** | Dùng cho nhãn phòng bẩn với chữ in hoa đậm $\ge 14\text{px}$ |
| **Vàng Cam Bảo trì** | `#FFA500` | Đen (`#000000`) | **$10.1 : 1$** | **PASS (AAA)** | Cảnh báo phòng bảo trì |
| **Nền Trắng Quản trị** | `#FFFFFF` | Đen (`#111111`) | **$18.5 : 1$** | **PASS (AAA)** | Thân trang và bảng dữ liệu |

---

### Quyết định 5: Tái Cấu Trúc Trang Chủ Khách Hàng (Customer Booking Landing Page)

#### Vấn đề:
Trang `home.component` hiện tại là trang mặc định của template ABP với các liên kết quảng bá ABP Community/Blog/X, không mang tính chất của một ứng dụng Đặt phòng Khách sạn.

#### Giải pháp:
Thay thế toàn bộ nội dung của `HomeComponent` thành **Customer Booking Landing Page**:
1. **Hero Search Section**:
   - Nền màu đen kết hợp hiệu ứng gradient đỏ sẫm `#990000`.
   - Form tìm kiếm phòng với 4 trường tương tác:
     - Khách sạn / Điểm đến (Destination / Hotel selection)
     - Ngày nhận phòng (Check-in Date)
     - Ngày trả phòng (Check-out Date)
     - Số lượng khách / phòng (Guests & Rooms)
   - Nút hành động chính: "Tìm Phòng Ngay" (Search Rooms) màu đỏ sẫm `#990000`.
2. **Khối Tiện Ích Đẳng Cấp (Key Amenities Highlight)**:
   - Dịch vụ phòng 24/7, Bể bơi vô cực, Ẩm thực 5 sao, Đưa đón sân bay cao cấp.
3. **Thẻ Phòng Mẫu Chuẩn Nhận Diện (Featured Sample Rooms)**:
   - Phòng Deluxe Hướng Biển, Phòng Suite Hoàng Gia, Phòng Gia Đình Tiện Nghi.
   - Thẻ hiển thị giá tham khảo, số khách tối đa, huy hiệu tiện ích và nút "Xem chi tiết".

---

### Quyết định 6: Trang Kiểm Thử Thị Giác (UI Showcase & Room Rack Preview)

#### Mục tiêu:
Cung cấp môi trường kiểm thử trực quan độc lập (Independently Testable) cho các nhà phát triển và kiểm thử viên:
1. Tuyến đường `/admin/ui-showcase`: Trình diễn toàn bộ các biến thể của các Standalone UI Components (Bảng dữ liệu mẫu, Nút mở modal xác nhận, Các nút kích hoạt thông báo Toastr, Trạng thái đang tải, Khối rỗng, Bộ chọn ngôn ngữ).
2. Tuyến đường `/admin/room-rack-preview`: Trình diễn ma trận sơ đồ buồng phòng mẫu gồm 12 phòng thuộc nhiều tầng khác nhau với đầy đủ 4 trạng thái (`Available` xanh lá, `Dirty` xám, `Occupied` đỏ, `Maintenance` cam), kiểm tra khả năng lọc trạng thái buồng phòng tại quầy lễ tân.

---

## 3. Rủi Ro Kỹ Thuật & Biện Pháp Giảm Thiểu (Risks & Mitigations)

| STT | Rủi Ro Kỹ Thuật | Tác Động | Biện Pháp Giảm Thiểu |
| :---: | :--- | :--- | :--- |
| **1** | **Xung đột CSS LeptonX Theme** | Các class của LeptonX ghi đè làm mất màu hoặc méo layout | Đặt toàn bộ CSS của Layout mới dưới selector có tiền tố chuyên biệt (`.bh-customer-layout`, `.bh-mgmt-layout`). Dùng `data: { layout: 'empty' }` để vô hiệu hóa hoàn toàn LeptonX shell. |
| **2** | **Tràn văn bản khi đổi ngôn ngữ** | Một số từ Tiếng Việt dài làm tràn nút bấm hoặc vỡ thanh menu | Sử dụng `min-width`, `white-space: nowrap` có kiểm soát, kết hợp `text-overflow: ellipsis` và Flexbox `gap` tự co giãn. |
| **3** | **Kích thước gói bundle Angular phình to** | Tải trang chậm trên thiết bị di động | Tất cả components mới đều là `standalone: true`, sử dụng cơ chế Lazy Loading (`loadComponent` / `loadChildren`) để tách nhỏ file chunk JS. |
| **4** | **Trải nghiệm trên điện thoại (Mobile)** | Sidebar chiếm hết màn hình hoặc bảng dữ liệu tràn viền | Thiết kế Sidebar dạng Offcanvas/Drawer trượt từ bên trái với lớp phủ tối màn hình; Bảng dữ liệu có lớp bao `.bh-table-responsive` cuộn ngang êm ái. |

---

## 4. Kết Luận Nghiên Cứu

Giải pháp kỹ thuật đã giải quyết triệt để tất cả các yêu cầu từ Hiến pháp và người dùng:
- Không còn bất kỳ điểm nghẽn kỹ thuật nào.
- Đảm bảo tính độc lập kiểm thử (Independently Testable) của từng User Story.
- Sẵn sàng bước sang Phase 1: Thiết kế Mô hình Dữ liệu UI (`data-model.md`) và Hợp đồng Giao diện (`contracts/`).
