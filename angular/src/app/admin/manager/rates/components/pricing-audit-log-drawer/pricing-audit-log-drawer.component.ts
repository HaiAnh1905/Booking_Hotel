import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  SimpleChanges,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalizationPipe, LocalizationService } from '@abp/ng.core';
import { BhModalComponent } from '../../../../../shared/components/modal/bh-modal.component';
import { BhModalConfig } from '../../../../../shared/models/ui-models';
import { BhEmptyStateComponent } from '../../../../../shared/components/empty-state/bh-empty-state.component';
import {
  BhSearchableSelectComponent,
  SelectOption,
} from '../../../../../shared/components/searchable-select/bh-searchable-select.component';
import {
  ManagerRateService,
  PricingAuditActionType,
  PricingAuditLogDto,
  RoomRatePlanItemDto,
} from '../../../../services/manager/manager-rate.service';

export interface AuditDiffSummary {
  primaryText: string;
  secondaryText?: string;
  statusVariant?: 'active' | 'inactive' | 'base' | 'override';
  hasCta?: boolean;
  hasCtd?: boolean;
  showNoRestrictions?: boolean;
}

@Component({
  selector: 'bh-pricing-audit-log-drawer',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LocalizationPipe,
    BhModalComponent,
    BhEmptyStateComponent,
    BhSearchableSelectComponent,
  ],
  templateUrl: './pricing-audit-log-drawer.component.html',
  styleUrls: ['./pricing-audit-log-drawer.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PricingAuditLogDrawerComponent implements OnChanges {
  private readonly rateService = inject(ManagerRateService);
  private readonly localization = inject(LocalizationService);

  @Input() isOpen = false;
  @Input() hotelId = '';
  @Input() roomTypeId = '';
  @Input() ratePlans: RoomRatePlanItemDto[] = [];

  @Output() closed = new EventEmitter<void>();

  readonly actionTypes = [
    PricingAuditActionType.CreatePriceOverride,
    PricingAuditActionType.UpdatePriceOverride,
    PricingAuditActionType.DeletePriceOverride,
    PricingAuditActionType.UpdateBasePrice,
    PricingAuditActionType.ToggleRatePlanStatus,
    PricingAuditActionType.UpdateRestrictions,
  ];

  readonly selectedRatePlanId = signal<string>('');
  readonly selectedActionType = signal<PricingAuditActionType | ''>('');
  readonly fromDate = signal<string>('');
  readonly toDate = signal<string>('');
  readonly page = signal<number>(1);
  readonly pageSize = 10;

  readonly isLoading = signal(false);
  readonly totalCount = signal(0);
  readonly logs = signal<PricingAuditLogDto[]>([]);

  readonly modalConfig: BhModalConfig = {
    titleKey: 'Manager:Rates:AuditDrawerTitle',
    confirmTextKey: 'Common:Close',
    hideCancelButton: true,
    iconClass: 'fa-solid fa-clock-rotate-left',
    size: 'xl',
    confirmVariant: 'primary',
  };

  get ratePlanFilterOptions(): SelectOption[] {
    return [
      {
        value: '',
        label: this.localization.instant('Manager:Rates:AuditFilterAllPlans'),
        icon: 'fa-solid fa-layer-group',
      },
      ...this.ratePlans.map(p => ({
        value: p.ratePlanId,
        label: p.name,
        sublabel: p.code,
        icon: 'fa-solid fa-tag',
      })),
    ];
  }

  get actionTypeFilterOptions(): SelectOption[] {
    return [
      {
        value: '',
        label: this.localization.instant('Manager:Rates:AuditFilterAllActions'),
        icon: 'fa-solid fa-list',
      },
      ...this.actionTypes.map(at => ({
        value: at,
        label: this.localization.instant(`Manager:Rates:AuditAction_${at}`),
      })),
    ];
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount() / this.pageSize));
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen'] && this.isOpen) {
      this.page.set(1);
      this.loadLogs();
    }
  }

  onFilterApply(): void {
    this.page.set(1);
    this.loadLogs();
  }

  goToPage(nextPage: number): void {
    if (nextPage < 1 || nextPage > this.totalPages) return;
    this.page.set(nextPage);
    this.loadLogs();
  }

  loadLogs(): void {
    if (!this.hotelId) return;
    this.isLoading.set(true);

    const actionFilter = this.selectedActionType();
    this.rateService
      .getPricingAuditLogs({
        hotelId: this.hotelId,
        roomTypeId: this.roomTypeId || null,
        ratePlanId: this.selectedRatePlanId() || null,
        actionType: actionFilter === '' ? null : Number(actionFilter),
        fromDate: this.fromDate() || null,
        toDate: this.toDate() || null,
        skipCount: (this.page() - 1) * this.pageSize,
        maxResultCount: this.pageSize,
      })
      .subscribe({
        next: res => {
          this.logs.set(res.items ?? []);
          this.totalCount.set(res.totalCount ?? 0);
          this.isLoading.set(false);
        },
        error: () => {
          this.logs.set([]);
          this.totalCount.set(0);
          this.isLoading.set(false);
        },
      });
  }

  isSingleAffectedDate(log: PricingAuditLogDto): boolean {
    if (!log.affectedStartDate || !log.affectedEndDate) return false;
    return log.affectedStartDate.substring(0, 10) === log.affectedEndDate.substring(0, 10);
  }

  getActionBadgeClass(actionType: PricingAuditActionType): string {
    switch (actionType) {
      case PricingAuditActionType.CreatePriceOverride:
        return 'action-create';
      case PricingAuditActionType.UpdatePriceOverride:
        return 'action-update';
      case PricingAuditActionType.DeletePriceOverride:
        return 'action-delete';
      case PricingAuditActionType.UpdateBasePrice:
        return 'action-base';
      case PricingAuditActionType.ToggleRatePlanStatus:
        return 'action-toggle';
      case PricingAuditActionType.UpdateRestrictions:
        return 'action-restriction';
      default:
        return 'action-toggle';
    }
  }

  parseAuditSummary(raw?: string | null): AuditDiffSummary {
    if (!raw) {
      return { primaryText: '-' };
    }

    try {
      const obj = JSON.parse(raw) as Record<string, any>;

      // 1. Toggle Rate Plan Status
      if (typeof obj['isActive'] === 'boolean' && Object.keys(obj).length === 1) {
        return {
          primaryText: this.localization.instant(
            obj['isActive'] ? 'Manager:Rates:StatusActiveShort' : 'Manager:Rates:StatusInactiveShort'
          ),
          statusVariant: obj['isActive'] ? 'active' : 'inactive',
        };
      }

      // 2. Pure Base Price change
      if (typeof obj['basePrice'] === 'number' && Object.keys(obj).length === 1) {
        return {
          primaryText: this.formatVnd(obj['basePrice']),
          statusVariant: 'base',
        };
      }

      // 3. Reverted to Base Price after delete
      if (obj['revertedToBasePrice'] === true) {
        return {
          primaryText: typeof obj['basePrice'] === 'number'
            ? this.formatVnd(obj['basePrice'])
            : this.localization.instant('Manager:Rates:AuditRevertedToBase'),
          secondaryText: this.localization.instant('Manager:Rates:AuditRevertedToBase'),
          statusVariant: 'base',
        };
      }

      // 4. Bulk Before summary (conflictingDatesCount + conflictResolution)
      if (typeof obj['conflictingDatesCount'] === 'number') {
        const count = obj['conflictingDatesCount'];
        const daysUnit = this.localization.instant('Manager:Rates:AuditDaysUnit');
        const conflictLabel = this.localization.instant('Manager:Rates:PreviewConflictCount');
        return {
          primaryText: `${conflictLabel}: ${count} ${daysUnit}`,
          secondaryText: obj['conflictResolution'] ? this.formatConflictMode(String(obj['conflictResolution'])) : undefined,
        };
      }

      // 5. Price Override / Quick-Edit / Bulk After snapshot
      const priceVal =
        typeof obj['calculatedEffectivePrice'] === 'number'
          ? obj['calculatedEffectivePrice']
          : typeof obj['effectivePrice'] === 'number'
            ? obj['effectivePrice']
            : typeof obj['basePrice'] === 'number'
              ? obj['basePrice']
              : null;

      const hasCta = Boolean(obj['isClosedToArrival'] ?? obj['cta']);
      const hasCtd = Boolean(obj['isClosedToDeparture'] ?? obj['ctd']);
      const adjType = obj['adjustmentType'] ? String(obj['adjustmentType']) : null;
      const adjVal = obj['adjustmentValue'];
      const isOverridden = obj['isOverridden'];

      const subParts: string[] = [];
      if (isOverridden === false) {
        subParts.push(this.localization.instant('Manager:Rates:CalendarLegendBase'));
      } else if (adjType === 'Percentage' && adjVal !== null && adjVal !== undefined) {
        const num = Number(adjVal);
        subParts.push(`${num > 0 ? '+' : ''}${num}%`);
      } else if (adjType === 'FixedPrice') {
        subParts.push(this.localization.instant('Manager:Rates:ModeFixedPrice'));
      } else if (adjType === 'RestrictionsOnly') {
        subParts.push(this.localization.instant('Manager:Rates:ModeRestrictionsOnly'));
      }

      if (typeof obj['appliedDaysCount'] === 'number') {
        const daysUnit = this.localization.instant('Manager:Rates:AuditDaysUnit');
        subParts.push(`${obj['appliedDaysCount']} ${daysUnit}`);
      }

      return {
        primaryText: priceVal !== null ? this.formatVnd(priceVal) : '-',
        secondaryText: subParts.length > 0 ? subParts.join(' · ') : undefined,
        statusVariant: isOverridden === false ? 'base' : 'override',
        hasCta,
        hasCtd,
        showNoRestrictions: !hasCta && !hasCtd && adjType === 'RestrictionsOnly',
      };
    } catch {
      return { primaryText: raw };
    }
  }

  private formatVnd(amount: number): string {
    return `${Math.round(amount).toLocaleString('vi-VN')} VNĐ`;
  }

  private formatConflictMode(mode: string): string {
    if (mode === 'ReplaceExisting') return this.localization.instant('Manager:Rates:ConflictReplace');
    if (mode === 'SkipExisting') return this.localization.instant('Manager:Rates:ConflictSkip');
    if (mode === 'Cancel') return this.localization.instant('Manager:Rates:ConflictCancel');
    return mode;
  }

  onClose(): void {
    this.closed.emit();
  }
}
