# Feature Specification: RM-004 - Shared UI & Layout Chuẩn Song Ngữ Theo Concept

**Feature Branch**: `004-shared-ui-layout`  
**Created**: 2026-11-13  
**Status**: Ready for Review  
**Input**: User description: "/speckit-specify RM-004 - Shared UI & Layout chuẩn song ngữ theo Concept (Đỏ sẫm `#990000`, Đen `#000000`, Lễ tân `#99FF99` & `#808080`, Quản lý `#00FFFF` & Đen)"

---

## 1. Giới Thiệu & Mục Tiêu Nghiệp Vụ (Executive Summary)

Tính năng **RM-004** xây dựng hạ tầng giao diện người dùng dùng chung (Shared UI & Layout Shells) và hệ thống quy chuẩn nhận diện thương hiệu (Design Tokens) cho toàn bộ nền tảng **Booking Hotel**. 

Mục tiêu cốt lõi:
1. **Hiện thực hóa Hiến pháp Dự án (Điều khoản VII & VIII)**: Áp dụng bảng màu đặc thù theo từng phân hệ người dùng (Customer: Đỏ sẫm `#990000` & Đen `#000000`; Quản trị/Quản lý: Nền Trắng `#FFFFFF`, Sidebar Đen `#000000`, Điểm nhấn Cyan `#00FFFF`; Lễ tân: Xanh lá tươi `#99FF99` & Xám `#808080`).
2. **Quy chuẩn hiển thị thương hiệu**: Chuẩn hóa Logo **`Booking Hotel`** viết cách rời có khoảng cách rõ ràng trên toàn bộ các thành phần hiển thị.
3. **Đa ngôn ngữ song ngữ tức thì (vi/en i18n)**: Cung cấp bộ chuyển đổi ngôn ngữ (Language Switcher) trực quan tại Header/Navbar, chuyển đổi mượt mà giữa Tiếng Việt và English với 100% chuỗi nhãn và thông báo hệ thống được đồng bộ hóa.
4. **Khung bố cục đa phân hệ (Role-Based Layout Shells)**: Thiết lập cấu trúc khung vỏ (Shell) phân tách rõ ràng giữa Trải nghiệm Đặt phòng Khách hàng (Customer Booking Shell) và Không gian Tác nghiệp Vận hành (Admin / Hotel Manager / Frontdesk Management Shell).
5. **Thư viện thành phần giao diện dùng chung (Shared UI Components & State Feedback)**: Đồng bộ phong cách hiển thị cho Bảng dữ liệu (Responsive DataTables), Hộp thoại xác nhận (Modal dialogs), Nhãn trạng thái (Status badges), Bộ chọn ngày (Date/DateRange picker) và Thông báo nổi (Toastr notifications).

---

## 2. Kịch Bản Người Dùng & Tiêu Chí Chấp Nhận (User Scenarios & Testing)

### User Story 1 - Hệ Thống Design Tokens & Khung Bố Cục Đặt Phòng Khách Hàng (Customer & Guest Layout Shell) (Priority: P1)

Là Khách hàng (Customer) hoặc Khách vãng lai (Guest), tôi muốn trải nghiệm một giao diện đặt phòng sang trọng, ấm cúng và chuyên nghiệp với tông màu Đỏ sẫm (`#990000`) và Đen (`#000000`), kèm theo thanh điều hướng trực quan và chân trang đầy đủ thông tin để tôi có thể dễ dàng tìm kiếm phòng, đăng ký, đăng nhập và tiếp cận các dịch vụ khách sạn.

**Why this priority**: Khách hàng là nguồn doanh thu trực tiếp của khách sạn. Giao diện cổng đặt phòng cần sự đồng nhất về thương hiệu, tạo sự tin cậy và đạt chuẩn thị giác ngay từ điểm chạm đầu tiên.

**Independent Test**:
1. Truy cập trang chủ hoặc trang cổng đặt phòng với tư cách khách chưa đăng nhập.
2. Kiểm tra thanh Header/Navbar: hiển thị Logo `Booking Hotel` chuẩn với từ `Booking` (đỏ sẫm `#990000`) và `Hotel` (đen `#111111`), có khoảng cách rõ ràng giữa 2 từ.
3. Kiểm tra các nút hành động chính (Primary CTA): nút tìm kiếm phòng, nút Đăng nhập / Đăng ký sử dụng nền đỏ sẫm `#990000`, hiệu ứng hover mượt mà và chữ trắng có độ tương phản cao.
4. Kiểm tra Chân trang (Footer): hiển thị đúng bản quyền `Booking Hotel`, thông tin hỗ trợ, liên kết chính sách bảo mật và điều khoản dịch vụ.

**Acceptance Scenarios**:
1. **Hiển thị Thanh điều hướng (Header/Navbar) Khách hàng**:
   - **Given** Người dùng truy cập vào trang Cổng đặt phòng.
   - **When** Trang tải hoàn tất.
   - **Then** Thanh Navbar hiển thị logo `Booking Hotel` với khoảng cách tối thiểu `0.35rem` giữa hai từ, menu điều hướng cơ bản (Trang chủ, Khách sạn, Ưu đãi) và nút tác vụ Đăng nhập / Đăng ký.
2. **Trạng thái Người dùng đã Đăng nhập trên Customer Shell**:
   - **Given** Khách hàng đã đăng nhập thành công.
   - **When** Người dùng nhìn vào góc phải thanh Navbar.
   - **Then** Nút Đăng nhập/Đăng ký được thay thế bằng Menu Tài khoản cá nhân (Avatar/Tên người dùng, Đơn đặt phòng của tôi, Đổi mật khẩu, Đăng xuất).
3. **Hiển thị Chân trang (Footer) Chuẩn Thương Hiệu**:
   - **Given** Người dùng cuộn xuống cuối trang.
   - **When** Khung chân trang hiển thị.
   - **Then** Footer hiển thị thương hiệu `Booking Hotel`, năm hiện tại động, thông tin liên hệ và các liên kết chính sách.

---

### User Story 2 - Khung Giao Diện Quản Trị & Vận Hành (Management / Admin Layout Shell) (Priority: P1)

Là Quản trị viên (Host Admin / Tenant Admin) hoặc Quản lý Khách sạn (Hotel Manager), tôi muốn làm việc trong một khung giao diện quản trị có Sidebar nền đen (`#000000`), nền nội dung chính màu trắng sáng (`#FFFFFF`), và điểm nhấn thao tác màu Xanh Cyan (`#00FFFF`) để tối ưu hóa khả năng tập trung, đọc số liệu thống kê và quản lý buồng phòng hiệu quả.

**Why this priority**: Phân hệ quản trị yêu cầu sự phân tách rõ rệt với giao diện khách hàng. Tông màu tương phản cao giúp các cấp quản lý theo dõi bảng biểu, biểu đồ và quản lý nhân sự/danh mục buồng phòng mà không bị mỏi mắt.

**Independent Test**:
1. Đăng nhập bằng tài khoản Quản trị viên hoặc Quản lý khách sạn.
2. Kiểm tra Sidebar bên trái: Nền đen sâu `#000000`, các mục menu hiển thị rõ ràng, icon sắc nét, mục đang chọn (active) hoặc hover được viền/highlight bằng màu Xanh Cyan (`#00FFFF`).
3. Kiểm tra Khu vực Nội dung chính (Main Content Area): Nền trắng `#FFFFFF`, thanh breadcrumb định vị rõ cấp độ trang, tiêu đề trang rõ ràng.
4. Kiểm tra Topbar Quản trị: Hiển thị bộ chuyển đổi Tenant (nếu là Host Admin), nút chuyển đổi ngôn ngữ, chuông thông báo và menu hồ sơ quản trị viên.

**Acceptance Scenarios**:
1. **Hiển thị Sidebar Quản trị theo Vai trò**:
   - **Given** Người dùng có vai trò Quản trị viên hệ thống hoặc Quản lý khách sạn đã xác thực.
   - **When** Truy cập vào phân hệ Quản trị.
   - **Then** Sidebar hiển thị danh mục chức năng tương ứng với quyền hạn đã cấp ở RM-003, với nền đen `#000000` và điểm nhấn Cyan `#00FFFF`.
2. **Tương tác Menu và Chỉ báo Trạng thái Chọn**:
   - **Given** Quản trị viên nhấp chuột vào một mục chức năng trên Sidebar.
   - **When** Tuyến đường (route) thay đổi.
   - **Then** Mục menu được kích hoạt (active item) đổi màu nổi bật bằng màu Cyan `#00FFFF` với độ tương phản văn bản rõ ràng.
3. **Thu gọn và Mở rộng Sidebar trên Màn hình Vừa & Nhỏ**:
   - **Given** Quản trị viên sử dụng màn hình máy tính bảng hoặc thu nhỏ cửa sổ trình duyệt.
   - **When** Nhấn nút toggle thu gọn (collapse).
   - **Then** Sidebar trượt gọn lại chỉ hiển thị icons, mở rộng tối đa diện tích hiển thị bảng dữ liệu.

---

### User Story 3 - Bộ Chuyển Đổi Ngôn Ngữ Song Ngữ (vi/en Language Switcher) & Đồng Bộ i18n (Priority: P1)

Là Người dùng (bất kể Khách hàng hay Nhân viên quản lý), tôi muốn có thể dễ dàng chuyển đổi qua lại giữa Tiếng Việt và Tiếng Anh trực tiếp trên thanh công cụ mọi lúc, để tôi có thể làm việc bằng ngôn ngữ mẹ đẻ hoặc ngôn ngữ quốc tế mà không bị gián đoạn hay mất trạng thái công việc.

**Why this priority**: Hệ thống khách sạn phục vụ cả khách du lịch quốc tế và nhân sự nội bộ. Chuyển đổi ngôn ngữ mượt mà là yêu cầu cơ bản trong tiêu chuẩn quốc tế và Hiến pháp dự án (Điều khoản V).

**Independent Test**:
1. Nhấp vào Bộ chuyển đổi ngôn ngữ trên Header/Navbar.
2. Chọn "English" khi đang ở Tiếng Việt $\rightarrow$ Toàn bộ nhãn menu, tiêu đề trang, nút bấm và thông báo đổi sang Tiếng Anh trong < 200ms mà không reload cứng toàn bộ trang web.
3. Chọn lại "Tiếng Việt" $\rightarrow$ Giao diện lập tức trở lại ngôn ngữ Tiếng Việt chuẩn hóa.
4. Đóng trình duyệt và mở lại $\rightarrow$ Hệ thống vẫn lưu giữ đúng ngôn ngữ đã chọn trước đó.

**Acceptance Scenarios**:
1. **Chuyển đổi Ngôn ngữ Tức thì trên Giao diện**:
   - **Given** Người dùng đang ở bất kỳ trang nào của hệ thống.
   - **When** Người dùng mở dropdown ngôn ngữ và chọn ngôn ngữ khác (`vi` hoặc `en`).
   - **Then** Hệ thống áp dụng gói ngôn ngữ mới ngay lập tức, toàn bộ các thành phần hiển thị dùng chung cập nhật bản dịch tương ứng.
2. **Lưu trữ Tùy chọn Ngôn ngữ (Language Persistence)**:
   - **Given** Người dùng đã chọn ngôn ngữ ưa thích là Tiếng Anh (`en`).
   - **When** Người dùng điều hướng giữa các trang hoặc tải lại trình duyệt.
   - **Then** Ngôn ngữ vẫn được duy trì là Tiếng Anh (`en`) theo cấu hình phiên lưu trữ của ABP/Angular.
3. **Độ Bao phủ Bản dịch 100% Khóa Dùng Chung**:
   - **Given** Người dùng chuyển đổi giữa hai ngôn ngữ.
   - **When** Kiểm tra tất cả các chuỗi giao diện thuộc phân hệ Shared UI (Menu, Nút bấm, Tiêu đề hộp thoại, Thông báo lỗi/thành công).
   - **Then** Không xuất hiện bất kỳ chuỗi thô nào bị thiếu dịch hoặc hiển thị dạng key `[::KeyName]`.

---

### User Story 4 - Bộ Tokens & Khung Giao Diện Lễ Tân / Sơ Đồ Buồng Phòng (Front Desk & Room Rack Theme Tokens) (Priority: P2)

Là Nhân viên Lễ tân (Receptionist) hoặc Quản lý Khách sạn trực ca, tôi muốn giao diện buồng phòng và quầy tiếp đón có màu nhận diện Xanh lá tươi nhạt (`#99FF99`) kết hợp Xám (`#808080`) để tôi dễ dàng phân biệt trạng thái buồng phòng trực quan (phòng sạch, phòng bẩn, phòng bảo trì) và thao tác nhanh chóng trong ca trực mà không bị căng thẳng thị giác.

**Why this priority**: Lễ tân là người trực tiếp đón khách và thao tác trên Sơ đồ buồng phòng (Room Rack Matrix) liên tục hàng giờ. Bảng màu dịu mắt với độ tương phản hợp lý giúp nhân viên giảm thiểu sai sót khi kiểm tra và xếp phòng cho khách.

**Independent Test**:
1. Kiểm tra bộ CSS Tokens dành riêng cho Phân hệ Lễ tân: biến màu `--bh-frontdesk-primary: #99FF99` và `--bh-frontdesk-secondary: #808080`.
2. Kiểm tra các mẫu Thẻ buồng phòng (Room Badges / Cards) mẫu:
   - Phòng Sẵn sàng / Sạch sẽ: Viền hoặc nhãn màu Xanh lá tươi `#99FF99` kèm chữ tương phản dễ đọc.
   - Phòng Bẩn / Cần dọn dẹp: Nhãn màu Xám `#808080` cảnh báo cần xử lý vệ sinh.
   - Phòng Có khách / Đang sử dụng: Màu trạng thái nổi bật phân biệt rõ.
3. Thử nghiệm trên màn hình quầy lễ tân với điều kiện ánh sáng văn phòng để xác nhận độ dễ đọc.

**Acceptance Scenarios**:
1. **Thiết lập Tokens Màu Sắc Lễ tân Theo Hiến pháp**:
   - **Given** Khung giao diện lễ tân hoặc thành phần buồng phòng được khởi tạo.
   - **When** Áp dụng lớp phong cách (CSS class) thuộc phân hệ Frontdesk.
   - **Then** Các nút hành động chính (Check-in nhanh, Xếp phòng) sử dụng gam màu xanh lá tươi `#99FF99` với chữ đen sắc nét; các chi tiết phụ trợ sử dụng gam màu xám `#808080`.
2. **Tính Thống Nhất của Huy Hiệu Trạng Thái Phòng (Room Status Badges)**:
   - **Given** Danh sách hoặc ma trận hiển thị trạng thái phòng.
   - **When** Render các trạng thái `Available` (Trống/Sạch), `Dirty` (Chưa dọn), `Occupied` (Đang ở), `Maintenance` (Bảo trì).
   - **Then** Các huy hiệu hiển thị đồng nhất về kích thước, khoảng cách và màu sắc chuẩn theo token quy định.

---

### User Story 5 - Thư Viện Thành Phần Giao Diện Dùng Chung & Trạng Thái Phản Hồi (Shared UI Components & State Feedback) (Priority: P2)

Là Lập trình viên xây dựng các tính năng tiếp theo (RM-005 Quản lý Tài khoản, Đặt phòng, Quản trị Buồng phòng) và Người dùng cuối, tôi muốn có sẵn một thư viện các thành phần giao diện dùng chung chuẩn hóa (Bảng dữ liệu DataTables, Hộp thoại xác nhận Modal, Bộ chọn khoảng ngày DateRangePicker, Thông báo Toastr, Trạng thái đang tải Spinner, Khối hiển thị dữ liệu trống Empty State) để đảm bảo toàn bộ hệ thống nhất quán 100% về trải nghiệm và giảm thời gian lập trình lại.

**Why this priority**: Các màn hình tiếp theo trong Wave 0 và Wave 1 phụ thuộc vào các khối giao diện cơ sở này. Nếu không có các thành phần chuẩn hóa từ RM-004, mã nguồn sẽ bị phân mảnh và vi phạm Hiến pháp về trải nghiệm người dùng.

**Independent Test**:
1. Mở trang giới thiệu / trình diễn thành phần dùng chung (Shared UI Showcase hoặc Storybook nội bộ).
2. Kiểm tra Bảng dữ liệu: hỗ trợ phân trang, sắp xếp, responsive trên mobile không tràn khung hình.
3. Kích hoạt một Hộp thoại xác nhận (Confirmation Modal): hiển thị tiêu đề, nội dung cảnh báo, nút Hủy và nút Xác nhận chuẩn màu.
4. Kích hoạt các loại thông báo Toastr (Thành công, Cảnh báo, Lỗi, Thông tin): hiển thị góc trên bên phải màn hình với màu sắc đồng bộ và tự động biến mất sau thời gian quy định.
5. Kích hoạt trạng thái Đang tải (Loading Skeleton / Spinner): hiển thị mượt mà không làm rung giật giao diện.

**Acceptance Scenarios**:
1. **Bảng Dữ Liệu Dùng Chung (Shared DataTable Wrapper)**:
   - **Given** Một danh sách dữ liệu cần hiển thị bảng trong phân hệ Quản trị.
   - **When** Dữ liệu được truyền vào thành phần bảng dùng chung.
   - **Then** Bảng tự động hiển thị tiêu đề cột, phân trang chuẩn ABP, hỗ trợ cuộn ngang trên màn hình nhỏ và có hiệu ứng hover dòng tinh tế.
2. **Hộp Thoại Xác Nhận Chuẩn Hóa (Confirmation Modal Component)**:
   - **Given** Người dùng chuẩn bị thực hiện một thao tác quan trọng (như khóa tài khoản, hủy đơn).
   - **When** Kích hoạt hộp thoại xác nhận.
   - **Then** Hộp thoại hiển thị trung tâm màn hình, có lớp phủ mờ (backdrop), nút Xác nhận có màu tương ứng với mức độ nguy cơ (Primary hoặc Danger), hỗ trợ phím ESC để đóng an toàn.
3. **Hệ Thống Thông Báo Nổi (Toastr Notifications)**:
   - **Given** Một thao tác bất đồng bộ hoàn thành hoặc gặp lỗi.
   - **When** Dịch vụ thông báo gọi hàm hiển thị `success`, `error`, `warning` hoặc `info`.
   - **Then** Toast hiển thị với biểu tượng tương ứng, văn bản song ngữ rõ ràng và viền màu đúng chuẩn nhận diện.

---

## 3. Các Trường Hợp Biên & Tình Huống Đặc Biệt (Edge Cases)

1. **Chuỗi Ký Tự Bản Dịch Dài (Text Expansion / Overflow)**:
   - Một số từ trong Tiếng Đức hoặc Tiếng Việt dài hơn đáng kể so với Tiếng Anh (ví dụ: "Registration" vs "Đăng ký thành viên").
   - *Cách xử lý*: Tất cả các nút bấm, thẻ tiêu đề và mục menu phải thiết lập thuộc tính chống tràn (`text-overflow: ellipsis`, `white-space: nowrap` hợp lý trên menu, hoặc tự động xuống hàng linh hoạt trên thẻ card) mà không làm vỡ bố cục tổng thể.
2. **Độ Tương Phản Ánh Sáng & Khả Năng Tiếp Cận (WCAG 2.1 AA Compliance)**:
   - Màu Cyan `#00FFFF` và Xanh lá tươi `#99FF99` là các màu nền rất sáng. Nếu để chữ trắng sẽ không thể đọc được.
   - *Cách xử lý*: Quy chuẩn bắt buộc mọi văn bản hiển thị trên nền Cyan `#00FFFF` hoặc Xanh lá `#99FF99` phải dùng màu chữ Đen sâu `#000000` hoặc `#111111` để đạt tỷ lệ tương phản trên 4.5:1. Ngược lại, trên nền Đỏ sẫm `#990000` và Đen `#000000` bắt buộc dùng chữ Trắng `#FFFFFF`.
3. **Màn Hình Thiết Bị Siêu Nhỏ (< 375px) và Cực Lớn (4K / Ultrawide)**:
   - *Cách xử lý*: Bố cục sử dụng hệ thống lưới linh hoạt (CSS Flexbox & CSS Grid) với `max-width` giới hạn cho phần nội dung trung tâm trên màn hình lớn (1440px) để không bị loãng, đồng thời co giãn an toàn trên điện thoại có màn hình hẹp.
4. **Mất Kết Nối Mạng Hoặc Gói Ngôn Ngữ Bị Lỗi Tải**:
   - *Cách xử lý*: Nếu một gói ngôn ngữ phụ trợ không tải được, cơ chế nội suy đa ngôn ngữ của ABP / Angular Core tự động lấy ngôn ngữ mặc định (Tiếng Việt `vi` hoặc Tiếng Anh `en`) làm fallback, không hiển thị trang trắng.
5. **Chuyển Đổi Phân Hệ Đột Ngột Giữa Khách Hàng và Quản Trị**:
   - Khi một người dùng là Quản lý đang duyệt trang Đặt phòng và nhấp vào liên kết chuyển sang Trang Quản trị.
   - *Cách xử lý*: Hệ thống chuyển đổi mượt mà giữa Customer Layout Shell và Management Layout Shell dựa trên định tuyến (Route Layout Configuration), không lưu đè hoặc gây xung đột biến CSS toàn cục.

---

## 4. Yêu Cầu Chức Năng Chi Tiết (Functional Requirements)

### Nhóm 1: Design Tokens & Bảng Màu Nhận Diện Thương Hiệu
- **FR-001**: Hệ thống PHẢI khai báo đầy đủ các biến CSS Custom Properties (Design Tokens) tại cấp độ `:root` trong `styles.scss` đại diện cho các mã màu cốt lõi:
  - `--bh-brand-red: #990000` (Đỏ sẫm thương hiệu)
  - `--bh-brand-black: #000000` (Đen chủ đạo)
  - `--bh-brand-white: #ffffff` (Trắng tinh khiết)
  - `--bh-mgmt-bg: #ffffff` (Nền chính quản trị)
  - `--bh-mgmt-sidebar: #000000` (Sidebar quản trị)
  - `--bh-mgmt-accent: #00ffff` (Điểm nhấn Cyan quản lý)
  - `--bh-frontdesk-primary: #99ff99` (Xanh lá tươi nhạt lễ tân)
  - `--bh-frontdesk-secondary: #808080` (Xám phụ trợ lễ tân)
- **FR-002**: Thành phần Logo thương hiệu PHẢI tuân thủ nghiêm ngặt Điều khoản VIII của Hiến pháp:
  - Tên hiển thị luôn luôn là **`Booking Hotel`** viết tách rời có dấu cách.
  - Sử dụng hai thẻ màu riêng biệt: `.logo-red` (`#990000`) cho từ `Booking` và `.logo-black` (`#111111` hoặc `#ffffff` tùy nền sáng/tối) cho từ `Hotel`.
  - Giữa hai từ bắt buộc có khoảng cách (margin-right tối thiểu `0.35rem` hoặc gap tương đương), tuyệt đối không để dính chữ `BookingHotel`.

### Nhóm 2: Khung Bố Cục (Layout Shells) & Điều Hướng
- **FR-003**: Hệ thống PHẢI cung cấp **Customer Layout Shell** tối ưu cho Khách hàng và Khách vãng lai, bao gồm:
  - Header Navbar màu nền thương hiệu hoặc trắng trang nhã với viền đỏ sẫm `#990000`.
  - Menu chuyển trang chính (Trang chủ, Khách sạn, Ưu đãi, Giới thiệu).
  - Khối tài khoản / Đăng nhập / Đăng ký đồng bộ với luồng xác thực RM-001 và RM-002.
  - Footer chứa bản quyền, thông tin liên hệ và liên kết tiện ích.
- **FR-004**: Hệ thống PHẢI cung cấp **Management Layout Shell** dành cho các vai trò quản trị (Host Admin, Tenant Admin, Hotel Manager), bao gồm:
  - Sidebar cố định bên trái màu đen `#000000`, hỗ trợ thu gọn/mở rộng.
  - Menu phân cấp hiển thị các chức năng dựa trên quyền hạn đã được khai báo ở RM-003.
  - Điểm nhấn Cyan `#00FFFF` khi hover hoặc kích hoạt mục menu.
  - Khu vực nội dung nền trắng `#FFFFFF` với thanh định vị Breadcrumb.
  - Topbar chứa công cụ đổi ngôn ngữ, thông tin Tenant hiện tại, và Menu hồ sơ cá nhân.
- **FR-005**: Hệ thống PHẢI hỗ trợ cơ chế chuyển đổi Layout linh hoạt theo từng Route thông qua cấu hình Angular Router (`layout: 'application' | 'empty' | 'management'`).

### Nhóm 3: Đa Ngôn Ngữ Song Ngữ (vi/en i18n)
- **FR-006**: Hệ thống PHẢI cung cấp thành phần **Language Switcher Component** đặt tại vị trí dễ nhìn trên thanh điều hướng của cả Customer Layout và Management Layout.
- **FR-007**: Language Switcher PHẢI cho phép chuyển đổi tức thì giữa Tiếng Việt (`vi`) và Tiếng Anh (`en`) thông qua cơ chế bản địa hóa của ABP Framework (`SessionStateService` / `LocalizationService`), lưu trạng thái lựa chọn vào `localStorage` hoặc cookie phiên làm việc.
- **FR-008**: Tất cả chuỗi hiển thị dùng chung của giao diện (Tên hệ thống, mục menu, nút bấm thao tác chung, tiêu đề bảng, thông báo hệ thống, nhãn trạng thái) PHẢI được định nghĩa đầy đủ 100% trong cả 2 tệp:
  - `BookingHotel.Domain.Shared/Localization/BookingHotel/vi.json`
  - `BookingHotel.Domain.Shared/Localization/BookingHotel/en.json`

### Nhóm 4: Thư Viện Thành Phần Dùng Chung (Shared UI Components)
- **FR-009**: Hệ thống PHẢI cung cấp mẫu kiểu dáng chuẩn (CSS Classes & Component) cho **Bảng dữ liệu (Responsive DataTable)** với tiêu đề phân biệt rõ, hiệu ứng vệt hàng (zebra striping) nhẹ nhàng và thanh phân trang đồng bộ.
- **FR-010**: Hệ thống PHẢI cung cấp thành phần **Hộp thoại xác nhận (Confirmation Modal)** tái sử dụng được, cho phép tùy biến thông điệp, biểu tượng cảnh báo và các nút tác vụ (Đồng ý / Hủy) theo phong cách màu của hệ thống.
- **FR-011**: Hệ thống PHẢI cung cấp tập hợp các kiểu dáng **Huy hiệu trạng thái (Status Badges)**:
  - Trạng thái phòng Lễ tân: `badge-room-available` (nền `#99FF99`, chữ `#000000`), `badge-room-dirty` (nền `#808080`, chữ `#FFFFFF`), `badge-room-occupied` (nền `#990000`, chữ `#FFFFFF`), `badge-room-maintenance` (nền `#FFA500`, chữ `#000000`).
  - Trạng thái chung: `badge-success`, `badge-danger`, `badge-warning`, `badge-info`.
- **FR-012**: Hệ thống PHẢI cung cấp định dạng chuẩn cho **Thông báo nổi (Toastr Notifications)**, tự động căn chỉnh màu sắc theo bảng màu thương hiệu cho các mức `success`, `error`, `warning`, `info`.
- **FR-013**: Hệ thống PHẢI cung cấp thành phần hiển thị trạng thái **Đang tải (Loading Indicator)** và **Dữ liệu trống (Empty State Component)** chuẩn với hình minh họa và nút hướng dẫn hành động tiếp theo.
- **FR-014**: Hệ thống PHẢI cung cấp phong cách chuẩn cho thành phần **Bộ chọn khoảng ngày (Date/DateRange Picker)** phù hợp với trải nghiệm chọn ngày nhận phòng / trả phòng trong toàn bộ hệ thống.

---

## 5. Mô Hình Dữ Liệu & Khái Niệm Cốt Lõi (Key Concepts & Data Contracts)

### 5.1 Cấu Hình Bảng Màu (Theme Color Specification)
| Token Variable | Giá Trị Màu Hex | Phân Hệ Áp Dụng | Mục Đích Sử Dụng |
| :--- | :--- | :--- | :--- |
| `--bh-brand-red` | `#990000` | Customer & Chung | Màu chủ đạo thương hiệu, nút CTA chính, Header accent |
| `--bh-brand-black` | `#000000` / `#111111` | Toàn bộ hệ thống | Nền Sidebar quản trị, màu chữ tiêu đề chính |
| `--bh-brand-white` | `#FFFFFF` | Toàn bộ hệ thống | Nền nội dung chính quản trị, chữ trên nền tối |
| `--bh-mgmt-accent` | `#00FFFF` | Quản trị / Quản lý | Điểm nhấn menu active, nút thao tác nghiệp vụ, highlight bảng số liệu |
| `--bh-frontdesk-primary` | `#99FF99` | Lễ tân / Buồng phòng | Trạng thái phòng trống/sạch, nút Check-in nhanh, thao tác sẵn sàng |
| `--bh-frontdesk-secondary`| `#808080` | Lễ tân / Buồng phòng | Trạng thái phòng bẩn/chờ dọn, viền và chi tiết phụ trợ sơ đồ phòng |

### 5.2 Khái Niệm Thành Phần Điều Hướng (Navigation Concept)
- **Customer Navigation Menu**: Chứa các liên kết hướng tới trải nghiệm khách lưu trú (Khám phá, Tìm kiếm phòng, Khuyến mại, Đơn đặt phòng).
- **Management Navigation Menu**: Chứa các nhóm quản trị phân quyền (Tổng quan Dashboard, Cơ sở khách sạn, Buồng phòng & Bảng giá, Lễ tân & Sơ đồ phòng, Nhân sự & Tài khoản, Cài đặt hệ thống).

### 5.3 Ngôn Ngữ Hỗ Trợ (Supported Languages)
- **Tiếng Việt (`vi`)**: Ngôn ngữ mặc định tại thị trường Việt Nam (Mã văn hóa: `vi`).
- **English (`en`)**: Ngôn ngữ quốc tế (Mã văn hóa: `en`).

---

## 6. Tiêu Chí Thành Công Có Thể Đo Lường (Measurable Success Criteria)

- **SC-001**: 100% các trang giao diện của hệ thống (Customer Portal, Management Portal, Frontdesk/Room Rack components) tuân thủ đúng tuyệt đối các mã màu quy định trong Hiến pháp Điều khoản VII (`#990000`, `#000000`, `#00FFFF`, `#99FF99`, `#808080`).
- **SC-002**: Logo thương hiệu hiển thị đúng chữ tách rời **`Booking Hotel`** trên 100% các vị trí xuất hiện (Header, Footer, Tab trình duyệt, Tiêu đề modal), không có bất kỳ trường hợp nào bị dính chữ `BookingHotel`.
- **SC-003**: Thao tác chuyển đổi ngôn ngữ giữa `vi` và `en` phản hồi tức thời trong vòng **dưới 200 mili-giây** trên giao diện phía client, không yêu cầu người dùng phải tải lại toàn bộ trang (no hard page refresh).
- **SC-004**: Toàn bộ các chuỗi văn bản giao diện dùng chung (nhãn menu, nút thao tác, tiêu đề hộp thoại, thông báo toast) đạt **tỷ lệ bản địa hóa 100%** trong cả hai tệp `vi.json` và `en.json`, hoàn toàn không xuất hiện chuỗi hardcode hoặc mã key chưa dịch `[::...]`.
- **SC-005**: Mọi cặp màu văn bản / nền trên toàn bộ hệ sinh thái giao diện đạt tiêu chuẩn tương phản tối thiểu **4.5:1** theo tiêu chuẩn tiếp cận **WCAG 2.1 AA**.
- **SC-006**: Khung giao diện hiển thị mượt mà, co giãn đáp ứng hoàn hảo (Responsive) trên 3 kích thước chuẩn: Desktop (1200px+), Tablet (768px - 1199px), và Mobile (375px - 767px) mà không có hiện tượng vỡ giao diện hay thanh cuộn ngang ngoài ý muốn.

---

## 7. Các Giả Định & Giới Hạn Phạm Vi (Assumptions & Boundaries)

### Các Giả Định Được Thiết Lập:
1. **Nền tảng Công nghệ**: Ứng dụng Angular 19+ chạy chế độ Standalone Components kết hợp với ABP LeptonX Theme Lite làm bộ khung cơ sở.
2. **Bộ Biểu Tượng (Icons)**: Sử dụng thư viện FontAwesome 6 Free đã được tích hợp sẵn trong template ABP.
3. **Phông Chữ (Typography)**: Hệ thống sử dụng phông chữ không chân hiện đại (Inter / Roboto / System-UI) hỗ trợ hiển thị mượt mà đầy đủ các dấu thanh Tiếng Việt UTF-8.
4. **Phạm Vi Phân Hệ Wave 0**:
   - RM-004 tập trung kiến tạo Bảng màu Tokens, Khung vỏ Layout (Customer & Management), Bộ chuyển đổi ngôn ngữ song ngữ, và các Thành phần UI dùng chung cơ sở.
   - Các màn hình nghiệp vụ chi tiết (như Màn hình Sơ đồ buồng phòng tương tác thực tế, Bảng điều khiển phân tích doanh thu chuyên sâu) sẽ sử dụng các tokens và components được thiết lập tại RM-004 này để triển khai ở các Wave tiếp theo (RM-006 trở đi).
