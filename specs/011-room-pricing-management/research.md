# Technical Research: RM-011 – Quản Lý Giá Phòng (Room Pricing Management)

**Feature**: `011-room-pricing-management`  
**Date**: 2026-11-25  
**Spec**: [spec.md](./spec.md)

---

## 1. Mô hình Lưu trữ Lịch Ghi Đè: Range-based `RoomPriceOverride` vs Per-day `DailyRateOverride`

- **Decision**: Giới thiệu Aggregate Root mới **`RoomPriceOverride`** lưu theo **khoảng ngày không chồng lấn** `[StartDate, EndDate]` (inclusive, date-only) gắn đầy đủ khóa 3 chiều `(TenantId, HotelId, RoomTypeId, RatePlanId)`. Giữ nguyên bảng cũ `AppDailyRateOverrides` (không xóa bảng để tránh phá vỡ migration/dữ liệu cũ trên môi trường dev) và chuyển toàn bộ luồng `ManagerRateAppService` sang `RoomPriceOverride`.
- **Rationale**:
  - `DailyRateOverride` hiện có từ RM-008 thiếu cột `RatePlanId` (chỉ có `(TenantId, HotelId, RoomTypeId, Date)`), không hỗ trợ `AdjustmentType` (`FixedPrice` / `Percentage` / `RestrictionsOnly`), không lưu `AdjustmentValue`, không hỗ trợ quản lý danh sách khoảng ghi đè (`StartDate`–`EndDate`) kèm thao tác `View` / `Edit` / `Delete` tại Section 5 (FR-014, FR-015, FR-016).
  - Lưu theo khoảng ngày không chồng lấn giúp Section 5 hiển thị trực tiếp từng đợt điều chỉnh (1 dòng = 1 khoảng ngày liên tục), giảm mạnh số bản ghi DB cho các đợt giá mùa dài (30–365 ngày), đồng thời khi dựng lịch tháng (Section 3) chỉ cần 1 truy vấn giao cắt `StartDate <= monthEnd && EndDate >= monthStart`.
- **Alternatives considered**:
  - *Mở rộng `DailyRateOverride` (lưu từng ngày 1 dòng + thêm `RatePlanId` & `BatchGroupId`)*: Gây bùng nổ số dòng DB khi áp dụng 1 năm cho nhiều gói giá, phức tạp khi gộp/tách nhóm để hiển thị và sửa khoảng ngày tại Section 5.
  - *Lưu song song cả bảng Range và bảng Daily*: Dư thừa dữ liệu, tăng nguy cơ lệch đồng bộ giữa 2 bảng khi sửa/tách khoảng trùng.

---

## 2. Thuật Toán Xử Lý Chồng Lấn, Lọc Thứ Trong Tuần & Bảo Toàn Ngày Quá Khứ

- **Decision**: Đóng gói toàn bộ logic toán học khoảng ngày và tính giá vào Domain Service thuần túy **`RoomPricingDomainService`** (trong `BookingHotel.Domain/Rooms/`) để đạt 100% khả năng unit test độc lập tại `BookingHotel.Domain.Tests` (Hiến pháp mục V & VI).
- **Rationale**:
  - **Lọc thứ trong tuần (`DaysOfWeek`, FR-009)**: Từ `[StartDate .. EndDate]`, mở rộng tập ngày khớp `DaysOfWeek` (mặc định đủ 7 ngày nếu bỏ trống), sau đó gom các ngày liên tiếp thành các tiểu khoảng liên tục `[subStart .. subEnd]` (ví dụ lọc T6–CN trong 2 tuần sinh 2 khoảng liên tiếp `[T6..CN]`).
  - **Phát hiện xung đột (`BR-07`)**: Mọi ngày trong tập đích giao với bất kỳ `RoomPriceOverride` nào hiện có của cùng `(HotelId, RoomTypeId, RatePlanId)` đều được trả về trong kết quả Preview (`ConflictingDates`) trước khi lưu.
  - **Xử lý xung đột (`FR-013`)**:
    - `Cancel`: Không thay đổi dữ liệu.
    - `SkipExisting`: Loại bỏ các ngày đã có override khỏi tập đích, gom các ngày còn lại thành tiểu khoảng mới và chèn vào DB.
    - `ReplaceExisting`: Đối với mỗi bản ghi override cũ có giao cắt với tập đích:
      - Cắt bỏ phần ngày giao cắt (tuyệt đối không chạm các ngày `< Today` theo `BR-05`), giữ nguyên phần trái `[old.Start .. overlapStart - 1]` và/hoặc phần phải `[overlapEnd + 1 .. old.End]` nếu còn hợp lệ; nếu bản ghi cũ bị phủ kín hoàn toàn (và nằm ở `>= Today`) thì soft-delete.
      - Chèn các tiểu khoảng đích mới đã gom liên tiếp.
  - **Bảo toàn ngày quá khứ (`BR-05`)**:
    - Tạo mới yêu cầu `StartDate >= Today` (theo ngày cục bộ hệ thống/chi nhánh, sử dụng `IClock.Now.Date`).
    - Bản ghi có `EndDate < Today` ở chế độ chỉ đọc tuyệt đối (chặn Edit và Delete).
    - Bản ghi vắt ngang hôm nay (`StartDate < Today && EndDate >= Today`):
      - Khi **Delete**: Thu hẹp `EndDate = Today.AddDays(-1)` để đóng băng lịch sử quá khứ, không xóa bản ghi.
      - Khi **Edit**: Thu hẹp bản ghi cũ về `[StartDate .. Today.AddDays(-1)]` để bảo toàn quá khứ, rồi tạo bản ghi mới cho phần tương lai đã chỉnh sửa (`newStart >= Today`) kèm kiểm tra xung đột như tạo mới.
- **Alternatives considered**:
  - *Xử lý trực tiếp trong `ManagerRateAppService` bằng vòng lặp CRUD từng ngày*: Khó kiểm thử đơn vị, dễ bỏ sót trường hợp biên vắt ngang `Today` hoặc tách 2 đầu (khi khoảng mới nằm lọt giữa khoảng cũ).

---

## 3. Quy Tắc Tính Giá, Snapshot Phần Trăm & Làm Tròn VNĐ

- **Decision**: Tính toán `BasePrice` và `CalculatedEffectivePrice` hoàn toàn tại Backend (`RoomPricingDomainService`), chốt cứng `Snapshot` tại thời điểm lưu và làm tròn về bội số `1.000 VNĐ` gần nhất (`MidpointRounding.AwayFromZero`, tối thiểu `1.000 VNĐ`).
- **Rationale**:
  - **Branch Base Price (`HotelRatePlan`)**: Nếu `HotelRatePlan.CustomBasePrice` có giá trị thì dùng giá đó; ngược lại tính mặc định từ `RoomType.BasePrice * (1 + RatePlan.DefaultPriceAdjustmentPercent / 100m)` làm tròn về bội số `1.000 VNĐ` gần nhất (tối thiểu `1.000 VNĐ`).
  - **Override Effective Price**:
    - `FixedPrice`: Bắt buộc `AdjustmentValue > 0`; lưu `CalculatedEffectivePrice = AdjustmentValue`.
    - `Percentage`: Tính `raw = currentBasePrice * (1 + AdjustmentValue / 100m)`; nếu `raw <= 0` thì ném lỗi nghiệp vụ (`BookingHotel:InvalidEffectivePrice`, BR-06); ngược lại `CalculatedEffectivePrice = Math.Max(1000m, Math.Round(raw / 1000m, 0, MidpointRounding.AwayFromZero) * 1000m)`.
    - `RestrictionsOnly`: `AdjustmentValue = null`, `CalculatedEffectivePrice = null` (khi hiển thị lịch ngày sẽ fallback về `BasePrice` hiện hành theo BR-03, đồng thời vẫn áp dụng cờ `CTA`/`CTD`).
  - **Preview Backend-Authoritative (Hiến pháp mục VI)**: Angular không tự tính giá dự kiến hay tự dò xung đột; Frontend gọi `POST /api/app/manager/rooms/pricing/bulk-preview` để Backend tính toán đầy đủ danh sách gói giá bị ảnh hưởng, giá hiện tại, giá mới dự kiến, số ngày ảnh hưởng và danh sách ngày trùng lặp.
  - **Effective Price hiển thị tại Section 2 (`FR-003`)**: Mặc định tính theo ngày hôm nay (`Today`) cho mỗi cặp `Room Type + Rate Plan` (áp dụng đúng thứ tự BR-03: nếu hôm nay có override giá thì hiển thị `CalculatedEffectivePrice`, ngược lại hiển thị `BasePrice`).
- **Alternatives considered**:
  - *Frontend tự tính Preview ở client*: Vi phạm nghiêm trọng Hiến pháp mục IV & VI (Zero Trust on Client-Calculated Data) và dễ lệch làm tròn với Backend.

---

## 4. Kiến Trúc Audit Log Chuyên Biệt (`PricingAuditLog`) & Bảo Toàn Booking

- **Decision**: Tạo bảng append-only **`AppPricingAuditLogs`** (`CreationAuditedEntity<Guid>, IMultiTenant`) ghi nhận đồng bộ trong cùng Unit of Work (transaction) cho tất cả 6 nhóm thao tác (`CreatePriceOverride`, `UpdatePriceOverride`, `DeletePriceOverride`, `UpdateBasePrice`, `ToggleRatePlanStatus`, `UpdateRestrictions`), kèm endpoint truy vấn phân trang/lọc và Modal/Drawer xem lịch sử ngay trên `/manager/rates`.
- **Rationale**:
  - Bảo đảm tính nguyên tử (ACID): nếu lưu giá thất bại thì không ghi log rác; nếu lưu thành công thì chắc chắn có bản ghi Audit Log (SC-006 = 100%).
  - Lưu snapshot tên `RoomTypeName`, `RatePlanCode`, `RatePlanName`, `ActorName` và JSON giá trị trước/sau (`BeforeValueJson`, `AfterValueJson`) giúp tra cứu nhanh không phụ thuộc soft-delete về sau.
  - **Bảo toàn Booking (`BR-12`, `SC-005`)**: Thực thể `Booking` đã lưu cố định `TotalAmount` tại thời điểm đặt phòng và không tham chiếu động lại `HotelRatePlan` hay `RoomPriceOverride`. Mọi thay đổi trong RM-011 tuyệt đối không cập nhật bảng `AppBookings`.
- **Alternatives considered**:
  - *Chỉ dùng ABP `AbpAuditLogs`*: Khó lọc theo tổ hợp nghiệp vụ `HotelId + RoomTypeId + RatePlanId + ActionType + AffectedDate` và không phù hợp hiển thị trực tiếp cho Hotel Manager.

---

## 5. Phân Quyền, Cách Ly Đa Khách Thuê & Kiến Trúc UI Angular

- **Decision**:
  - **Backend Authorization**: Tái sử dụng quyền hiện có `BookingHotelPermissions.Rooms.SetRates` (đã cấp sẵn cho `HotelManager` và `TenantAdmin`, chặn `Receptionist` & `Customer` ở RM-003) trên toàn bộ `ManagerRateAppService`, kết hợp `CheckHotelManagerAccessAsync(hotelId)` (từ `ManagerAppServiceBase`) và kiểm tra cứng `roomType.HotelId == hotelId` + tồn tại `RatePlan` trong Tenant hiện tại.
  - **Frontend Architecture**: Tái cấu trúc `angular/src/app/admin/manager/rates/` thành các Standalone Components dùng chung (`OnPush` + Angular Signals + Reactive Forms) tách rõ theo 5 khu vực + 3 modal chuyên trách:
    1. `RatesManagementComponent` (container điều phối ngữ cảnh `PropertyContextService`, Section 1 Header & KPI tổng quan, Section 2 Danh mục Gói giá, Section 5 Danh sách Lịch ghi đè).
    2. `PricingCalendarComponent` (Section 3 – Lịch giá theo tháng trực quan với ô ngày phân màu `Base` / `Override` / `Past` / `Today`, huy hiệu `CTA`/`CTD`, điều hướng tháng/năm).
    3. `DayPricingDetailModalComponent` (Modal chi tiết ngày + Quick-Edit giá & `CTA`/`CTD` cho ngày `>= Today`).
    4. `BulkPriceAdjustmentComponent` (Section 4 – Form điều chỉnh hàng loạt có lọc thứ `T2–CN`, chọn 1 hoặc nhiều gói giá, chuyển chế độ `Fixed` / `Percentage` / `RestrictionsOnly`, kèm Modal Preview & Xử lý trùng ngày `Replace Existing` / `Skip Existing` / `Cancel`).
    5. `EditBasePriceModalComponent` & `EditOverrideModalComponent` (Chỉnh sửa `Base Price` chi nhánh với thông tin chuỗi chỉ đọc; xem/sửa lịch ghi đè).
    6. `PricingAuditLogDrawerComponent` (Modal/Drawer tra cứu phân trang 6 nhóm thao tác).
  - **Shared UI & i18n**: Tận dụng `BhSearchableSelectComponent`, `BhModalComponent`, `BhConfirmService`, `BhToastrService`, `BhEmptyStateComponent`, `BhLoadingSpinnerComponent`. Xóa bỏ hoàn toàn các chuỗi hardcode cũ trong `rates-management.component.*` và đồng bộ 100% key song ngữ tại `vi.json`, `en.json`, `fallback-localizations.ts`.
- **Alternatives considered**:
  - *Dồn toàn bộ 5 khu vực + 4 modal vào 1 component `rates-management.component.ts` duy nhất*: Tệp vượt 1.000 dòng, khó bảo trì và khó viết test/verify từng phần.
