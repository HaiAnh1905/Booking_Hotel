# Research & Architectural Decisions: RM-007 - Tenant Admin Operations & Management Portal

**Feature**: `007-tenant-admin-management` | **Date**: 2026-11-18  
**Spec Reference**: [specs/007-tenant-admin-management/spec.md](spec.md)

---

## 1. Nghiên Cứu 1: Quản Lý Cơ Sở Khách Sạn & Ràng Buộc Mã Cơ Sở (Hotel Property Management & Code Uniqueness)

### Bối cảnh & Thách thức
Mỗi Tenant (chuỗi khách sạn) quản lý nhiều cơ sở/chi nhánh lưu trú. Cần đảm bảo mã định danh cơ sở (`HotelCode`) ngắn gọn, dễ nhớ (ví dụ: `BH-HAN-01`, `BH-SGN-02`) để nhận diện trên hợp đồng, mã booking và hệ thống nội bộ.

### Quyết định kỹ thuật
- **Thực thể**: Mở rộng và chuẩn hóa `Hotel` (`FullAuditedAggregateRoot<Guid>, IMultiTenant`).
- **Chỉ mục CSDL (Database Index)**:
  ```csharp
  b.HasIndex(h => new { h.TenantId, h.Code })
   .IsUnique()
   .HasFilter("[IsDeleted] = 0");
  ```
- **Ràng buộc**: Mã `Code` là duy nhất trong phạm vi từng Tenant (`TenantId`). Hai Tenant khác nhau có quyền sử dụng cùng một mã (ví dụ: cùng có mã `HOTEL-01`).
- **Xác thực định dạng**: Regex validation `^[A-Z0-9\-_]{3,20}$` (chữ in hoa, số, dấu gạch nối/gạch dưới, độ dài từ 3 đến 20 ký tự).

### Đánh giá các giải pháp thay thế
- *Mã duy nhất toàn cầu (Global Unique)*: Bị loại bỏ vì gây xung đột không cần thiết giữa các doanh nghiệp độc lập trên cùng nền tảng SaaS đa khách thuê.
- *Không áp dụng ràng buộc duy nhất*: Bị loại bỏ vì dễ dẫn đến trùng lặp khi lễ tân hoặc quản lý tìm kiếm chi nhánh công tác.

---

## 2. Nghiên Cứu 2: Quy Trình Tạm Ngừng Cơ Sở Khách Sạn (Scheduled vs Emergency Suspension Workflow)

### Bối cảnh & Thách thức
Một khách sạn có thể cần dừng hoạt động vì lý do kế hoạch (bảo trì định kỳ, sửa chữa buồng phòng) hoặc vì lý do bất khả kháng khẩn cấp (sự cố hỏa hoạn, ngập lụt, thanh tra an toàn). Việc tạm ngừng phải xử lý nhân văn đối với khách hàng đã đặt phòng trước và điều phối nhân sự.

### Quyết định kỹ thuật
Hệ thống phân tách thành 2 quy trình độc lập qua enum `HotelStatus` và thực thể `HotelSuspensionRecord`:
1. **Tạm ngừng định kỳ (Scheduled Suspension)**:
   - Điều kiện tiên quyết: Ngày bắt đầu tạm ngừng phải được lên lịch trước ít nhất 30 ngày so với ngày hiện tại (`StartDate - Clock.Now >= 30 days`).
   - Ngừng nhận đơn mới: Căn cứ theo cấu hình cutoff của cơ sở (`BookingCutoffDays`, mặc định 14 ngày trước khi bắt đầu tạm ngừng).
   - Đơn đặt phòng hiện hữu: Tiếp tục phục vụ và check-out bình thường cho đến khi hết khách.
2. **Tạm ngừng khẩn cấp (Emergency Suspension)**:
   - Kích hoạt ngay lập tức (`Status = HotelStatus.Closed`).
   - Bắt buộc nhập văn bản giải trình lý do chi tiết ($\ge 20$ ký tự).
   - Tự động phát sinh sự kiện miền `HotelEmergencySuspendedEto` (Event Bus):
     - Gửi email/thông báo hệ thống tới toàn bộ khách hàng có booking trong tương lai chưa check-in.
     - Phát cảnh báo khẩn cấp tới Quản lý và Nhân sự chuỗi để kích hoạt quy trình hỗ trợ đổi sang chi nhánh khác trong cùng Tenant hoặc hoàn tiền cọc 100%.

### Đánh giá các giải pháp thay thế
- *Tạm ngừng ngay lập tức không phân loại*: Bị loại bỏ vì gây hoang mang cho khách hàng và vi phạm cam kết chất lượng dịch vụ khi bảo trì định kỳ.
- *Tự động hủy và hoàn tiền toàn bộ booking mà không có tùy chọn đổi cơ sở*: Bị loại bỏ vì làm mất doanh thu chuỗi và gây bất tiện cho khách lưu trú.

---

## 3. Nghiên Cứu 3: Phân Bổ Nhân Sự & Cơ Chế Vai Trò Kép Kiêm Nhiệm (Dual-Role Personnel Architecture)

### Bối cảnh & Thách thức
Tại các khách sạn vừa và nhỏ, mô hình nhân sự kiêm nhiệm rất phổ biến: một Quản lý khách sạn có thể kiêm luôn vai trò Lễ tân trực ca tại một cơ sở; hoặc một nhân viên Lễ tân xuất sắc được thăng chức kiêm nhiệm hỗ trợ quản lý chi nhánh.

### Quyết định kỹ thuật
- **Lưu trữ vai trò**: Tài khoản người dùng `IdentityUser` được gán đồng thời 2 vai trò `BookingHotelRoleConsts.HotelManager` và `BookingHotelRoleConsts.Receptionist` trong bảng `IdentityUserRole`.
- **Ràng buộc phân bổ cơ sở (`UserHotelAssignment`)**:
  - Đối với vai trò `HotelManager`: Được phân bổ quản lý danh sách **1 hoặc nhiều cơ sở khách sạn** (`IsPrimary = false`).
  - Đối với vai trò `Receptionist`: Bắt buộc chỉ định **1 cơ sở tác nghiệp chính (`IsPrimary = true`)** để khóa chặt phạm vi dữ liệu buồng phòng khi họ chuyển sang ca trực lễ tân.
- **Trải nghiệm giao diện (UX)**:
  - Khi đăng nhập, hệ thống ưu tiên mở không gian có thẩm quyền cao hơn: `/manager/dashboard`.
  - Trên Topbar, hiển thị nút chuyển đổi không gian làm việc (`Portal Switcher Pill`): Cho phép chuyển đổi tức thì giữa `/manager/dashboard` và `/frontdesk/room-rack` mà không cần đăng xuất.

### Đánh giá các giải pháp thay thế
- *Bắt buộc tạo 2 tài khoản riêng cho 1 người*: Bị loại bỏ vì gây phiền toái cực lớn khi phải đăng xuất - đăng nhập liên tục, vi phạm nguyên tắc một định danh người dùng duy nhất.
- *Cho phép Lễ tân tác nghiệp đa chi nhánh*: Bị loại bỏ vì vi phạm Hiến pháp Điều khoản VII và quy tắc cô lập buồng phòng của Lễ tân quầy.

---

## 4. Nghiên Cứu 4: Giám Sát Đặt Phòng Hợp Nhất Toàn Chuỗi (Consolidated Surveillance Boundary)

### Bối cảnh & Thách thức
Tenant Admin cần nắm bắt toàn bộ dòng giao dịch đặt phòng của toàn chuỗi để kiểm tra tính tuân thủ và giải quyết tranh chấp, nhưng không được can thiệp vào nghiệp vụ trực tiếp tại quầy của Lễ tân.

### Quyết định kỹ thuật
- **Chế độ CHỈ XEM (Read-Only 100%)**: Màn hình `/tenant/bookings` chỉ cung cấp các thao tác:
  - Tra cứu danh sách phân trang (`BhDataTableComponent`).
  - Lọc theo cơ sở, trạng thái booking, trạng thái thanh toán, khoảng ngày lưu trú.
  - Xem chi tiết đơn đặt phòng, lịch sử kiểm toán (Audit Trail) và dòng tiền hóa đơn Folio.
  - Xuất dữ liệu danh sách sang Excel/CSV.
- **Ranh giới bất khả xâm phạm**: Tuyệt đối không hiển thị các nút thao tác nghiệp vụ quầy (như Check-in, Check-out, Hủy đơn, Đổi phòng, Thêm phụ thu). Nếu Tenant Admin muốn can thiệp, họ phải thao tác từ phân hệ tương ứng theo thẩm quyền nghiệp vụ.

### Đánh giá các giải pháp thay thế
- *Cho phép Tenant Admin Hủy đơn khẩn cấp trên màn hình này*: Đã được xem xét trong Clarification Loop và bị từ chối (Chốt Option A: Read-Only 100%) để tránh phá vỡ quy trình vận hành và quy trách nhiệm tại quầy.

---

## 5. Nghiên Cứu 5: Nguyên Tắc Ghi Nhận Doanh Thu & Thước Đo Hiệu Suất (Cash Basis & RevPAR Metric)

### Bối cảnh & Thách thức
Khi đánh giá hiệu quả kinh doanh của các cơ sở khách sạn có quy mô chênh lệch lớn (ví dụ: cơ sở 150 phòng tại Đà Nẵng vs cơ sở 30 phòng boutique tại Hội An), so sánh tổng doanh thu thuần sẽ tạo ra cái nhìn sai lệch.

### Quyết định kỹ thuật
1. **Nguyên tắc ghi nhận Doanh thu (Cash / Settlement Basis)**:
   $$\text{Doanh thu thực thu} = \sum (\text{Hóa đơn Folio đã quyết toán / Settled}) - \sum (\text{Các khoản hoàn tiền / Refunded})$$
   - Chỉ ghi nhận tiền thực tế thu được, loại bỏ hoàn toàn rủi ro ghi nhận trước doanh thu khi khách có thể hủy phòng hoặc đổi lịch.
2. **Thước đo so sánh hiệu suất chuẩn quốc tế (RevPAR)**:
   $$\text{RevPAR} = \frac{\text{Tổng doanh thu phòng thực thu}}{\text{Tổng số phòng sẵn có}} = \text{ADR} \times \text{Occupancy Rate}$$
   - RevPAR là tiêu chí sắp xếp mặc định trên Bảng điều khiển và Báo cáo hiệu suất, đảm bảo đánh giá công bằng tuyệt đối hiệu quả khai thác tài sản bất kể khách sạn lớn hay nhỏ.

### Đánh giá các giải pháp thay thế
- *Xếp hạng theo tổng doanh thu thuần*: Bị loại bỏ vì luôn thiên vị khách sạn quy mô lớn, che giấu tình trạng hoạt động kém hiệu quả của các cơ sở lớn này.
- *Ghi nhận doanh thu dồn tích (Accrual Basis theo đêm phòng)*: Hoãn lại cho các giai đoạn kế toán nâng cao; Wave 0 tập trung vào Dòng tiền thực thu để bảo đảm tính chuẩn xác 100% với cổng thanh toán.

---

## 6. Nghiên Cứu 6: Cấu Hình Hồ Sơ Doanh Nghiệp & Mô Hình Chính Sách Chuỗi Hybrid

### Bối cảnh & Thách thức
Một chuỗi khách sạn cần duy trì tính nhất quán của thương hiệu, nhưng các cơ sở tại các vùng miền khác nhau có thói quen sinh hoạt và lịch trình di chuyển khác nhau (ví dụ: khách tại khu nghỉ dưỡng ven biển thường check-out muộn hơn khách tại khách sạn thương mại gần sân bay).

### Quyết định kỹ thuật
Áp dụng **Mô hình Chính sách Phân cấp Linh hoạt (Hybrid Policy Model)**:
1. **Khung giờ Check-in / Check-out**:
   - Cấp Tenant cấu hình khung giờ chuẩn mặc định (ví dụ: Check-in `14:00`, Check-out `12:00`).
   - Từng cơ sở khách sạn được phép ghi đè (`Override`) giá trị này trong thuộc tính `StandardCheckInTime` và `StandardCheckOutTime` của entity `Hotel`.
2. **Chính sách Hoàn / Hủy phòng (Cancellation Policy)**:
   - Khóa cứng và áp dụng đồng nhất 100% trên toàn chuỗi (ví dụ: Hủy trước 48h miễn phí, hủy trong vòng 24-48h phạt 50%, hủy dưới 24h phạt 100%).
   - Các chi nhánh cơ sở không được phép tự ý thay đổi chính sách này nhằm đảm bảo tính minh bạch và uy tín thương hiệu đối với khách hàng đặt phòng.

### Đánh giá các giải pháp thay thế
- *Khóa cứng toàn bộ chính sách*: Gây khó khăn cho vận hành tại các cơ sở có tính chất đặc thù (resort, homestay, transit hotel).
- *Cho phép chi nhánh tự do đổi chính sách hủy*: Gây hỗn loạn trải nghiệm khách hàng và tranh chấp khiếu nại tài chính cấp chuỗi.
