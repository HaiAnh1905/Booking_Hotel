# Feature Specification: RM-003 - Chuẩn Bị Phân Quyền Hệ Thống & Vai Trò Người Dùng (Role-Based Permissions Preparation)

**Feature Branch**: `003-prepare-permissions`  
**Created**: 2026-11-12  
**Status**: Ready for Planning  
**Input**: User description: "Chuẩn bị các phân quyền cho việc xây dựng hệ thống bao gồm: Customer(khách hàng), Admin hệ thống, Admin tenant, Hotel Manager, Receptionist"

---

## Clarifications

### Session 2026-11-12
- Q: Hệ thống sẽ áp dụng chính sách vai trò cố định (5 Standard Roles định sẵn) hay cho phép Quản trị viên Tenant tạo thêm các vai trò tùy chỉnh (Custom Roles) và tùy biến bật/tắt quyền hạt nhân? → A: Option A (Cố định 5 Standard Roles chuẩn hóa: `Customer`, `System Admin`, `Tenant Admin`, `Hotel Manager`, `Receptionist` với ma trận quyền đóng gói sẵn; người quản lý chỉ thực hiện gán/hủy vai trò cho người dùng, không tạo vai trò tùy chỉnh trong Wave 0).
- Q: Thẩm quyền mở khóa tài khoản người dùng bị vô hiệu hóa do vượt quá số lần đăng nhập sai (Brute-Force) được phân định như thế nào giữa Quản trị viên Hệ thống và Quản trị viên Tenant? → A: Option A (Phân tầng theo phạm vi Tenant: System Admin mở khóa cho tài khoản cấp Host; Tenant Admin có toàn quyền mở khóa cho các tài khoản nhân sự Hotel Manager, Receptionist thuộc phạm vi Tenant của mình).
- Q: Tài khoản Quản lý Khách sạn (Hotel Manager) được thiết kế để quản lý cố định duy nhất một cơ sở khách sạn hay có thể được phân công phụ trách đồng thời nhiều chi nhánh trong cùng một chuỗi (Tenant)? → A: Option A (Phân công linh hoạt 1 hoặc nhiều chi nhánh trong cùng Tenant: một Hotel Manager có thể được Tenant Admin chỉ định phụ trách một hoặc danh sách nhiều chi nhánh khách sạn trong cùng Tenant; quyền quản lý buồng phòng, bảng giá và báo cáo doanh thu được lọc giới hạn theo đúng các chi nhánh được phân công).
- Q: Tài khoản Khách hàng (Customer) là tài khoản định danh dùng chung toàn hệ thống (Global Customer) hay phân tách độc lập theo từng Tenant (Tenant-Scoped Customer)? → A: Option A (Tài khoản Khách hàng dùng chung toàn hệ thống - Global Customer: tài khoản khách hàng được lưu trữ ở cấp nền tảng với `TenantId = null`; khách hàng chỉ cần đăng ký 1 tài khoản duy nhất để tìm kiếm, đặt phòng và quản lý kỳ nghỉ tại bất kỳ chuỗi khách sạn/tenant nào).
- Q: Tài khoản Lễ tân (Receptionist) được phân bổ làm việc cố định tại một cơ sở khách sạn duy nhất hay có thể được phân công linh hoạt theo ca trực tại các chi nhánh khác nhau trong cùng một Tenant? → A: Option A (Cố định 1 cơ sở khách sạn - Single-Property: mỗi tài khoản Receptionist được gán cố định với 1 chi nhánh khách sạn cụ thể trong cùng Tenant; khi đăng nhập hệ thống tự động tải trực tiếp sơ đồ buồng phòng và các đơn đặt phòng của chi nhánh đó, tối ưu tốc độ tác nghiệp tại quầy).

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Phân quyền & Quản trị Nền tảng Đa Khách thuê (System / Host Admin) (Priority: P1)

Là Quản trị viên Hệ thống (System / Host Admin), tôi muốn có toàn quyền quản trị hạ tầng nền tảng đa khách thuê (Multi-Tenant Platform) để tôi có thể quản lý vòng đời doanh nghiệp đối tác (Tenants), giám sát toàn bộ hoạt động an ninh/kiểm toán, mở khóa các tài khoản bị sự cố trên toàn hệ thống và duy trì các cấu hình dùng chung.

**Why this priority**: Đây là tầng quản trị cao nhất của nền tảng SaaS khách sạn. Thiếu ranh giới phân quyền rõ ràng ở cấp Host sẽ gây rủi ro rò rỉ dữ liệu giữa các doanh nghiệp đối tác hoặc làm tê liệt khả năng vận hành toàn hệ thống.

**Independent Test**: Có thể kiểm thử độc lập:
1. Đăng nhập bằng tài khoản Quản trị viên hệ thống (Host scope).
2. Tạo mới, xem danh sách, cập nhật thông tin và thực hiện tạm khóa/kích hoạt lại một tổ chức Tenant (chuỗi khách sạn).
3. Tra cứu lịch sử kiểm toán an ninh toàn hệ thống và thực hiện mở khóa một tài khoản người dùng bị khóa do brute-force.
4. Xác minh tài khoản Host Admin không bị trộn lẫn hoặc nhìn thấy nhầm dữ liệu nghiệp vụ phòng của tenant khi không được ủy quyền.

**Acceptance Scenarios**:
1. **Quản lý Vòng đời Tenant (Khách thuê)**:
   - **Given** Quản trị viên hệ thống đã xác thực thành công.
   - **When** Quản trị viên tạo mới một tổ chức Tenant (tên doanh nghiệp, thông tin liên hệ, mã định danh duy nhất).
   - **Then** Hệ thống khởi tạo không gian dữ liệu riêng biệt cho Tenant, gán trạng thái hoạt động (`Active`) và ghi nhận vào nhật ký kiểm toán hệ thống.
2. **Khóa và Mở khóa Tenant**:
   - **Given** Một Tenant đang có dấu hiệu vi phạm hoặc tạm ngừng dịch vụ.
   - **When** Quản trị viên hệ thống thực hiện thao tác tạm khóa (`Suspend`) Tenant đó.
   - **Then** Toàn bộ người dùng thuộc Tenant đó (Admin tenant, Hotel Manager, Receptionist) ngay lập tức bị từ chối truy cập mọi nghiệp vụ vận hành; chỉ Quản trị viên hệ thống mới có thể kích hoạt lại.
3. **Mở khóa Tài khoản cấp Hệ thống**:
   - **Given** Một tài khoản Quản trị viên bị vô hiệu hóa do vượt quá số lần đăng nhập sai theo cơ chế bảo mật.
   - **When** Quản trị viên hệ thống thực hiện lệnh mở khóa tài khoản (`Unlock User`).
   - **Then** Trạng thái tài khoản được khôi phục về kích hoạt (`Active`), xóa toàn bộ số lần đếm thất bại và ghi nhật ký an ninh.

---

### User Story 2 - Phân quyền & Quản trị Doanh nghiệp Khách sạn (Tenant Admin) (Priority: P1)

Là Quản trị viên Doanh nghiệp (Tenant Admin / Đối tác chuỗi khách sạn), tôi muốn quản lý độc lập toàn bộ các cơ sở khách sạn trực thuộc, thiết lập chính sách chung, phân bổ nhân sự và cấp quyền cho Quản lý khách sạn và Lễ tân trong phạm vi doanh nghiệp của mình.

**Why this priority**: Đảm bảo tính tự chủ của từng đối tác khách sạn trong mô hình SaaS, cho phép doanh nghiệp tự quản lý nhân viên và chính sách kinh doanh mà không cần can thiệp thủ công từ đội ngũ kỹ thuật hệ thống.

**Independent Test**: Có thể kiểm thử độc lập:
1. Đăng nhập bằng tài khoản Tenant Admin của Tenant A.
2. Thêm mới một cơ sở khách sạn trực thuộc chuỗi và phân công tài khoản Hotel Manager phụ trách cơ sở đó.
3. Tạo và gán vai trò Receptionist cho nhân sự mới trong chi nhánh.
4. Kiểm tra việc truy cập sang dữ liệu của Tenant B $\rightarrow$ Hệ thống hoàn toàn ngăn chặn (403 Forbidden / Not Found).

**Acceptance Scenarios**:
1. **Phân quyền và Quản lý Nhân sự Nội bộ Tenant**:
   - **Given** Quản trị viên Tenant đã đăng nhập vào hệ thống quản trị của doanh nghiệp mình.
   - **When** Quản trị viên tạo tài khoản nhân sự mới và gán vai trò `Hotel Manager` hoặc `Receptionist`.
   - **Then** Tài khoản mới được cấp đúng tập quyền hạn quy định cho vai trò đó trong phạm vi Tenant, không có bất kỳ quyền quản trị cấp Host nào.
2. **Thiết lập Chính sách Vận hành Chung của Doanh nghiệp**:
   - **Given** Quản trị viên Tenant muốn chuẩn hóa quy định đặt phòng trên toàn chuỗi.
   - **When** Quản trị viên cập nhật chính sách giờ nhận phòng/trả phòng tiêu chuẩn và quy tắc hoàn hủy phòng.
   - **Then** Các chính sách này tự động áp dụng làm mặc định cho tất cả cơ sở khách sạn trực thuộc tenant.
3. **Cách ly Dữ liệu Tuyệt đối giữa các Tenant**:
   - **Given** Quản trị viên của Tenant A đang xem danh sách nhân sự và khách sạn.
   - **When** Quản trị viên cố gắng truy vấn hoặc thao tác tới định danh thuộc Tenant B.
   - **Then** Hệ thống lập tức từ chối quyền truy cập, bảo vệ toàn vẹn tính bảo mật riêng tư giữa các đối tác.
4. **Mở khóa Tài khoản Nhân sự Nội bộ Tenant**:
   - **Given** Một tài khoản nhân sự (Hotel Manager hoặc Receptionist) thuộc Tenant bị vô hiệu hóa do vượt quá số lần đăng nhập sai theo cơ chế brute-force.
   - **When** Quản trị viên Tenant thực hiện lệnh mở khóa tài khoản (`Unlock User`) trong trang quản trị nhân sự của Tenant.
   - **Then** Tài khoản nhân sự được khôi phục trạng thái hoạt động ngay lập tức, đặt lại toàn bộ bộ đếm brute-force và lưu vết kiểm toán mà không cần yêu cầu hỗ trợ từ cấp Host.

---

### User Story 3 - Phân quyền Vận hành Cơ sở Khách sạn (Hotel Manager) (Priority: P2)

Là Quản lý Khách sạn (Hotel Manager), tôi muốn quản lý danh mục phòng, chính sách giá, dịch vụ bổ sung, phân công ca trực lễ tân và giám sát báo cáo doanh thu/công suất của cơ sở khách sạn được giao quản lý để đảm bảo hoạt động kinh doanh đạt hiệu quả cao nhất.

**Why this priority**: Hotel Manager là người trực tiếp điều phối doanh thu, giá bán và buồng phòng tại từng cơ sở. Phân quyền này tách biệt trách nhiệm điều hành kinh doanh khỏi việc can thiệp cấu hình hệ thống của Tenant Admin.

**Independent Test**: Có thể kiểm thử độc lập:
1. Đăng nhập bằng tài khoản Hotel Manager được phân công quản lý Khách sạn X.
2. Cấu hình danh mục hạng phòng (Deluxe, Suite), đơn giá theo ngày/mùa, các tiện ích phòng và bảng giá minibar/dịch vụ giặt là.
3. Xem báo cáo công suất phòng (Occupancy Rate) và báo cáo doanh thu theo kỳ của Khách sạn X.
4. Cố gắng chỉnh sửa cấu hình cấp Doanh nghiệp của Tenant Admin $\rightarrow$ Hệ thống báo không đủ thẩm quyền.

**Acceptance Scenarios**:
1. **Quản lý Danh mục Hạng phòng & Giá bán Cơ sở**:
   - **Given** Quản lý khách sạn đang truy cập vào phân hệ buồng phòng của cơ sở được phân công.
   - **When** Quản lý thêm mới hạng phòng, cài đặt số lượng phòng thực tế, thiết lập đơn giá tiêu chuẩn và phụ thu nhận phòng sớm/trả phòng trễ.
   - **Then** Thông tin phòng và giá bán được cập nhật vào hệ thống để mở bán cho khách hàng và sẵn sàng cho quầy lễ tân tác nghiệp.
2. **Cấu hình Dịch vụ Bổ sung & Khuyến mại**:
   - **Given** Khách sạn triển khai chương trình kích cầu mùa thấp điểm.
   - **When** Quản lý tạo mã giảm giá (voucher) và cập nhật đơn giá dịch vụ ăn uống, minibar, spa.
   - **Then** Khách hàng đặt phòng có thể áp dụng mã hợp lệ và Lễ tân có thể chọn đúng bảng giá dịch vụ khi phát sinh thanh toán.
3. **Phê duyệt Ngoại lệ Vận hành**:
   - **Given** Có phát sinh khiếu nại khách hàng cần hoàn hủy ngoài chính sách tiêu chuẩn hoặc áp dụng chiết khấu đặc biệt.
   - **When** Quản lý thực hiện phê duyệt yêu cầu ngoại lệ của Lễ tân.
   - **Then** Hệ thống ghi nhận lý do ngoại lệ, điều chỉnh hóa đơn theo thẩm quyền và lưu vết lịch sử phê duyệt.

---

### User Story 4 - Phân quyền Tác nghiệp Quầy Lễ tân (Receptionist) (Priority: P2)

Là Nhân viên Lễ tân (Receptionist), tôi muốn có không gian tác nghiệp chuyên biệt được gắn cố định với cơ sở khách sạn của mình để ngay sau khi đăng nhập, hệ thống tự động tải trực tiếp sơ đồ buồng phòng của khách sạn đó, giúp tôi tiếp đón khách Check-in / Check-out nhanh chóng, lập hóa đơn tổng hợp (Folio), ghi nhận dịch vụ phát sinh và cập nhật trạng thái buồng phòng mà không phải thao tác chọn lại chi nhánh.

**Why this priority**: Nghiệp vụ quầy lễ tân yêu cầu tốc độ thao tác cao, trực quan và hạn chế tối đa các nhầm lẫn tài chính. Phân quyền chặt chẽ giúp Lễ tân tập trung đúng vào nghiệp vụ phục vụ khách mà không có quyền thay đổi bảng giá gốc hay can thiệp cấu hình hệ thống.

**Independent Test**: Có thể kiểm thử độc lập:
1. Đăng nhập với vai trò Receptionist tại một cơ sở khách sạn cụ thể.
2. Mở sơ đồ buồng phòng thời gian thực (Room Rack Matrix).
3. Thực hiện tạo đặt phòng trực tiếp tại quầy (walk-in) và làm thủ tục Check-in cho khách vào phòng trống sạch.
4. Thêm chi phí đồ uống minibar vào Folio phòng và thực hiện thủ tục Check-out, quyết toán hóa đơn.
5. Cập nhật trạng thái phòng vừa checkout sang trạng thái "Bẩn / Cần dọn dẹp".
6. Thử truy cập vào màn hình cấu hình giá phòng gốc hoặc báo cáo doanh thu cấp điều hành $\rightarrow$ Bị chặn quyền.

**Acceptance Scenarios**:
1. **Theo dõi Sơ đồ Buồng phòng & Tiếp đón Khách (Check-in)**:
   - **Given** Lễ tân đang trực ca tại quầy lễ tân khách sạn.
   - **When** Khách hàng đến nhận phòng đã đặt trước hoặc khách vãng lai (walk-in) cần phòng ngay.
   - **Then** Lễ tân tra cứu phòng trống sạch trên sơ đồ phòng, kiểm tra thông tin khách, gán số phòng cụ thể và chuyển trạng thái phòng sang "Đang ở" (`Occupied`).
2. **Ghi nhận Dịch vụ & Quyết toán Hóa đơn (Check-out)**:
   - **Given** Khách hàng hoàn tất kỳ nghỉ và làm thủ tục trả phòng tại quầy.
   - **When** Lễ tân kiểm tra tiêu thụ minibar/dịch vụ, nhập vào hóa đơn Folio và tiến hành thu tiền quyết toán.
   - **Then** Hệ thống chốt hóa đơn, chuyển phòng sang trạng thái "Bẩn / Chờ dọn" (`Dirty`) và gửi thông báo sẵn sàng xuất hóa đơn.
3. **Giới hạn Quyền Hạn Tài chính Cốt lõi**:
   - **Given** Lễ tân đang tạo đơn đặt phòng hoặc folio thanh toán.
   - **When** Lễ tân cố gắng sửa đổi đơn giá niêm yết của hạng phòng hoặc xóa vĩnh viễn hóa đơn đã thu tiền.
   - **Then** Hệ thống từ chối thao tác, yêu cầu quyền phê duyệt cấp Quản lý (`Hotel Manager`).

---

### User Story 5 - Phân quyền Trải nghiệm Đặt phòng Cá nhân (Customer) (Priority: P3)

Là Khách hàng (Customer), tôi muốn có một tài khoản dùng chung toàn hệ thống (Global Customer Account) an toàn và tiện lợi để tôi có thể tìm kiếm, đặt phòng, thanh toán, quản lý tất cả các kỳ nghỉ của mình tại bất kỳ khách sạn/tenant nào trên nền tảng mà không phải đăng ký tài khoản lặp lại cho từng chuỗi khách sạn riêng biệt.

**Why this priority**: Khách hàng là đối tượng mang lại nguồn doanh thu trực tiếp cho nền tảng. Phân quyền khách hàng phải đảm bảo tính tự phục vụ tối đa nhưng được cô lập tuyệt đối khỏi các dữ liệu nội bộ khách sạn.

**Independent Test**: Có thể kiểm thử độc lập:
1. Đăng nhập bằng tài khoản Customer thông qua kênh Email hoặc Google.
2. Tìm kiếm phòng trống theo ngày, địa điểm và thực hiện đặt phòng thành công.
3. Xem chi tiết đơn đặt phòng, lấy mã xác nhận nhận phòng và thực hiện hủy phòng hợp lệ theo chính sách.
4. Cố gắng gọi hoặc điều hướng tới các đường dẫn quản trị của nhân viên/quản lý $\rightarrow$ Hệ thống tự động chuyển hướng về trang cá nhân khách hàng.

**Acceptance Scenarios**:
1. **Tự phục vụ Đặt phòng & Thanh toán Trực tuyến**:
   - **Given** Khách hàng đã đăng nhập tài khoản trên cổng thông tin đặt phòng.
   - **When** Khách hàng chọn phòng ưng ý, nhập số lượng khách và tiến hành thanh toán trực tuyến.
   - **Then** Đơn đặt phòng được tạo ở trạng thái xác nhận, gửi email xác nhận kèm mã đặt phòng duy nhất cho khách hàng.
2. **Quản lý Đơn đặt phòng & Hủy phòng Tự phục vụ**:
   - **Given** Khách hàng có đơn đặt phòng chưa đến ngày nhận phòng và vẫn trong khung giờ hủy phòng miễn phí.
   - **When** Khách hàng chọn "Hủy đặt phòng" trên trang cá nhân.
   - **Then** Hệ thống tự động hủy đơn, cập nhật trạng thái phòng khả dụng trở lại và thông báo chính sách hoàn tiền tương ứng.
3. **Cách ly Tuyệt đối Khỏi Khu vực Quản trị Nhân viên**:
   - **Given** Người dùng có vai trò `Customer`.
   - **When** Cố gắng truy cập các chức năng của Lễ tân, Quản lý khách sạn hay Quản trị hệ thống.
   - **Then** Hệ thống chặn ngay lập tức và giữ chân người dùng trong không gian Customer Portal.

---

### Edge Cases

- **Truy cập Chéo Khách thuê (Cross-Tenant Spoofing)**: Người dùng thuộc Tenant A cố tình thay đổi tham số nhận dạng trên yêu cầu để truy cập dữ liệu của Tenant B. Hệ thống bắt buộc phải kiểm tra ngữ cảnh Tenant ở mọi tác vụ nghiệp vụ và từ chối ngay với mã lỗi không đủ quyền.
- **Leo thang Đặc quyền (Privilege Escalation)**: Người dùng có vai trò cấp dưới (ví dụ: Receptionist hoặc Hotel Manager) cố gắng tự gán vai trò cao hơn cho chính mình hoặc tài khoản khác. Hệ thống chỉ cho phép người dùng gán các quyền và vai trò nằm trong thẩm quyền quản lý của họ.
- **Xung đột Đa vai trò (Multi-Role Assignment)**: Khi một tài khoản nhân sự được gán đồng thời nhiều vai trò (ví dụ: Hotel Manager kiêm nhiệm ca trực Receptionist ban đêm), hệ thống áp dụng cơ chế cộng dồn quyền (additive permissions) có phạm vi cao nhất trong Tenant đó.
- **Đóng băng Dữ liệu khi Tenant bị Tạm ngưng**: Khi một Tenant bị Quản trị viên hệ thống tạm khóa (`Suspended`), mọi tài khoản nhân viên (Admin tenant, Manager, Receptionist) của Tenant đó sẽ lập tức bị chặn mọi quyền thay đổi dữ liệu, ngăn ngừa phát sinh giao dịch không mong muốn.
- **Khôi phục Tài khoản bị Vô hiệu hóa**: Phân định ranh giới mở khóa rõ ràng: Quản trị viên Hệ thống (System Admin) mở khóa cho các tài khoản cấp Host; Quản trị viên Tenant (Tenant Admin) có toàn quyền mở khóa trực tiếp cho các tài khoản nhân sự (`Hotel Manager`, `Receptionist`) trực thuộc Tenant của mình mà không cần chuyển tiếp yêu cầu lên cấp Host.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Hệ thống PHẢI áp dụng chính sách vai trò cố định (Fixed Standard Roles), chỉ định nghĩa và hỗ trợ đúng 5 nhóm vai trò chuẩn hóa phục vụ toàn diện mô hình vận hành khách sạn (không cho phép tạo vai trò tùy chỉnh hay tùy biến quyền lẻ theo người dùng trong Wave 0):
  1. `Customer` (Khách hàng)
  2. `System Admin` (Quản trị viên Hệ thống / Host Admin)
  3. `Tenant Admin` (Quản trị viên Doanh nghiệp / Tenant Admin)
  4. `Hotel Manager` (Quản lý Khách sạn)
  5. `Receptionist` (Nhân viên Lễ tân)
- **FR-002**: Hệ thống PHẢI phân định phạm vi phân quyền theo 3 cấp độ Multi-Tenancy rõ ràng:
  - Cấp độ Nền tảng (`Host`): Áp dụng cho các tài khoản không trực thuộc Tenant nào (System Admin).
  - Cấp độ Khách thuê (`Tenant`): Áp dụng cho các tài khoản nhân sự trực thuộc doanh nghiệp/chuỗi khách sạn (Tenant Admin, Hotel Manager, Receptionist).
  - Cấp độ Dùng chung Toàn cầu (`Both` / Global Customer): Tài khoản `Customer` được lưu trữ ở cấp độ nền tảng (`TenantId = null`), cho phép khách hàng dùng chung một danh tính duy nhất để đặt phòng tại tất cả các Tenant.
- **FR-003**: Hệ thống PHẢI trao toàn quyền quản lý vòng đời Tenant (tạo mới, chỉnh sửa, tạm khóa, kích hoạt lại, xem danh sách), mở khóa các tài khoản người dùng cấp Host bị khóa do brute-force (`BookingHotelPermissions.Identity.UnlockUser`), và xem toàn bộ nhật ký an ninh/kiểm toán nền tảng cho vai trò `System Admin`.
- **FR-004**: Hệ thống PHẢI giới hạn quyền hạn của `Tenant Admin` nghiêm ngặt trong phạm vi Tenant của mình, bao gồm: quản lý danh sách khách sạn chi nhánh, phân công và gán vai trò cho Hotel Manager và Receptionist, toàn quyền mở khóa các tài khoản nhân sự nội bộ thuộc Tenant bị khóa do brute-force, cấu hình chính sách vận hành chung và xem báo cáo hợp nhất toàn chuỗi.
- **FR-005**: Hệ thống PHẢI hỗ trợ cơ chế phân bổ chi nhánh linh hoạt (Multi-Property Assignment), cho phép `Tenant Admin` phân công một tài khoản `Hotel Manager` phụ trách một hoặc danh sách nhiều cơ sở khách sạn trong cùng Tenant; `Hotel Manager` chỉ có quyền quản lý danh mục buồng phòng, hạng phòng, tiện ích, bảng giá theo mùa, dịch vụ bổ sung, quản lý ca trực lễ tân và xem báo cáo tài chính/công suất trong phạm vi các cơ sở được phân công phụ trách.
- **FR-006**: Hệ thống PHẢI gắn cố định mỗi tài khoản `Receptionist` với duy nhất một cơ sở khách sạn tại một thời điểm (Single-Property Assignment); ngay khi đăng nhập, hệ thống tự động tải trực tiếp sơ đồ buồng phòng (Room Rack) của khách sạn đó và cấp quyền thực hiện các nghiệp vụ: tiếp đón Check-in, Check-out, tạo đặt phòng tại quầy (walk-in), gán số phòng, lập hóa đơn Folio, ghi nhận dịch vụ phát sinh và cập nhật trạng thái buồng phòng thực tế.
- **FR-007**: Hệ thống PHẢI ngăn chặn người dùng có vai trò `Receptionist` tự ý sửa đổi đơn giá niêm yết gốc, sửa chính sách vận hành của khách sạn hoặc truy cập các báo cáo tài chính cấp chiến lược.
- **FR-008**: Hệ thống PHẢI cung cấp cho vai trò `Customer` (tài khoản Global Customer) các quyền tự phục vụ xuyên suốt toàn bộ các Tenant: tra cứu thông tin phòng và tình trạng phòng trống, tạo yêu cầu đặt phòng, thanh toán trực tuyến, xem lịch sử đặt phòng tổng hợp, thực hiện hủy phòng hợp lệ theo chính sách của khách sạn và quản lý hồ sơ cá nhân.
- **FR-009**: Hệ thống PHẢI ngăn chặn tuyệt đối vai trò `Customer` truy cập vào bất kỳ phân hệ điều hành nội bộ, danh sách khách hàng khác hoặc dữ liệu buồng phòng chưa công bố của khách sạn.
- **FR-010**: Hệ thống PHẢI ngăn chặn triệt để hành vi leo thang đặc quyền (Privilege Escalation), đảm bảo người quản lý chỉ có thể cấp các vai trò và quyền hạn thấp hơn hoặc bằng thẩm quyền hiện tại của mình trong cùng một Tenant.
- **FR-011**: Khi một Tenant chuyển sang trạng thái tạm ngưng (`Suspended`), hệ thống PHẢI lập tức chặn toàn bộ quyền tác nghiệp của các tài khoản thuộc Tenant đó.
- **FR-012**: Mọi thao tác gán vai trò, cấp quyền, thu hồi quyền hoặc mở khóa tài khoản PHẢI được ghi nhận đầy đủ vào nhật ký kiểm toán không thể xóa sửa.
- **FR-013**: Hệ thống PHẢI chuẩn bị cơ chế khởi tạo dữ liệu mẫu (Data Seeding) tự động cấp phát 5 vai trò chuẩn này cùng ma trận phân quyền mặc định ngay khi thiết lập môi trường mới.
- **FR-014**: Tất cả tên quyền, nhóm quyền và tên vai trò PHẢI được chuẩn bị tài nguyên hiển thị song ngữ hoàn chỉnh (tiếng Việt và tiếng Anh) trước khi đưa vào khai thác giao diện.

---

### Key Entities

- **Vai trò Hệ thống (System Role)**: Đại diện cho một tập hợp quyền hạn được đóng gói sẵn tương ứng với chức danh công việc (`Customer`, `System Admin`, `Tenant Admin`, `Hotel Manager`, `Receptionist`).
- **Quyền hạn Hạt nhân (Atomic Permission)**: Đơn vị cấp phép nhỏ nhất đại diện cho một khả năng thực thi hành động cụ thể trên một đối tượng tài nguyên (ví dụ: `BookingHotel.Rooms.Create`, `BookingHotel.Bookings.CheckIn`, `BookingHotel.Tenants.Manage`).
- **Phạm vi Đa khách thuê (Multi-Tenancy Scope)**: Thuộc tính xác định quyền hoặc vai trò áp dụng ở tầng Host, Tenant hay Dùng chung.
- **Ma trận Cấp quyền (Permission Grant Matrix)**: Bảng ánh xạ quan hệ nhiều-nhiều giữa Vai trò và Quyền hạn, xác định vai trò nào mặc định được phép thực thi hành động nào.
- **Phân bổ Chi nhánh Người dùng (User Hotel Assignment)**: Mối quan hệ liên kết phân công một tài khoản nhân sự với cơ sở khách sạn trong cùng Tenant: `Hotel Manager` có thể được phân công 1 hoặc nhiều chi nhánh (1-n), `Receptionist` được gắn cố định với duy nhất 1 chi nhánh (1-1) tại một thời điểm để kiểm soát phạm vi truy cập dữ liệu (Data Scoping).
- **Gán Vai trò Người dùng (User Role Mapping)**: Mối liên kết gán một hoặc nhiều vai trò cho một tài khoản người dùng cụ thể trong ngữ cảnh một Tenant (hoặc Host).

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: **100%** các nghiệp vụ cốt lõi trong hệ thống quản lý khách sạn và đặt phòng được phân bổ tương ứng vào đúng 5 vai trò chuẩn mà không bị chồng chéo hoặc mâu thuẫn thẩm quyền.
- **SC-002**: **0%** rò rỉ dữ liệu hoặc vượt quyền chéo giữa các Tenant: 100% các yêu cầu truy cập trái phép qua ranh giới Tenant đều bị từ chối truy cập.
- **SC-003**: Thời gian thiết lập và phân quyền hoàn chỉnh cho một nhân sự mới (Hotel Manager hoặc Receptionist) thông qua vai trò mẫu chuẩn mất **dưới 2 phút**.
- **SC-004**: **0%** khả năng leo thang quyền hạn trái phép: Người dùng cấp dưới hoàn toàn không thể tự kích hoạt hoặc gán quyền cấp cao hơn cho bản thân hoặc tài khoản khác.
- **SC-005**: Khách hàng cá nhân hoàn tất **100%** quy trình tự đặt phòng, thanh toán và tra cứu thông tin kỳ nghỉ độc lập mà không cần bất kỳ sự can thiệp thủ công nào từ nhân viên khách sạn.

---

## Assumptions

- Hệ thống áp dụng 5 vai trò chuẩn đóng gói sẵn với ma trận quyền định sẵn cho Wave 0; việc mở rộng cho phép tạo thêm Custom Roles và tùy chỉnh quyền hạt nhân linh hoạt sẽ được xem xét ở các giai đoạn sau khi hoàn thiện các phân hệ phòng và đặt phòng.
- Hệ thống vận hành trên kiến trúc Đa khách thuê (Multi-Tenancy) nơi Host quản lý danh mục Tenants, và mỗi Tenant đại diện cho một doanh nghiệp/chuỗi khách sạn độc lập.
- Tất cả người dùng đều phải trải qua bước xác thực danh tính hợp lệ trước khi hệ thống đánh giá quyền hạn và vai trò.
- Một người dùng có thể được gán nhiều vai trò trong cùng một Tenant nếu tính chất công việc yêu cầu, và hệ thống sẽ áp dụng nguyên tắc cộng dồn quyền hạn hợp lệ.
- Vai trò `Customer` là tài khoản định danh dùng chung toàn hệ thống (Global Customer Account với `TenantId = null`), cho phép đặt phòng trên tất cả các khách sạn thuộc mọi Tenant; khách vãng lai (Guest) không có tài khoản chỉ có thể thực hiện tra cứu và đặt phòng qua luồng xác thực tạm thời theo số điện thoại/email.
- Dữ liệu cấu hình phân quyền và vai trò chuẩn sẽ được hệ thống nạp tự động qua bộ gieo dữ liệu (Data Seeder) để sẵn sàng sử dụng ngay khi triển khai.

