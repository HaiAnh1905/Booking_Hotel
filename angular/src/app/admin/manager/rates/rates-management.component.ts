import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalizationPipe, LocalizationService } from '@abp/ng.core';
import { PropertyContextService } from '../../services/property-context.service';
import { ManagerRoomService, RoomTypeDto } from '../../services/manager/manager-room.service';
import {
  DailyPricingCellDto,
  ManagerRateService,
  MonthlyPricingCalendarDto,
  PriceAdjustmentType,
  RoomPriceOverrideDto,
  RoomRatePlanItemDto,
  RoomTypePricingContextDto,
} from '../../services/manager/manager-rate.service';
import {
  BhSearchableSelectComponent,
  SelectOption,
} from '../../../shared/components/searchable-select/bh-searchable-select.component';
import { BhEmptyStateComponent } from '../../../shared/components/empty-state/bh-empty-state.component';
import { BhConfirmService } from '../../../shared/components/confirm-modal/bh-confirm.service';
import { BhToastrService } from '../../../shared/components/toastr/bh-toastr.service';
import { EditBasePriceModalComponent } from './components/edit-base-price-modal/edit-base-price-modal.component';
import { PricingCalendarComponent } from './components/pricing-calendar/pricing-calendar.component';
import { DayPricingDetailModalComponent } from './components/day-pricing-detail-modal/day-pricing-detail-modal.component';
import { BulkPriceAdjustmentComponent } from './components/bulk-price-adjustment/bulk-price-adjustment.component';
import { EditOverrideModalComponent } from './components/edit-override-modal/edit-override-modal.component';
import { PricingAuditLogDrawerComponent } from './components/pricing-audit-log-drawer/pricing-audit-log-drawer.component';

@Component({
  selector: 'bh-rates-management',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LocalizationPipe,
    BhSearchableSelectComponent,
    BhEmptyStateComponent,
    EditBasePriceModalComponent,
    PricingCalendarComponent,
    DayPricingDetailModalComponent,
    BulkPriceAdjustmentComponent,
    EditOverrideModalComponent,
    PricingAuditLogDrawerComponent,
  ],
  templateUrl: './rates-management.component.html',
  styleUrls: ['./rates-management.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RatesManagementComponent {
  readonly propertyService = inject(PropertyContextService);
  private readonly roomService = inject(ManagerRoomService);
  private readonly rateService = inject(ManagerRateService);
  private readonly confirmService = inject(BhConfirmService);
  private readonly toastr = inject(BhToastrService);
  private readonly localization = inject(LocalizationService);

  readonly PriceAdjustmentType = PriceAdjustmentType;

  readonly roomTypes = signal<RoomTypeDto[]>([]);
  readonly selectedRoomTypeId = signal<string>('');
  readonly pricingContext = signal<RoomTypePricingContextDto | null>(null);
  readonly selectedRatePlanId = signal<string>('');

  readonly currentYear = signal<number>(new Date().getFullYear());
  readonly currentMonth = signal<number>(new Date().getMonth() + 1);
  readonly calendarData = signal<MonthlyPricingCalendarDto | null>(null);
  readonly overrides = signal<RoomPriceOverrideDto[]>([]);

  readonly isLoadingContext = signal<boolean>(false);
  readonly isLoadingCalendar = signal<boolean>(false);
  readonly isLoadingOverrides = signal<boolean>(false);

  // Modals & Drawer state
  readonly editBasePriceModalOpen = signal<boolean>(false);
  readonly editingBasePricePlan = signal<RoomRatePlanItemDto | null>(null);

  readonly dayDetailModalOpen = signal<boolean>(false);
  readonly selectedDayCell = signal<DailyPricingCellDto | null>(null);

  readonly overrideModalOpen = signal<boolean>(false);
  readonly overrideModalMode = signal<'view' | 'edit'>('view');
  readonly activeOverrideItem = signal<RoomPriceOverrideDto | null>(null);

  readonly auditDrawerOpen = signal<boolean>(false);

  readonly ratePlans = computed<RoomRatePlanItemDto[]>(() => this.pricingContext()?.ratePlans ?? []);

  readonly selectedRatePlan = computed<RoomRatePlanItemDto | null>(() => {
    const id = this.selectedRatePlanId();
    return this.ratePlans().find(p => p.ratePlanId === id) ?? null;
  });

  readonly roomTypeOptions = computed<SelectOption[]>(() => {
    const types = this.roomTypes();
    if (!types || types.length === 0) {
      return [
        {
          value: '',
          label: this.localization.instant('Manager:Room:NoRoomTypesAvailable'),
          sublabel: this.localization.instant('Manager:Room:PleaseCreateRoomTypeFirst'),
          icon: 'fa-solid fa-circle-exclamation text-warning',
          disabled: true,
        },
      ];
    }

    const baseLabel = this.localization.instant('Manager:Rates:ColBasePrice');
    return types.map(t => ({
      value: t.id,
      label: t.name,
      sublabel: `${baseLabel}: ${Number(t.basePrice).toLocaleString()} VNĐ`,
      icon: 'fa-solid fa-door-open',
    }));
  });

  constructor() {
    effect(() => {
      const hotelId = this.propertyService.currentHotelId();
      this.resetWorkspaceState();
      if (hotelId) {
        this.loadRoomTypes(hotelId);
      }
    });
  }

  private resetWorkspaceState(): void {
    this.roomTypes.set([]);
    this.selectedRoomTypeId.set('');
    this.pricingContext.set(null);
    this.selectedRatePlanId.set('');
    this.calendarData.set(null);
    this.overrides.set([]);
    this.editBasePriceModalOpen.set(false);
    this.dayDetailModalOpen.set(false);
    this.overrideModalOpen.set(false);
    this.auditDrawerOpen.set(false);
  }

  loadRoomTypes(hotelId: string): void {
    this.isLoadingContext.set(true);
    this.roomService.getRoomTypes(hotelId).subscribe({
      next: types => {
        this.roomTypes.set(types);
        if (types.length > 0) {
          this.selectedRoomTypeId.set(types[0].id);
          this.loadPricingContext(hotelId, types[0].id, true);
        } else {
          this.pricingContext.set(null);
          this.isLoadingContext.set(false);
        }
      },
      error: () => {
        this.isLoadingContext.set(false);
      },
    });
  }

  onRoomTypeChange(roomTypeId: string): void {
    const hotelId = this.propertyService.currentHotelId();
    if (!hotelId || !roomTypeId) return;
    this.selectedRoomTypeId.set(roomTypeId);
    this.selectedRatePlanId.set('');
    this.calendarData.set(null);
    this.overrides.set([]);
    this.loadPricingContext(hotelId, roomTypeId, true);
  }

  loadPricingContext(hotelId: string, roomTypeId: string, resetPlanSelection: boolean): void {
    this.isLoadingContext.set(true);
    this.rateService.getPricingContext(hotelId, roomTypeId).subscribe({
      next: ctx => {
        this.pricingContext.set(ctx);
        this.isLoadingContext.set(false);

        const currentId = this.selectedRatePlanId();
        const stillExists = ctx.ratePlans.some(p => p.ratePlanId === currentId);
        let nextPlanId = stillExists && !resetPlanSelection ? currentId : '';

        if (!nextPlanId && ctx.ratePlans.length > 0) {
          const firstActive = ctx.ratePlans.find(p => p.isActive) ?? ctx.ratePlans[0];
          nextPlanId = firstActive.ratePlanId;
        }

        this.selectedRatePlanId.set(nextPlanId);
        if (nextPlanId) {
          this.loadCalendarAndOverrides(hotelId, roomTypeId, nextPlanId);
        } else {
          this.calendarData.set(null);
          this.overrides.set([]);
        }
      },
      error: () => {
        this.isLoadingContext.set(false);
      },
    });
  }

  onSelectRatePlan(ratePlanId: string): void {
    const hotelId = this.propertyService.currentHotelId();
    const roomTypeId = this.selectedRoomTypeId();
    if (!hotelId || !roomTypeId || !ratePlanId) return;
    this.selectedRatePlanId.set(ratePlanId);
    this.loadCalendarAndOverrides(hotelId, roomTypeId, ratePlanId);
  }

  loadCalendarAndOverrides(hotelId: string, roomTypeId: string, ratePlanId: string): void {
    this.loadMonthlyCalendar(hotelId, roomTypeId, ratePlanId, this.currentYear(), this.currentMonth());
    this.loadOverrides(hotelId, roomTypeId, ratePlanId);
  }

  loadMonthlyCalendar(hotelId: string, roomTypeId: string, ratePlanId: string, year: number, month: number): void {
    this.isLoadingCalendar.set(true);
    this.rateService.getMonthlyCalendar(hotelId, roomTypeId, ratePlanId, year, month).subscribe({
      next: cal => {
        this.calendarData.set(cal);
        this.isLoadingCalendar.set(false);
      },
      error: () => {
        this.calendarData.set(null);
        this.isLoadingCalendar.set(false);
      },
    });
  }

  loadOverrides(hotelId: string, roomTypeId: string, ratePlanId: string): void {
    this.isLoadingOverrides.set(true);
    this.rateService.getPriceOverrides(hotelId, roomTypeId, ratePlanId).subscribe({
      next: list => {
        this.overrides.set(list);
        this.isLoadingOverrides.set(false);
      },
      error: () => {
        this.overrides.set([]);
        this.isLoadingOverrides.set(false);
      },
    });
  }

  onMonthChange(event: { year: number; month: number }): void {
    this.currentYear.set(event.year);
    this.currentMonth.set(event.month);
    const hotelId = this.propertyService.currentHotelId();
    const roomTypeId = this.selectedRoomTypeId();
    const ratePlanId = this.selectedRatePlanId();
    if (hotelId && roomTypeId && ratePlanId) {
      this.loadMonthlyCalendar(hotelId, roomTypeId, ratePlanId, event.year, event.month);
    }
  }

  async onToggleRatePlan(plan: RoomRatePlanItemDto, event: Event): Promise<void> {
    event.stopPropagation();
    const inputEl = event.target as HTMLInputElement;
    const targetActive = !plan.isActive;
    // Revert visually until confirmed & saved
    inputEl.checked = plan.isActive;

    const confirmed = await this.confirmService.confirm({
      title: this.localization.instant(
        targetActive ? 'Manager:Rates:ToggleConfirmActivateTitle' : 'Manager:Rates:ToggleConfirmDeactivateTitle'
      ),
      message: this.localization.instant(
        targetActive ? 'Manager:Rates:ToggleConfirmActivateMsg' : 'Manager:Rates:ToggleConfirmDeactivateMsg'
      ),
      confirmText: this.localization.instant('Common:Confirm'),
      cancelText: this.localization.instant('Common:Cancel'),
      type: targetActive ? 'primary' : 'warning',
    });

    if (!confirmed) return;

    const hotelId = this.propertyService.currentHotelId();
    const roomTypeId = this.selectedRoomTypeId();
    if (!hotelId || !roomTypeId) return;

    this.rateService
      .toggleRatePlanStatus({
        hotelId,
        roomTypeId,
        ratePlanId: plan.ratePlanId,
        isActive: targetActive,
      })
      .subscribe({
        next: () => {
          this.toastr.success('Manager:Rates:ToggleSuccess');
          this.refreshAll();
        },
        error: err => {
          const code = err?.error?.error?.code;
          this.toastr.error(code || 'Manager:Rates:ErrorSaveFailed');
        },
      });
  }

  openEditBasePriceModal(plan: RoomRatePlanItemDto, event: Event): void {
    event.stopPropagation();
    this.editingBasePricePlan.set(plan);
    this.editBasePriceModalOpen.set(true);
  }

  onBasePriceSaved(): void {
    this.editBasePriceModalOpen.set(false);
    this.editingBasePricePlan.set(null);
    this.refreshAll();
  }

  onDaySelected(cell: DailyPricingCellDto): void {
    this.selectedDayCell.set(cell);
    this.dayDetailModalOpen.set(true);
  }

  onDayQuickEditSaved(): void {
    this.dayDetailModalOpen.set(false);
    this.selectedDayCell.set(null);
    this.refreshAll();
  }

  onBulkApplied(): void {
    this.refreshAll();
  }

  openOverrideModal(item: RoomPriceOverrideDto, mode: 'view' | 'edit'): void {
    this.activeOverrideItem.set(item);
    this.overrideModalMode.set(mode);
    this.overrideModalOpen.set(true);
  }

  onOverrideSaved(): void {
    this.overrideModalOpen.set(false);
    this.activeOverrideItem.set(null);
    this.refreshAll();
  }

  async onDeleteOverride(item: RoomPriceOverrideDto): Promise<void> {
    if (item.isPastOnly) return;

    const confirmed = await this.confirmService.confirm({
      title: this.localization.instant('Manager:Rates:DeleteOverrideTitle'),
      message: this.localization.instant(
        item.isStraddlingToday ? 'Manager:Rates:StraddlingTodayNotice' : 'Manager:Rates:DeleteOverrideMsg'
      ),
      confirmText: this.localization.instant('Common:Delete'),
      cancelText: this.localization.instant('Common:Cancel'),
      type: 'danger',
    });

    if (!confirmed) return;

    const hotelId = this.propertyService.currentHotelId();
    if (!hotelId) return;

    this.rateService.deletePriceOverride(item.id, hotelId).subscribe({
      next: () => {
        this.toastr.success('Manager:Rates:DeleteOverrideSuccess');
        this.refreshAll();
      },
      error: err => {
        const code = err?.error?.error?.code;
        this.toastr.error(code || 'Manager:Rates:ErrorSaveFailed');
      },
    });
  }

  refreshAll(): void {
    const hotelId = this.propertyService.currentHotelId();
    const roomTypeId = this.selectedRoomTypeId();
    if (!hotelId || !roomTypeId) return;
    this.loadPricingContext(hotelId, roomTypeId, false);
  }
}
