import { ChangeDetectionStrategy, Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalizationPipe } from '@abp/ng.core';
import {
  TenantRatePlanService,
  RatePlanDto,
  CreateUpdateRatePlanDto,
} from '../../services/tenant/tenant-rate-plan.service';
import { BhConfirmService } from '../../../shared/components/confirm-modal/bh-confirm.service';

@Component({
  selector: 'bh-tenant-rate-plans',
  standalone: true,
  imports: [CommonModule, FormsModule, LocalizationPipe],
  templateUrl: './tenant-rate-plans.component.html',
  styleUrls: ['./tenant-rate-plans.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TenantRatePlansComponent implements OnInit {
  private service = inject(TenantRatePlanService);
  private confirmService = inject(BhConfirmService);

  readonly ratePlans = signal<RatePlanDto[]>([]);
  readonly isLoading = signal<boolean>(false);
  readonly isSaving = signal<boolean>(false);
  readonly successMessage = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);

  // Search & Filter
  readonly searchTerm = signal<string>('');

  readonly filteredPlans = computed(() => {
    const term = this.searchTerm().toLowerCase().trim();
    const plans = this.ratePlans();
    if (!term) return plans;
    return plans.filter(
      p =>
        p.name.toLowerCase().includes(term) ||
        p.code.toLowerCase().includes(term) ||
        (p.description && p.description.toLowerCase().includes(term))
    );
  });

  // Modal State
  readonly isModalOpen = signal<boolean>(false);
  readonly editingId = signal<string | null>(null);

  // Form Fields
  formName = '';
  formCode = '';
  formDescription = '';
  formIsRefundable = true;
  formIncludesBreakfast = false;
  formCancellationCutoffHours = 24;
  formDefaultPriceAdjustmentPercent = 0;

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.service.getList().subscribe({
      next: data => {
        this.ratePlans.set(data);
        this.isLoading.set(false);
      },
      error: err => {
        this.errorMessage.set(err.error?.error?.message || 'Không thể tải danh sách gói giá chuỗi');
        this.isLoading.set(false);
      },
    });
  }

  openCreateModal(): void {
    this.editingId.set(null);
    this.formName = '';
    this.formCode = '';
    this.formDescription = '';
    this.formIsRefundable = true;
    this.formIncludesBreakfast = false;
    this.formCancellationCutoffHours = 24;
    this.formDefaultPriceAdjustmentPercent = 0;
    this.errorMessage.set(null);
    this.isModalOpen.set(true);
  }

  openEditModal(plan: RatePlanDto): void {
    this.editingId.set(plan.id);
    this.formName = plan.name;
    this.formCode = plan.code;
    this.formDescription = plan.description || '';
    this.formIsRefundable = plan.isRefundable;
    this.formIncludesBreakfast = plan.includesBreakfast;
    this.formCancellationCutoffHours = plan.cancellationCutoffHours || 0;
    this.formDefaultPriceAdjustmentPercent = plan.defaultPriceAdjustmentPercent || 0;
    this.errorMessage.set(null);
    this.isModalOpen.set(true);
  }

  closeModal(): void {
    this.isModalOpen.set(false);
    this.editingId.set(null);
    this.errorMessage.set(null);
  }

  onCodeInput(): void {
    this.formCode = this.formCode.toUpperCase().replace(/[^A-Z0-9_]/g, '');
  }

  save(): void {
    if (!this.formName.trim() || !this.formCode.trim()) {
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);

    const input: CreateUpdateRatePlanDto = {
      name: this.formName.trim(),
      code: this.formCode.trim().toUpperCase(),
      description: this.formDescription.trim(),
      isRefundable: this.formIsRefundable,
      includesBreakfast: this.formIncludesBreakfast,
      cancellationCutoffHours: this.formIsRefundable ? Number(this.formCancellationCutoffHours) : 0,
      defaultPriceAdjustmentPercent: Number(this.formDefaultPriceAdjustmentPercent),
    };

    const id = this.editingId();
    if (id) {
      this.service.update(id, input).subscribe({
        next: updated => {
          this.ratePlans.update(list => list.map(p => (p.id === updated.id ? updated : p)));
          this.isSaving.set(false);
          this.closeModal();
          this.showSuccess('Cập nhật gói giá thành công');
        },
        error: err => {
          this.isSaving.set(false);
          this.errorMessage.set(err.error?.error?.message || 'Không thể cập nhật gói giá');
        },
      });
    } else {
      this.service.create(input).subscribe({
        next: created => {
          this.ratePlans.update(list => [...list, created]);
          this.isSaving.set(false);
          this.closeModal();
          this.showSuccess('Tạo mới gói giá chuỗi thành công');
        },
        error: err => {
          this.isSaving.set(false);
          this.errorMessage.set(err.error?.error?.message || 'Không thể tạo mới gói giá (có thể do trùng mã)');
        },
      });
    }
  }

  async deletePlan(plan: RatePlanDto): Promise<void> {
    const confirmed = await this.confirmService.confirm({
      title: 'Xóa gói giá',
      message: `Xác nhận xóa gói giá "${plan.name}" (${plan.code})? Tất cả cơ sở sẽ dừng mở bán gói này.`,
      confirmText: 'Xóa gói giá',
      cancelText: 'Hủy',
      type: 'danger',
    });
    if (!confirmed) {
      return;
    }

    this.service.delete(plan.id).subscribe({
      next: () => {
        this.ratePlans.update(list => list.filter(p => p.id !== plan.id));
        this.showSuccess(`Đã xóa gói giá "${plan.name}" thành công`);
      },
      error: err => {
        alert(err.error?.error?.message || 'Không thể xóa gói giá');
      },
    });
  }

  private showSuccess(msg: string): void {
    this.successMessage.set(msg);
    setTimeout(() => {
      this.successMessage.set(null);
    }, 4000);
  }
}
