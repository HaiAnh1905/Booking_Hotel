# Kế Hoạch Triển Khai Kiến Trúc Kỹ Thuật (Implementation Plan): RM-004

**Feature Branch**: `004-shared-ui-layout` | **Date**: 2026-11-13 | **Spec**: [specs/004-shared-ui-layout/spec.md](spec.md)  
**Input**: Feature specification from `specs/004-shared-ui-layout/spec.md` and Phase 0 Research `specs/004-shared-ui-layout/research.md`.

---

## 1. Tóm Tắt Kế Hoạch (Summary)

Xây dựng hạ tầng giao diện người dùng dùng chung (Shared UI & Layout Shells) và hệ thống quy chuẩn nhận diện thương hiệu (Design Tokens) cho toàn bộ giải pháp **Booking Hotel** trên nền tảng Angular 19+ và ABP Framework 10.7 theo đúng Hiến pháp Dự án:
1. **Hệ thống CSS Design Tokens Toàn Cục**:
   - Định nghĩa toàn bộ biến CSS Custom Properties tại `:root` trong `styles.scss` đại diện cho các mã màu cốt lõi: Đỏ sẫm `#990000` & Đen `#000000` (Customer), Nền Trắng `#FFFFFF` & Sidebar Đen `#000000` & Điểm nhấn Cyan `#00FFFF` (Quản lý), Xanh lá tươi `#99FF99` & Xám `#808080` (Lễ tân).
   - Kiểm định độ tương phản ánh sáng đạt chuẩn **WCAG 2.1 AA** ($\ge 4.5:1$): chữ đen trên nền Cyan `#00FFFF` và Xanh lá `#99FF99`, chữ trắng trên nền Đỏ sẫm `#990000` và Đen `#000000`.
   - Chuẩn hóa thương hiệu Logo **`Booking Hotel`** viết cách rời với khoảng cách tối thiểu `0.35rem` giữa hai từ.
2. **Khung Bố Cục Độc Lập Đa Phân Hệ (Dual Standalone Layouts)**:
   - `CustomerLayoutComponent`: Đảm nhận Cổng đặt phòng khách hàng với Header thanh lịch, Dropdown ngôn ngữ, Khối tài khoản và Footer thương hiệu.
   - `ManagementLayoutComponent`: Đảm nhận Phân hệ Quản trị với Sidebar đen hỗ trợ thu gọn/mở rộng, Topbar điểm nhấn Cyan, Breadcrumb và Menu phân quyền theo RM-003.
   - Tích hợp thay thế `ApplicationLayout` qua `ReplaceableComponentsService` của ABP để các module quản trị sẵn có (`identity`, `tenant-management`, `setting-management`) tự động kế thừa layout quản trị chuẩn.
3. **Bộ Chuyển Đổi Ngôn Ngữ Song Ngữ Tức Thì (Instant vi/en Language Switcher)**:
   - Dropdown ngôn ngữ `BhLanguageSwitcherComponent` kết nối với `SessionStateService` của ABP, cập nhật giao diện trong $< 200\text{ ms}$ không cần reload trang và bảo lưu trạng thái vào `localStorage`.
   - Đồng bộ 100% tài nguyên bản địa hóa giữa `vi.json` và `en.json`.
4. **Tái Cấu Trúc Trang Chủ Khách Hàng (Customer Booking Landing Page)**:
   - Chuyển đổi `HomeComponent` thành trang đích đặt phòng sang trọng theo concept Đỏ sẫm & Đen, có Hero Search Banner tìm kiếm phòng, tiện ích cao cấp và thẻ phòng mẫu.
5. **Thư Viện Standalone Components Dùng Chung & Màn Hình Thử Nghiệm**:
   - Đóng gói thư viện tại `angular/src/app/shared/components/`: `BhDataTable`, `BhModal`, `BhStatusBadge`, `BhToastrService`, `BhLoadingSpinner`, `BhEmptyState`.
   - Xây dựng 2 màn hình kiểm thử độc lập: UI Showcase (`/admin/ui-showcase`) và Room Rack Preview (`/admin/room-rack-preview`).

---

## 2. Bối Cảnh Kỹ Thuật (Technical Context)

- **Language / Version**: TypeScript 5.x / Angular 22 (tương thích Angular 19+ standalone mode), C# .NET 10.
- **Primary Dependencies**:
  - ABP Framework 10.7 (`@abp/ng.core`, `@abp/ng.theme.shared`, `@abp/ng.theme.lepton-x`).
  - Bootstrap 5 & FontAwesome 6 Free (tích hợp sẵn trong template).
  - RxJS 7.8+.
- **Storage**: Client-side storage (`localStorage` & Session Cookies) cho trạng thái ngôn ngữ và sidebar toggle; CSDL quan hệ cho tài nguyên bản địa hóa backend.
- **Testing**: Angular build verification (`npm run build`), Vitest / Unit Tests cho shared components.
- **Target Platform**: Responsive Web Application (Desktop $\ge 1200\text{px}$, Tablet $768\text{px} - 1199\text{px}$, Mobile $< 768\text{px}$).
- **Performance Goals**:
  - Chuyển đổi ngôn ngữ song ngữ client-side $< 200\text{ ms}$ (không reload trang).
  - First Contentful Paint (FCP) cho trang chủ $< 1.2\text{s}$.
  - Thời gian build Angular production $< 20\text{s}$.
- **Constraints (Constitution)**:
  - Tuân thủ nghiêm ngặt bảng màu và tỷ lệ tương phản WCAG 2.1 AA (Điều khoản VII).
  - Logo thương hiệu luôn viết cách rời `Booking Hotel` (Điều khoản VIII).
  - 100% chuỗi giao diện dùng chung được dịch song ngữ trong `vi.json` và `en.json` (Điều khoản V).
  - Frontend là Thin Presentation Layer, không can thiệp vào logic tính toán giá cả backend (Điều khoản IV & VI).

---

## 3. Kiểm Tra Tính Tuân Thủ Hiến Pháp (Constitution Check)

*GATE: Kiểm tra tính tuân thủ 9 Điều khoản tối cao trong `constitution.md`*

| Điều khoản Hiến pháp | Tiêu chí đánh giá | Trạng thái | Giải pháp kỹ thuật tương ứng |
| :--- | :--- | :---: | :--- |
| **I. Phân lớp DDD & ABP** | Đúng tầng, không lẫn lộn trách nhiệm | **PASS** | Tài nguyên chuỗi giao diện dùng chung đặt tại `BookingHotel.Domain.Shared/Localization/BookingHotel`; mã nguồn giao diện đặt trọn vẹn trong project `BookingHotel/angular`. |
| **II. Contract-First & Security** | Bắt buộc kiểm tra quyền, bảo mật | **PASS** | Các mục menu trong Sidebar quản trị được kiểm soát hiển thị thông qua `requiredPermission` tương ứng với cây quyền đã định nghĩa ở RM-003. |
| **III. EF Core Migrations** | Không can thiệp DB thủ công | **PASS** | RM-004 là phân hệ Shared UI thuần túy trên Frontend và Localization, không thay đổi schema CSDL. |
| **IV. Chuẩn hóa Frontend** | Phân tách giao diện, Thin Presentation | **PASS** | Áp dụng mô hình Standalone Components với Strict TypeScript, các component dùng chung nhận dữ liệu qua `@Input()` và phát sự kiện qua `@Output()`; không tự tính toán tiền tệ. |
| **V. Đa ngôn ngữ (i18n)** | Không hardcode chuỗi hiển thị | **PASS** | 100% nhãn menu, tiêu đề cột bảng, nút bấm, thông báo toast và trạng thái phòng đều ánh xạ qua khóa dịch `::*` và đồng bộ đầy đủ trong `vi.json` và `en.json`. |
| **VI. Backend-Authoritative** | Toàn quyền kiểm soát tại Backend | **PASS** | Mọi dữ liệu buồng phòng, quyền hạn và trạng thái người dùng hiển thị trên layout đều lấy từ API backend; frontend chỉ hiển thị tạm tính tham khảo. |
| **VII. UI/UX Concept Hệ Màu** | Nhận diện thương hiệu theo phân hệ | **PASS** | Thiết lập trọn bộ Design Tokens cho Customer (Đỏ `#990000`/Đen), Quản trị (Trắng/Sidebar Đen/Cyan `#00FFFF`), Lễ tân (Xanh lá `#99FF99`/Xám `#808080`) kèm kiểm định tương phản WCAG 2.1 AA. |
| **VIII. Brand Naming** | Chuẩn hóa thương hiệu `Booking Hotel` | **PASS** | Logo HTML/SCSS tách biệt `.logo-red` và `.logo-black` với `margin-right: 0.35rem`, hiển thị chuẩn tên `Booking Hotel` trên toàn bộ hệ sinh thái. |
| **IX. Bảo Mật Phiên & Unlock** | Mở khóa brute-force phân tầng | **PASS** | Tích hợp menu tài khoản người dùng hiển thị đúng vai trò và hỗ trợ đăng xuất an toàn xóa sạch token. |

---

## 4. Cấu Trúc Dự Án & Sơ Đồ Ánh Xạ Mã Nguồn (Project Structure & Code Mapping)

### 4.1 Tài Liệu Đặc Tả & Thiết Kế (Documentation Artifacts)
```text
BookingHotel/specs/004-shared-ui-layout/
├── spec.md                         # Đặc tả yêu cầu chức năng (Feature Spec)
├── checklists/
│   └── requirements.md             # Thẩm định chất lượng đặc tả (Quality Checklist)
├── plan.md                         # Kế hoạch kiến trúc kỹ thuật (Tài liệu này)
├── research.md                     # Báo cáo nghiên cứu giải pháp kỹ thuật (Phase 0)
├── data-model.md                   # Mô hình dữ liệu & hợp đồng trạng thái UI (Phase 1)
├── contracts/
│   ├── ui-components-api.md        # Đặc tả chi tiết Input/Output của Shared Components (Phase 1)
│   └── design-tokens.md            # Bảng mã CSS Tokens & ma trận WCAG AA (Phase 1)
├── quickstart.md                   # Hướng dẫn kiểm thử & chạy thử nghiệm (Phase 1)
└── tasks.md                        # Danh mục đầu việc triển khai (Phase 2 - speckit.tasks)
```

### 4.2 Cấu Trúc Mã Nguồn Triển Khai (Source Code Layout)
```text
BookingHotel/
├── angular/
│   └── src/
│       ├── styles.scss                                     # Khai báo CSS Design Tokens, Logo & Global Utilities
│       │
│       ├── app/
│       │   ├── app.config.ts                               # Đăng ký Layout thay thế qua ReplaceableComponents
│       │   ├── app.routes.ts                               # Định tuyến tích hợp Customer & Management Layouts
│       │   │
│       │   ├── layout/
│       │   │   ├── customer/
│       │   │   │   ├── customer-layout.component.ts        # Standalone Customer Shell Component
│       │   │   │   ├── customer-layout.component.html      # Header thương hiệu, Navbar, RouterOutlet, Footer
│       │   │   │   └── customer-layout.component.scss      # Styling Đỏ sẫm & Đen sang trọng
│       │   │   │
│       │   │   └── management/
│       │   │       ├── management-layout.component.ts      # Standalone Management Shell Component
│       │   │       ├── management-layout.component.html    # Sidebar đen sâu, Topbar Cyan, Breadcrumb, RouterOutlet
│       │   │       └── management-layout.component.scss    # Styling Sidebar collapse, Active highlights
│       │   │
│       │   ├── shared/
│       │   │   ├── components/
│       │   │   │   ├── data-table/
│       │   │   │   │   ├── bh-data-table.component.ts      # Bảng dữ liệu responsive chuẩn
│       │   │   │   │   ├── bh-data-table.component.html    # Phân trang, sắp xếp cột
│       │   │   │   │   └── bh-data-table.component.scss
│       │   │   │   ├── modal/
│       │   │   │   │   ├── bh-modal.component.ts           # Hộp thoại xác nhận / popup
│       │   │   │   │   ├── bh-modal.component.html         # Backdrop, animation, phím ESC
│       │   │   │   │   └── bh-modal.component.scss
│       │   │   │   ├── status-badge/
│       │   │   │   │   ├── bh-status-badge.component.ts    # Huy hiệu trạng thái phòng & chung
│       │   │   │   │   └── bh-status-badge.component.scss  # Bảng màu #99FF99, #808080, #990000, #FFA500
│       │   │   │   ├── language-switcher/
│       │   │   │   │   ├── bh-language-switcher.component.ts # Dropdown đổi ngôn ngữ tức thì vi/en
│       │   │   │   │   └── bh-language-switcher.component.scss
│       │   │   │   ├── toastr/
│       │   │   │   │   ├── bh-toastr.service.ts            # Dịch vụ phát thông báo nổi
│       │   │   │   │   ├── bh-toastr-container.component.ts # Khung hiển thị toasts góc màn hình
│       │   │   │   │   └── bh-toastr.component.scss
│       │   │   │   ├── loading-spinner/
│       │   │   │   │   ├── bh-loading-spinner.component.ts # Vòng xoay tải dữ liệu
│       │   │   │   │   └── bh-loading-spinner.component.scss
│       │   │   │   └── empty-state/
│       │   │   │       ├── bh-empty-state.component.ts     # Khối hiển thị khi dữ liệu rỗng
│       │   │   │       └── bh-empty-state.component.scss
│       │   │   │
│       │   │   └── models/
│       │   │       └── ui-models.ts                        # Interfaces TypeScript cho Navigation, DataTable, Modals
│       │   │
│       │   ├── home/
│       │   │   ├── home.component.ts                       # Customer Booking Landing Page Component
│       │   │   ├── home.component.html                     # Hero Search Banner, Tiện ích, Phòng mẫu
│       │   │   └── home.component.scss                     # Styling tông Đỏ sẫm & Đen
│       │   │
│       │   └── admin/
│       │       ├── admin.routes.ts                         # Định tuyến các trang quản trị nội bộ
│       │       ├── ui-showcase/
│       │       │   ├── ui-showcase.component.ts            # Trang trình diễn thử nghiệm toàn bộ Shared Components
│       │       │   ├── ui-showcase.component.html
│       │       │   └── ui-showcase.component.scss
│       │       └── room-rack-preview/
│       │           ├── room-rack-preview.component.ts      # Trang xem trước Sơ đồ buồng phòng Lễ tân
│       │           ├── room-rack-preview.component.html    # Ma trận 12 phòng mẫu với 4 trạng thái màu chuẩn
│       │           └── room-rack-preview.component.scss
│       │
│       └── assets/images/
│           └── logo/                                       # Logo assets chuẩn hóa (nếu có)
│
└── src/
    └── BookingHotel.Domain.Shared/
        └── Localization/BookingHotel/
            ├── vi.json                                     # 100% từ điển bản địa hóa Tiếng Việt
            └── en.json                                     # 100% từ điển bản địa hóa Tiếng Anh
```

---

## 5. Kế Hoạch Triển Khai Theo Giai Đoạn (Phased Execution Breakdown)

1. **Giai đoạn 1 (Foundation & Tokens)**:
   - Cập nhật `styles.scss`: Thêm toàn bộ CSS Custom Properties, Utility classes, Logo thương hiệu `Booking Hotel`.
   - Cập nhật `vi.json` và `en.json`: Bổ sung toàn bộ các khóa bản địa hóa cho Shared UI, Menu, Table, Modal, Toastr, Room Rack, Landing Page.
2. **Giai đoạn 2 (Shared Components Library)**:
   - Xây dựng các interface TypeScript trong `shared/models/ui-models.ts`.
   - Xây dựng `BhStatusBadgeComponent` (chuẩn màu `#99FF99`, `#808080`, `#990000`, `#FFA500`).
   - Xây dựng `BhLanguageSwitcherComponent` (kết nối `SessionStateService` và `localStorage`).
   - Xây dựng `BhToastrService` & `BhToastrContainerComponent`.
   - Xây dựng `BhModalComponent` & `BhDataTableComponent`.
   - Xây dựng `BhLoadingSpinnerComponent` & `BhEmptyStateComponent`.
3. **Giai đoạn 3 (Layout Shells Architecture)**:
   - Xây dựng `CustomerLayoutComponent` (Header đỏ/đen, Navbar, Language Switcher, User Menu, Footer).
   - Xây dựng `ManagementLayoutComponent` (Sidebar đen sâu thu gọn/mở rộng, Topbar Cyan, Breadcrumb, RouterOutlet).
   - Cấu hình `app.config.ts` thay thế `ApplicationLayout` bằng `ManagementLayoutComponent`.
   - Cấu hình lại `app.routes.ts` phân định rõ layout cho từng tuyến đường.
4. **Giai đoạn 4 (Home Landing Page & Showcase Pages)**:
   - Tái thiết kế `HomeComponent` thành Customer Booking Landing Page theo concept Đỏ sẫm & Đen.
   - Xây dựng `UiShowcaseComponent` tại `/admin/ui-showcase`.
   - Xây dựng `RoomRackPreviewComponent` tại `/admin/room-rack-preview`.
5. **Giai đoạn 5 (Verification & Quality Gate)**:
   - Chạy `npm run build` xác nhận không có lỗi cú pháp hay thiếu import.
   - Kiểm thử thực tế các kịch bản người dùng (User Story 1 $\rightarrow$ 5) theo `quickstart.md`.
