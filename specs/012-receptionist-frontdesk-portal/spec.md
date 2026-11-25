# Feature Specification: Phân hệ Tác nghiệp Lễ tân (Receptionist Portal)

**Feature Branch**: `012-receptionist-frontdesk-portal`  
**Roadmap Item**: `RM-012`  
**Created**: 2026-11-25  
**Status**: Draft  
**Input**: User description: "RM-012: Xây dựng phân hệ dành cho nhân viên Lễ tân, hỗ trợ tiếp nhận và xử lý các booking được tạo thông qua nền tảng Booking Hotel Platform. Các chức năng chính: Dashboard, Room Rack, Reservation Management, Guest Management, Check-in / Check-out. Phạm vi: Tập trung vào nghiệp vụ tiếp nhận booking và quản lý quá trình lưu trú tại khách sạn. Không bao gồm quản lý giá phòng, gói giá, nhân sự, Housekeeping, bảo trì hoặc các nghiệp vụ quản trị khách sạn chuyên sâu."

## Phạm vi & Giới hạn (Scope & Out-of-Scope)

### Trong phạm vi (In-Scope)
- Toàn bộ nghiệp vụ tiếp nhận booking từ nền tảng **Booking Hotel Platform** và điều phối quá trình lưu trú của khách tại cơ sở khách sạn mà nhân viên Lễ tân được phân công.
- 5 phân khu chức năng cốt lõi của phân hệ Lễ tân:
  1. **Dashboard**: Tổng quan nhanh số lượng booking, khách dự kiến đến, khách đang lưu trú và khách dự kiến trả phòng.
  2. **Room Rack**: Theo dõi sơ đồ phòng, trạng thái sử dụng và tình trạng sẵn sàng đón khách của từng phòng.
  3. **Reservation Management**: Tiếp nhận, tìm kiếm, lọc, xem chi tiết, xác nhận, cập nhật và xử lý các booking.
  4. **Guest Management**: Xem danh sách, tra cứu chi tiết và cập nhật thông tin khách hàng liên quan đến các booking của cơ sở.
  5. **Check-in / Check-out**: Xác minh thông tin khách, xác nhận nhận phòng, trả phòng và cập nhật trạng thái lưu trú đồng bộ với trạng thái phòng.

### Ngoài phạm vi (Out-of-Scope)
- **Không bao gồm** tạo mới booking trực tiếp tại quầy (*Walk-in booking*) — phân hệ Lễ tân chỉ tiếp nhận và xử lý các booking đã được tạo qua nền tảng Booking Hotel Platform.
- **Không bao gồm** quản lý giá phòng, chỉnh sửa giá niêm yết, ghi đè giá theo ngày, quản lý gói giá (*Rate Plans*), chương trình khuyến mại hay tự ý giảm giá.
- **Không bao gồm** quản lý tài khoản nhân sự, phân ca trực, phân quyền vai trò hay cấu hình thông tin chi nhánh khách sạn.
- **Không bao gồm** nghiệp vụ buồng phòng (*Housekeeping* — phân công dọn phòng, cập nhật quy trình dọn dẹp) hay quản lý bảo trì/sửa chữa, lập lịch đóng phòng (*Room Closures*).
- **Không bao gồm** các nghiệp vụ quản trị khách sạn chuyên sâu (báo cáo tài chính/doanh thu chi nhánh, xử lý khiếu nại/đánh giá, cấu hình dịch vụ gia tăng/minibar, hoặc quản lý đa chi nhánh).

## Clarifications

### Session 2026-11-25
- Q: Nhân viên Lễ tân được phép thực hiện những thao tác xử lý trạng thái booking nào trên màn hình Reservation Management? → A: Lễ tân được Xác nhận booking chờ duyệt, Gán/Đổi phòng, Cập nhật thông tin khách/ghi chú, và Hủy booking (bắt buộc nhập lý do); các thao tác Từ chối booking và Đánh dấu No-Show giữ ở thẩm quyền Hotel Manager.
- Q: Ngay sau khi Lễ tân xác nhận Check-out thành công, trạng thái sẵn sàng đón khách của phòng vật lý đó nên chuyển về trạng thái nào? → A: Chuyển phòng về Trống (Vacant) và Sẵn sàng đón khách ngay (không chuyển sang trạng thái Chờ dọn/Bẩn, đảm bảo Lễ tân có thể tiếp tục gán phòng và Check-in booking kế tiếp).
- Q: Trên màn hình Reservation Management, nhân viên Lễ tân có được phép tạo mới booking trực tiếp tại quầy (Walk-in) hay chỉ tiếp nhận và xử lý các booking đã tạo từ nền tảng Booking Hotel Platform? → A: Chỉ tiếp nhận và xử lý các booking đã được tạo từ nền tảng Booking Hotel Platform (không có form tạo booking mới tại quầy trong RM-012).
- Q: Khi Lễ tân xác minh thông tin khách lúc Check-in và quản lý hồ sơ khách trên Guest Management, số giấy tờ tùy thân (CCCD/Hộ chiếu) cần được nhập và lưu trữ theo quy tắc nào? → A: Bắt buộc nhập số CCCD/Hộ chiếu (hoặc đã có sẵn hợp lệ) khi Check-in; hệ thống tự động che một phần (masked, giữ 4 ký tự cuối, ví dụ ********1234) khi lưu/hiển thị và đánh dấu đã xác minh; không lưu ảnh chụp giấy tờ.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Bảng điều khiển tổng quan Lễ tân (Dashboard) (Priority: P1)

Nhân viên Lễ tân khi bắt đầu ca trực cần nhìn thấy ngay bức tranh tổng quan về lưu lượng khách trong ngày của cơ sở mình phụ trách, bao gồm tổng số booking hiện hành, số khách dự kiến đến hôm nay, số khách đang lưu trú và số khách dự kiến trả phòng hôm nay, đồng thời có thể đi thẳng vào danh sách tác nghiệp tương ứng chỉ với một lần chọn.

**Why this priority**: Đây là màn hình làm việc trung tâm giúp Lễ tân nắm bắt khối lượng công việc trong ca trực ngay khi đăng nhập mà không phải dò thủ công qua nhiều danh sách.

**Independent Test**: Có thể kiểm thử độc lập bằng cách đăng nhập bằng tài khoản Lễ tân được phân công vào một cơ sở có các booking ở nhiều trạng thái khác nhau (chờ xác nhận, dự kiến đến hôm nay, đang lưu trú, dự kiến trả phòng hôm nay), kiểm tra các con số thống kê và danh sách tóm tắt khớp hoàn toàn với dữ liệu thực tế của cơ sở đó.

**Acceptance Scenarios**:

1. **Given** nhân viên Lễ tân đã đăng nhập và được phân công vào một cơ sở khách sạn có dữ liệu booking thực tế, **When** truy cập trang Dashboard, **Then** hệ thống hiển thị chính xác 4 chỉ số tổng quan của riêng cơ sở đó: Tổng số booking, Khách dự kiến đến hôm nay, Khách đang lưu trú và Khách dự kiến trả phòng hôm nay.
2. **Given** đang xem trang Dashboard, **When** Lễ tân xem danh sách khách dự kiến đến hôm nay hoặc khách dự kiến trả phòng hôm nay, **Then** mỗi dòng hiển thị rõ mã booking, tên khách, hạng phòng/số phòng (nếu đã gán), ngày nhận–trả phòng, trạng thái booking và thao tác đi nhanh tới màn hình chi tiết hoặc nhận/trả phòng tương ứng.
3. **Given** Lễ tân chọn một thẻ chỉ số tổng quan (ví dụ: Khách dự kiến đến hôm nay), **When** kích hoạt thẻ đó, **Then** hệ thống chuyển sang màn hình quản lý booking hoặc màn hình nhận/trả phòng tương ứng với bộ lọc đã được áp dụng sẵn.
4. **Given** cơ sở khách sạn chưa có bất kỳ booking nào trong ngày, **When** Lễ tân truy cập Dashboard, **Then** các chỉ số hiển thị giá trị `0` và khu vực danh sách hiển thị trạng thái trống chuẩn mực (tuyệt đối không hiển thị dữ liệu mẫu hoặc số liệu giả).

---

### User Story 2 - Quản lý Đặt phòng (Reservation Management) (Priority: P1)

Nhân viên Lễ tân tiếp nhận toàn bộ các booking được tạo thông qua nền tảng Booking Hotel Platform gửi về cơ sở của mình. Lễ tân có thể tìm kiếm nhanh theo mã booking hoặc thông tin khách, lọc theo trạng thái và khoảng ngày lưu trú, xem chi tiết đầy đủ của từng booking, xác nhận booking đang chờ tiếp nhận, gán/đổi phòng phù hợp, cập nhật các thông tin vận hành cho phép và hủy booking theo yêu cầu có lý do bắt buộc (các thao tác Từ chối booking và Đánh dấu No-Show vẫn thuộc thẩm quyền Hotel Manager).

**Why this priority**: Tiếp nhận và xử lý booking từ nền tảng là chức năng xương sống của bộ phận Lễ tân, kết nối trực tiếp từ đơn đặt phòng của khách hàng tới quá trình lưu trú thực tế.

**Independent Test**: Có thể kiểm thử độc lập bằng cách tạo các booking từ nền tảng ở trạng thái Chờ xác nhận và Đã xác nhận, sau đó dùng tài khoản Lễ tân tìm kiếm, lọc, mở chi tiết, xác nhận booking, gán phòng trống cùng hạng phòng và cập nhật ghi chú/yêu cầu đặc biệt; kiểm tra mọi thay đổi được lưu bền vững và hiển thị chính xác sau khi tải lại trang.

**Acceptance Scenarios**:

1. **Given** cơ sở có nhiều booking ở các trạng thái khác nhau, **When** Lễ tân tìm kiếm theo mã booking, tên khách hoặc số điện thoại và kết hợp bộ lọc trạng thái booking, trạng thái gán phòng, khoảng ngày nhận/trả phòng, **Then** danh sách chỉ trả về các booking thuộc cơ sở được phân công khớp đầy đủ điều kiện lọc, sắp xếp ưu tiên theo thời gian nhận phòng/tạo mới rõ ràng.
2. **Given** một booking đang ở trạng thái **Chờ xác nhận** (*Pending Confirmation*), **When** Lễ tân mở chi tiết booking và chọn thao tác **Xác nhận booking**, **Then** hệ thống kiểm tra tính khả dụng của hạng phòng trong khoảng ngày lưu trú; nếu hợp lệ, trạng thái booking chuyển sang **Đã xác nhận** (*Confirmed*) và thông báo thành công.
3. **Given** một booking ở trạng thái **Đã xác nhận** chưa được gán số phòng cụ thể (hoặc cần đổi phòng trước khi nhận phòng), **When** Lễ tân chọn gán/đổi phòng, **Then** hệ thống chỉ gợi ý các phòng vật lý thuộc đúng cơ sở, đúng hạng phòng đã đặt, đang ở trạng thái sẵn sàng và không bị trùng lịch với booking hoặc lịch ngừng khai thác nào khác trong khoảng ngày lưu trú.
4. **Given** Lễ tân mở chi tiết một booking đang hoạt động, **When** cập nhật các thông tin cho phép (thông tin liên hệ khách, yêu cầu đặc biệt, ghi chú tiếp nhận) và lưu lại, **Then** thông tin mới được lưu chính thức, hiển thị ngay trên chi tiết booking mà không làm thay đổi giá phòng hay tổng tiền gốc của booking.
5. **Given** một booking đang ở trạng thái **Chờ xác nhận** hoặc **Đã xác nhận** cần hủy theo yêu cầu, **When** Lễ tân thực hiện thao tác **Hủy booking** và nhập lý do hủy bắt buộc, **Then** hệ thống chuyển trạng thái booking sang **Đã hủy** (*Cancelled*), tự động giải phóng phòng đã gán (nếu có) và lưu lý do hủy vào lịch sử booking; các thao tác **Từ chối booking** và **Đánh dấu No-Show** không hiển thị và bị chặn ở phía máy chủ đối với vai trò Lễ tân.
6. **Given** Lễ tân xem chi tiết booking, **When** quan sát thông tin tài chính (tổng tiền, tiền cọc, trạng thái thanh toán), **Then** các thông tin này hiển thị ở chế độ chỉ đọc để đối chiếu; Lễ tân không có chức năng tự sửa đơn giá phòng hay thay đổi gói giá.

---

### User Story 3 - Xác minh khách & Quy trình Nhận phòng / Trả phòng (Check-in / Check-out) (Priority: P1)

Khi khách đến quầy nhận phòng, Lễ tân tìm nhanh booking của khách, xác minh thông tin định danh của khách (họ tên, số giấy tờ tùy thân CCCD/Hộ chiếu, số điện thoại liên hệ), kiểm tra hoặc gán phòng vật lý sẵn sàng và xác nhận **Check-in** để chuyển booking sang trạng thái đang lưu trú và phòng sang trạng thái đang có khách. Khi khách kết thúc kỳ lưu trú, Lễ tân mở màn hình **Check-out**, rà soát lại thông tin lưu trú và xác nhận trả phòng để hoàn tất booking và cập nhật trạng thái phòng tương ứng.

**Why this priority**: Check-in và Check-out là nghiệp vụ lõi trực tiếp quyết định trạng thái lưu trú của khách và tình trạng sử dụng phòng theo thời gian thực.

**Independent Test**: Có thể kiểm thử độc lập bằng cách chuẩn bị một booking ở trạng thái Đã xác nhận có ngày nhận phòng hôm nay; thực hiện xác minh giấy tờ, gán phòng sẵn sàng và bấm Check-in → xác nhận booking chuyển sang Đang lưu trú và phòng chuyển sang Có khách trên Room Rack; sau đó thực hiện Check-out → xác nhận booking chuyển sang Hoàn tất và phòng được giải phóng khỏi trạng thái Có khách.

**Acceptance Scenarios**:

1. **Given** khách đến nhận phòng với booking ở trạng thái **Đã xác nhận**, **When** Lễ tân mở luồng Check-in, xác minh/bổ sung đầy đủ thông tin bắt buộc của khách (họ tên, số giấy tờ tùy thân, số điện thoại), đảm bảo đã có số phòng vật lý sẵn sàng được gán và nhấn **Xác nhận Check-in**, **Then** trạng thái booking chuyển sang **Đang lưu trú** (*Checked-in*), thời điểm nhận phòng thực tế được ghi nhận, và phòng vật lý tương ứng chuyển ngay sang trạng thái **Có khách** (*Occupied*).
2. **Given** booking đang chờ Check-in nhưng chưa có số phòng cụ thể, **When** Lễ tân thực hiện Check-in, **Then** hệ thống yêu cầu chọn một phòng trống sẵn sàng phù hợp ngay trong bước Check-in trước khi cho phép hoàn tất nhận phòng.
3. **Given** booking không ở trạng thái **Đã xác nhận** (ví dụ: đang Chờ xác nhận, Đã hủy, Đã hoàn tất) hoặc phòng được gán đang bận/ngưng khai thác, **When** Lễ tân cố gắng thực hiện Check-in, **Then** hệ thống từ chối thao tác và hiển thị thông báo rõ nguyên nhân để Lễ tân xử lý trước (ví dụ: cần xác nhận booking hoặc đổi sang phòng khác sẵn sàng).
4. **Given** một booking đang ở trạng thái **Đang lưu trú** (*Checked-in*), **When** Lễ tân mở luồng Check-out, rà soát thông tin phòng và thời gian lưu trú, sau đó nhấn **Xác nhận Check-out**, **Then** trạng thái booking chuyển sang **Hoàn tất** (*Completed*), thời điểm trả phòng thực tế được ghi nhận, liên kết gán phòng hiện tại được kết thúc và phòng vật lý chuyển ngay về trạng thái **Trống** (*Vacant*) và **Sẵn sàng đón khách** (nếu không còn booking tương lai đang giữ phòng ngay lập tức).
5. **Given** thao tác Check-in hoặc Check-out hoàn tất, **When** Lễ tân quay lại Dashboard, Room Rack hoặc Reservation Management, **Then** tất cả số liệu tổng quan, màu sắc/trạng thái phòng và trạng thái booking đều phản ánh đồng nhất dữ liệu mới nhất.

---

### User Story 4 - Sơ đồ Buồng phòng trực quan (Room Rack) (Priority: P2)

Nhân viên Lễ tân cần theo dõi nhanh toàn bộ các phòng vật lý của cơ sở trên một sơ đồ trực quan (theo lưới thẻ hoặc dạng bảng) để biết ngay phòng nào đang sẵn sàng đón khách, phòng nào đã có khách đặt trước, phòng nào đang có khách lưu trú và phòng nào đang không khả dụng/ngưng khai thác, từ đó dễ dàng phối hợp tiếp đón và nhận/trả phòng.

**Why this priority**: Room Rack giúp Lễ tân ra quyết định nhanh tại quầy trong giờ cao điểm, bổ trợ trực tiếp cho Reservation Management và Check-in / Check-out.

**Independent Test**: Có thể kiểm thử độc lập bằng cách truy cập màn hình Room Rack với một cơ sở có nhiều tầng và nhiều trạng thái phòng khác nhau; kiểm tra hiển thị thống kê trạng thái, lọc theo tầng, lọc theo trạng thái, tìm kiếm theo số phòng/tên khách, chuyển đổi giữa chế độ Lưới và Bảng, và nhấn vào từng phòng để xem nhanh thông tin booking tương ứng.

**Acceptance Scenarios**:

1. **Given** cơ sở khách sạn có danh sách phòng vật lý trên nhiều tầng, **When** Lễ tân mở màn hình Room Rack, **Then** hệ thống hiển thị toàn bộ phòng thực tế của cơ sở kèm thanh đếm nhanh theo từng trạng thái sử dụng/sẵn sàng (Tổng số phòng, Sẵn sàng, Đã đặt, Có khách, Không khả dụng), danh sách nút lọc tầng được sinh động từ dữ liệu thật của cơ sở.
2. **Given** đang xem Room Rack, **When** Lễ tân lọc theo Tầng, lọc theo Trạng thái, hoặc nhập từ khóa tìm kiếm theo số phòng / tên hạng phòng / tên khách đang ở, **Then** danh sách phòng ở cả chế độ Lưới trực quan và chế độ Bảng đều cập nhật tức thời chính xác theo bộ lọc.
3. **Given** một phòng đang ở trạng thái **Có khách** hoặc **Đã đặt**, **When** Lễ tân chọn xem nhanh phòng đó, **Then** hệ thống hiển thị thông tin booking gắn liền (mã booking, tên khách, ngày nhận–trả phòng) kèm nút chuyển nhanh tới thao tác **Check-out** (nếu đang có khách) hoặc **Check-in / Xem booking** (nếu đã đặt).
4. **Given** một phòng đang ở trạng thái không sẵn sàng/ngưng khai thác (ví dụ: đang bảo trì, tạm ngưng khai thác hoặc chưa sẵn sàng đón khách), **When** Lễ tân quan sát trên Room Rack, **Then** phòng được đánh dấu rõ ràng là **Không khả dụng / Chưa sẵn sàng** ở chế độ chỉ đọc; Lễ tân không có nút cấu hình bảo trì hay tác vụ quản trị Housekeeping trên màn hình này.

---

### User Story 5 - Quản lý Thông tin Khách hàng theo Booking (Guest Management) (Priority: P2)

Nhân viên Lễ tân cần tra cứu thông tin các khách hàng đã và đang có booking tại cơ sở mình phụ trách, xem lịch sử các lượt đặt phòng của khách tại cơ sở và cập nhật thông tin liên hệ hoặc giấy tờ định danh của khách khi phát hiện sai lệch hoặc bổ sung lúc đón tiếp.

**Why this priority**: Dữ liệu khách hàng chính xác giúp rút ngắn thời gian xác minh khi khách đến nhận phòng và hỗ trợ chăm sóc khách hàng quay lại mà không phụ thuộc vào Quản lý khách sạn.

**Independent Test**: Có thể kiểm thử độc lập bằng cách mở màn hình Guest Management, tìm kiếm một khách hàng theo tên, số điện thoại hoặc email, xem chi tiết hồ sơ kèm danh sách các booking của khách tại cơ sở, sau đó chỉnh sửa số điện thoại/email/số giấy tờ tùy thân của khách và xác nhận dữ liệu mới được lưu và phản ánh đồng bộ trên các booking liên quan.

**Acceptance Scenarios**:

1. **Given** cơ sở đã tiếp nhận các booking từ nền tảng, **When** Lễ tân mở màn hình Guest Management, **Then** hệ thống hiển thị danh sách khách hàng gắn với các booking của cơ sở đó (họ tên, số điện thoại, email, số giấy tờ định danh đã định dạng an toàn, số lượt lưu trú và lượt lưu trú gần nhất tại cơ sở).
2. **Given** đang ở màn hình Guest Management, **When** Lễ tân tìm kiếm theo họ tên, số điện thoại, email hoặc số giấy tờ định danh, **Then** danh sách lọc chính xác các khách hàng thuộc cơ sở hiện tại khớp với từ khóa.
3. **Given** Lễ tân mở chi tiết một khách hàng, **When** xem hồ sơ khách hàng, **Then** hệ thống hiển thị đầy đủ thông tin cá nhân liên quan và danh sách các booking của khách tại cơ sở này (mã booking, ngày nhận–trả phòng, hạng phòng/số phòng, trạng thái booking).
4. **Given** Lễ tân phát hiện sai sót thông tin liên hệ hoặc cần cập nhật số giấy tờ định danh khi tiếp đón khách, **When** chỉnh sửa các trường thông tin hợp lệ (họ tên, số điện thoại, email, số giấy tờ tùy thân) và lưu lại, **Then** hệ thống kiểm tra định dạng hợp lệ, lưu cập nhật và đồng bộ thông tin khách hàng trên các booking đang hoạt động liên quan.
5. **Given** cơ sở chưa có khách hàng/booking nào, **When** Lễ tân mở Guest Management, **Then** màn hình hiển thị trạng thái trống chuẩn mực kèm hướng dẫn rõ ràng.

---

### Edge Cases

- **Xung đột gán phòng đồng thời (Double Assignment / Race Condition)**: Hai nhân viên Lễ tân cùng chọn gán một phòng vật lý trống cho hai booking có khoảng ngày giao nhau → hệ thống phải kiểm tra xung đột tại thời điểm lưu và chỉ cho phép một thao tác thành công; thao tác còn lại nhận thông báo rõ ràng rằng phòng đã được gán và gợi ý chọn phòng khác.
- **Phòng chuyển sang ngưng khai thác trước giờ nhận phòng**: Booking đã được gán phòng từ trước nhưng phòng đó bị Quản lý chuyển sang trạng thái bảo trì/ngưng khai thác trước khi khách đến → khi Lễ tân mở Check-in, hệ thống cảnh báo phòng hiện không sẵn sàng và yêu cầu đổi sang một phòng trống sẵn sàng khác cùng hạng phòng trước khi cho phép Check-in.
- **Check-in sớm hơn ngày dự kiến hoặc Check-out sớm/muộn hơn ngày dự kiến**: Hệ thống cảnh báo trực quan cho Lễ tân biết sự chênh lệch so với lịch dự kiến của booking, ghi nhận chính xác thời điểm nhận/trả phòng thực tế và không cho phép Check-in nếu phòng vẫn đang có khách khác ở.
- **Thông tin định danh khách còn thiếu khi Check-in**: Khách đặt phòng trực tuyến chưa điền số giấy tờ tùy thân (CCCD/Hộ chiếu) → bước Check-in bắt buộc Lễ tân bổ sung và xác nhận thông tin định danh trước khi hoàn tất nhận phòng.
- **Lễ tân chưa được phân công cơ sở khách sạn**: Nếu tài khoản Lễ tân chưa có bản ghi phân công cơ sở hoạt động, toàn bộ 5 màn hình hiển thị thông báo rõ ràng trạng thái chưa được phân công cơ sở và không tải hay rò rỉ dữ liệu từ bất kỳ cơ sở nào khác.
- **Cố tình truy cập chức năng ngoài phạm vi hoặc cơ sở khác**: Nếu người dùng Lễ tân cố tình nhập trực tiếp đường dẫn hoặc gửi yêu cầu tới chức năng Quản lý giá phòng, gói giá, nhân sự, Housekeeping, bảo trì hoặc gửi mã cơ sở khác cơ sở được phân công → hệ thống chặn hoàn toàn với thông báo không có quyền truy cập.
- **Dữ liệu rỗng và lỗi kết nối**: Khi cơ sở chưa có phòng/booking/khách hàng, luôn hiển thị trạng thái trống chuẩn (tuyệt đối không chèn dữ liệu giả). Khi thao tác Xác nhận / Cập nhật / Check-in / Check-out gặp lỗi mạng hoặc lỗi nghiệp vụ, trạng thái trên màn hình không được tự đổi giả mà phải giữ nguyên trạng thái thực từ máy chủ và hiển thị thông báo lỗi cụ thể.

---

## Requirements *(mandatory)*

### Functional Requirements

#### A. Không gian làm việc, Điều hướng & An toàn phân quyền
- **FR-001**: Phân hệ Lễ tân PHẢI được tổ chức thành 5 mục điều hướng chính rõ ràng trên thanh điều hướng bên: **Dashboard**, **Room Rack**, **Reservation Management**, **Guest Management**, và **Check-in / Check-out** (gỡ bỏ/thay thế các mục không thuộc phạm vi như quản lý hóa đơn/Housekeeping khỏi phạm vi phân hệ này).
- **FR-002**: Phân hệ Lễ tân PHẢI kế thừa chuẩn nhận diện màu sắc Lễ tân theo Hiến pháp dự án (màu nhấn Xanh lá tươi `#99FF99`, cấu trúc trung tính Xám `#808080`, chữ đen tương phản cao dễ đọc trong ca trực dài), đồng bộ song ngữ Việt – Anh cho 100% nhãn, trạng thái, thông báo và không chứa văn bản cố định (Zero Fixed Text).
- **FR-003**: Toàn bộ dữ liệu hiển thị và thao tác trên cả 5 chức năng PHẢI được cô lập nghiêm ngặt theo đúng duy nhất cơ sở khách sạn mà nhân viên Lễ tân đang được phân công; hệ thống PHẢI từ chối mọi yêu cầu truy xuất hoặc thay đổi dữ liệu thuộc cơ sở khác hoặc doanh nghiệp (Tenant) khác.
- **FR-004**: Hệ thống PHẢI đảm bảo 100% dữ liệu hiển thị đến từ cơ sở dữ liệu thực (Zero Mock Data), hiển thị trạng thái tải (*loading*) khi truy xuất và trạng thái trống chuẩn mực (*empty state*) khi không có bản ghi.
- **FR-005**: Mọi trường dữ liệu bắt buộc trong biểu mẫu (tìm kiếm có điều kiện bắt buộc, cập nhật khách hàng, xác minh giấy tờ Check-in, lý do hủy booking) PHẢI đánh dấu rõ bằng dấu sao màu đỏ (`*`) theo đúng chuẩn giao diện dự án (chỉ dấu sao màu đỏ, chữ nhãn không in đậm và giữ màu chữ tự nhiên).

#### B. Dashboard (Tổng quan Lễ tân)
- **FR-006**: Màn hình Dashboard PHẢI hiển thị theo thời gian thực 4 thẻ chỉ số tổng quan cho cơ sở được phân công: (1) Tổng số booking đang hiệu lực/trong kỳ, (2) Số khách dự kiến đến hôm nay (*Expected Arrivals*), (3) Số khách đang lưu trú (*In-House*), và (4) Số khách dự kiến trả phòng hôm nay (*Expected Departures*).
- **FR-007**: Màn hình Dashboard PHẢI hiển thị danh sách tác nghiệp nhanh trong ngày (khách sắp đến cần nhận phòng, khách sắp trả phòng và booking mới chờ xác nhận) với nút chuyển nhanh sang xem chi tiết hoặc thực hiện Check-in / Check-out tương ứng.
- **FR-008**: Màn hình Dashboard PHẢI cung cấp nút làm mới dữ liệu thủ công và cập nhật số liệu đồng bộ ngay sau khi có thay đổi từ các thao tác xác nhận booking, Check-in hoặc Check-out.

#### C. Room Rack (Sơ đồ Buồng phòng)
- **FR-009**: Màn hình Room Rack PHẢI hiển thị toàn bộ danh sách phòng vật lý thực tế của cơ sở kèm thông tin cốt lõi: Số phòng, Tầng, Hạng phòng, Trạng thái sử dụng (*Trống / Đã đặt / Đang có khách / Bị chặn*) và Tình trạng sẵn sàng đón khách (*Sẵn sàng / Chưa sẵn sàng / Ngưng khai thác*).
- **FR-010**: Màn hình Room Rack PHẢI hỗ trợ chuyển đổi linh hoạt giữa chế độ **Lưới trực quan (Grid)** và **Dạng bảng (Table)**, hỗ trợ bộ lọc tầng sinh tự động từ dữ liệu phòng thực tế, bộ lọc trạng thái phòng và thanh tìm kiếm nhanh theo số phòng, hạng phòng hoặc tên khách đang lưu trú/đã đặt.
- **FR-011**: Khi Lễ tân chọn một phòng trên Room Rack, hệ thống PHẢI hiển thị khung thông tin chi tiết nhanh về phòng và booking hiện hành gắn với phòng đó (nếu có: mã booking, tên khách, khoảng ngày lưu trú), đồng thời cung cấp lối tắt đi tới màn hình Check-in, Check-out hoặc Chi tiết booking tương ứng.
- **FR-012**: Màn hình Room Rack PHẢI ở chế độ **chỉ theo dõi** đối với các trạng thái vận hành thuộc thẩm quyền Quản lý/Buồng phòng; tuyệt đối không cung cấp thao tác chỉnh sửa giá phòng, chỉnh sửa thông tin phòng, bảo trì phòng hoặc quản trị quy trình Housekeeping.

#### D. Reservation Management (Quản lý Đặt phòng)
- **FR-013**: Màn hình Reservation Management PHẢI hiển thị danh sách có phân trang tất cả booking của cơ sở đã được tạo qua nền tảng Booking Hotel Platform (không bao gồm chức năng tạo booking mới tại quầy), kèm bộ lọc đa tiêu chí: tìm kiếm từ khóa (mã booking, họ tên khách, số điện thoại, email), trạng thái booking, trạng thái gán phòng và khoảng ngày nhận/trả phòng.
- **FR-014**: Hệ thống PHẢI cho phép Lễ tân mở trang/ngăn xem **Chi tiết Booking** hiển thị đầy đủ: mã booking, trạng thái booking, ngày nhận–trả phòng dự kiến và thực tế, hạng phòng đã đặt, số phòng đã gán (nếu có), thông tin khách lưu trú, yêu cầu đặc biệt, ghi chú xử lý, tổng tiền & trạng thái thanh toán (chỉ đọc), và mốc thời gian cập nhật.
- **FR-015**: Hệ thống PHẢI cho phép Lễ tân **Xác nhận (Confirm)** các booking đang ở trạng thái **Chờ xác nhận** sau khi máy chủ kiểm tra vẫn còn quỹ phòng hợp lệ cho hạng phòng và khoảng ngày đã đặt.
- **FR-016**: Hệ thống PHẢI cho phép Lễ tân **Gán phòng hoặc Đổi phòng** cho các booking chưa hoàn tất lưu trú, chỉ liệt kê các phòng trống, sẵn sàng, thuộc đúng hạng phòng đã đặt và không trùng lịch trong khoảng ngày lưu trú; máy chủ PHẢI kiểm tra chống trùng lịch nguyên tử khi lưu.
- **FR-017**: Hệ thống PHẢI cho phép Lễ tân **Cập nhật thông tin vận hành của booking** (thông tin liên hệ khách, yêu cầu đặc biệt, ghi chú lễ tân) và **Hủy booking** (khi booking đang ở trạng thái Chờ xác nhận hoặc Đã xác nhận) với lý do hủy bắt buộc; khi hủy, hệ thống PHẢI tự động giải phóng phòng đã gán (nếu có). Các thao tác **Từ chối booking (*Reject*)** và **Đánh dấu Không đến (*No-Show*)** giữ ở thẩm quyền Hotel Manager và PHẢI bị chặn đối với vai trò Lễ tân.
- **FR-018**: Hệ thống PHẢI ngăn chặn Lễ tân thay đổi đơn giá phòng, gói giá (*Rate Plan*) hoặc tổng giá trị gốc của booking trên cả giao diện lẫn máy chủ.

#### E. Guest Management (Quản lý Khách hàng theo Booking)
- **FR-019**: Màn hình Guest Management PHẢI tổng hợp và hiển thị có phân trang danh sách khách hàng gắn với các booking tại cơ sở hiện tại, hỗ trợ tìm kiếm theo họ tên, số điện thoại, email hoặc số giấy tờ định danh.
- **FR-020**: Khi chọn một khách hàng, hệ thống PHẢI hiển thị hồ sơ chi tiết của khách kèm danh sách đầy đủ các booking và lịch sử lưu trú của khách tại cơ sở đó (liên kết trực tiếp tới chi tiết từng booking).
- **FR-021**: Hệ thống PHẢI cho phép Lễ tân cập nhật thông tin định danh và liên hệ của khách hàng liên quan đến booking (họ tên, số điện thoại, email, số giấy tờ tùy thân CCCD/Hộ chiếu, ghi chú yêu cầu của khách), kiểm tra định dạng hợp lệ trên máy chủ, tự động che một phần số giấy tờ tùy thân (*masked*, chỉ giữ lại 4 ký tự cuối, không lưu bản đầy đủ hay ảnh chụp giấy tờ) và đồng bộ thông tin đã cập nhật vào các booking đang hoạt động tương ứng của khách tại cơ sở.

#### F. Check-in / Check-out (Nhận phòng & Trả phòng)
- **FR-022**: Màn hình Check-in PHẢI hiển thị danh sách booking sẵn sàng nhận phòng (ưu tiên các booking dự kiến đến hôm nay ở trạng thái Đã xác nhận) và hỗ trợ tìm nhanh theo mã booking/tên/SĐT khách. Trước khi hoàn tất Check-in, hệ thống PHẢI yêu cầu Lễ tân: (1) xác minh đầy đủ thông tin khách (họ tên, số điện thoại và số giấy tờ tùy thân CCCD/Hộ chiếu bắt buộc — nếu booking chưa có hoặc cần đối chiếu lại thì bắt buộc nhập, sau đó máy chủ tự động lưu ở dạng *masked* giữ 4 ký tự cuối và ghi nhận trạng thái đã xác minh; không lưu ảnh chụp giấy tờ), và (2) xác nhận phòng vật lý sẵn sàng đã được gán cho booking.
- **FR-023**: Khi Lễ tân xác nhận **Check-in**, máy chủ PHẢI kiểm tra tính hợp lệ (booking đang ở trạng thái Đã xác nhận, phòng được gán đang trống và sẵn sàng, không bị trùng lịch), sau đó thực hiện chuyển trạng thái đồng bộ: booking $\rightarrow$ **Đang lưu trú** (*Checked-in*), lưu thời điểm nhận phòng thực tế, và phòng vật lý $\rightarrow$ **Có khách** (*Occupied*).
- **FR-024**: Màn hình Check-out PHẢI hiển thị danh sách booking đang ở trạng thái **Đang lưu trú** (ưu tiên các booking dự kiến trả phòng hôm nay). Khi Lễ tân rà soát thông tin lưu trú và xác nhận **Check-out**, máy chủ PHẢI kiểm tra booking đang ở trạng thái **Đang lưu trú**, sau đó thực hiện chuyển trạng thái đồng bộ: booking $\rightarrow$ **Hoàn tất** (*Completed*), lưu thời điểm trả phòng thực tế, đóng bản ghi phân bổ phòng hiện hành và chuyển phòng vật lý về **Trống** (*Vacant*) ở trạng thái **Sẵn sàng đón khách ngay** (không chuyển sang trạng thái Chờ dọn/Housekeeping).

### Key Entities *(include if feature involves data)*

- **Booking (Đơn đặt phòng)**: Đại diện cho một lượt đặt phòng từ nền tảng Booking Hotel Platform thuộc một cơ sở khách sạn; gồm mã booking, trạng thái vòng đời (Chờ xác nhận, Đã xác nhận, Đang lưu trú, Hoàn tất, Đã từ chối, Đã hủy, Không đến), khoảng ngày nhận–trả phòng dự kiến, thời điểm nhận–trả phòng thực tế, hạng phòng đặt, thông tin khách hàng, yêu cầu đặc biệt, ghi chú lễ tân, lý do hủy/từ chối và thông tin tài chính chỉ đọc (tổng tiền, tiền cọc, trạng thái thanh toán).
- **Phòng lưu trú (Room)**: Phòng vật lý thuộc cơ sở khách sạn; gồm số phòng, tầng, hạng phòng, trạng thái sử dụng (Trống, Đã đặt, Đang có khách, Bị chặn) và tình trạng sẵn sàng đón khách (dựa trên trạng thái vận hành và trạng thái phòng hiện tại ở chế độ chỉ đọc đối với Lễ tân).
- **Phân bổ phòng (Room Assignment)**: Liên kết giữa một Booking và một Phòng vật lý cụ thể trong khoảng thời gian lưu trú; quản lý hiệu lực gán phòng hiện tại để chống trùng lịch và tự động kết thúc khi trả phòng hoặc hủy booking.
- **Khách lưu trú (Guest Profile / Booking Guest)**: Thông tin nhận diện khách hàng gắn với các booking tại cơ sở; gồm họ tên, số điện thoại, email, số giấy tờ tùy thân (CCCD/Hộ chiếu), trạng thái đã xác minh định danh và lịch sử các lượt đặt phòng tại cơ sở đó.
- **Cơ sở khách sạn được phân công (Assigned Property)**: Cơ sở khách sạn duy nhất mà nhân viên Lễ tân trực thuộc và bị khóa phạm vi dữ liệu trong suốt phiên làm việc.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Nhân viên Lễ tân có thể nắm bắt tình hình ca trực (tổng booking, khách sắp đến, đang ở, sắp trả phòng) trên Dashboard trong vòng **3 giây** kể từ khi mở trang mà không cần thao tác lọc thủ công.
- **SC-002**: Nhân viên Lễ tân tìm thấy một booking hoặc hồ sơ khách cụ thể bằng mã booking, tên hoặc số điện thoại trong vòng **5 giây** và hoàn tất thao tác xác nhận hoặc gán phòng trong **dưới 30 giây**.
- **SC-003**: Quy trình xác minh khách và hoàn tất **Check-in** cho một booking đã xác nhận mất **dưới 90 giây**; quy trình rà soát và hoàn tất **Check-out** mất **dưới 60 giây**.
- **SC-004**: **100%** thay đổi trạng thái từ Xác nhận booking, Gán/Đổi phòng, Check-in, Check-out hoặc Hủy booking được đồng bộ tức thì và nhất quán giữa Dashboard, Room Rack, Reservation Management và Guest Management (0 trường hợp sai lệch trạng thái giữa phòng và booking).
- **SC-005**: **0%** trường hợp gán trùng một phòng vật lý cho hai booking giao nhau về thời gian và **0%** trường hợp tài khoản Lễ tân truy cập hoặc thay đổi được dữ liệu của cơ sở khác hay các nghiệp vụ ngoài phạm vi (giá phòng, gói giá, nhân sự, Housekeeping, bảo trì).
- **SC-006**: **100%** màn hình, nhãn trường, thông báo lỗi/thành công và trạng thái hiển thị đầy đủ song ngữ Việt – Anh và không chứa bất kỳ dữ liệu giả lập nào khi cơ sở dữ liệu trống.

---

## Assumptions

- **Quy trình thanh toán & Hóa đơn (Folio)**: Trong phạm vi RM-012, thông tin tổng tiền, tiền đặt cọc và trạng thái thanh toán của booking từ nền tảng Booking Hotel Platform được hiển thị để Lễ tân đối chiếu khi nhận/trả phòng; việc lập hóa đơn dịch vụ phát sinh (minibar, giặt là) hoặc điều chỉnh giá không thuộc phạm vi RM-012.
- **Trạng thái phòng sau Check-out**: Do nghiệp vụ Housekeeping nằm ngoài phạm vi RM-012, khi Lễ tân xác nhận Check-out, phòng chuyển trực tiếp về **Trống (*Vacant*)** và **Sẵn sàng đón khách ngay** (không chuyển sang trạng thái Chờ dọn) để quy trình Check-in/Check-out khép kín.
- **Nguồn tạo Booking**: Toàn bộ booking được khởi tạo từ nền tảng Booking Hotel Platform; trong phạm vi RM-012, Lễ tân chỉ tiếp nhận, xác nhận, gán/đổi phòng, cập nhật thông tin vận hành và thực hiện Check-in/Check-out, không có màn hình tạo Walk-in booking tại quầy.
- **Định danh & Bảo mật thông tin khách**: Khi Check-in, bắt buộc có số giấy tờ tùy thân (CCCD/Hộ chiếu) hợp lệ; máy chủ tự động chuẩn hóa và lưu ở dạng che một phần (*masked*, giữ 4 ký tự cuối, ví dụ `********1234`) kèm trạng thái đã xác minh nhằm bảo vệ quyền riêng tư, không lưu ảnh chụp giấy tờ.
- **Phân công cơ sở (1-1 Constraint)**: Nhân viên Lễ tân thuần túy gắn với đúng một cơ sở khách sạn; nhân sự kiêm nhiệm Quản lý & Lễ tân khi chuyển sang phân hệ Lễ tân vẫn giữ ngữ cảnh cơ sở đang chọn hợp lệ trong danh sách được phân công.
- **Hạ tầng có sẵn**: Tái sử dụng hệ thống xác thực, phân quyền người dùng, phân công cơ sở khách sạn, danh mục phòng vật lý/hạng phòng (từ RM-010) và dữ liệu booking hiện hữu của hệ thống Booking Hotel.
