# Tasks: RM-004 - Shared UI & Layout Chuẩn Song Ngữ Theo Concept

**Input**: Design documents from `/specs/004-shared-ui-layout/` (`plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/ui-components-api.md`, `contracts/design-tokens.md`, `quickstart.md`)  
**Prerequisites**: `plan.md` (required), `spec.md` (required), `research.md`, `data-model.md`, `contracts/`  
**Organization**: Tasks được nhóm theo từng Phase và User Story để đảm bảo tính độc lập triển khai và khả năng kiểm thử độc lập cho từng phân hệ.

## Format: `- [ ] [ID] [P?] [Story?] Description with file path`

- **[P]**: Có thể thực hiện song song (khác file, không phụ thuộc vào task chưa hoàn thành).
- **[Story]**: Đánh dấu User Story tương ứng (`[US1]`, `[US2]`, `[US3]`, `[US4]`, `[US5]`).
- Đường dẫn file mã nguồn chính xác trong mô tả task.

---

## Phase 1: Setup (Design Tokens & Localization Foundation)

**Mục đích**: Thiết lập hệ thống biến CSS Design Tokens toàn cục, lớp kiểu dáng Logo chuẩn Hiến pháp, các kiểu dữ liệu TypeScript và đồng bộ 100% tài nguyên bản địa hóa song ngữ làm nền tảng dùng chung.

- [ ] T001 Khai báo toàn bộ Design Tokens (CSS Custom Properties) cho 3 phân hệ (Customer `#990000`/`#000000`, Quản lý `#FFFFFF`/`#000000`/`#00FFFF`, Lễ tân `#99FF99`/`#808080`), typography và layout dimensions trong `BookingHotel/angular/src/styles.scss`
- [ ] T002 Bổ sung lớp kiểu dáng Logo chuẩn thương hiệu `.bh-brand-logo`, `.logo-red`, `.logo-black` với khoảng cách tối thiểu `0.35rem` và biến thể `.on-dark` trong `BookingHotel/angular/src/styles.scss`
- [ ] T003 [P] Bổ sung các biến CSS tiện ích & biến thể nền/viền/tương phản đạt chuẩn WCAG 2.1 AA vào `BookingHotel/angular/src/styles.scss`
- [ ] T004 [P] Khởi tạo tệp mô hình dữ liệu TypeScript dùng chung `ui-models.ts` trong `BookingHotel/angular/src/app/shared/models/ui-models.ts`
- [ ] T005 [P] Bổ sung đầy đủ 100% các chuỗi bản địa hóa Tiếng Việt (Common, Menu, Table, Modal, Toastr, RoomRack, Landing) trong `BookingHotel/src/BookingHotel.Domain.Shared/Localization/BookingHotel/vi.json`
- [ ] T006 [P] Bổ sung đầy đủ 100% các chuỗi bản địa hóa Tiếng Anh tương ứng trong `BookingHotel/src/BookingHotel.Domain.Shared/Localization/BookingHotel/en.json`

---

## Phase 2: Foundational (Shared UI Components Library)

**Mục đích**: Xây dựng thư viện Standalone Components dùng chung làm hạ tầng cơ sở cho tất cả các Layout Shells và màn hình chức năng tiếp theo.

**⚠️ CRITICAL**: Các User Story phía sau phụ thuộc vào các components này.

- [ ] T007 [P] Xây dựng component huy hiệu trạng thái `BhStatusBadgeComponent` hỗ trợ 4 trạng thái phòng (`available` `#99FF99`, `dirty` `#808080`, `occupied` `#990000`, `maintenance` `#FFA500`) và nghiệp vụ chung trong `BookingHotel/angular/src/app/shared/components/status-badge/bh-status-badge.component.ts` (kèm SCSS)
- [ ] T008 [P] Xây dựng component chuyển đổi ngôn ngữ song ngữ `BhLanguageSwitcherComponent` (kết nối `SessionStateService` của ABP và `localStorage`) trong `BookingHotel/angular/src/app/shared/components/language-switcher/bh-language-switcher.component.ts` (kèm SCSS)
- [ ] T009 [P] Xây dựng dịch vụ thông báo nổi `BhToastrService` và component khung chứa `BhToastrContainerComponent` với tone màu nhận diện trong `BookingHotel/angular/src/app/shared/components/toastr/`
- [ ] T010 [P] Xây dựng component vòng xoay tải `BhLoadingSpinnerComponent` trong `BookingHotel/angular/src/app/shared/components/loading-spinner/`
- [ ] T011 [P] Xây dựng component hiển thị dữ liệu rỗng `BhEmptyStateComponent` trong `BookingHotel/angular/src/app/shared/components/empty-state/`
- [ ] T012 Xây dựng component hộp thoại xác nhận `BhModalComponent` với backdrop làm mờ, phím ESC và phím xác nhận/hủy trong `BookingHotel/angular/src/app/shared/components/modal/`
- [ ] T013 Xây dựng component bảng dữ liệu dùng chung `BhDataTableComponent` với responsive container, phân trang, sắp xếp và slots template tùy biến trong `BookingHotel/angular/src/app/shared/components/data-table/`

**Checkpoint**: Nền tảng Design Tokens và Thư viện UI Components đã sẵn sàng - tiến hành triển khai các User Story theo thứ tự ưu tiên.

---

## Phase 3: User Story 1 - Cổng Đặt Phòng & Trang Chủ Khách Hàng (Customer Layout Shell & Landing Page) (Priority: P1) 🎯 MVP

**Mục tiêu**: Cung cấp giao diện Cổng Đặt phòng Khách hàng sang trọng với tông màu Đỏ sẫm `#990000` & Đen, Header thương hiệu `Booking Hotel`, Hero Search Banner tìm kiếm phòng, tiện ích và thẻ phòng mẫu.

**Independent Test**: Mở trình duyệt truy cập `http://localhost:4200/`: kiểm tra Logo hiển thị đúng khoảng cách, màu sắc Đỏ sẫm/Đen, form tìm kiếm phòng trực quan, và Footer bản quyền đầy đủ.

- [ ] T014 [US1] Xây dựng component khung bố cục khách hàng `CustomerLayoutComponent` độc lập với Header thương hiệu, Logo `Booking Hotel`, Dropdown ngôn ngữ, nút Đăng nhập / Menu tài khoản và Footer trong `BookingHotel/angular/src/app/layout/customer/customer-layout.component.ts` (kèm HTML & SCSS)
- [ ] T015 [US1] Tái thiết kế toàn diện `HomeComponent` thành Customer Booking Landing Page với Hero Search Banner (Khách sạn, Ngày nhận - trả, Số khách) tông màu Đỏ sẫm `#990000` & Đen trong `BookingHotel/angular/src/app/home/home.component.ts` (kèm HTML & SCSS)
- [ ] T016 [US1] Bổ sung khối Tiện ích cao cấp và Khối Thẻ phòng mẫu (Deluxe, Suite, Family) trong `BookingHotel/angular/src/app/home/home.component.html` và SCSS
- [ ] T017 [US1] Định tuyến Cổng đặt phòng khách hàng với `CustomerLayoutComponent` và `HomeComponent` trong `BookingHotel/angular/src/app/app.routes.ts`

**Checkpoint**: User Story 1 hoàn tất - Cổng đặt phòng khách hàng hoạt động độc lập và đạt chuẩn nhận diện thương hiệu.

---

## Phase 4: User Story 2 - Khung Giao Diện Quản Trị & Vận Hành (Management / Admin Layout Shell) (Priority: P1)

**Mục tiêu**: Cung cấp giao diện Quản trị có Sidebar đen sâu `#000000`, Nền chính trắng `#FFFFFF`, Điểm nhấn thao tác Xanh Cyan `#00FFFF`, Menu phân cấp theo vai trò đã định nghĩa ở RM-003 và thanh Breadcrumb.

**Independent Test**: Mở trình duyệt truy cập `http://localhost:4200/admin`: kiểm tra Sidebar đen sâu hỗ trợ thu gọn/mở rộng, mục active sáng màu Cyan `#00FFFF`, Topbar có Tenant switcher và hồ sơ quản trị viên.

- [ ] T018 [US2] Xây dựng component khung bố cục quản trị `ManagementLayoutComponent` độc lập với Sidebar đen sâu `#000000`, nút thu gọn/mở rộng, Topbar trắng điểm nhấn Cyan `#00FFFF`, Dropdown ngôn ngữ và Breadcrumb trong `BookingHotel/angular/src/app/layout/management/management-layout.component.ts` (kèm HTML & SCSS)
- [ ] T019 [US2] Tích hợp cấu trúc cây điều hướng phân quyền (Dashboard, Khách sạn, Buồng phòng & Bảng giá, Lễ tân & Sơ đồ phòng, Nhân sự & Tài khoản) vào `ManagementLayoutComponent` tự động lọc theo `BookingHotelPermissions` từ RM-003
- [ ] T020 [US2] Cấu hình thay thế `ApplicationLayout` bằng `ManagementLayoutComponent` qua `ReplaceableComponentsService` trong `BookingHotel/angular/src/app/app.config.ts`
- [ ] T021 [US2] Thiết lập module định tuyến quản trị `BookingHotel/angular/src/app/admin/admin.routes.ts` và đăng ký trong `BookingHotel/angular/src/app/app.routes.ts`

**Checkpoint**: User Story 2 hoàn tất - Khung quản trị hoạt động mượt mà và các module ABP mặc định tự động kế thừa layout quản trị chuẩn.

---

## Phase 5: User Story 3 - Bộ Chuyển Đổi Ngôn Ngữ Song Ngữ (vi/en Language Switcher) & Đồng Bộ i18n (Priority: P1)

**Mục tiêu**: Cung cấp khả năng chuyển đổi tức thì giữa Tiếng Việt và Tiếng Anh trong $< 200\text{ ms}$ trên Header/Topbar mà không tải lại toàn bộ trang web.

**Independent Test**: Nhấp chọn dropdown ngôn ngữ trên Header: chuyển từ "Tiếng Việt" sang "English" và ngược lại; xác nhận toàn bộ nhãn giao diện thay đổi tức thì và được bảo lưu khi mở lại trình duyệt.

- [ ] T022 [US3] Tích hợp `BhLanguageSwitcherComponent` vào cả thanh Header của `CustomerLayoutComponent` và Topbar của `ManagementLayoutComponent`
- [ ] T023 [US3] Triển khai logic lưu trữ và khôi phục ngôn ngữ đã chọn từ `localStorage` khi khởi chạy ứng dụng trong `BhLanguageSwitcherComponent`
- [ ] T024 [US3] Rà soát và kiểm thử việc chuyển đổi ngôn ngữ tức thời $< 200\text{ ms}$ trên toàn bộ các chuỗi giao diện dùng chung (Menu, Header, Footer, Hero Search, Badges)

**Checkpoint**: User Story 3 hoàn tất - Toàn bộ hệ thống song ngữ mượt mà và không gián đoạn thao tác người dùng.

---

## Phase 6: User Story 4 - Bộ Tokens & Màn Hình Xem Trước Sơ Đồ Buồng Phòng Lễ Tân (Front Desk & Room Rack Preview) (Priority: P2)

**Mục tiêu**: Hiện thực hóa bảng màu Lễ tân theo Hiến pháp (Xanh lá tươi `#99FF99` & Xám `#808080`) kèm màn hình mẫu Sơ đồ buồng phòng với 4 trạng thái phòng để kiểm thử thực tế thị giác và tương phản.

**Independent Test**: Mở trình duyệt truy cập `http://localhost:4200/admin/room-rack-preview`: kiểm tra 12 phòng mẫu hiển thị đúng màu trạng thái (`Available` xanh lá, `Dirty` xám, `Occupied` đỏ, `Maintenance` cam) và bộ lọc trạng thái hoạt động chính xác.

- [ ] T025 [US4] Xây dựng component xem trước sơ đồ phòng mẫu `RoomRackPreviewComponent` trong `BookingHotel/angular/src/app/admin/room-rack-preview/room-rack-preview.component.ts` (kèm HTML & SCSS)
- [ ] T026 [US4] Khởi tạo dữ liệu mẫu 12 phòng thuộc nhiều tầng với đầy đủ 4 trạng thái buồng phòng (`Available` `#99FF99`, `Dirty` `#808080`, `Occupied` `#990000`, `Maintenance` `#FFA500`)
- [ ] T027 [US4] Tích hợp thanh công cụ lọc trạng thái phòng (Tất cả, Phòng sạch, Phòng bẩn, Có khách, Bảo trì) và hiển thị thẻ phòng trực quan tại quầy lễ tân
- [ ] T028 [US4] Đăng ký tuyến đường `/admin/room-rack-preview` trong `BookingHotel/angular/src/app/admin/admin.routes.ts`

**Checkpoint**: User Story 4 hoàn tất - Phân hệ màu Lễ tân được kiểm thử trực quan và sẵn sàng cho các module vận hành ca trực.

---

## Phase 7: User Story 5 - Thư Viện Thành Phần Dùng Chung & Trạng Thái Phản Hồi (Shared UI Showcase) (Priority: P2)

**Mục tiêu**: Cung cấp trang trình diễn UI Showcase để lập trình viên và kiểm thử viên kiểm tra toàn diện các thành phần dùng chung (Bảng dữ liệu, Modal, Toastr, Spinner, Empty State).

**Independent Test**: Mở trình duyệt truy cập `http://localhost:4200/admin/ui-showcase`: kiểm tra tương tác phân trang bảng, mở modal xác nhận, kích hoạt 4 loại toastr, bật/tắt spinner và hiển thị khối dữ liệu trống.

- [ ] T029 [US5] Xây dựng component trình diễn `UiShowcaseComponent` trong `BookingHotel/angular/src/app/admin/ui-showcase/ui-showcase.component.ts` (kèm HTML & SCSS)
- [ ] T030 [US5] Trình diễn các trường hợp của Bảng dữ liệu `BhDataTableComponent` (dữ liệu mẫu, sắp xếp, phân trang)
- [ ] T031 [US5] Trình diễn Modal xác nhận `BhModalComponent`, các loại Toast qua `BhToastrService`, Spinner tải và Khối dữ liệu trống `BhEmptyStateComponent`
- [ ] T032 [US5] Đăng ký tuyến đường `/admin/ui-showcase` trong `BookingHotel/angular/src/app/admin/admin.routes.ts`

**Checkpoint**: User Story 5 hoàn tất - Thư viện thành phần dùng chung được minh chứng đầy đủ khả năng tái sử dụng.

---

## Phase 8: Polish & Quality Gates

**Mục đích**: Hoàn thiện tích hợp toàn cục, dọn dẹp CSS xung đột, kiểm tra biên dịch production và xác nhận chất lượng.

- [ ] T033 Gắn thẻ `<bh-toastr-container />` vào `AppComponent` để thông báo Toast hiển thị toàn cục trên toàn hệ thống trong `BookingHotel/angular/src/app/app.component.ts`
- [ ] T034 Kiểm tra và dọn dẹp các rule CSS xung đột hoặc đè lên nhau trong `BookingHotel/angular/src/styles.scss`
- [ ] T035 Chạy kiểm tra biên dịch toàn bộ mã nguồn Angular bằng `npm run build` xác nhận 0 lỗi cú pháp
- [ ] T036 Thực hiện kiểm thử toàn diện 5 kịch bản theo `quickstart.md` và kiểm tra độ tương phản WCAG 2.1 AA
