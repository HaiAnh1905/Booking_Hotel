# Implementation Plan: RM-011 – QUẢN LÝ GIÁ PHÒNG (ROOM PRICING MANAGEMENT)

**Branch**: `011-room-pricing-management` *(lưu ý: hiện working tree đang ở `dev`, tách nhánh `011-room-pricing-management` trước khi implement)* | **Date**: 2026-11-25 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/011-room-pricing-management/spec.md` (đã hoàn tất 5/5 làm rõ nghiệp vụ ngày 2026-11-25).

---

## Summary

Xây dựng và nâng cấp toàn diện phân hệ **Quản lý giá phòng (Room Pricing Management)** tại `/manager/rates` cho vai trò **Hotel Manager** và **Tenant Admin** theo phạm vi tổ hợp 3 chiều **`Room Type + Rate Plan + Applicable Date`**:
- **Backend (.NET 10 / ABP 10.7 / EF Core)**: Tách biệt rõ `RatePlan` chuẩn chuỗi (chỉ đọc từ RM-009) với `HotelRatePlan` chi nhánh (chỉnh sửa `Base Price` & `Active`/`Inactive`); giới thiệu Aggregate Root **`RoomPriceOverride`** (khoảng ngày không chồng lấn hỗ trợ `FixedPrice`, `Percentage` snapshot làm tròn bội số `1.000 VNĐ`, `RestrictionsOnly`), bảng append-only **`PricingAuditLog`** (6 nhóm thao tác), và **`RoomPricingDomainService`** xử lý toán học tách/gộp xung đột (`ReplaceExisting` / `SkipExisting` / `Cancel`), lọc thứ trong tuần (`T2–CN`) và bảo toàn bất biến ngày quá khứ (`< Today`).
- **Frontend (Angular 22 Standalone Components + Signals)**: Tổ chức lại giao diện `/manager/rates` theo chuẩn Concept Quản trị (`#000000` / `#00FFFF` / `#FFFFFF`) gồm 5 khu vực rõ ràng (Room Type Selection + KPI, Rate Plan Catalog, Monthly Daily Pricing Calendar kèm Quick-Edit, Bulk Price Adjustment kèm Preview & Conflict Resolution, Price Override List) và Modal/Drawer tra cứu `PricingAuditLog`, bản địa hóa 100% song ngữ Việt–Anh.

---

## Technical Context

**Language/Version**: C# / .NET 10 (Backend), TypeScript `~6.0.3` / Angular `~22.0.2` (Frontend)  
**Primary Dependencies**: ABP Framework `~10.7.0`, Entity Framework Core, `@abp/ng.core`, `@abp/ng.theme.lepton-x`, RxJS `~7.8.0`, Shared UI (`BhSearchableSelectComponent`, `BhModalComponent`, `BhConfirmService`, `BhToastrService`, `BhEmptyStateComponent`)  
**Storage**: SQL Server via EF Core Code-First Migrations (`BookingHotelDbContext` + `BookingHotel.DbMigrator`)  
**Testing**: xUnit + Shouldly (`BookingHotel.Domain.Tests`, `BookingHotel.Application.Tests`, `BookingHotel.EntityFrameworkCore.Tests`); Angular build/lint & Vitest (`angular/`)  
**Target Platform**: Web application (Desktop & Tablet responsive) — ASP.NET Core Host + Angular SPA  
**Project Type**: Layered DDD Web Application (ABP Backend + Angular Frontend)  
**Performance Goals**: Tải ngữ cảnh hạng phòng + lịch giá tháng `< 500ms` (đạt SC-001 < 5s end-to-end UX); Preview & Bulk Apply khoảng 365 ngày cho 5 gói giá `< 1.5s` (đạt SC-002 < 30s UX)  
**Constraints**: Backend-Authoritative 100% cho mọi phép tính giá/làm tròn/xung đột; Zero Fixed Text (i18n `vi.json` + `en.json` + `fallback-localizations.ts`); Zero Mock Data; không ảnh hưởng bảng `AppBookings` (BR-12); ngày `< Today` chỉ đọc bất biến (BR-05)  
**Scale/Scope**: ~5–30 hạng phòng/khách sạn, ~3–15 gói giá/tenant, ~10–200 khoảng ghi đè/gói giá/năm; 11 API endpoints, 2 bảng DB mới, 6 Angular standalone components/modals

---

## Constitution Check

*GATE: Đã kiểm tra trước Phase 0 và tái kiểm tra sau Phase 1 Design.*

| Nguyên tắc Hiến pháp | Trạng thái (Pre / Post Design) | Cơ chế bảo đảm trong thiết kế |
|---|---|---|
| **I. Kiến Trúc Phân Lớp DDD & ABP** | **PASS / PASS** | Tách chuẩn `Domain.Shared` (enums, i18n) → `Domain` (entities, `RoomPricingDomainService`) → `Application.Contracts` (DTOs, interface) → `Application` (`ManagerRateAppService`) → `EFCore` (`DbContext`, Migration) → `HttpApi` (`ManagerRateController`) → `Angular`. |
| **II. Contract-First & Bảo Mật Mặc Định** | **PASS / PASS** | 100% endpoints dùng DTO validation (`[Required]`, `[Range]`), gắn `[Authorize(BookingHotelPermissions.Rooms.SetRates)]`, `CheckHotelManagerAccessAsync(hotelId)` và `IMultiTenant`. |
| **III. Quản Lý CSDL Qua EF Core Migrations** | **PASS / PASS** | Tạo Migration `AddRoomPricingManagement` qua `dotnet ef` và áp dụng bằng `BookingHotel.DbMigrator`; Fluent API cấu hình trong `BookingHotelDbContext`. |
| **IV. Chuẩn Hóa Frontend & Thin Presentation** | **PASS / PASS** | Standalone Components, `OnPush`, Signals, Reactive Forms; Frontend tuyệt đối không tự tính giá dự kiến hay dò xung đột (gọi `POST /bulk-preview`). |
| **V. Đa Ngôn Ngữ (Zero Fixed Text) & Unit Test** | **PASS / PASS** | 100% chuỗi hiển thị lấy từ `vi.json` / `en.json` / `fallback-localizations.ts` (dọn sạch hardcode cũ ở `rates-management.component.*`); Unit Test `RoomPricingDomainService` trong `BookingHotel.Domain.Tests`. |
| **VI. Backend-Authoritative** | **PASS / PASS** | Mọi tính toán `BasePrice`, `CalculatedEffectivePrice`, làm tròn bội số `1.000 VNĐ`, Preview, tách khoảng xung đột, khóa ngày quá khứ và ghi Audit Log đều thực thi tại Backend. |
| **VII. Role-Based UI/UX Concepts** | **PASS / PASS** | Kế thừa `HotelManagerLayoutComponent` chuẩn Concept Quản lý (`#000000` Sidebar, `#00FFFF` Cyan highlight, `#FFFFFF` nền chính). |
| **VIII. Brand Naming (`Booking Hotel`)** | **PASS / PASS** | Giữ tên hiển thị có dấu cách `Booking Hotel` trong UI/i18n và PascalCase `BookingHotel` trong code namespace. |
| **IX. Auth & Session Security** | **N/A (PASS)** | Không thay đổi luồng đăng nhập; thừa hưởng xác thực hiện hữu. |
| **X. Zero Mock / Fake Data** | **PASS / PASS** | 100% truy xuất EF Core; hiển thị `bh-empty-state` chuẩn khi chưa có hạng phòng, gói giá, lịch ghi đè hoặc audit log. |
| **XI. Required Field UI Standard** | **PASS / PASS** | Mọi trường bắt buộc dùng `<label class="form-label">{{ 'Key' | abpLocalization }} <span class="text-danger">*</span></label>` (chỉ dấu `*` màu đỏ `#dc2626`, chữ nhãn thường không in đậm). |

**Gate Result**: **ALL GATES PASSED** — Không có vi phạm Hiến pháp cần ngoại lệ.

---

## Project Structure

### Documentation (this feature)

```text
BookingHotel/specs/011-room-pricing-management/
├── spec.md                         # Đặc tả nghiệp vụ đã làm rõ 5/5 câu hỏi
├── plan.md                         # Kế hoạch kiến trúc & triển khai (tệp này)
├── research.md                     # Phase 0: Quyết định kiến trúc & thuật toán
├── data-model.md                   # Phase 1: Lược đồ thực thể, enum, index & state machine
├── quickstart.md                   # Phase 1: Hướng dẫn build, migration & kịch bản nghiệm thu
├── checklists/
│   └── requirements.md             # Spec Quality Checklist (16/16 passing)
├── contracts/
│   └── manager-rate-api.md         # Phase 1: Hợp đồng 11 REST API endpoints, DTO & Error Codes
└── tasks.md                        # Phase 2: Sẽ được sinh bởi /speckit-tasks
```

### Source Code (`BookingHotel/`)

```text
src/
├── BookingHotel.Domain.Shared/
│   ├── Rooms/
│   │   └── PricingEnums.cs                                  # [NEW] PriceAdjustmentType, PriceConflictResolution, PricingAuditActionType
│   └── Localization/BookingHotel/
│       ├── vi.json                                          # [UPDATE] Keys Manager:Rates:* & Error Codes
│       └── en.json                                          # [UPDATE] Keys Manager:Rates:* & Error Codes
├── BookingHotel.Domain/
│   └── Rooms/
│       ├── RoomPriceOverride.cs                             # [NEW] Aggregate Root khoảng ghi đè 3 chiều
│       ├── PricingAuditLog.cs                               # [NEW] Entity append-only nhật ký 6 nhóm thao tác
│       └── RoomPricingDomainService.cs                      # [NEW] Domain Service tính giá, làm tròn 1.000 VNĐ, tách/gộp xung đột & bảo toàn quá khứ
├── BookingHotel.Application.Contracts/
│   └── Manager/Rooms/
│       ├── IManagerRoomAppService.cs                        # [UPDATE] Mở rộng interface IManagerRateAppService
│       └── ManagerPricingDtos.cs                            # [NEW] Toàn bộ Input/Output DTOs có DataAnnotations validation
├── BookingHotel.Application/
│   └── Manager/Rooms/
│       └── ManagerRateAppService.cs                         # [UPDATE] Triển khai 11 nghiệp vụ + [Authorize(SetRates)] + AuditLog
├── BookingHotel.EntityFrameworkCore/
│   ├── EntityFrameworkCore/
│   │   └── BookingHotelDbContext.cs                         # [UPDATE] DbSet & Fluent API cho RoomPriceOverride, PricingAuditLog
│   └── Migrations/
│       └── *_AddRoomPricingManagement.cs                    # [NEW] EF Core Migration
└── BookingHotel.HttpApi/
    └── Controllers/Manager/
        └── ManagerRateController.cs                         # [UPDATE] 11 REST endpoints tại /api/app/manager/rooms/pricing

test/
├── BookingHotel.Domain.Tests/
│   └── Rooms/
│       └── RoomPricingDomainServiceTests.cs                 # [NEW] Unit tests BR-01..BR-12, rounding 1.000 VNĐ, split/merge, past-date lock
└── BookingHotel.Application.Tests/
    └── Manager/
        └── ManagerRateAppServiceTests.cs                    # [NEW] Integration tests 11 APIs, 6 AuditLog types, Booking preservation, Cross-Hotel/Tenant 403

angular/src/app/
├── shared/localization/
│   └── fallback-localizations.ts                            # [UPDATE] Đồng bộ fallback keys VI/EN cho RM-011
└── admin/
    ├── services/manager/
    │   └── manager-rate.service.ts                          # [NEW] Strong-typed Angular REST service cho /api/app/manager/rooms/pricing/*
    └── manager/rates/
        ├── rates-management.component.ts/.html/.scss        # [UPDATE] Container + Section 1 (KPI) + Section 2 (Rate Plans) + Section 5 (Overrides)
        ├── components/
        │   ├── pricing-calendar/
        │   │   └── pricing-calendar.component.ts/.html/.scss          # [NEW] Section 3: Lịch tháng + điều hướng + badge Base/Override/CTA/CTD
        │   ├── day-pricing-detail-modal/
        │   │   └── day-pricing-detail-modal.component.ts/.html/.scss  # [NEW] Modal chi tiết ngày + Quick-Edit (>= Today)
        │   ├── bulk-price-adjustment/
        │   │   └── bulk-price-adjustment.component.ts/.html/.scss     # [NEW] Section 4: Bulk form + lọc thứ T2–CN + Preview & Conflict Modal
        │   ├── edit-base-price-modal/
        │   │   └── edit-base-price-modal.component.ts/.html/.scss     # [NEW] Sửa Base Price chi nhánh (thông tin chuỗi chỉ đọc)
        │   ├── edit-override-modal/
        │   │   └── edit-override-modal.component.ts/.html/.scss       # [NEW] View/Edit lịch ghi đè từ Section 5 + kiểm tra trùng
        │   └── pricing-audit-log-drawer/
        │       └── pricing-audit-log-drawer.component.ts/.html/.scss  # [NEW] Modal/Drawer tra cứu phân trang PricingAuditLog
```

**Structure Decision**: Giữ nguyên kiến trúc ABP Multi-Project Solution (`src/` + `test/` + `angular/`) đang dùng xuyên suốt RM-001 đến RM-010. Tách riêng `ManagerPricingDtos.cs` và `manager-rate.service.ts` để tránh phình to `ManagerRoomDtos.cs` / `manager-room.service.ts` (vốn đã phục vụ RM-010), và chia nhỏ UI `/manager/rates` thành các standalone sub-components theo từng Section/Modal để giữ mỗi tệp gọn gàng, dễ bảo trì.

---

## Phased Implementation Roadmap (Tóm Tắt Cho Bước `/speckit-tasks`)

1. **Phase 1 — Domain, Shared Enums & EF Core Migration**:
   - Tạo `PricingEnums.cs`, `RoomPriceOverride`, `PricingAuditLog`, `RoomPricingDomainService`.
   - Cấu hình `BookingHotelDbContext`, tạo migration `AddRoomPricingManagement` và chạy `DbMigrator`.
   - Viết unit test bao phủ 100% nhánh thuật toán trong `RoomPricingDomainServiceTests`.
2. **Phase 2 — Application Contracts, AppService & HttpApi**:
   - Định nghĩa `ManagerPricingDtos.cs`, mở rộng `IManagerRateAppService`, triển khai đầy đủ 11 phương thức trong `ManagerRateAppService` với `[Authorize(BookingHotelPermissions.Rooms.SetRates)]`, ghi `PricingAuditLog` nguyên tử trong cùng transaction.
   - Mở rộng `ManagerRateController`, thêm khóa i18n song ngữ vào `vi.json` & `en.json`.
   - Viết integration test trong `ManagerRateAppServiceTests` (6 loại Audit Log, bảo toàn Booking, Cross-Hotel/Tenant 403, Receptionist 403).
3. **Phase 3 — Angular Service & 5-Section UI + Audit Drawer**:
   - Xây dựng `ManagerRateService`, đồng bộ `fallback-localizations.ts`.
   - Hiện thực hóa 5 Section và các Modal/Drawer theo chuẩn Hiến pháp (Concept Quản trị, Required `*` đỏ không in đậm, Zero Fixed Text, Zero Mock Data, `PropertyContextService` reset khi đổi khách sạn).
4. **Phase 4 — Verification & Acceptance**:
   - Chạy `dotnet build`, `dotnet test` (Domain + Application tests), `npm run build`, `npm run lint`, và nghiệm thu 5 kịch bản trong [quickstart.md](./quickstart.md).

---

## Complexity Tracking

> Không có vi phạm Hiến pháp. Mọi thành phần bổ sung (`RoomPricingDomainService`, `RoomPriceOverride`, `PricingAuditLog`) đều phục vụ trực tiếp các yêu cầu cốt lõi (`BR-01`–`BR-14`, `FR-001`–`FR-018`) và tuân thủ nghiêm ngặt DDD/ABP.
