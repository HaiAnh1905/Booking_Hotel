# Tasks: Phân hệ Tác nghiệp Lễ tân (RM-012)

**Input**: Design documents from `BookingHotel/specs/012-receptionist-frontdesk-portal/`  
**Prerequisites**: `plan.md`, `spec.md` (clarified Q1–Q4), `research.md`, `data-model.md`, `contracts/frontdesk-api.md`, `quickstart.md`

**Tests**: Included for core Domain business rules (`BookingHotel.Domain.Tests` per Constitution Principle V) and Backend AppService integration + security scoping (`BookingHotel.EntityFrameworkCore.Tests`).

**Organization**: Tasks are grouped by user story (US1–US5 in priority order P1 → P2) so each story can be implemented, tested, and verified independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: User story label (`[US1]`–`[US5]`) — applied ONLY to User Story phases
- All file paths are relative to `BookingHotel/` (unless explicitly prefixed with `../docs/`)

---

## Phase 1: Setup & Localization Scaffolding

**Purpose**: Shared Frontdesk enum and complete bilingual localization keys (Zero Fixed Text compliance across VI/EN + Angular fallback).

- [X] T001 [P] Create `FrontdeskRoomRackStatus` enum (`Ready = 1`, `Reserved = 2`, `Occupied = 3`, `NotReady = 4`, `Unavailable = 5`) in `src/BookingHotel.Domain.Shared/Bookings/FrontdeskRoomRackStatus.cs`
- [X] T002 [P] Add all Vietnamese localization keys (`Menu:FrontdeskDashboard`, `Menu:FrontdeskReservations`, `Menu:FrontdeskGuests`, `Menu:FrontdeskCheckInOut`, `Frontdesk:*` labels/statuses/empty states, and all 8 `BookingHotel:Frontdesk*` error codes) in `src/BookingHotel.Domain.Shared/Localization/BookingHotel/vi.json`
- [X] T003 [P] Add matching English localization keys in `src/BookingHotel.Domain.Shared/Localization/BookingHotel/en.json`
- [X] T004 [P] Mirror all new `Menu:Frontdesk*`, `Frontdesk:*`, and `BookingHotel:Frontdesk*` keys into `FALLBACK_VI` and `FALLBACK_EN` in `angular/src/app/shared/localization/fallback-localizations.ts`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core Domain invariants, minimal EF Core schema extension on `AppBookings`, security base class, Angular service client, and 5-item Receptionist Portal navigation shell.

**⚠️ CRITICAL**: No User Story implementation can begin until Phase 2 completes.

- [X] T005 Extend `Booking` aggregate root with 6 stay-tracking properties (`RoomTypeId: Guid?`, `ActualCheckInTime: DateTime?`, `ActualCheckOutTime: DateTime?`, `IsIdentityVerified: bool = false`, `CancellationReason: string?`, `FrontdeskNotes: string?`) in `src/BookingHotel.Domain/Bookings/Booking.cs` and populate `booking.RoomTypeId = input.RoomTypeId` in `src/BookingHotel.Application/Manager/Bookings/ManagerBookingAppService.cs` (`CreateDirectBookingAsync`)
- [X] T006 Configure EF Core Fluent API mappings (`CancellationReason` max 1000, `FrontdeskNotes` max 2000, `IsIdentityVerified` default `false`) and index `IX_AppBookings_Tenant_Hotel_RoomType` on `(TenantId, HotelId, RoomTypeId)` in `src/BookingHotel.EntityFrameworkCore/EntityFrameworkCore/BookingHotelDbContext.cs`
- [X] T007 Generate EF Core Code-First migration `AddFrontdeskStayTrackingToBookings` in `src/BookingHotel.EntityFrameworkCore/Migrations/` and apply it to SQL Server via `src/BookingHotel.DbMigrator/BookingHotel.DbMigrator.csproj`
- [X] T008 [P] Implement `FrontdeskStayDomainService` with `MaskIdentityNumber` (required min 6..32 chars, preserves last 4 chars prefixed with `*`, preserves valid already-masked value), booking state transition validators (`ValidateCanConfirm`, `ValidateCanCancel` requiring reason 5..1000 chars, `ValidateCanCheckIn`, `ValidateCanCheckOut`), and room availability/overlap/closure conflict checks in `src/BookingHotel.Domain/Bookings/FrontdeskStayDomainService.cs`
- [X] T009 [P] Implement `FrontdeskAppServiceBase` with `CheckFrontdeskHotelAccessAsync(Guid hotelId)` enforcing active `UserHotelAssignment` (or `TenantAdmin` within same tenant) and throwing `BookingHotel:CrossHotelOperationForbidden` in `src/BookingHotel.Application/Frontdesk/FrontdeskAppServiceBase.cs`
- [X] T010 [P] Write comprehensive Domain unit tests for `FrontdeskStayDomainService` (identity masking edge cases, Confirm/Cancel/CheckIn/CheckOut transition rules, room overlap and closure conflict detection) in `test/BookingHotel.Domain.Tests/Bookings/FrontdeskStayDomainServiceTests.cs`
- [X] T011 [P] Create strong-typed Angular REST client `FrontdeskService` with TypeScript interfaces matching all 4 Frontdesk API contracts (`dashboard`, `room-rack`, `reservations`, `guests`) in `angular/src/app/admin/services/frontdesk/frontdesk.service.ts`
- [X] T012 [P] Update Receptionist sidebar navigation to exact 5 items (`Dashboard`, `Room Rack`, `Reservation Management`, `Guest Management`, `Check-in / Check-out` — removing `Folios`) in `angular/src/app/admin/frontdesk/receptionist-layout/receptionist-layout.component.ts`
- [X] T013 Configure 5 lazy-loaded routes (`/dashboard`, `/room-rack`, `/reservations`, `/guests`, `/check-in-out`, plus `/check-in` and `/check-out` aliases and default redirect to `dashboard`) in `angular/src/app/admin/frontdesk/frontdesk.routes.ts` and update Receptionist default landing route to `/frontdesk/dashboard` in `angular/src/app/admin/guards/role-based-redirect.guard.ts`

**Checkpoint**: Foundation ready (DB migrated, Domain rules tested, security base & 5-route Frontdesk shell ready) — User Stories 1–5 can now proceed.

---

## Phase 3: User Story 1 - Bảng điều khiển tổng quan Lễ tân (Dashboard) (Priority: P1) 🎯 MVP

**Goal**: Receptionist sees live shift overview (total active bookings, expected arrivals today, in-house guests, expected departures today) plus actionable today-arrival, today-departure, and pending-confirmation lists with one-click navigation.

**Independent Test**: Log in as Receptionist assigned to a hotel → `/frontdesk/dashboard` displays accurate live counts matching SQL Server data, quick lists with action buttons, refresh control, and `<bh-empty-state>` when 0 bookings exist.

- [X] T014 [P] [US1] Create `FrontdeskDashboardDto`, `FrontdeskQuickBookingItemDto`, and `IFrontdeskDashboardAppService` in `src/BookingHotel.Application.Contracts/Frontdesk/FrontdeskDashboardDtos.cs` and `src/BookingHotel.Application.Contracts/Frontdesk/IFrontdeskDashboardAppService.cs`
- [X] T015 [US1] Implement `FrontdeskDashboardAppService` (`[Authorize(BookingHotelPermissions.Bookings.Default)]`, scoped via `CheckFrontdeskHotelAccessAsync`, computing live KPIs and today's arrival/departure/pending lists) in `src/BookingHotel.Application/Frontdesk/FrontdeskDashboardAppService.cs`
- [X] T016 [US1] Create `FrontdeskDashboardController` exposing `GET /api/app/frontdesk/dashboard/overview` in `src/BookingHotel.HttpApi/Controllers/Frontdesk/FrontdeskDashboardController.cs`
- [X] T017 [P] [US1] Create `FrontdeskDashboardComponent` logic (Angular Signals reacting to `propertyService.currentHotelId()`, live refresh, click-through routing with query params to Reservations / Check-in / Check-out) in `angular/src/app/admin/frontdesk/dashboard/frontdesk-dashboard.component.ts`
- [X] T018 [US1] Build `FrontdeskDashboardComponent` template and styles (4 KPI cards, 3 quick-action lists, loading spinner, `<bh-empty-state>`, Receptionist `#99FF99`/`#808080` concept) in `angular/src/app/admin/frontdesk/dashboard/frontdesk-dashboard.component.html` and `angular/src/app/admin/frontdesk/dashboard/frontdesk-dashboard.component.scss`

**Checkpoint**: User Story 1 (MVP Dashboard) works end-to-end and is independently testable.

---

## Phase 4: User Story 2 - Quản lý Đặt phòng (Reservation Management) (Priority: P1)

**Goal**: Receptionist receives, searches, filters, inspects, confirms, assigns/reassigns rooms, updates operational details, and cancels bookings (with mandatory reason) from Booking Hotel Platform — with read-only financials and zero Reject/No-Show/Walk-in controls.

**Independent Test**: Open `/frontdesk/reservations`, filter by keyword/status/assignment/date range, open detail drawer, confirm a `PendingConfirmation` booking, assign an available room of the booked room type, update contact/special requests/frontdesk notes without altering `TotalAmount`, and cancel a booking with mandatory reason verifying room release.

- [X] T019 [P] [US2] Create Reservation DTOs (`GetFrontdeskReservationsInputDto`, `FrontdeskReservationListItemDto`, `FrontdeskReservationDetailDto`, `FrontdeskAvailableRoomDto`, `AssignFrontdeskRoomDto`, `UpdateFrontdeskReservationDto` with `[Required]`/`[StringLength]`/`[EmailAddress]` and zero financial write fields, `CancelFrontdeskReservationDto` with `[Required]` reason 5..1000 chars) and `IFrontdeskReservationAppService` in `src/BookingHotel.Application.Contracts/Frontdesk/FrontdeskReservationDtos.cs` and `src/BookingHotel.Application.Contracts/Frontdesk/IFrontdeskReservationAppService.cs`
- [X] T020 [US2] Implement `FrontdeskReservationAppService` methods (`GetListAsync`, `GetAsync`, `GetAvailableRoomsAsync` with fallback `Booking.RoomTypeId` → latest `RoomAssignment.RoomTypeId` → all ready rooms when legacy booking has `RoomTypeId == null`, `ConfirmAsync` checking room-type inventory, `AssignRoomAsync` with atomic overlap/closure check, `Booking.RoomTypeId` backfill, and `Room.OccupancyStatus` sync, `UpdateAsync`, `CancelAsync` with `[Authorize(BookingHotelPermissions.Bookings.Cancel)]` releasing room assignment) in `src/BookingHotel.Application/Frontdesk/FrontdeskReservationAppService.cs`
- [X] T021 [US2] Create `FrontdeskReservationController` exposing `GET`, `GET {id}`, `GET {id}/available-rooms`, `POST {id}/confirm`, `POST {id}/assign-room`, `PUT {id}`, and `POST {id}/cancel` under `/api/app/frontdesk/reservations` in `src/BookingHotel.HttpApi/Controllers/Frontdesk/FrontdeskReservationController.cs`
- [X] T022 [P] [US2] Create `FrontdeskReservationsComponent` logic (multi-criteria filter signals synced with URL query params, pagination, detail drawer state, Confirm/AssignRoom/Update/Cancel modals with backend error handling and Toastr feedback) in `angular/src/app/admin/frontdesk/reservations/frontdesk-reservations.component.ts`
- [X] T023 [US2] Build `FrontdeskReservationsComponent` template (filter toolbar with `<bh-searchable-select>`, paginated table, slide-over detail drawer with read-only financials, Assign Room modal, Edit Operational Info modal, Cancel Booking modal with red `*` required markers, `<bh-empty-state>`) in `angular/src/app/admin/frontdesk/reservations/frontdesk-reservations.component.html`
- [X] T024 [P] [US2] Style `FrontdeskReservationsComponent` following Receptionist concept (`#99FF99`, `#808080`, high-contrast `#000000`, responsive grid filter bar and detail drawer) in `angular/src/app/admin/frontdesk/reservations/frontdesk-reservations.component.scss`

**Checkpoint**: User Story 2 (Reservation Management) works end-to-end and is independently testable.

---

## Phase 5: User Story 3 - Quy trình Nhận phòng & Trả phòng (Check-in / Check-out) (Priority: P1)

**Goal**: Receptionist verifies guest identity (mandatory CCCD/Passport auto-masked to last 4 chars), ensures a ready room is assigned, confirms Check-in (`Confirmed -> CheckedIn`, `Room -> Occupied`), and confirms Check-out (`CheckedIn -> Completed`, `Room -> Vacant` & `Clean` ready immediately per Q2).

**Independent Test**: Open `/frontdesk/check-in-out`, complete Check-in on a `Confirmed` booking with CCCD input and room selection → verify `ActualCheckInTime`, masked ID (`********1234`), `IsIdentityVerified = true`, and room `Occupied`; then switch to Check-out tab and confirm Check-out → verify `Completed`, `ActualCheckOutTime`, and room immediately `Vacant` & `Ready`.

- [X] T025 [P] [US3] Add `FrontdeskCheckInDto` (`HotelId`, `RoomId`, `GuestFullName`, `GuestPhone`, `GuestEmail?`, required `IdentityNumber` 6..32 chars, `FrontdeskNotes?`) and `FrontdeskCheckOutDto` (`HotelId`, `FrontdeskNotes?`) plus `CheckInAsync`/`CheckOutAsync` method signatures in `src/BookingHotel.Application.Contracts/Frontdesk/FrontdeskReservationDtos.cs` and `src/BookingHotel.Application.Contracts/Frontdesk/IFrontdeskReservationAppService.cs`
- [X] T026 [US3] Implement `CheckInAsync` (`[Authorize(BookingHotelPermissions.Bookings.CheckIn)]` with identity masking, atomic room readiness & overlap verification, `BookingStatus -> CheckedIn`, `ActualCheckInTime`, `IsIdentityVerified = true`, `Room.OccupancyStatus -> Occupied`) and `CheckOutAsync` (`[Authorize(BookingHotelPermissions.Bookings.CheckOut)]`, `BookingStatus -> Completed`, `ActualCheckOutTime`, closing `RoomAssignment`, setting `Room.OccupancyStatus` to `Vacant`/`Reserved` and `Room.HousekeepingStatus = Clean`) in `src/BookingHotel.Application/Frontdesk/FrontdeskReservationAppService.cs`
- [X] T027 [US3] Expose `POST /api/app/frontdesk/reservations/{id}/check-in` and `POST /api/app/frontdesk/reservations/{id}/check-out` in `src/BookingHotel.HttpApi/Controllers/Frontdesk/FrontdeskReservationController.cs`
- [X] T028 [P] [US3] Create `FrontdeskCheckInOutComponent` logic (two tabs `checkin`/`checkout` synced with route/query params, quick search, Check-in verification modal loading available rooms when unassigned or current room unavailable, early/late check-in/out indicator, Check-out review modal) in `angular/src/app/admin/frontdesk/check-in-out/frontdesk-check-in-out.component.ts`
- [X] T029 [US3] Build `FrontdeskCheckInOutComponent` template and styles (arrival/in-house action cards/tables, Check-in modal with red `*` required markers on Guest Name, Phone, CCCD/Passport, Room selector; Check-out confirmation modal; loading & `<bh-empty-state>`) in `angular/src/app/admin/frontdesk/check-in-out/frontdesk-check-in-out.component.html` and `angular/src/app/admin/frontdesk/check-in-out/frontdesk-check-in-out.component.scss`

**Checkpoint**: All 3 P1 User Stories (Dashboard, Reservations, Check-in/Check-out) form a complete end-to-end Frontdesk stay lifecycle.

---

## Phase 6: User Story 4 - Sơ đồ Buồng phòng trực quan (Room Rack) (Priority: P2)

**Goal**: Receptionist monitors all physical rooms of their assigned property in Grid or Table view with live read-only occupancy & readiness status (`Ready`, `Reserved`, `Occupied`, `NotReady`, `Unavailable`), dynamic floor filters, and quick room-detail drawer linking to Check-in / Check-out / Reservation Detail — with zero Housekeeping or Maintenance actions.

**Independent Test**: Open `/frontdesk/room-rack`, verify dynamic floors, status counters, Grid/Table toggle, search, click an `Occupied` or `Reserved` room to inspect linked booking in the slide-over drawer and navigate directly to Check-out / Check-in, and confirm no Housekeeping/Maintenance buttons exist.

- [X] T030 [P] [US4] Create `FrontdeskRoomRackDto`, `FrontdeskRoomRackItemDto`, `FrontdeskRoomBookingSummaryDto`, `GetFrontdeskRoomRackInputDto`, and `IFrontdeskRoomRackAppService` in `src/BookingHotel.Application.Contracts/Frontdesk/FrontdeskRoomRackDtos.cs` and `src/BookingHotel.Application.Contracts/Frontdesk/IFrontdeskRoomRackAppService.cs`
- [X] T031 [US4] Implement read-only `FrontdeskRoomRackAppService` (`[Authorize(BookingHotelPermissions.Rooms.Default)]`, scoped via `CheckFrontdeskHotelAccessAsync`, computing `FrontdeskRoomRackStatus`, dynamic `Floors`, counts, and active linked booking summary per room) in `src/BookingHotel.Application/Frontdesk/FrontdeskRoomRackAppService.cs`
- [X] T032 [US4] Create `FrontdeskRoomRackController` exposing `GET /api/app/frontdesk/room-rack` in `src/BookingHotel.HttpApi/Controllers/Frontdesk/FrontdeskRoomRackController.cs`
- [X] T033 [US4] Refactor `RoomRackComponent` TypeScript to use `FrontdeskService.getRoomRack()`, remove `quickClean` Housekeeping mutation and out-of-scope approval modal, and add quick room-detail drawer state + deep-link navigation to `/frontdesk/check-in-out` and `/frontdesk/reservations` in `angular/src/app/admin/frontdesk/room-rack/room-rack.component.ts`
- [X] T034 [US4] Update `RoomRackComponent` template and styles (5 read-only status states `ready`/`reserved`/`occupied`/`not-ready`/`unavailable`, dynamic floor filter chips, Grid/Table toggle, slide-over quick room & booking detail drawer, `<bh-empty-state>`) in `angular/src/app/admin/frontdesk/room-rack/room-rack.component.html` and `angular/src/app/admin/frontdesk/room-rack/room-rack.component.scss`

**Checkpoint**: User Story 4 (Read-Only Room Rack) is live and synchronized with Reservations and Check-in/Check-out.

---

## Phase 7: User Story 5 - Quản lý Khách lưu trú theo Booking (Guest Management) (Priority: P2)

**Goal**: Receptionist searches and views all guests associated with bookings at their assigned property, inspects booking/stay history at that property, and updates guest contact & identity info (auto-masked on backend and synced to active bookings).

**Independent Test**: Open `/frontdesk/guests`, search guest by name/phone/email/masked ID, open guest detail drawer verifying property booking history with links to Reservations, update phone/email/CCCD in edit modal, and verify masked ID and synced active booking details persist on reload.

- [X] T035 [P] [US5] Create Guest DTOs (`GetFrontdeskGuestsInputDto`, `FrontdeskGuestListItemDto`, `FrontdeskGuestDetailDto`, `FrontdeskGuestBookingItemDto`, `UpdateFrontdeskGuestDto` with `[Required]` fullName/phone, optional email & identityNumber) and `IFrontdeskGuestAppService` in `src/BookingHotel.Application.Contracts/Frontdesk/FrontdeskGuestDtos.cs` and `src/BookingHotel.Application.Contracts/Frontdesk/IFrontdeskGuestAppService.cs`
- [X] T036 [US5] Implement `FrontdeskGuestAppService` (`[Authorize(BookingHotelPermissions.Bookings.Default)]`, scoped via `CheckFrontdeskHotelAccessAsync`, grouping property bookings with canonical URL-safe `guestKey` `id:{guid}` / `phone:{digitsOnly}` / `booking:{id}`, returning stay history, updating contact & auto-masked identity across active bookings of that guest within the hotel and returning the refreshed `guestKey`) in `src/BookingHotel.Application/Frontdesk/FrontdeskGuestAppService.cs`
- [X] T037 [US5] Create `FrontdeskGuestController` exposing `GET`, `GET detail` (`?hotelId&guestKey`), and `PUT detail` (`?guestKey`) under `/api/app/frontdesk/guests` in `src/BookingHotel.HttpApi/Controllers/Frontdesk/FrontdeskGuestController.cs`
- [X] T038 [P] [US5] Create `FrontdeskGuestsComponent` logic (paginated guest directory, keyword search, detail slide-over drawer with stay history links to `/frontdesk/reservations`, edit guest modal with validation and Toastr feedback) in `angular/src/app/admin/frontdesk/guests/frontdesk-guests.component.ts`
- [X] T039 [US5] Build `FrontdeskGuestsComponent` template and styles (search toolbar, paginated table, guest detail drawer with booking history list, edit guest modal with red `*` required markers, loading & `<bh-empty-state>`) in `angular/src/app/admin/frontdesk/guests/frontdesk-guests.component.html` and `angular/src/app/admin/frontdesk/guests/frontdesk-guests.component.scss`

**Checkpoint**: All 5 User Stories (US1–US5) are independently functional and fully integrated.

---

## Phase 8: Polish, Automated Verification & Quality Gates

**Purpose**: End-to-end integration tests, security verification, clean builds across Backend & Frontend, Zero Mock / Zero Fixed Text audit, and roadmap update.

- [X] T040 Write EF Core + Application integration tests covering Dashboard KPIs, Room Rack read-only statuses, Reservation Confirm/Assign/Update/Cancel, Check-in (identity masking + room Occupied) & Check-out (room Vacant + Clean), Guest update sync, and Cross-Hotel forbidden access in `test/BookingHotel.EntityFrameworkCore.Tests/Frontdesk/FrontdeskAppServiceTests.cs`
- [X] T041 Execute Backend build and automated test suites (`BookingHotel.Domain.Tests` and `BookingHotel.EntityFrameworkCore.Tests` for `Frontdesk*`) and resolve any compiler warnings or test failures
- [X] T042 Execute Frontend verification (`npx ngc -p tsconfig.app.json --noEmit`, `npm run build`, and JSON syntax check on `vi.json`/`en.json`) in `angular/`
- [X] T043 Audit `angular/src/app/admin/frontdesk/` and `src/BookingHotel.Application/Frontdesk/` for Zero Mock Data, Zero Fixed Text, red `*` required-field markers, and zero out-of-scope controls (no pricing, rate plans, staff, Housekeeping, maintenance, Reject, or No-Show; confirm legacy `frontdesk-approval-modal` is not imported by any RM-012 route)
- [X] T044 Update RM-012 status to `xong` with verification summary in `../docs/roadmap.md`

---

## Dependencies & Execution Order

### Phase Dependencies
- **Phase 1 (Setup & Localization)** → no dependencies; all 4 tasks `[P]` run in parallel.
- **Phase 2 (Foundational)** → depends on T001; T005 → T006 → T007 (schema & migration) must run sequentially, while T008, T009, T010, T011, T012 can run in parallel, followed by T013. **BLOCKS Phases 3–7**.
- **Phases 3–7 (User Stories)**:
  - **US1 (Dashboard, P1)** — independent after Phase 2.
  - **US2 (Reservation Management, P1)** — independent after Phase 2.
  - **US3 (Check-in / Check-out, P1)** — builds on `FrontdeskReservationAppService` & `FrontdeskReservationController` from US2 (T020–T021) for backend check-in/out endpoints, while its UI (`FrontdeskCheckInOutComponent`) is an independent route/component.
  - **US4 (Room Rack, P2)** — independent after Phase 2 (deep-links to US2 & US3 routes).
  - **US5 (Guest Management, P2)** — independent after Phase 2 (deep-links to US2 route).
- **Phase 8 (Polish & Quality Gates)** → runs after Phases 3–7 complete.

### Parallel Execution Examples

```text
# Phase 1 — all 4 tasks in parallel:
T001 (FrontdeskRoomRackStatus.cs) || T002 (vi.json) || T003 (en.json) || T004 (fallback-localizations.ts)

# Phase 2 (after T005 Booking.cs):
T006 (DbContext) -> T007 (Migration + DbMigrator)
|| T008 (FrontdeskStayDomainService) || T009 (FrontdeskAppServiceBase) || T011 (frontdesk.service.ts) || T012 (receptionist-layout)

# User Stories after Phase 2:
US1 (T014-T018) || US2 -> US3 (T019-T029) || US4 (T030-T034) || US5 (T035-T039)
```

---

## Implementation Strategy

1. **MVP Increment**: Complete **Phase 1 + Phase 2 + Phase 3 (US1 Dashboard)** → validate live Receptionist login landing on `/frontdesk/dashboard` with real SQL Server metrics.
2. **Core Operations Increment**: Complete **Phase 4 (US2 Reservations)** + **Phase 5 (US3 Check-in / Check-out)** → full booking reception & stay lifecycle works end-to-end.
3. **Full Frontdesk Portal**: Complete **Phase 6 (US4 Read-Only Room Rack)** + **Phase 7 (US5 Guest Management)** → complete 5-module Receptionist Portal.
4. **Quality Gate**: Complete **Phase 8** → Domain unit tests, EF Core integration tests, Angular production build, security & zero-mock audit, and roadmap update.
