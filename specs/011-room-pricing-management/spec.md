# Feature Specification: RM-011 – QUẢN LÝ GIÁ PHÒNG (ROOM PRICING MANAGEMENT)

**Feature Branch**: `011-room-pricing-management`  
**Created**: 2026-11-25  
**Status**: Draft  
**Spec Path**: `specs/011-room-pricing-management/spec.md`  
**Input**: `/speckit-specify RM-011 # SPECIFICATION – QUẢN LÝ GIÁ PHÒNG (ROOM PRICING MANAGEMENT)`

---

## 1. Tổng Quan & Phạm Vi (Overview & Scope)

### 1.1. Mục đích
Chức năng **Quản lý giá phòng (Room Pricing Management)** cho phép **Hotel Manager** thiết lập, theo dõi và điều chỉnh giá bán phòng linh hoạt theo từng **Hạng phòng (Room Type)** và **Gói giá (Rate Plan)** tại cơ sở khách sạn đang phụ trách.

**Nguyên tắc thiết kế cốt lõi**:
- Tách biệt rõ ràng **Giá cơ bản (`Base Price`)**, **Giá áp dụng thực tế (`Effective Price`)** và **Giá ghi đè (`Price Override`)**.
- Mọi thao tác thiết lập và điều chỉnh giá đều xác định tường minh phạm vi tổ hợp 3 chiều:
  **`Room Type + Rate Plan + Applicable Date`**

### 1.2. Đối tượng sử dụng
| Vai trò | Phạm vi & Quyền hạn |
|---|---|
| **Hotel Manager** | Quản lý toàn bộ giá phòng, gói giá, lịch giá theo ngày, điều chỉnh hàng loạt, CTA/CTD và lịch ghi đè thuộc các khách sạn được phân công (`UserHotelAssignment`). |
| **Tenant Admin** | Quản lý và giám sát giá phòng các cơ sở thuộc Tenant theo phạm vi phân quyền hệ thống. |

### 1.3. Phạm vi dữ liệu & Ví dụ nghiệp vụ
- Mỗi gói giá thuộc một hạng phòng cụ thể và có mức `Base Price` riêng.
- Ví dụ:
  - Room Type: `Phòng đôi Superior`
  - Rate Plan: `Standard Rate`
  - Applicable Date: `25/11/2026`
  - Base Price: `400.000 VNĐ`
  - Override Price: `2.000.000 VNĐ` $\rightarrow$ Effective Price: `2.000.000 VNĐ`
- Không hiển thị hoặc cho phép tác động tới dữ liệu giá thuộc khách sạn hoặc Tenant khác.
- **Ngoài phạm vi (Out of Scope)**: Cấu hình phụ thu nhận phòng sớm (`Early Check-in`), trả phòng muộn (`Late Check-out`) và phụ thu thêm khách (`Extra Guest`) không thuộc phạm vi RM-011 và được tách sang hạng mục độc lập sau.

---

## Clarifications

### Session 2026-11-25

- Q: Khi Hotel Manager chỉnh sửa thông tin một gói giá tại màn hình Quản lý giá phòng, phạm vi các trường được phép thay đổi so với gói giá chuẩn chuỗi (RM-009) nên được áp dụng theo cơ chế nào? → A: Hotel Manager chỉ chỉnh sửa `Base Price` (giá cơ bản tại chi nhánh cho hạng phòng) và bật/tắt trạng thái `Active`/`Inactive`; các thông tin chuẩn chuỗi (Tên gói, Mã gói, Chính sách ăn sáng, Chính sách hoàn hủy, Mô tả) hiển thị chỉ đọc và do Tenant Admin quản lý tại RM-009.
- Q: Khi một `Price Override` được tạo theo phương thức phần trăm (`Percentage Adjustment`) và sau đó `Base Price` của gói giá thay đổi, mức giá hiệu lực (`Effective Price`) của các ngày đã ghi đè nên được xử lý và làm tròn VNĐ như thế nào? → A: Chốt giá tại thời điểm lưu (`Snapshot`), làm tròn `CalculatedEffectivePrice` về bội số `1.000 VNĐ` gần nhất (tối thiểu `1.000 VNĐ` và luôn `> 0`); thay đổi `Base Price` về sau không làm đổi giá của các override đã lưu.
- Q: Hệ thống nên xử lý như thế nào đối với các thao tác tạo mới, chỉnh sửa hoặc xóa `Price Override` (và `CTA`/`CTD`) rơi vào các ngày trong quá khứ (`Applicable Date < Today`)? → A: Khóa hoàn toàn ngày quá khứ (`< Today`) ở chế độ chỉ đọc; chặn tạo mới khi `Start Date < Today`; nếu chỉnh sửa hoặc xóa một khoảng ghi đè cũ vắt ngang ngày hôm nay (`Start Date < Today <= End Date`), hệ thống bảo toàn nguyên vẹn phần quá khứ (`< Today`) và chỉ áp dụng thay đổi/xóa cho phần từ `Today` trở đi.
- Q: So với mô tả tổng quan trên `roadmap.md`, phạm vi tính năng bổ sung nào nên được chốt vào RM-011 (lọc thứ trong tuần khi điều chỉnh hàng loạt, chỉnh sửa nhanh trực tiếp trên ô lịch kèm bật/tắt riêng `CTA`/`CTD`, và cấu hình phụ thu nhận/trả phòng sớm-muộn)? → A: Bổ sung lọc thứ trong tuần (`T2–CN`, mặc định chọn cả tuần) ở Bulk Adjustment, cho phép chỉnh sửa nhanh giá và `CTA`/`CTD` trực tiếp trong modal chi tiết ngày trên lịch (với ngày `>= Today`), hỗ trợ chế độ chỉ thay đổi `CTA`/`CTD` mà giữ nguyên giá; tách cấu hình Phụ thu (`Early Check-in`, `Late Check-out`, `Extra Guest`) ra ngoài phạm vi RM-011.
- Q: Nhật ký thay đổi giá (`PricingAuditLog` cho 6 nhóm thao tác) nên được lưu trữ và hiển thị cho Hotel Manager / Tenant Admin như thế nào trong RM-011? → A: Lưu trữ vào bảng `PricingAuditLog` chuyên biệt và bổ sung modal/drawer "Lịch sử thay đổi giá" ngay trên màn hình Quản lý giá phòng, hỗ trợ phân trang và lọc theo Gói giá, Loại thao tác và Khoảng ngày thực hiện (hiển thị Người thực hiện, Thời điểm, Giá trị trước/sau, Phạm vi ngày bị ảnh hưởng).

---

## 2. User Scenarios & Testing *(mandatory)*

### User Story 1 - Lựa chọn Hạng phòng & Quản lý Danh mục Gói giá (Priority: P1)

Hotel Manager chọn hạng phòng cần cấu hình tại khách sạn hiện tại, xem tổng quan chỉ số giá và quản lý danh mục gói giá (Rate Plans) thuộc hạng phòng đó bao gồm bật/tắt kinh doanh và chỉnh sửa `Base Price` tại chi nhánh.

**Why this priority**: Là điểm vào bắt buộc xác định phạm vi `Room Type + Rate Plan` cho toàn bộ màn hình lịch giá, điều chỉnh hàng loạt và danh sách ghi đè.

**Independent Test**: Có thể kiểm thử độc lập bằng cách chọn từng hạng phòng, bật/tắt trạng thái Active/Inactive của gói giá, chỉnh sửa `Base Price` tại chi nhánh của gói giá (trong khi các chính sách chuẩn chuỗi hiển thị chỉ đọc) và xác nhận dữ liệu cập nhật chính xác mà không làm mất lịch ghi đè cũ.

**Acceptance Scenarios**:

1. **Given** Hotel Manager đang mở màn hình Quản lý giá phòng của khách sạn hiện tại, **When** màn hình tải xong, **Then** Section 1 hiển thị đầy đủ Tên khách sạn, Mã cơ sở, Hạng phòng đang chọn, Giá cơ bản tham khảo, Số lượng gói giá thuộc hạng phòng và Số lượng lịch ghi đè đang tồn tại.
2. **Given** Hotel Manager thay đổi Hạng phòng đang chọn, **When** lựa chọn mới được áp dụng, **Then** toàn bộ danh sách gói giá, lịch giá theo ngày và danh sách lịch ghi đè được tải lại đồng bộ theo hạng phòng mới.
3. **Given** danh mục gói giá (Section 2) hiển thị dưới dạng bảng hoặc card thu gọn, **When** Hotel Manager xem từng gói giá, **Then** hiển thị đầy đủ Tên gói giá, Mã gói giá, Loại gói giá, Giá cơ bản (`Base Price`), Giá áp dụng thực tế (`Effective Price`), Chính sách ăn sáng, Chính sách hoàn hủy và Trạng thái kinh doanh (`Active`/`Inactive`).
4. **Given** một gói giá đang `Active` và có lịch ghi đè tồn tại, **When** Hotel Manager chuyển trạng thái sang `Inactive`, **Then** gói giá không còn được sử dụng cho booking mới nhưng toàn bộ `Base Price`, lịch ghi đè và lịch sử đặt phòng trước đó vẫn được bảo toàn nguyên vẹn.
5. **Given** một gói giá đã có các Price Override theo ngày, **When** Hotel Manager chỉnh sửa `Base Price` tại chi nhánh của gói giá (các trường chuẩn chuỗi như tên gói, mã gói, chính sách ăn sáng, chính sách hoàn hủy, mô tả ở chế độ chỉ đọc theo RM-009), **Then** `Base Price` mới chỉ áp dụng cho các ngày không có Price Override và không tự động ghi đè hoặc xóa các Price Override đã tồn tại.

---

### User Story 2 - Lịch giá theo ngày (Daily Pricing Calendar) (Priority: P1)

Hotel Manager theo dõi giá bán thực tế (`Effective Price`) từng ngày trong tháng trên giao diện lịch trực quan cho cặp `Room Type + Rate Plan` đang chọn, nhận diện nhanh ngày có giá ghi đè hoặc hạn chế `CTA`/`CTD`, và chọn từng ngày để xem chi tiết.

**Why this priority**: Cung cấp tầm nhìn trực quan cốt lõi giúp quản lý kiểm soát giá bán thực tế từng ngày và phát hiện ngay sai lệch giá hoặc hạn chế bán phòng.

**Independent Test**: Có thể kiểm thử độc lập bằng cách điều hướng qua các tháng, kiểm tra màu sắc/nhãn phân biệt `Base Price`, `Override Price`, `CTA`, `CTD` trên từng ô ngày và click vào một ngày bất kỳ để kiểm tra bảng chi tiết ngày.

**Acceptance Scenarios**:

1. **Given** Hotel Manager đã chọn một Hạng phòng và một Gói giá, **When** xem Section 3 (Daily Pricing Calendar), **Then** lịch hiển thị theo tháng đầy đủ các ngày trong tháng với `Effective Price`, nhãn/màu phân biệt `Base Price` so với `Override Price`, trạng thái `CTA`, `CTD` và trạng thái đóng/mở bán theo ngày.
2. **Given** một ngày đồng thời có giá ghi đè và cấu hình `CTA` hoặc `CTD`, **When** hiển thị trên lịch, **Then** ô ngày hiển thị rõ ràng đồng thời cả giá ghi đè và các huy hiệu `CTA`/`CTD`.
3. **Given** Hotel Manager nhấn chuyển tháng trước, tháng tiếp theo hoặc chọn tháng/năm cụ thể, **When** tháng thay đổi, **Then** hệ thống tải và hiển thị chính xác dữ liệu lịch giá của tháng tương ứng.
4. **Given** Hotel Manager click chọn một ngày cụ thể trên lịch, **When** panel/modal chi tiết ngày mở ra, **Then** hiển thị đầy đủ `Base Price`, `Effective Price`, thông tin `Price Override` (nếu có), trạng thái `CTA`, `CTD` và trạng thái kinh doanh của gói giá; đồng thời với các ngày `>= Today` (và gói giá đang `Active`), cho phép chỉnh sửa nhanh giá hoặc chỉ bật/tắt `CTA`/`CTD` trực tiếp ngay trong modal (ngày `< Today` hiển thị chỉ đọc).
5. **Given** Hotel Manager vừa thực hiện thay đổi `Base Price`, tạo/sửa/xóa `Price Override` hoặc thay đổi `CTA`/`CTD`, **When** thao tác lưu thành công, **Then** lịch giá theo ngày được làm mới tức thì phản ánh đúng giá trị mới nhất.

---

### User Story 3 - Điều chỉnh giá hàng loạt có Xem trước & Xử lý trùng ngày (Priority: P1)

Hotel Manager thiết lập giá bán mới hoặc điều chỉnh theo phần trăm kèm cấu hình `CTA`/`CTD` cho một khoảng thời gian (`Start Date` đến `End Date`), xem trước toàn bộ tác động và chủ động chọn phương án xử lý khi phát hiện ngày đã có Price Override trùng lặp.

**Why this priority**: Là nghiệp vụ cốt lõi giúp vận hành giá phòng theo mùa, cuối tuần và dịp lễ nhanh chóng, an toàn mà không phải sửa thủ công từng ngày và không vô tình ghi đè dữ liệu cũ.

**Independent Test**: Có thể kiểm thử độc lập bằng cách điền form Bulk Price Adjustment với cả hai chế độ `Fixed Price` và `Percentage Adjustment`, kiểm tra màn hình Preview và xác minh cả 3 nhánh xử lý trùng ngày (`Replace Existing`, `Skip Existing`, `Cancel`).

**Acceptance Scenarios**:

1. **Given** một gói giá đang ở trạng thái `Active` được chọn, **When** Hotel Manager nhập `Start Date`, `End Date` (tính cả 2 đầu mút), chọn phương thức **Fixed Price** (ví dụ `2.000.000 VNĐ`) và bật/tắt `CTA`/`CTD` tùy ý, **Then** hệ thống hiển thị bước **Preview** gồm: Khách sạn, Hạng phòng, Gói giá, Khoảng ngày, Loại điều chỉnh, Giá hiện tại, Giá mới dự kiến, CTA/CTD, Tổng số ngày bị ảnh hưởng và danh sách các ngày bị trùng lặp (nếu có).
2. **Given** Hotel Manager chọn phương thức **Percentage Adjustment** (ví dụ tăng `+20%` trên `Base Price = 400.000 VNĐ`), **When** mở Preview và xác nhận, **Then** `Effective Price` dự kiến được tính đúng theo `Base Price` (`480.000 VNĐ`) và lưu thành công sau khi xác nhận.
3. **Given** khoảng ngày được chọn có các ngày đã tồn tại Price Override, **When** Hotel Manager nhấn áp dụng, **Then** hệ thống bắt buộc cảnh báo danh sách ngày trùng và yêu cầu chọn 1 trong 3 phương án:
   - **Replace Existing**: Thay thế các giá ghi đè cũ trong phạm vi đã chọn.
   - **Skip Existing**: Giữ nguyên các ngày đã có ghi đè, chỉ áp dụng cho các ngày chưa có.
   - **Cancel**: Hủy thao tác, không thay đổi dữ liệu.
4. **Given** Hotel Manager muốn áp dụng điều chỉnh cho nhiều gói giá cùng lúc, **When** mở chức năng chọn phạm vi nhiều Rate Plan, **Then** hệ thống hiển thị danh sách chọn rõ ràng và liệt kê đầy đủ tất cả các Rate Plan bị tác động trong bước Preview trước khi xác nhận.
5. **Given** gói giá đang ở trạng thái `Inactive`, **When** Hotel Manager cố gắng tạo điều chỉnh giá mới cho gói đó, **Then** hệ thống chặn thao tác và hiển thị thông báo rõ ràng.

---

### User Story 4 - Quản lý danh sách Lịch giá ghi đè (Price Override Management) (Priority: P2)

Hotel Manager xem, chỉnh sửa hoặc xóa các khoảng thời gian đã được thiết lập Price Override thuộc hạng phòng và gói giá đang chọn.

**Why this priority**: Giúp kiểm soát vòng đời các đợt điều chỉnh giá đã tạo, dễ dàng sửa đổi khi kế hoạch giá thay đổi hoặc hủy bỏ để giá tự động quay về `Base Price`.

**Independent Test**: Có thể kiểm thử độc lập bằng cách xem bảng danh sách lịch ghi đè, mở xem chi tiết, chỉnh sửa khoảng ngày (kèm kiểm tra xung đột) và xóa một lịch ghi đè để xác nhận lịch giá quay về `Base Price`.

**Acceptance Scenarios**:

1. **Given** cặp `Room Type + Rate Plan` đang chọn có các khoảng giá ghi đè, **When** Hotel Manager xem Section 5 (Price Override List), **Then** danh sách hiển thị đầy đủ: Ngày bắt đầu, Ngày kết thúc, Loại điều chỉnh, Giá hoặc tỷ lệ điều chỉnh, Trạng thái `CTA`, Trạng thái `CTD`, Ngày tạo, Người thực hiện và các nút thao tác (`View`, `Edit`, `Delete`).
2. **Given** Hotel Manager chọn `Edit` một lịch ghi đè, **When** thay đổi khoảng thời gian, mức giá/tỷ lệ hoặc `CTA`/`CTD` và lưu, **Then** hệ thống kiểm tra xung đột với các lịch ghi đè khác của cùng `Room Type + Rate Plan`; nếu không xung đột thì cập nhật và đồng bộ lại lịch giá.
3. **Given** Hotel Manager chọn `Delete` một lịch ghi đè, **When** xác nhận trên hộp thoại xác nhận, **Then** lịch ghi đè bị gỡ bỏ, các ngày liên quan quay về sử dụng `Base Price` (hoặc mức giá hợp lệ khác), lịch giá được cập nhật lại và lịch sử các booking đã phát sinh trước đó không bị thay đổi.

---

### User Story 5 - Lịch sử thao tác (Audit Log) & Bảo toàn Booking (Priority: P2)

Mọi thay đổi liên quan đến giá phòng, gói giá và hạn chế `CTA`/`CTD` được ghi vết Audit Log đầy đủ và hoàn toàn không ảnh hưởng đến giá đã chốt trong các booking đã xác nhận.

**Why this priority**: Đảm bảo tính minh bạch tài chính, khả năng truy vết khi xảy ra sai lệch doanh thu và tính nhất quán dữ liệu giao dịch.

**Independent Test**: Thực hiện 6 nhóm thao tác (tạo/sửa/xóa Override, sửa Base Price, bật/tắt Rate Plan, đổi CTA/CTD), sau đó kiểm tra bản ghi Audit Log tương ứng và kiểm tra giá của một booking đã xác nhận trước đó không hề thay đổi.

**Acceptance Scenarios**:

1. **Given** người dùng thực hiện tạo/sửa/xóa Price Override, thay đổi Base Price, bật/tắt Rate Plan hoặc thay đổi CTA/CTD, **When** thao tác hoàn tất, **Then** hệ thống ghi nhận bản ghi `PricingAuditLog` chứa đầy đủ: Người thực hiện, Thời điểm, Room Type, Rate Plan, Loại thao tác, Giá trị trước thay đổi, Giá trị sau thay đổi và Khoảng thời gian bị ảnh hưởng.
2. **Given** Hotel Manager hoặc Tenant Admin nhấn nút "Lịch sử thay đổi giá" trên màn hình Quản lý giá phòng, **When** modal/drawer nhật ký mở ra và lọc theo Gói giá, Loại thao tác hoặc Khoảng ngày thực hiện, **Then** hệ thống hiển thị danh sách `PricingAuditLog` phân trang đúng phạm vi khách sạn đang chọn với đầy đủ thông tin truy vết.
3. **Given** một booking đã được xác nhận trước đó với giá `400.000 VNĐ/đêm`, **When** Hotel Manager tạo Price Override `2.000.000 VNĐ/đêm` cho đúng ngày lưu trú đó, **Then** tổng giá và đơn giá đã lưu trong booking cũ vẫn giữ nguyên `400.000 VNĐ/đêm`.

---

### Edge Cases

- **Khoảng ngày biên & Ngày quá khứ**: `Start Date == End Date` (áp dụng đúng 1 ngày, `>= Today`) phải hợp lệ; `Start Date > End Date` hoặc tạo mới với `Start Date < Today` phải bị chặn ở cả Frontend và Backend. Các ngày quá khứ (`< Today`) hiển thị chỉ đọc; các khoảng ghi đè cũ đã kết thúc hoàn toàn (`End Date < Today`) không cho phép sửa/xóa; nếu khoảng ghi đè cũ vắt ngang ngày hôm nay (`Start Date < Today <= End Date`), thao tác sửa/xóa phải tách giữ nguyên phần quá khứ (`[Start Date .. Yesterday]`) và chỉ tác động phần từ `Today` trở đi (`[Today .. End Date]`).
- **Giá trị phần trăm âm cực đoan**: Điều chỉnh giảm phần trăm (ví dụ `-100%` hoặc `-120%`) dẫn đến giá tính ra $\le 0$ phải bị từ chối với thông báo lỗi rõ ràng (vi phạm BR-06).
- **Xung đột chồng lấn một phần (Partial Overlap)**: Khi tạo/sửa Price Override `[05/12 – 15/12]` chồng lấn một phần với lịch cũ `[10/12 – 20/12]`, hệ thống phải phát hiện chính xác các ngày giao nhau `[10/12 – 15/12]` và xử lý tách/gộp nhất quán theo lựa chọn `Replace Existing` hoặc `Skip Existing` mà không để tồn tại 2 bản ghi xung đột cho cùng một ngày (BR-07).
- **Lỗi mạng / lỗi máy chủ khi lưu**: Hệ thống hiển thị thông báo lỗi rõ ràng và giữ nguyên toàn bộ dữ liệu người dùng đã nhập trên form để thử lại, không làm mất dữ liệu form.
- **Chuyển đổi khách sạn đang quản lý (Property Switcher)**: Khi Hotel Manager đổi sang khách sạn khác trên Topbar, toàn bộ trạng thái đang chọn (Room Type, Rate Plan, Calendar, Override List) phải reset và tải dữ liệu của khách sạn mới (BR-14).
- **Truy cập trái phép / Cross-Tenant / Cross-Hotel**: Mọi request gửi `HotelId`, `RoomTypeId` hoặc `RatePlanId` không thuộc phạm vi phân quyền của người dùng phải bị từ chối ở Backend (BR-13, BR-14).

---

## 3. Requirements *(mandatory)*

### 3.1. Functional Requirements

- **FR-001**: Hệ thống PHẢI hiển thị thông tin khách sạn hiện tại (tên khách sạn, mã cơ sở) và bộ chọn Hạng phòng kèm tổng quan (giá cơ bản tham khảo, số lượng gói giá, số lượng lịch ghi đè).
- **FR-002**: Hệ thống PHẢI tự động cập nhật đồng bộ danh mục gói giá, lịch giá theo ngày và danh sách lịch ghi đè ngay khi Hotel Manager thay đổi Hạng phòng.
- **FR-003**: Hệ thống PHẢI hiển thị toàn bộ gói giá thuộc hạng phòng đang chọn (dạng bảng hoặc card thu gọn) với đầy đủ: Tên gói, Mã gói, Loại gói, `Base Price`, `Effective Price`, Chính sách ăn sáng, Chính sách hoàn hủy và Trạng thái kinh doanh (`Active`/`Inactive`).
- **FR-004**: Hệ thống PHẢI cho phép bật/tắt trạng thái `Active`/`Inactive` của từng gói giá mà không xóa `Base Price`, lịch ghi đè hay lịch sử đặt phòng.
- **FR-005**: Hệ thống PHẢI cho phép Hotel Manager chỉnh sửa `Base Price` (giá cơ bản tại chi nhánh cho hạng phòng) của gói giá mà không tự động xóa hoặc ghi đè các `Price Override` hiện có; các thông tin chuẩn chuỗi (Tên gói, Mã gói, Chính sách ăn sáng, Chính sách hoàn hủy, Mô tả) hiển thị chỉ đọc và chỉ do Tenant Admin quản lý tại RM-009.
- **FR-006**: Hệ thống PHẢI cung cấp Lịch giá theo tháng (Daily Pricing Calendar) hiển thị từng ngày với `Effective Price`, phân biệt trực quan `Base Price` vs `Override Price`, trạng thái `CTA`, `CTD` và trạng thái đóng/mở bán theo ngày.
- **FR-007**: Hệ thống PHẢI hỗ trợ điều hướng lịch (tháng trước, tháng sau, chọn tháng/năm cụ thể) và cho phép click vào một ngày bất kỳ để xem chi tiết đầy đủ (`Base Price`, `Effective Price`, `Price Override`, `CTA`, `CTD`, trạng thái gói giá) kèm thao tác chỉnh sửa nhanh giá và/hoặc `CTA`/`CTD` trực tiếp cho các ngày `>= Today` (khi gói giá `Active`; ngày `< Today` chỉ đọc).
- **FR-008**: Hệ thống PHẢI tự động làm mới lịch giá ngay sau mọi thao tác thay đổi `Base Price`, tạo/sửa/xóa `Price Override` hoặc cập nhật `CTA`/`CTD`.
- **FR-009**: Hệ thống PHẢI cung cấp chức năng Điều chỉnh giá hàng loạt theo khoảng ngày (`Start Date` đến `End Date`, tính cả hai ngày đầu mút, `Start Date >= Today`) kèm bộ lọc **thứ trong tuần** (`T2–CN`, mặc định chọn cả 7 ngày) cho 1 gói giá đang chọn mặc định hoặc nhiều gói giá được chọn tường minh.
- **FR-010**: Hệ thống PHẢI hỗ trợ 3 chế độ áp dụng: **Fixed Price** (giá cố định), **Percentage Adjustment** (tăng/giảm theo `%` tính trên `Base Price` tại thời điểm lưu, làm tròn `CalculatedEffectivePrice` về bội số `1.000 VNĐ` gần nhất, tối thiểu `1.000 VNĐ`, và chốt cứng giá trị `Snapshot` này để không biến động khi `Base Price` thay đổi về sau), và **Restrictions Only** (giữ nguyên giá hiện hành/giá cơ bản, chỉ cập nhật cờ `CTA`/`CTD`).
- **FR-011**: Hệ thống PHẢI hỗ trợ cấu hình độc lập hai cờ `CTA` (Closed to Arrival) và `CTD` (Closed to Departure) khi điều chỉnh giá hàng loạt, chỉnh sửa nhanh theo ngày hoặc chỉnh sửa lịch ghi đè, bao gồm khả năng chỉ bật/tắt `CTA`/`CTD` mà không thay đổi giá bán.
- **FR-012**: Hệ thống PHẢI hiển thị màn hình/hộp thoại **Preview** trước khi lưu điều chỉnh giá hàng loạt, gồm: Khách sạn, Hạng phòng, Gói giá, Khoảng ngày, Loại điều chỉnh, Giá hiện tại, Giá mới dự kiến, trạng thái `CTA`/`CTD`, số ngày bị ảnh hưởng và các ngày có ghi đè trùng lặp.
- **FR-013**: Khi phát hiện ngày trùng lặp `Price Override`, hệ thống PHẢI không âm thầm ghi đè mà bắt buộc thông báo và cho người dùng chọn giữa `Replace Existing`, `Skip Existing` hoặc `Cancel`.
- **FR-014**: Hệ thống PHẢI hiển thị Danh sách lịch giá ghi đè (`Price Override List`) thuộc `Room Type + Rate Plan` đang chọn với các cột: Ngày bắt đầu, Ngày kết thúc, Loại điều chỉnh, Giá/Tỷ lệ, `CTA`, `CTD`, Ngày tạo, Người thực hiện và thao tác `View`, `Edit`, `Delete`.
- **FR-015**: Hệ thống PHẢI cho phép chỉnh sửa lịch ghi đè (khoảng ngày, giá/tỷ lệ, `CTA`, `CTD`) kèm kiểm tra xung đột với các lịch ghi đè khác.
- **FR-016**: Hệ thống PHẢI yêu cầu xác nhận trước khi xóa lịch ghi đè; sau khi xóa, các ngày bị ảnh hưởng quay về `Base Price` (hoặc mức giá hợp lệ khác) và không ảnh hưởng tới lịch sử booking.
- **FR-017**: Hệ thống PHẢI ghi nhận nhật ký chuyên biệt (`PricingAuditLog`) cho tất cả 6 loại thao tác (Tạo Price Override, Chỉnh sửa Price Override, Xóa Price Override, Thay đổi Base Price, Bật/Tắt Rate Plan và Thay đổi CTA/CTD) và cung cấp modal/drawer **Lịch sử thay đổi giá** ngay trên màn hình Quản lý giá phòng hỗ trợ phân trang và lọc theo Gói giá, Loại thao tác, Khoảng ngày thực hiện trong phạm vi khách sạn được phân quyền.
- **FR-018**: Toàn bộ nhãn, thông báo, trạng thái và thông báo lỗi PHẢI hỗ trợ song ngữ Việt - Anh (`vi.json`, `en.json`).

### 3.2. Business Rules (BR-01 – BR-14)

- **BR-01 (Phạm vi quản lý giá)**: Mỗi mức giá được xác định duy nhất theo tổ hợp `Room Type + Rate Plan + Applicable Date`.
- **BR-02 (Giá cơ bản)**: Mỗi Rate Plan thuộc một Room Type phải có `Base Price` hợp lệ (`> 0`).
- **BR-03 (Thứ tự tính Effective Price)**:
  1. Nếu tồn tại `Price Override` có ghi đè giá (`FixedPrice` hoặc `Percentage`) hợp lệ tại ngày đang xét $\rightarrow$ sử dụng giá ghi đè đã chốt (`CalculatedEffectivePrice`).
  2. Nếu không tồn tại ghi đè giá (bao gồm trường hợp chỉ cấu hình `RestrictionsOnly` cho `CTA`/`CTD`) $\rightarrow$ sử dụng `Base Price`.
- **BR-04 (Phạm vi Price Override)**: Một `Price Override` chỉ áp dụng cho đúng `Room Type` và `Rate Plan` đã chỉ định.
- **BR-05 (Khoảng thời gian & Bảo toàn quá khứ)**: `Start Date <= End Date`; mọi thao tác tạo mới yêu cầu `Start Date >= Today`; mọi ngày `< Today` ở chế độ chỉ đọc (không cho tạo/sửa/xóa), và khi sửa/xóa khoảng ghi đè vắt ngang ngày hiện tại chỉ tác động từ `Today` trở đi, giữ nguyên lịch sử quá khứ.
- **BR-06 (Giá hợp lệ)**: Mọi mức giá bán và `Effective Price` kết quả phải `> 0`, không cho phép giá bằng 0 hoặc số âm.
- **BR-07 (Chống trùng lịch ghi đè)**: Không cho phép tồn tại nhiều `Price Override` xung đột cho cùng tổ hợp `Room Type + Rate Plan + Applicable Date`.
- **BR-08 (Thay đổi Base Price)**: Thay đổi `Base Price` không được tự động xóa hoặc thay đổi giá đã chốt (`CalculatedEffectivePrice` snapshot) của bất kỳ `Price Override` nào đã tồn tại (áp dụng cho cả loại `Fixed Price` và `Percentage Adjustment`).
- **BR-09 (CTA)**: `CTA` chỉ ngăn tạo booking mới có ngày nhận phòng (`CheckInDate`) trùng ngày được đánh dấu `CTA`.
- **BR-10 (CTD)**: `CTD` chỉ ngăn tạo booking mới có ngày trả phòng (`CheckOutDate`) trùng ngày được đánh dấu `CTD`.
- **BR-11 (Trạng thái Rate Plan)**: Rate Plan ở trạng thái `Inactive` không được sử dụng cho booking mới và không cho phép tạo điều chỉnh giá mới.
- **BR-12 (Bảo toàn lịch sử Booking)**: Thay đổi giá không được làm thay đổi giá đã xác nhận của các booking hiện có; giá tại thời điểm đặt phòng phải được lưu snapshot trong booking.
- **BR-13 (Phân quyền)**: Chỉ người dùng có quyền quản lý giá phòng mới được tạo, chỉnh sửa hoặc xóa Price Override / Rate Plan.
- **BR-14 (Phân tách Tenant & Hotel)**: Mọi thao tác bị giới hạn nghiêm ngặt trong phạm vi `TenantId` và `HotelId` được phân quyền.

### 3.3. Validation & Error Handling Matrix

| Trường hợp | Cách xử lý bắt buộc |
|---|---|
| Không chọn Room Type | Yêu cầu chọn hạng phòng trước khi thao tác |
| Không chọn Rate Plan | Yêu cầu chọn gói giá trước khi thao tác |
| Không nhập Start Date | Thông báo trường bắt buộc |
| Không nhập End Date | Thông báo trường bắt buộc |
| `Start Date > End Date` | Chặn lưu và hiển thị lỗi khoảng ngày không hợp lệ |
| `Start Date < Today` (tạo mới) hoặc sửa/xóa lịch đã kết thúc trong quá khứ (`End Date < Today`) | Chặn thao tác ở cả Frontend (khóa DatePicker/nút) và Backend với thông báo không thể thay đổi giá ngày quá khứ |
| Giá `<= 0` (bằng 0 hoặc âm) | Chặn lưu và hiển thị lỗi giá không hợp lệ |
| Tỷ lệ phần trăm không hợp lệ (hoặc tạo giá kết quả `<= 0`) | Hiển thị lỗi validation rõ ràng |
| Khoảng ngày bị trùng Price Override | Hiển thị cảnh báo ngày trùng và yêu cầu chọn `Replace Existing` / `Skip Existing` / `Cancel` |
| Rate Plan đang `Inactive` | Không cho phép tạo điều chỉnh giá mới và thông báo trạng thái |
| Không có quyền thao tác | Từ chối (403 Forbidden) và hiển thị thông báo phân quyền |
| Lỗi hệ thống / mạng khi lưu | Thông báo lỗi, giữ nguyên dữ liệu người dùng đã nhập trên form để thử lại |

### 3.4. Key Entities *(Data Scope)*

- **RoomTypePricingContext**: Thông tin ngữ cảnh hạng phòng thuộc khách sạn (HotelId, RoomTypeId, ReferenceBasePrice, RatePlanCount, ActiveOverrideCount).
- **RoomRatePlan** *(kết hợp danh mục chuẩn chuỗi `RatePlan` chỉ đọc từ RM-009 và cấu hình chi nhánh `HotelRatePlan`)*: Gói giá gắn với hạng phòng tại khách sạn (Id, TenantId, HotelId, RoomTypeId, RatePlanId, Name [read-only], Code [read-only], RatePlanType [read-only], BasePrice [branch-editable], BreakfastPolicy [read-only], CancellationPolicy [read-only], Description [read-only], IsActive [branch-editable]).
- **RoomPriceOverride**: Bản ghi điều chỉnh giá/hạn chế theo khoảng ngày hoặc ngày áp dụng (Id, TenantId, HotelId, RoomTypeId, RatePlanId, StartDate, EndDate, AdjustmentType [`FixedPrice` | `Percentage` | `RestrictionsOnly`], AdjustmentValue, CalculatedEffectivePrice [nullable khi `RestrictionsOnly`, dùng `BasePrice`], IsClosedToArrival, IsClosedToDeparture, CreationTime, CreatorId).
- **DailyPricingCell**: Dữ liệu hiển thị từng ô ngày trên lịch (ApplicableDate, BasePrice, EffectivePrice, IsOverridden, OverrideId, IsClosedToArrival, IsClosedToDeparture, IsOpenForSale).
- **PricingAuditLog**: Nhật ký kiểm toán thay đổi giá (Id, TenantId, HotelId, RoomTypeId, RatePlanId, ActionType, ActorId, ActorName, ActionTime, BeforeValue, AfterValue, AffectedStartDate, AffectedEndDate).

### 3.5. Bố cục Giao diện 5 Khu vực (UI Structure)

1. **Section 1 – Room Type Selection**: Thông tin khách sạn, bộ chọn hạng phòng và các thẻ chỉ số tổng quan về giá.
2. **Section 2 – Rate Plan Management**: Danh mục gói giá dạng bảng/card thu gọn hiển thị `Base Price`, `Effective Price`, chính sách ăn sáng, chính sách hoàn hủy, trạng thái `Active`/`Inactive` và thao tác chọn/chỉnh sửa.
3. **Section 3 – Daily Pricing Calendar**: Lịch giá theo tháng, hiển thị `Effective Price` từng ngày, phân biệt màu/nhãn `Base Price` vs `Override Price`, hiển thị `CTA`/`CTD`, điều hướng tháng và click ngày để xem chi tiết kèm chỉnh sửa nhanh giá/`CTA`/`CTD` (với ngày `>= Today`).
4. **Section 4 – Bulk Price Adjustment**: Form chọn khoảng thời gian, bộ lọc thứ trong tuần (`T2–CN`), phạm vi gói giá, chế độ áp dụng (`Fixed Price` / `Percentage` / `Restrictions Only`), cấu hình `CTA`/`CTD`, bước `Preview` trước khi áp dụng và hộp thoại xử lý ngày trùng.
5. **Section 5 – Price Override List**: Bảng danh sách các khoảng ngày đã ghi đè kèm thao tác `View`, `Edit`, `Delete`.
- **On-Demand Modal/Drawer – Lịch sử thay đổi giá (`PricingAuditLog`)**: Nút mở từ thanh tiêu đề màn hình cho phép tra cứu phân trang và lọc 6 nhóm thao tác thay đổi giá theo Gói giá, Loại thao tác và Khoảng ngày.

---

## 4. Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Hotel Manager có thể chọn hạng phòng, chọn gói giá và tra cứu giá áp dụng (`Effective Price`) của bất kỳ ngày nào trong tháng trong vòng **dưới 5 giây** mà không cần chuyển trang.
- **SC-002**: Hotel Manager hoàn thành thao tác điều chỉnh giá hàng loạt cho một khoảng ngày (bao gồm xem Preview và xác nhận) trong vòng **dưới 30 giây** với 0 lỗi ghi đè nhầm dữ liệu cũ.
- **SC-003**: **100%** trường hợp khoảng ngày bị trùng lặp `Price Override` đều được phát hiện trước khi lưu và xử lý chính xác theo lựa chọn `Replace Existing`, `Skip Existing` hoặc `Cancel` (0 bản ghi xung đột trong dữ liệu).
- **SC-004**: **100%** thay đổi giá sau khi xác nhận (Base Price, Override, CTA/CTD, xóa Override) được phản ánh đồng bộ ngay lập tức trên Lịch giá theo ngày và Danh sách lịch ghi đè mà không cần reload trang.
- **SC-005**: **100%** các booking đã xác nhận trước thời điểm thay đổi giá giữ nguyên tuyệt đối mức giá đã chốt (0 sai lệch lịch sử booking).
- **SC-006**: **100%** trong 6 loại thao tác thay đổi giá đều sinh bản ghi Audit Log đầy đủ thông tin truy vết (người thực hiện, thời điểm, giá trị trước/sau, phạm vi ngày).
- **SC-007**: **100%** truy cập dữ liệu giá của Tenant khác hoặc Khách sạn không được phân công bị chặn tuyệt đối ở tầng Backend.

---

## 5. Assumptions & Dependencies

- **Phụ thuộc**: Kế thừa hạ tầng phân quyền & chi nhánh từ `RM-008` (Hotel Manager Workspace), danh mục gói giá chuẩn chuỗi từ `RM-009` (Tenant Rate Plans) và danh mục hạng phòng từ `RM-010` (Room Management).
- **Đơn vị tiền tệ**: Mặc định hiển thị và nhập liệu theo `VNĐ` với định dạng ngăn cách hàng nghìn rõ ràng.
- **Làm tròn giá phần trăm**: Khi áp dụng `Percentage Adjustment`, giá sau tính toán (`CalculatedEffectivePrice`) được chốt cứng (`Snapshot`) tại thời điểm lưu và làm tròn về bội số `1.000 VNĐ` gần nhất (làm tròn nửa lên, tối thiểu `1.000 VNĐ` và luôn đảm bảo `> 0`).
- **Bản địa hóa**: Sử dụng hệ thống localization song ngữ hiện có (`vi.json`, `en.json`, `fallback-localizations.ts`) và các thành phần dùng chung (`BhConfirmService`, `BhToastrService`, `BhModalComponent`, `BhSearchableSelectComponent`).
