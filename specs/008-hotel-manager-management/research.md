# Nghiên Cứu Kiến Trúc Kỹ Thuật (Phase 0 Research): RM-008 - Hotel Manager Console

**Feature**: `008-hotel-manager-management`  
**Ngày thực hiện**: 2026-11-19  
**Người thực hiện**: Antigravity Assistant & Engineering Team  
**Mục tiêu**: Nghiên cứu kiến trúc phân hệ điều hành cơ sở khách sạn dành cho vai trò Hotel Manager (`HotelManager`), đảm bảo tính toàn vẹn dữ liệu, kiểm soát truy cập phân tầng, tuân thủ Hiến pháp `Booking Hotel` và 5 quyết định nghiệp vụ đã được thống nhất.

---

## 1. Cơ Chế Ủy Quyền & Giới Hạn Ngữ Cảnh Cơ Sở (Backend-Authoritative Property Scoping)

### 1.1. Hiện Trạng & Rủi Ro Tiềm Ẩn
- Trên giao diện Frontend, Hotel Manager có thể được phân công quản lý nhiều chi nhánh trong cùng Tenant (ví dụ: `PLA-HCM-1` và `HS-VT-1`).
- Nếu chỉ kiểm tra quyền dựa trên `HotelId` lưu ở `localStorage` hoặc Client state:
  1. Người dùng có thể can thiệp URL Query Parameter (`?hotelId=...`) hoặc payload HTTP request để gửi `HotelId` của một cơ sở khác không thuộc quyền quản lý.
  2. Khi người dùng mở đồng thời nhiều tab trên trình duyệt (Tab 1 chọn khách sạn A, Tab 2 chọn khách sạn B), việc dùng chung `localStorage` gây xung đột dữ liệu chéo giữa các tab (Cross-tab state leakage).

### 1.2. Giải Pháp Kỹ Thuật Chốt
1. **Lớp Kiểm Soát Tập Trung Tại Backend (Centralized Authorization Guard)**:
   - Xây dựng phương thức kiểm tra quyền truy cập dùng chung `CheckHotelManagerAccessAsync(Guid hotelId)` (hoặc Action Filter `[RequireHotelAccess]`):
     - Lấy `UserId = CurrentUser.GetId()` và `TenantId = CurrentTenant.Id`.
     - Truy vấn bảng `UserHotelAssignment`:
       $$\exists a \in \text{UserHotelAssignments} : (a.\text{UserId} == \text{UserId}) \land (a.\text{HotelId} == \text{hotelId}) \land (a.\text{IsActive} == \text{true}) \land (a.\text{UnassignedAt} == \text{null})$$
     - Nếu không thỏa mãn: Ném ngay ngoại lệ `BusinessException("BookingHotel:CrossHotelOperationForbidden")` trả về mã lỗi HTTP 403 Forbidden và ghi log bảo mật vào `SecurityLogs`.
2. **Khả Năng Độc Lập Giữa Các Tab (Multi-Tab Resilience Trên Frontend)**:
   - Dùng Angular Router Navigation State kết hợp URL Query Parameter `?hotelId=...` làm nguồn sự thật (Single Source of Truth) cho mỗi tab.
   - `PropertyContextService` quản lý trạng thái phản ứng (Angular Signals) riêng biệt theo từng phiên làm việc của tab (lưu `sessionStorage` thay vì `localStorage`), đảm bảo khi chuyển đổi cơ sở ở Tab 1 thì Tab 2 vẫn giữ nguyên ngữ cảnh hoạt động của Tab 2.

---

## 2. Kiến Trúc Buồng Phòng Đa Chiều (Orthogonal Room States & Rate Management)

### 2.1. Phân Tách Trạng Thái Vận Hành & Trạng Thái Vệ Sinh
- Gộp chung trạng thái buồng phòng làm phát sinh lỗi nghiêm trọng: Lễ tân vô tình xếp khách vào phòng vừa Check-out nhưng chưa dọn sạch (`Dirty`).
- Giải pháp: Tách biệt 2 trường độc lập trên thực thể `Room`:
  - **`RoomOperationalStatus` (Vận hành)**: `Available` (1), `Occupied` (2), `Reserved` (3), `Maintenance` (4), `OutOfService` (5).
  - **`HousekeepingStatus` (Vệ sinh)**: `Clean` (1), `Dirty` (2), `Cleaning` (3), `Inspected` (4).
- **Quy tắc Sẵn sàng Đón khách (Sellable Rule)**:
  $$\text{CanCheckIn} \iff (\text{OperationalStatus} == \text{Available}) \land (\text{HousekeepingStatus} \in \{\text{Clean}, \text{Inspected}\}) \land (\text{Không xung đột RoomAssignment})$$

### 2.2. Kiểm Soát Đưa Phòng Vào `OutOfService` & `Maintenance` (Decision Q5)
- Việc đưa phòng vào `OutOfService` làm giảm mẫu số tính tỷ lệ lấp đầy (`AvailableInventory`), có nguy cơ bị lạm dụng để "làm đẹp" chỉ số Occupancy.
- Giải pháp:
  - Hotel Manager toàn quyền chuyển trạng thái buồng phòng tại cơ sở của mình.
  - **Bắt buộc ràng buộc (Guardrails)**:
    1. Bắt buộc nhập lý do (`Reason`) và ngày kết thúc dự kiến (`ExpectedEndDate`).
    2. Backend bắt buộc kiểm tra xung đột: Từ chối chuyển nếu phòng đang `Occupied` hoặc có bất kỳ `RoomAssignment` nào trong khoảng thời gian đóng phòng.
    3. Ghi vết đầy đủ vào `AuditLog` và tổng hợp vào mục "Thất thoát công suất do ngừng kinh doanh" trong báo cáo gửi lên Tenant Admin.

### 2.3. Mô Hình Quản Lý Giá Đa Tầng (Decision Q1)
- Tenant Admin ban hành danh mục `RatePlan` chuẩn toàn chuỗi (Standard Flexible, Non-Refundable Promo, Breakfast Included).
- Hotel Manager chọn kích hoạt/vô hiệu hóa các gói giá áp dụng cho từng `RoomType` tại cơ sở.
- Cấu hình giá bao gồm:
  - `BasePrice`: Giá niêm yết chuẩn theo từng `RoomType`.
  - `DailyRateOverride`: Bảng ghi đè giá theo khoảng ngày cụ thể (ngày lễ, cuối tuần, mùa cao điểm) kèm các điều kiện chặn nhận phòng/trả phòng (`IsClosedToArrival`, `IsClosedToDeparture`).

---

## 3. Vòng Đời Đặt Phòng & Thao Tác Nghiệp Vụ (Booking Lifecycle & Operations)

### 3.1. Tách Bạch 3 Trạng Thái Độc Lập
Thực thể `Booking` quản lý phân tách 3 trường:
1. `BookingStatus`: `PendingConfirmation`, `Confirmed`, `CheckedIn`, `Completed`, `Rejected`, `Cancelled`, `NoShow`.
2. `PaymentStatus`: `Unpaid`, `PartiallyPaid`, `Paid`, `Refunded`.
3. `RoomAssignmentStatus`: `Unassigned`, `Assigned`.

### 3.2. Tạo Đặt Phòng Trực Tiếp (Decision Q4)
- Hotel Manager được cấp quyền `Bookings.Create` để tiếp nhận và tạo mới các đơn đặt phòng trực tiếp cho khách đoàn, khách VIP, hoặc hợp đồng doanh nghiệp (Corporate).
- Cho phép áp dụng bảng giá niêm yết hoặc ghi đè giá thỏa thuận theo hợp đồng, nhập số tiền cọc ban đầu và tạo liên kết phòng giữ chỗ.

### 3.3. Xử Lý Khách Không Đến (No-Show) & Quyết Toán Phạt (Decision Q2)
- Khi quá giờ check-in cutoff mà khách không đến:
  1. Hủy liên kết `RoomAssignment` của booking theo khoảng ngày lưu trú, giải phóng phòng để tiếp tục bán cho các ngày tiếp theo.
  2. Trạng thái phòng vật lý được giữ nguyên theo thực tế (không ép chuyển `Available` nếu phòng đang bẩn hoặc đã có khách khác kế tiếp).
  3. **Tính phạt minh bạch**: Mặc định áp dụng mức phạt theo Tenant Cancellation Policy; Hotel Manager có quyền điều chỉnh giảm hoặc miễn phạt (0 VNĐ) nếu có lý do bất khả kháng hợp lệ (`WaivePenaltyReason`), bắt buộc ghi vết vào `AuditLog` và tạo giao dịch hoàn cọc thừa nếu có.

---

## 4. Bảo Vệ Dữ Liệu Khách Hàng & Ghi Chú Nội Bộ

### 4.1. Phạm Vi Dữ Liệu Khách Hàng
- Hotel Manager **CHỈ ĐƯỢC XEM** thông tin các khách hàng đã từng phát sinh booking tại cơ sở khách sạn phụ trách.
- Tuyệt đối cấm truy xuất danh sách khách toàn chuỗi hoặc của cơ sở khác.
- Giấy tờ tùy thân (CCCD / Passport) **bắt buộc che bảo mật (Masked)**: Chỉ hiển thị 4 số cuối (ví dụ: `***-***-1234`).
- Cấm tuyệt đối can thiệp sửa đổi tài khoản người dùng (`IdentityUser`).

### 4.2. Ghi Chú Nội Bộ (`InternalGuestNote`) Có Audit Log
- Phục vụ lưu ý chăm sóc khách VIP, sở thích, yêu cầu dị ứng thực phẩm.
- Chỉ lưu hành nội bộ nhân viên khách sạn, cấm hiển thị ra Customer Portal.
- Mọi thao tác tạo, sửa, xóa đều được ghi nhận đầy đủ `CreatorId`, `CreationTime`, `LastModifierId`, `LastModificationTime`.

---

## 5. Báo Cáo & Phân Tích Hiệu Suất Theo Chuẩn Quốc Tế (USALI)

### 5.1. Công Thức Đo Lường
- **Tồn kho sẵn có (Available Inventory)**:
  $$\text{Available Inventory} = \text{Tổng phòng} - \text{Số phòng OutOfService}$$
- **Tỷ lệ lấp đầy (Occupancy Rate)**:
  $$\text{Occupancy Rate (\%)} = \left(\frac{\text{Số phòng có khách (Occupied Rooms)}}{\text{Available Inventory}}\right) \times 100\%$$
- **Giá phòng bình quân ngày (ADR)**:
  $$\text{ADR} = \frac{\text{Doanh thu tiền phòng thuần (Room Revenue)}}{\text{Số phòng bán được (Rooms Sold)}}$$
- **Doanh thu trên mỗi phòng sẵn có (RevPAR)**:
  $$\text{RevPAR} = \frac{\text{Doanh thu tiền phòng thuần}}{\text{Available Inventory}} = \text{ADR} \times \text{Occupancy Rate}$$

### 5.2. Phân Tách Dòng Tiền Minh Bạch
- `RoomRevenue`: Doanh thu thuần tiền phòng.
- `ServiceRevenue`: Doanh thu dịch vụ bổ sung (minibar, giặt là, ẩm thực, spa).
- `CollectedRevenue`: Tiền thực thu từ khách qua thanh toán cọc hoặc thanh toán tại quầy.
- `RefundAmount`: Tiền đã hoàn lại cho khách do hủy đơn hoặc giải quyết khiếu nại.

---

## 6. Đánh Giá Khách Hàng & Luồng Khiếu Nại SLA 48h (Decision Q3)

### 6.1. Đánh Giá Khách Hàng & Phản Hồi Chính Thức
- Hotel Manager xem danh sách đánh giá kèm điểm trung bình theo 4 tiêu chí cốt lõi: Vệ sinh, Thái độ nhân viên, Tiện nghi, Vị trí.
- Đăng **Phản hồi chính thức của khách sạn** hiển thị công khai kèm huy hiệu đại diện quản lý.

### 6.2. Luồng Khiếu Nại & Leo Thang (Escalation)
- Vòng đời: `Open` $\rightarrow$ `InInvestigation` $\rightarrow$ `Resolved`.
- Cam kết thời hạn **SLA chuẩn 48 giờ**.
- Nếu sau 48h khiếu nại chưa chuyển sang `Resolved`:
  - Hệ thống tự động gắn nhãn cảnh báo `Overdue SLA`.
  - Tự động chuyển trạng thái `Escalated` và phát thông báo đến Tenant Admin để ban quản trị chuỗi kịp thời can thiệp bảo vệ thương hiệu.

---

## 7. Quy Chuẩn Giao Diện & Tuân Thủ Hiến Pháp (Constitution Alignment)

1. **Hệ màu Concept Quản lý (Điều khoản VII)**:
   - Nền chính: Trắng (`#FFFFFF`).
   - Sidebar: Đen sâu (`#000000`).
   - Điểm nhấn thao tác & Biểu đồ: Xanh Cyan (`#00FFFF`).
   - Huy hiệu vai trò: Xanh dương (`#2563EB`).
   - Độ tương phản chữ/nền WCAG 2.1 AA ($\ge 4.5:1$).
2. **Cấm Tuyệt Đối Fixed Text (Điều khoản V)**:
   - 100% nhãn form, trạng thái enum, toastr khai báo tại `vi.json` và `en.json` với tiền tố `Manager:*`.
3. **Dấu Sao Đỏ Bắt Buộc Nhập (Điều khoản XI)**:
   - `<label class="form-label">{{ 'Key' | abpLocalization }} <span class="text-danger">*</span></label>`.
4. **Không Dùng Mock Data (Điều khoản X)**:
   - Toàn bộ dữ liệu buồng phòng, booking, dashboard lấy trực tiếp từ CSDL; xử lý trạng thái rỗng bằng component `bh-empty-state`.