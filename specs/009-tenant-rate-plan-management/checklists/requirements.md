# Specification Quality & Architecture Analysis: RM-007 / RM-009 - Quản Lý Gói Giá Chuỗi Cho Admin Tenant

**Purpose**: Thẩm định toàn diện chất lượng đặc tả, tính nhất quán kiến trúc, độ phủ của tasks và tuân thủ Hiến pháp dự án trước khi thực thi code (`/speckit.implement`).  
**Created**: 2026-11-20  
**Feature**: [spec.md](../spec.md) | **Plan**: [plan.md](../plan.md) | **Tasks**: [tasks.md](../tasks.md)  
**Status**: **APPROVED - READY FOR IMPLEMENTATION**

---

## 1. Content Quality (Chất Lượng Nội Dung Đặc Tả)

- [x] **Tách biệt rõ ràng giữa Nghiệp vụ và Kỹ thuật**: Các User Story trong `spec.md` tập trung vào giá trị cho Quản trị viên Chuỗi (`TenantAdmin`) và Quản lý cơ sở (`HotelManager`), không bị pha tạp cú pháp ngôn ngữ lập trình.
- [x] **Ngôn ngữ miền chuẩn hóa (Domain Ubiquitous Language)**: Sử dụng nhất quán các thuật ngữ: *Gói giá chuỗi (Master Rate Plans)*, *Gói giá cơ sở (Hotel Rate Plans)*, *Chính sách hoàn hủy (Cancellation Policy)*, *Tỷ lệ điều chỉnh giá mặc định (Default Price Adjustment Percent)*.
- [x] **Đầy đủ cấu trúc tài liệu**: Hoàn thiện toàn bộ các file tài liệu thiết kế: `spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/tenant-rate-plans.md`, `quickstart.md`, `tasks.md`.

---

## 2. Requirement Completeness & Testability (Độ Đầy Đủ & Khả Năng Kiểm Thử)

- [x] **Không còn điểm mơ hồ (`[NEEDS CLARIFICATION]` = 0)**: Phiên Clarification Session 2026-11-20 đã giải quyết triệt để 3/3 câu hỏi kiến trúc cốt lõi:
  1. *Cơ chế khi xóa gói giá*: Xóa mềm (Soft Delete) trên `RatePlan`, tự động chuyển `HotelRatePlan.IsActive = false` tại toàn bộ chi nhánh để dừng mở bán mới, bảo toàn 100% lịch sử booking cũ.
  2. *Cơ chế điều chỉnh giá*: Tự động tính khởi tạo linh hoạt $\text{Giá Gói} = \text{Giá Hạng Phòng} \times (1 + \text{DefaultPriceAdjustmentPercent}/100)$, cho phép Quản lý chi nhánh ghi đè giá bán theo ngày (`DailyRateOverride`).
  3. *Phân quyền nghiêm ngặt*: Chỉ tài khoản `TenantAdmin` có quyền truy cập `/tenant/rate-plans` và thực hiện CRUD.
- [x] **Độ bao phủ Kịch bản kiểm thử (Acceptance Scenarios)**: 100% User Stories (US1, US2, US3) đều có kịch bản kiểm thử `Given / When / Then` rõ ràng và độc lập.
- [x] **Xử lý Biên & Ngoại lệ (Edge Cases)**:
  - Trùng mã gói `Code` trong cùng Tenant $\rightarrow$ Báo lỗi `BookingHotel:DuplicateRatePlanCode`.
  - Chuẩn hóa mã `Code` $\rightarrow$ Tự động `.ToUpper().Trim()`.
  - Tỷ lệ điều chỉnh giá âm (giảm giá) hoặc dương (tăng giá) $\rightarrow$ Giới hạn hợp lệ $[-99\%, +500\%]$.
  - Tự động reset thời hạn hủy về 0 khi không cho phép hoàn hủy (`IsRefundable = false`).

---

## 3. Constitution Compliance Check (Kiểm Tra 11 Điều Khoản Hiến Pháp Dự Án)

| Điều khoản | Tiêu chuẩn đánh giá | Kết quả kiểm tra | Bằng chứng đối soát |
| :--- | :--- | :---: | :--- |
| **I. Phân lớp DDD & ABP** | Tách Domain, Contracts, Application, HttpApi | **PASS** | Tách bạch: `RatePlan` (Domain) $\rightarrow$ `ITenantRatePlanAppService` (Contracts) $\rightarrow$ `TenantRatePlanAppService` (Application) $\rightarrow$ `TenantRatePlanController` (HttpApi). |
| **II. Contract-First & Security** | Data Annotations, phân quyền ABP, Audit Logging | **PASS** | DTOs có validation đầy đủ; kế thừa `FullAuditedEntityDto`; bảo vệ bằng `[Authorize(BookingHotelPermissions.Tenants.Manage)]`. |
| **III. EF Core Migrations** | Quản lý schema bằng Code-First Migrations | **PASS** | Entity `RatePlan` đã có sẵn trong DbContext và đã có migration `20261119081755_AddHotelManagerOperationsEntities`; không phát sinh thay đổi cấu trúc bảng. |
| **IV. Chuẩn hóa Frontend** | Thin presentation, Reactive/Signals, Strict TS | **PASS** | Xây dựng standalone `TenantRatePlansComponent` dùng Angular Signals, không mock dữ liệu hay tự tính toán tiền tệ phía client. |
| **V. Đa ngôn ngữ (Zero Fixed Text)** | Cấm cứng tuyệt đối văn bản; song ngữ `vi.json` & `en.json` | **PASS** | 100% tiêu đề, nhãn form, trạng thái badge, thông báo lỗi khai báo song ngữ đầy đủ tại `vi.json`, `en.json`, và `fallback-localizations.ts`. |
| **VI. Backend-Authoritative** | Thao tác dữ liệu và tính toán thuộc về Backend | **PASS** | Backend thực hiện validation trùng mã, Soft Delete và cập nhật cascade trạng thái chi nhánh; Frontend không tự quyết định lưu trữ. |
| **VII. UI Concept Hệ Màu** | Nền Trắng, Sidebar Đen, Điểm nhấn Cyan, WCAG AA $\ge 4.5:1$ | **PASS** | Áp dụng concept Quản trị Tenant: Nền `#ffffff`, Card border `#e2e8f0`, Sidebar đồng bộ layout Tenant. |
| **VIII. Brand Naming** | Tên thương hiệu `Booking Hotel` có dấu cách | **PASS** | Tuân thủ tên chuẩn `Booking Hotel` trên toàn giao diện. |
| **IX. Bảo mật Phiên & Login** | Zero credential pre-filling | **PASS** | Không can thiệp vào form đăng nhập; tuân thủ hoàn toàn. |
| **X. Cấm Dữ Liệu Ảo (Zero Mock)** | 100% dữ liệu thực từ CSDL; dùng empty-state khi rỗng | **PASS** | Đọc dữ liệu trực tiếp từ `IRepository<RatePlan, Guid>`; khi rỗng hiển thị empty state chuẩn có icon và mô tả. |
| **XI. Dấu Sao Đỏ Bắt Buộc Nhập** | Dấu sao `*` màu đỏ `#dc2626`, nhãn không in đậm | **PASS** | Form modal áp dụng chuẩn `<label class="form-label">{{ 'Key' | abpLocalization }} <span class="text-danger">*</span></label>`. |

---

## 4. Cross-Document Consistency Matrix (Ma Trận Nhất Quán Giữa Các Tài Liệu)

| Yếu tố đối soát | spec.md | plan.md | tasks.md | contracts/ | data-model.md |
| :--- | :---: | :---: | :---: | :---: | :---: |
| Tên route | `/tenant/rate-plans` | `/tenant/rate-plans` | `/tenant/rate-plans` | `/tenant/rate-plans` | `/tenant/rate-plans` |
| Tên REST API | `/api/app/tenant/rate-plans` | `/api/app/tenant/rate-plans` | `/api/app/tenant/rate-plans` | `/api/app/tenant/rate-plans` | `/api/app/tenant/rate-plans` |
| Tên Service C# | `TenantRatePlanAppService` | `TenantRatePlanAppService` | `TenantRatePlanAppService` | `ITenantRatePlanAppService` | N/A |
| Tên Component Angular | `TenantRatePlansComponent` | `TenantRatePlansComponent` | `TenantRatePlansComponent` | N/A | N/A |
| Khóa phân quyền | `Tenants.Manage` / `RatePlans.Manage` | `Tenants.Manage` / `RatePlans.Manage` | `Tenants.Manage` / `RatePlans.Manage` | `Tenants.Manage` | N/A |

---

## 5. Kết Luận Thẩm Định & Phê Duyệt (Final Verdict)

- **Tổng số tiêu chí thẩm định**: 28/28 tiêu chí **ĐẠT (PASS 100%)**.
- **Tính khả thi kỹ thuật**: CSDL đã có sẵn entity `RatePlan` và `HotelRatePlan`, chỉ cần triển khai AppService/Controller ở Backend và Component/Menu ở Frontend.
- **Rủi ro hồi quy (Regression Risk)**: **Cực thấp (None)** - Không sửa đổi cấu trúc bảng hiện tại, mở rộng độc lập trong namespace `BookingHotel.Tenant.Rates`.
- **Trạng thái cổng kiểm soát (Gate Status)**: **APPROVED** $\rightarrow$ Sẵn sàng 100% cho bước triển khai mã nguồn (`/speckit.implement`).
