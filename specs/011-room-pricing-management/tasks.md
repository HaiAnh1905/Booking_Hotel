# Tasks: RM-011 â€“ QUáº¢N LÃ GIÃ PHÃ’NG (ROOM PRICING MANAGEMENT)

**Input**: Design documents from `BookingHotel/specs/011-room-pricing-management/`  
**Prerequisites**: [plan.md](./plan.md) (required), [spec.md](./spec.md) (required), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/manager-rate-api.md](./contracts/manager-rate-api.md), [quickstart.md](./quickstart.md)

**Tests**: Unit tests (`BookingHotel.Domain.Tests`) and Application Integration tests (`BookingHotel.Application.Tests`) are **included** per Constitution Principle V & VI (`RoomPricingDomainService`, 6 Audit Log actions, Booking preservation, Cross-Hotel/Tenant security).

**Organization**: Tasks are grouped by user story (`US1`â€“`US5`) to enable independent implementation and testing of each increment.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: User story label (`[US1]`â€“`[US5]`) for user story phases only
- Exact file paths are relative to workspace root (`BookingHotel/...` and `docs/...`)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Chuáº©n bá»‹ cáº¥u trÃºc thÆ° má»¥c UI, Ä‘á»‹nh nghÄ©a enum dÃ¹ng chung vÃ  Ä‘á»“ng bá»™ khÃ³a Ä‘a ngÃ´n ngá»¯ song ngá»¯ Viá»‡tâ€“Anh (Constitution Principle V).

- [X] T001 Create/checkout feature branch `011-room-pricing-management` and create sub-component directory structure under `BookingHotel/angular/src/app/admin/manager/rates/components/`
- [X] T002 [P] Define pricing enums (`PriceAdjustmentType`: `FixedPrice = 1`, `Percentage = 2`, `RestrictionsOnly = 3`; `PriceConflictResolution`: `Cancel = 0`, `ReplaceExisting = 1`, `SkipExisting = 2`; `PricingAuditActionType`: `CreatePriceOverride = 1`, `UpdatePriceOverride = 2`, `DeletePriceOverride = 3`, `UpdateBasePrice = 4`, `ToggleRatePlanStatus = 5`, `UpdateRestrictions = 6`) in `BookingHotel/src/BookingHotel.Domain.Shared/Rooms/PricingEnums.cs`
- [X] T003 [P] Add bilingual localization keys (`Manager:Rates:*` for all 5 UI sections, modals, audit drawer, status badges, and error codes `BookingHotel:InvalidDateRange`, `BookingHotel:CannotModifyPastPricing`, `BookingHotel:InvalidEffectivePrice`, `BookingHotel:RatePlanInactive`, `BookingHotel:NoMatchingDaysInRange`, `BookingHotel:PriceOverrideConflictDetected`, `BookingHotel:RatePlanNotFound`) in `BookingHotel/src/BookingHotel.Domain.Shared/Localization/BookingHotel/vi.json` and `BookingHotel/src/BookingHotel.Domain.Shared/Localization/BookingHotel/en.json`
- [X] T004 [P] Mirror all RM-011 Vietnamese and English fallback localization keys in `BookingHotel/angular/src/app/shared/localization/fallback-localizations.ts`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: HoÃ n thiá»‡n Domain Entities, Domain Service tÃ­nh giÃ¡/tÃ¡ch xung Ä‘á»™t, EF Core Migration, Contract DTOs, khung phÃ¢n quyá»n vÃ  Angular REST Service â€” **báº¯t buá»™c hoÃ n thÃ nh trÆ°á»›c khi triá»ƒn khai báº¥t ká»³ User Story nÃ o**.

**âš ï¸ CRITICAL**: KhÃ´ng báº¯t Ä‘áº§u Phase 3â€“7 cho tá»›i khi toÃ n bá»™ Phase 2 hoÃ n táº¥t vÃ  build xanh.

- [X] T005 [P] Create `RoomPriceOverride` aggregate root (`FullAuditedAggregateRoot<Guid>, IMultiTenant`) with `HotelId` (required), `RoomTypeId` (required), `RatePlanId` (required), `StartDate` (Date-only required), `EndDate` (Date-only required, `>= StartDate`), `AdjustmentType` (`PriceAdjustmentType` required), `AdjustmentValue` (`decimal(18,2)` nullable), `CalculatedEffectivePrice` (`decimal(18,2)` nullable), `IsClosedToArrival` (bool default `false`), `IsClosedToDeparture` (bool default `false`), `CreatorName` (`MaxLength(256)` required) and invariant guard methods in `BookingHotel/src/BookingHotel.Domain/Rooms/RoomPriceOverride.cs`
- [X] T006 [P] Create append-only `PricingAuditLog` entity (`CreationAuditedEntity<Guid>, IMultiTenant`) with `HotelId` (required), `RoomTypeId` (required), `RoomTypeName` (`MaxLength(128)` required), `RatePlanId` (required), `RatePlanCode` (`MaxLength(32)` required), `RatePlanName` (`MaxLength(128)` required), `ActionType` (`PricingAuditActionType` required), `ActorId` (required), `ActorName` (`MaxLength(256)` required), `ActionTime` (required UTC), `BeforeValueJson` (`MaxLength(4000)` nullable), `AfterValueJson` (`MaxLength(4000)` nullable), `AffectedStartDate` (Date-only nullable), `AffectedEndDate` (Date-only nullable), `SummaryNote` (`MaxLength(500)` nullable) in `BookingHotel/src/BookingHotel.Domain/Rooms/PricingAuditLog.cs`
- [X] T007 Implement `RoomPricingDomainService` with: branch `BasePrice` fallback resolution (`CustomBasePrice` or `RoomType.BasePrice * (1 + DefaultPriceAdjustmentPercent / 100m)` rounded to nearest `1.000 VNÄ`, min `1.000 VNÄ`), `CalculateEffectivePrice` (`FixedPrice > 0`, `Percentage` snapshot rounded via `MidpointRounding.AwayFromZero` to nearest `1.000 VNÄ` with min `1.000 VNÄ` rejecting `raw <= 0`, `RestrictionsOnly` returning `null`), `ExpandMatchingDates` with `DaysOfWeek` contiguous grouping, `ResolveConflicts` interval splitting (`Cancel`/`ReplaceExisting`/`SkipExisting` guaranteeing zero overlapping dates), and `SplitForPastDateProtection` (`< Today` read-only freeze and straddling-today split) in `BookingHotel/src/BookingHotel.Domain/Rooms/RoomPricingDomainService.cs`
- [X] T008 Register `DbSet<RoomPriceOverride>` and `DbSet<PricingAuditLog>` with Fluent API table mappings (`AppRoomPriceOverrides`, `AppPricingAuditLogs`), column precisions (`decimal(18,2)`), max-lengths, and composite/filtered indexes (`IX_RoomPriceOverrides_Scope_Dates`, `IX_RoomPriceOverrides_Hotel_RoomType`, `IX_PricingAuditLogs_Hotel_Time`, `IX_PricingAuditLogs_Scope_Time`, `IX_PricingAuditLogs_Hotel_Action_Time`) in `BookingHotel/src/BookingHotel.EntityFrameworkCore/EntityFrameworkCore/BookingHotelDbContext.cs`
- [X] T009 Generate EF Core Code-First migration `AddRoomPricingManagement` in `BookingHotel/src/BookingHotel.EntityFrameworkCore/Migrations/` and apply database schema update via `BookingHotel/src/BookingHotel.DbMigrator/`
- [X] T010 [P] Create all RM-011 request/response DTOs with DataAnnotations validation (`[Required]`, `[Range(1, 1000000000)]`, `[Range(2000, 2100)]`, `[Range(1, 12)]`) matching `contracts/manager-rate-api.md` (`RoomTypePricingContextDto`, `RoomRatePlanItemDto`, `ToggleRoomRatePlanStatusDto`, `UpdateRoomRatePlanBasePriceDto`, `MonthlyPricingCalendarDto`, `DailyPricingCellDto`, `QuickEditDailyPricingDto`, `BulkPriceAdjustmentPreviewInputDto`, `BulkPriceAdjustmentPreviewDto`, `RatePlanBulkPreviewItemDto`, `ApplyBulkPriceAdjustmentDto`, `BulkPriceAdjustmentResultDto`, `RoomPriceOverrideDto`, `UpdateRoomPriceOverrideDto`, `GetPricingAuditLogsInputDto`, `PricingAuditLogDto`) in `BookingHotel/src/BookingHotel.Application.Contracts/Manager/Rooms/ManagerPricingDtos.cs`
- [X] T011 Extend `IManagerRateAppService` with all 11 RM-011 contract methods (`GetPricingContextAsync`, `ToggleRatePlanStatusAsync`, `UpdateRatePlanBasePriceAsync`, `GetMonthlyCalendarAsync`, `QuickEditDailyPricingAsync`, `PreviewBulkAdjustmentAsync`, `ApplyBulkAdjustmentAsync`, `GetPriceOverridesAsync`, `UpdatePriceOverrideAsync`, `DeletePriceOverrideAsync`, `GetPricingAuditLogsAsync`) in `BookingHotel/src/BookingHotel.Application.Contracts/Manager/Rooms/IManagerRoomAppService.cs`
- [X] T012 [P] Configure `[Authorize(BookingHotelPermissions.Rooms.SetRates)]` class-level authorization, inject `IRepository<RoomPriceOverride, Guid>`, `IRepository<PricingAuditLog, Guid>`, `IRepository<Hotel, Guid>`, and `RoomPricingDomainService`, and add shared Hotel/RoomType/RatePlan ownership guards + atomic `WriteAuditLogAsync` helper in `BookingHotel/src/BookingHotel.Application/Manager/Rooms/ManagerRateAppService.cs`
- [X] T013 [P] Create strongly-typed Angular `ManagerRateService` with TypeScript enums and DTO interfaces for all 11 `/api/app/manager/rooms/pricing/*` endpoints in `BookingHotel/angular/src/app/admin/services/manager/manager-rate.service.ts`
- [X] T014 [P] Write unit tests for `RoomPricingDomainService` covering BR-02/BR-03/BR-05/BR-06/BR-07/BR-08, rounding to nearest `1.000 VNÄ`, negative percentage `<= 0` rejection, `RestrictionsOnly`, `DaysOfWeek` contiguous grouping, partial overlap split for `ReplaceExisting`/`SkipExisting`, and past-date/straddling-today protection in `BookingHotel/test/BookingHotel.Domain.Tests/Rooms/RoomPricingDomainServiceTests.cs`

**Checkpoint**: Foundation ready â€” `dotnet build BookingHotel.sln` and `RoomPricingDomainServiceTests` pass; User Story phases can now begin.

---

## Phase 3: User Story 1 - Lá»±a chá»n Háº¡ng phÃ²ng & Quáº£n lÃ½ Danh má»¥c GÃ³i giÃ¡ (Priority: P1) ðŸŽ¯ MVP

**Goal**: Cho phÃ©p Hotel Manager chá»n Háº¡ng phÃ²ng táº¡i cÆ¡ sá»Ÿ hiá»‡n hÃ nh, xem cÃ¡c chá»‰ sá»‘ tá»•ng quan vá» giÃ¡ (Section 1), xem danh má»¥c GÃ³i giÃ¡ chuáº©n chuá»—i kÃ¨m `Base Price` & `Effective Price` hÃ´m nay (Section 2), báº­t/táº¯t `Active`/`Inactive` vÃ  chá»‰nh sá»­a `Base Price` táº¡i chi nhÃ¡nh (giá»¯ chá»‰ Ä‘á»c chÃ­nh sÃ¡ch chuá»—i tá»« RM-009, khÃ´ng lÃ m máº¥t lá»‹ch ghi Ä‘Ã¨ cÅ©).

**Independent Test**: Chá»n tá»«ng háº¡ng phÃ²ng, kiá»ƒm tra Ä‘á»“ng bá»™ KPI Section 1 vÃ  danh má»¥c gÃ³i giÃ¡ Section 2, báº­t/táº¯t tráº¡ng thÃ¡i `Active`/`Inactive`, sá»­a `Base Price` chi nhÃ¡nh vÃ  xÃ¡c nháº­n chÃ­nh sÃ¡ch chuá»—i chá»‰ Ä‘á»c cÃ¹ng cÃ¡c `Price Override` cÅ© Ä‘Æ°á»£c báº£o toÃ n nguyÃªn váº¹n.

### Implementation for User Story 1

- [X] T015 [US1] Implement `GetPricingContextAsync`, `ToggleRatePlanStatusAsync`, and `UpdateRatePlanBasePriceAsync` (enforcing `BasePrice > 0`, Cross-Hotel/Tenant checks, today's `EffectivePrice` calculation per BR-03, preserving all existing `RoomPriceOverride` records per BR-08/BR-11, and writing `PricingAuditLog` for `UpdateBasePrice` and `ToggleRatePlanStatus`) in `BookingHotel/src/BookingHotel.Application/Manager/Rooms/ManagerRateAppService.cs`
- [X] T016 [US1] Add `GET /context`, `POST /rate-plans/toggle`, and `PUT /rate-plans/base-price` endpoints under `[Route("api/app/manager/rooms/pricing")]` in `BookingHotel/src/BookingHotel.HttpApi/Controllers/Manager/ManagerRateController.cs`
- [X] T017 [P] [US1] Create `EditBasePriceModalComponent` showing read-only chain Rate Plan metadata (`Name`, `Code`, `BreakfastPolicy`, `CancellationPolicy`, `Description` from RM-009) and editable branch `BasePrice` input (`> 0`, VNÄ formatted, required `*` red non-bold label per Constitution XI) in `BookingHotel/angular/src/app/admin/manager/rates/components/edit-base-price-modal/edit-base-price-modal.component.ts` (with `.html` and `.scss`)
- [X] T018 [US1] Rebuild `RatesManagementComponent` container with `PropertyContextService` reset on hotel switch (BR-14), Section 1 (Hotel Name/Code, `BhSearchableSelectComponent` Room Type selector, KPI cards: Reference Base Price, Rate Plan count, Active Override count), Section 2 (Rate Plan table/cards displaying Name, Code, RatePlanType, `BasePrice`, today's `EffectivePrice`, Breakfast/Cancellation policies, `Active`/`Inactive` switch via `BhConfirmService` + `BhToastrService`, row selection for active Rate Plan, `EditBasePriceModalComponent` trigger, `bh-empty-state` for empty Room Types / Rate Plans, zero hardcoded text) in `BookingHotel/angular/src/app/admin/manager/rates/rates-management.component.ts` (with `.html` and `.scss`)

**Checkpoint**: User Story 1 (MVP) is independently functional and testable at `/manager/rates`.

---

## Phase 4: User Story 2 - Lá»‹ch giÃ¡ theo ngÃ y (Daily Pricing Calendar) (Priority: P1)

**Goal**: Hiá»ƒn thá»‹ Lá»‹ch giÃ¡ theo thÃ¡ng trá»±c quan cho cáº·p `Room Type + Rate Plan` Ä‘ang chá»n (Section 3), phÃ¢n biá»‡t rÃµ `Base Price` vs `Override Price`, hiá»ƒn thá»‹ huy hiá»‡u `CTA`/`CTD` vÃ  tráº¡ng thÃ¡i Ä‘Ã³ng/má»Ÿ bÃ¡n, Ä‘iá»u hÆ°á»›ng thÃ¡ng/nÄƒm vÃ  cho phÃ©p click ngÃ y xem chi tiáº¿t kÃ¨m chá»‰nh sá»­a nhanh (cho ngÃ y `>= Today`, khÃ³a chá»‰ Ä‘á»c `< Today`).

**Independent Test**: Äiá»u hÆ°á»›ng qua cÃ¡c thÃ¡ng, kiá»ƒm tra mÃ u sáº¯c/nhÃ£n phÃ¢n biá»‡t `Base Price`, `Override Price`, `CTA`, `CTD`, click ngÃ y quÃ¡ khá»© (`< Today`) xÃ¡c nháº­n chá»‰ Ä‘á»c, click ngÃ y tÆ°Æ¡ng lai (`>= Today`) thá»±c hiá»‡n Quick-Edit giÃ¡ hoáº·c chá»‰ báº­t/táº¯t `CTA`/`CTD` vÃ  xÃ¡c nháº­n lá»‹ch cáº­p nháº­t tá»©c thÃ¬ khÃ´ng cáº§n reload trang.

### Implementation for User Story 2

- [X] T019 [US2] Implement `GetMonthlyCalendarAsync` (mapping every day of `year`/`month` via BR-03 with `isPastDate`, `isToday`, `isPriceOverridden`, `hasRestrictionsOverride`, `isClosedToArrival`, `isClosedToDeparture`, `isOpenForSale`) and `QuickEditDailyPricingAsync` (validating `applicableDate >= Today` and Rate Plan `IsActive`, single-day split preserving adjacent override days, supporting `FixedPrice`/`Percentage`/`RestrictionsOnly`/`clearOverride`, and recording `PricingAuditLog`) in `BookingHotel/src/BookingHotel.Application/Manager/Rooms/ManagerRateAppService.cs`
- [X] T020 [US2] Add `GET /calendar` and `PUT /daily-quick-edit` endpoints in `BookingHotel/src/BookingHotel.HttpApi/Controllers/Manager/ManagerRateController.cs`
- [X] T021 [P] [US2] Create `PricingCalendarComponent` (Section 3) rendering monthly calendar grid (`T2â€“CN`) with prev/next month, month/year dropdown, "Today" button, legend, visual differentiation for `BasePrice` vs `OverridePrice`, `CTA`/`CTD` badges, `Inactive` rate plan indicator, read-only past-date styling, and day-cell click emission in `BookingHotel/angular/src/app/admin/manager/rates/components/pricing-calendar/pricing-calendar.component.ts` (with `.html` and `.scss`)
- [X] T022 [P] [US2] Create `DayPricingDetailModalComponent` showing `BasePrice`, `EffectivePrice`, active `PriceOverride` info, `CTA`/`CTD`, and Rate Plan status, with Quick-Edit form (`FixedPrice`/`Percentage`/`RestrictionsOnly`, `CTA`/`CTD` toggles, Reset-to-Base action) enabled only when `!isPastDate && isRatePlanActive` and preserving form inputs on error in `BookingHotel/angular/src/app/admin/manager/rates/components/day-pricing-detail-modal/day-pricing-detail-modal.component.ts` (with `.html` and `.scss`)
- [X] T023 [US2] Integrate `PricingCalendarComponent` and `DayPricingDetailModalComponent` into `RatesManagementComponent` so monthly calendar reloads synchronously on Room Type change, Rate Plan change, Base Price edit, or Quick-Edit save in `BookingHotel/angular/src/app/admin/manager/rates/rates-management.component.ts` (and `.html`)

**Checkpoint**: User Stories 1 and 2 work together seamlessly with live calendar synchronization.

---

## Phase 5: User Story 3 - Äiá»u chá»‰nh giÃ¡ hÃ ng loáº¡t cÃ³ Xem trÆ°á»›c & Xá»­ lÃ½ trÃ¹ng ngÃ y (Priority: P1)

**Goal**: Cho phÃ©p Hotel Manager cáº¥u hÃ¬nh Ä‘iá»u chá»‰nh giÃ¡ hÃ ng loáº¡t theo khoáº£ng ngÃ y (`StartDate >= Today`, inclusive), lá»c thá»© trong tuáº§n (`T2â€“CN`), Ã¡p dá»¥ng cho 1 hoáº·c nhiá»u gÃ³i giÃ¡ Ä‘ang `Active` theo 3 cháº¿ Ä‘á»™ (`FixedPrice`, `Percentage`, `RestrictionsOnly`) + `CTA`/`CTD`, báº¯t buá»™c qua bÆ°á»›c **Preview Backend-Authoritative** vÃ  xá»­ lÃ½ ngÃ y trÃ¹ng tÆ°á»ng minh (`Replace Existing` / `Skip Existing` / `Cancel`).

**Independent Test**: Cháº¡y Bulk Adjustment vá»›i cáº£ `FixedPrice`, `Percentage` (kiá»ƒm tra lÃ m trÃ²n `1.000 VNÄ`), `RestrictionsOnly` vÃ  lá»c thá»© `T6â€“CN`, kiá»ƒm tra mÃ n Preview hiá»ƒn thá»‹ Ä‘Ãºng danh sÃ¡ch ngÃ y trÃ¹ng, xÃ¡c minh Ä‘á»§ 3 nhÃ¡nh `Replace Existing`, `Skip Existing`, `Cancel`, vÃ  kiá»ƒm tra cháº·n gÃ³i `Inactive`, giÃ¡/pháº§n trÄƒm ra `<= 0` vÃ  `StartDate < Today`.

### Implementation for User Story 3

- [X] T024 [US3] Implement `PreviewBulkAdjustmentAsync` (Backend-Authoritative projection per Rate Plan using `RoomPricingDomainService`: current `BasePrice`, rounded `CalculatedEffectivePrice`, `DaysOfWeek` matching dates, and conflicting dates) and `ApplyBulkAdjustmentAsync` (enforcing `StartDate >= Today`, `StartDate <= EndDate`, active Rate Plans only, conflict resolution `Cancel`/`ReplaceExisting`/`SkipExisting` in atomic UoW, and writing `PricingAuditLog` per affected Rate Plan) in `BookingHotel/src/BookingHotel.Application/Manager/Rooms/ManagerRateAppService.cs`
- [X] T025 [US3] Add `POST /bulk-preview` and `POST /bulk-apply` endpoints in `BookingHotel/src/BookingHotel.HttpApi/Controllers/Manager/ManagerRateController.cs`
- [X] T026 [P] [US3] Create `BulkPriceAdjustmentComponent` (Section 4) with Reactive Form (`StartDate` min=`Today`, `EndDate`, `DaysOfWeek` toggle chips `T2â€“CN` defaulting to all 7 days, single/multi Active Rate Plan picker, `FixedPrice`/`Percentage`/`RestrictionsOnly` mode selector, independent `CTA`/`CTD` checkboxes, client validation synced with Backend, keeping form inputs intact on API error) and Backend-driven Preview + Conflict Resolution Modal (`Replace Existing` / `Skip Existing` / `Cancel`) in `BookingHotel/angular/src/app/admin/manager/rates/components/bulk-price-adjustment/bulk-price-adjustment.component.ts` (with `.html` and `.scss`)
- [X] T027 [US3] Wire `BulkPriceAdjustmentComponent` into `RatesManagementComponent` so successful bulk apply refreshes Section 1 KPIs, Section 2 `EffectivePrice`, Section 3 Calendar, and Section 5 Override List immediately in `BookingHotel/angular/src/app/admin/manager/rates/rates-management.component.ts` (and `.html`)

**Checkpoint**: All P1 user stories (`US1`, `US2`, `US3`) are complete and end-to-end operational.

---

## Phase 6: User Story 4 - Quáº£n lÃ½ danh sÃ¡ch Lá»‹ch giÃ¡ ghi Ä‘Ã¨ (Price Override Management) (Priority: P2)

**Goal**: Cho phÃ©p Hotel Manager xem toÃ n bá»™ danh sÃ¡ch khoáº£ng ngÃ y ghi Ä‘Ã¨ thuá»™c `Room Type + Rate Plan` Ä‘ang chá»n (Section 5), má»Ÿ xem chi tiáº¿t (`View`), chá»‰nh sá»­a khoáº£ng ngÃ y/giÃ¡/CTA/CTD (`Edit` kÃ¨m kiá»ƒm tra xung Ä‘á»™t vÃ  báº£o toÃ n ngÃ y quÃ¡ khá»©) vÃ  xÃ³a (`Delete` kÃ¨m há»™p thoáº¡i xÃ¡c nháº­n, thu háº¹p vá» `Yesterday` náº¿u váº¯t ngang hÃ´m nay).

**Independent Test**: Xem báº£ng Section 5 Ä‘áº§y Ä‘á»§ cá»™t, má»Ÿ `View`, thá»±c hiá»‡n `Edit` cÃ³ kiá»ƒm tra trÃ¹ng lá»‹ch, thá»±c hiá»‡n `Delete` trÃªn lá»‹ch tÆ°Æ¡ng lai vÃ  lá»‹ch váº¯t ngang hÃ´m nay (`StartDate < Today <= EndDate`) Ä‘á»ƒ xÃ¡c nháº­n pháº§n tÆ°Æ¡ng lai quay vá» `Base Price` cÃ²n pháº§n quÃ¡ khá»© giá»¯ nguyÃªn báº¥t biáº¿n.

### Implementation for User Story 4

- [X] T028 [US4] Implement `GetPriceOverridesAsync`, `UpdatePriceOverrideAsync` (blocking `EndDate < Today`, splitting straddling-today ranges to freeze `[StartDate..Yesterday]`, conflict check against sibling overrides with `ReplaceExisting`/`SkipExisting`/`Cancel`, and recording `PricingAuditLog` type `UpdatePriceOverride`/`UpdateRestrictions`), and `DeletePriceOverrideAsync` (blocking `EndDate < Today`, shrinking `EndDate = Today - 1` when straddling today, soft-deleting future ranges, and recording `PricingAuditLog` type `DeletePriceOverride`) in `BookingHotel/src/BookingHotel.Application/Manager/Rooms/ManagerRateAppService.cs`
- [X] T029 [US4] Add `GET /overrides`, `PUT /overrides/{id}`, and `DELETE /overrides/{id}` endpoints in `BookingHotel/src/BookingHotel.HttpApi/Controllers/Manager/ManagerRateController.cs`
- [X] T030 [P] [US4] Create `EditOverrideModalComponent` supporting `View` (read-only) and `Edit` modes for `RoomPriceOverrideDto` (enforcing `StartDate >= Today`, displaying straddling-today freeze banner when `isStraddlingToday`, and prompting conflict resolution on `409 BookingHotel:PriceOverrideConflictDetected`) in `BookingHotel/angular/src/app/admin/manager/rates/components/edit-override-modal/edit-override-modal.component.ts` (with `.html` and `.scss`)
- [X] T031 [US4] Implement Section 5 (`Price Override List` table showing `StartDate`, `EndDate`, `AdjustmentType`, `AdjustmentValue`/`CalculatedEffectivePrice`, `CTA`, `CTD`, `CreationTime`, `CreatorName`, `View`/`Edit`/`Delete` buttons disabling `Edit`/`Delete` on `isPastOnly` rows, delete confirm dialog via `BhConfirmService`, `bh-empty-state`, and live calendar/KPI sync) in `BookingHotel/angular/src/app/admin/manager/rates/rates-management.component.ts` (with `.html` and `.scss`)

**Checkpoint**: Full CRUD lifecycle of `RoomPriceOverride` is independently verifiable.

---

## Phase 7: User Story 5 - Lá»‹ch sá»­ thao tÃ¡c (Audit Log) & Báº£o toÃ n Booking (Priority: P2)

**Goal**: Cung cáº¥p kháº£ nÄƒng tra cá»©u minh báº¡ch nháº­t kÃ½ thay Ä‘á»•i giÃ¡ (`PricingAuditLog`) ngay trÃªn mÃ n hÃ¬nh Quáº£n lÃ½ giÃ¡ phÃ²ng cho cáº£ 6 nhÃ³m thao tÃ¡c vÃ  kiá»ƒm chá»©ng báº£o toÃ n tuyá»‡t Ä‘á»‘i giÃ¡ cá»§a cÃ¡c booking Ä‘Ã£ xÃ¡c nháº­n cÅ©ng nhÆ° cÃ¡ch ly báº£o máº­t Cross-Tenant / Cross-Hotel.

**Independent Test**: Thá»±c hiá»‡n Ä‘á»§ 6 nhÃ³m thao tÃ¡c, má»Ÿ modal/drawer "Lá»‹ch sá»­ thay Ä‘á»•i giÃ¡", lá»c theo GÃ³i giÃ¡ / Loáº¡i thao tÃ¡c / Khoáº£ng ngÃ y Ä‘á»ƒ xÃ¡c nháº­n Ä‘áº§y Ä‘á»§ thÃ´ng tin truy váº¿t, Ä‘á»“ng thá»i cháº¡y integration test xÃ¡c nháº­n `Booking.TotalAmount` khÃ´ng Ä‘á»•i vÃ  truy cáº­p trÃ¡i phÃ©p tráº£ vá» `403 Forbidden`.

### Implementation for User Story 5

- [X] T032 [US5] Implement `GetPricingAuditLogsAsync` returning `PagedResultDto<PricingAuditLogDto>` scoped strictly by `TenantId` and `HotelId` (after `CheckHotelManagerAccessAsync`), filterable by optional `RoomTypeId`, `RatePlanId`, `ActionType`, `FromDate`, `ToDate`, ordered by `ActionTime DESC` in `BookingHotel/src/BookingHotel.Application/Manager/Rooms/ManagerRateAppService.cs`
- [X] T033 [US5] Add `GET /audit-logs` endpoint in `BookingHotel/src/BookingHotel.HttpApi/Controllers/Manager/ManagerRateController.cs`
- [X] T034 [P] [US5] Create `PricingAuditLogDrawerComponent` (on-demand Modal/Drawer with Rate Plan filter, 6-action ActionType filter, Date Range filter, paginated table showing Actor, Action Time, Room Type & Rate Plan, Action Badge, formatted Before/After values, Affected Date Range, Summary Note, and `bh-empty-state`) in `BookingHotel/angular/src/app/admin/manager/rates/components/pricing-audit-log-drawer/pricing-audit-log-drawer.component.ts` (with `.html` and `.scss`)
- [X] T035 [US5] Add "Lá»‹ch sá»­ thay Ä‘á»•i giÃ¡" button on the page header in `RatesManagementComponent` opening `PricingAuditLogDrawerComponent` scoped to the active `HotelId` and `RoomTypeId` in `BookingHotel/angular/src/app/admin/manager/rates/rates-management.component.ts` (and `.html`)
- [X] T036 [P] [US5] Write application integration tests in `BookingHotel/test/BookingHotel.Application.Tests/Manager/ManagerRateAppServiceTests.cs` covering all 11 endpoints, verifying all 6 `PricingAuditActionType` entries (SC-006), confirming existing `Booking.TotalAmount` remains untouched after overrides/base-price edits (BR-12, SC-005), and asserting `403 Forbidden` for unassigned Hotel, Cross-Tenant, and `Receptionist` role (BR-13, BR-14, SC-007)

**Checkpoint**: All 5 user stories (`US1`â€“`US5`) are complete with automated Domain + Application tests.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: RÃ  soÃ¡t cháº¥t lÆ°á»£ng i18n, tuÃ¢n thá»§ Hiáº¿n phÃ¡p UI/UX, tÃ i liá»‡u roadmap vÃ  cháº¡y kiá»ƒm chá»©ng tá»•ng thá»ƒ theo `quickstart.md`.

- [X] T037 [P] Audit all templates and TypeScript files under `BookingHotel/angular/src/app/admin/manager/rates/` for Zero Fixed Text (every label/placeholder/badge/toastr via `abpLocalization` / `LocalizationService`), Constitution XI required red `*` non-bold labels, Operational Manager Console theme tokens (`#000000`/`#00FFFF`/`#FFFFFF`), and desktop/tablet responsive layout
- [X] T038 [P] Update RM-011 status (`xong`) and implementation summary in `docs/roadmap.md`
- [X] T039 Run full build & verification suite (`dotnet build BookingHotel.sln`, `dotnet test test/BookingHotel.Domain.Tests --filter "FullyQualifiedName~RoomPricing"`, `dotnet test test/BookingHotel.Application.Tests --filter "FullyQualifiedName~ManagerRate"`, `npm run build` and `npm run lint` in `BookingHotel/angular/`) and verify all 5 End-to-End scenarios in `BookingHotel/specs/011-room-pricing-management/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: KhÃ´ng cÃ³ phá»¥ thuá»™c â€” báº¯t Ä‘áº§u ngay.
- **Foundational (Phase 2)**: Phá»¥ thuá»™c Phase 1 â€” **cháº·n toÃ n bá»™ Phases 3â€“7** (`T005`â€“`T007` â†’ `T008` â†’ `T009`; song song `T010` â†’ `T011` â†’ `T012`, `T013`, `T014`).
- **User Stories (Phases 3â€“7)**: Táº¥t cáº£ phá»¥ thuá»™c hoÃ n thÃ nh Phase 2:
  - **US1 (Phase 3, P1)** â†’ ná»n táº£ng giao diá»‡n container `RatesManagementComponent` cho `US2`, `US3`, `US4`, `US5`.
  - **US2 (Phase 4, P1)**, **US3 (Phase 5, P1)**, **US4 (Phase 6, P2)**, **US5 (Phase 7, P2)**: Backend endpoints vÃ  standalone Angular sub-components (`PricingCalendarComponent`, `DayPricingDetailModalComponent`, `BulkPriceAdjustmentComponent`, `EditOverrideModalComponent`, `PricingAuditLogDrawerComponent`) cÃ³ thá»ƒ phÃ¡t triá»ƒn song song, sau Ä‘Ã³ tÃ­ch há»£p vÃ o `RatesManagementComponent`.
- **Polish (Phase 8)**: Phá»¥ thuá»™c hoÃ n thÃ nh Phases 3â€“7.

### Parallel Opportunities

- **Phase 1**: `T002`, `T003`, `T004` cháº¡y song song.
- **Phase 2**: (`T005`, `T006`), (`T010`, `T013`, `T014`) cháº¡y song song trÃªn cÃ¡c dá»± Ã¡n/tá»‡p tÃ¡ch biá»‡t.
- **Phase 3â€“7 (UI Sub-Components)**: `T017`, `T021`, `T022`, `T026`, `T030`, `T034` Ä‘á»u lÃ  cÃ¡c Angular Standalone Components Ä‘á»™c láº­p á»Ÿ thÆ° má»¥c riÃªng â€” cÃ³ thá»ƒ phÃ¡t triá»ƒn song song ngay sau khi `T013` (`ManagerRateService`) hoÃ n táº¥t.
- **Phase 7â€“8**: `T036`, `T037`, `T038` cháº¡y song song trÆ°á»›c cá»•ng nghiá»‡m thu cuá»‘i `T039`.

---

## Parallel Example: Foundational & UI Sub-Components

```text
# Song song trong Phase 2 (Entity & Contracts):
- T005: BookingHotel/src/BookingHotel.Domain/Rooms/RoomPriceOverride.cs
- T006: BookingHotel/src/BookingHotel.Domain/Rooms/PricingAuditLog.cs
- T010: BookingHotel/src/BookingHotel.Application.Contracts/Manager/Rooms/ManagerPricingDtos.cs
- T013: BookingHotel/angular/src/app/admin/services/manager/manager-rate.service.ts

# Song song cho cÃ¡c UI Sub-Components (sau khi T013 hoÃ n táº¥t):
- T017 [US1]: EditBasePriceModalComponent
- T021 [US2]: PricingCalendarComponent
- T022 [US2]: DayPricingDetailModalComponent
- T026 [US3]: BulkPriceAdjustmentComponent
- T030 [US4]: EditOverrideModalComponent
- T034 [US5]: PricingAuditLogDrawerComponent
```

---

## Implementation Strategy

### MVP First (Phase 1 + Phase 2 + User Story 1)
1. HoÃ n thÃ nh Phase 1 (`T001`â€“`T004`) vÃ  Phase 2 (`T005`â€“`T014`) â†’ DB migration + `RoomPricingDomainService` + Unit Tests PASS.
2. HoÃ n thÃ nh Phase 3 (`T015`â€“`T018`, User Story 1) â†’ Hotel Manager chá»n Háº¡ng phÃ²ng, xem KPI, báº­t/táº¯t GÃ³i giÃ¡ vÃ  sá»­a `Base Price` chi nhÃ¡nh.
3. **STOP & VALIDATE**: Kiá»ƒm thá»­ Ä‘á»™c láº­p US-1 trÃªn `/manager/rates`.

### Incremental Delivery
1. **Increment 1 (MVP)**: Setup + Foundational + **US1** (Section 1 & 2).
2. **Increment 2 (Core Calendar & Bulk)**: **US2** (Section 3 + Quick-Edit) + **US3** (Section 4 + Preview & Conflict Resolution).
3. **Increment 3 (Lifecycle & Compliance)**: **US4** (Section 5 Override CRUD) + **US5** (`PricingAuditLog` Drawer + Integration Tests).
4. **Final Gate**: Phase 8 Polish + `quickstart.md` E2E verification (`T037`â€“`T039`).
