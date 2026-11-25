import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { LocalizationPipe } from '@abp/ng.core';
import { BhModalComponent } from '../../../shared/components/modal/bh-modal.component';
import { BhModalConfig } from '../../../shared/models/ui-models';
import { BhSearchableSelectComponent, SelectOption } from '../../../shared/components/searchable-select/bh-searchable-select.component';
import { AssignHotelManagerDto, HotelDto } from '../../services/hotel.service';

@Component({
  selector: 'bh-hotel-manager-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, LocalizationPipe, BhModalComponent, BhSearchableSelectComponent],
  template: `
    <bh-modal
      [isOpen]="isOpen"
      [config]="modalConfig"
      (confirm)="onSubmit()"
      (cancel)="onCancel()"
    >
      <form [formGroup]="form" class="manager-form" (ngSubmit)="onSubmit()">
        <p class="text-muted mb-3" *ngIf="hotel">
          {{ 'TenantHotels:AssignManagerModalTitle' | abpLocalization }}:
          <strong class="text-primary">{{ hotel.name }} ({{ hotel.code }})</strong>
        </p>

        <div class="mb-3">
          <label class="form-label">{{ 'TenantHotels:FieldManager' | abpLocalization }} <span class="text-danger">*</span></label>
          <bh-searchable-select
            formControlName="managerUserId"
            [options]="managerOptions"
            [placeholder]="'TenantHotels:SelectManager' | abpLocalization"
            [searchPlaceholder]="'TenantHotels:SearchManagerPlaceholder' | abpLocalization"
            [emptyText]="'TenantHotels:NoMatchingResults' | abpLocalization"
            [clearable]="false"
          ></bh-searchable-select>
          <div class="text-danger small mt-1" *ngIf="form.get('managerUserId')?.touched && form.get('managerUserId')?.invalid">
            {{ 'Common:FieldRequired' | abpLocalization }}
          </div>
        </div>
      </form>
    </bh-modal>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HotelManagerModalComponent implements OnChanges {
  private fb = inject(FormBuilder);

  @Input() isOpen = false;
  @Input() hotel: HotelDto | null = null;
  @Input() managerOptions: SelectOption[] = [];

  @Output() assign = new EventEmitter<AssignHotelManagerDto>();
  @Output() cancel = new EventEmitter<void>();

  form: FormGroup = this.fb.group({
    managerUserId: [''],
  });

  get modalConfig(): BhModalConfig {
    return {
      titleKey: 'TenantHotels:AssignManagerModalTitle',
      confirmTextKey: 'TenantHotels:AssignManager',
      cancelTextKey: 'Common:Cancel',
      confirmVariant: 'primary',
      size: 'md',
      iconClass: 'fa-solid fa-user-gear',
    };
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (
      (changes['isOpen'] && this.isOpen) ||
      changes['hotel'] ||
      (changes['managerOptions'] && this.isOpen)
    ) {
      let currentManagerValue = this.hotel?.managerUserId || '';
      if (!currentManagerValue && this.hotel?.managerName) {
        const target = this.hotel.managerName.trim().toLowerCase();
        const found = this.managerOptions.find(
          (o) =>
            o.label.toLowerCase().includes(target) ||
            String(o.value).toLowerCase() === target
        );
        if (found) {
          currentManagerValue = found.value;
        }
      }

      if (!this.form.dirty || changes['isOpen'] || changes['hotel']) {
        this.form.reset({ managerUserId: currentManagerValue });
      }
    }
  }

  onSubmit(): void {
    this.assign.emit({
      managerUserId: this.form.value.managerUserId || undefined,
    });
  }

  onCancel(): void {
    this.cancel.emit();
  }
}
