import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { LocalizationPipe, LocalizationService } from '@abp/ng.core';
import { BhToastrService } from '../../../shared/components/toastr/bh-toastr.service';
import { TenantOrganizationProfileDto, TenantOrgService, UpdateTenantOrganizationDto } from '../../services/tenant-org.service';

@Component({
  selector: 'bh-organization-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, LocalizationPipe],
  templateUrl: './organization-settings.component.html',
  styleUrls: ['./organization-settings.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrganizationSettingsComponent implements OnInit {
  private fb = inject(FormBuilder);
  private cdr = inject(ChangeDetectorRef);
  private toastr = inject(BhToastrService);
  private localization = inject(LocalizationService);
  private orgService = inject(TenantOrgService);

  isLoading = true;
  isSaving = false;
  activeTab: 'profile' | 'policy' = 'profile';

  form: FormGroup = this.fb.group({
    organizationName: ['', [Validators.required, Validators.maxLength(256)]],
    logoUrl: [''],
    headquarterAddress: [''],
    hotline: [''],
    contactEmail: ['', [Validators.email]],
    websiteUrl: [''],
    defaultCheckInTime: ['14:00', [Validators.required]],
    defaultCheckOutTime: ['12:00', [Validators.required]],
    // Cấu hình chính sách hủy phòng thân thiện UI
    freeCancellationHours: [48, [Validators.required, Validators.min(0)]],
    partialPenaltyHours: [24, [Validators.required, Validators.min(0)]],
    partialPenaltyPercent: [50, [Validators.required, Validators.min(0), Validators.max(100)]],
    fullPenaltyPercent: [100, [Validators.required, Validators.min(0), Validators.max(100)]],
  });

  get previewFreeCondition(): string {
    const hours = this.form.get('freeCancellationHours')?.value ?? 48;
    return this.localization.instant('TenantOrg:PreviewFreeCondition', hours);
  }

  get previewPartialPenalty(): string {
    const pct = this.form.get('partialPenaltyPercent')?.value ?? 50;
    return this.localization.instant('TenantOrg:PreviewPartialPenalty', pct);
  }

  get previewPartialCondition(): string {
    const partialH = this.form.get('partialPenaltyHours')?.value ?? 24;
    const freeH = this.form.get('freeCancellationHours')?.value ?? 48;
    return this.localization.instant('TenantOrg:PreviewPartialCondition', partialH, freeH);
  }

  get previewFullPenalty(): string {
    const pct = this.form.get('fullPenaltyPercent')?.value ?? 100;
    return this.localization.instant('TenantOrg:PreviewFullPenalty', pct);
  }

  get previewFullCondition(): string {
    const partialH = this.form.get('partialPenaltyHours')?.value ?? 24;
    return this.localization.instant('TenantOrg:PreviewFullCondition', partialH);
  }

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.isLoading = true;
    this.cdr.markForCheck();

    this.orgService.getProfile().subscribe({
      next: (profile) => {
        let policy = {
          freeCancellationHours: 48,
          partialPenaltyHours: 24,
          partialPenaltyPercent: 50,
          fullPenaltyHours: 0,
          fullPenaltyPercent: 100,
        };

        if (profile.cancellationPolicyJson) {
          try {
            const parsed = JSON.parse(profile.cancellationPolicyJson);
            policy = { ...policy, ...parsed };
          } catch {
            // Sử dụng mặc định nếu json không hợp lệ
          }
        }

        this.form.patchValue({
          organizationName: profile.organizationName,
          logoUrl: profile.logoUrl || '',
          headquarterAddress: profile.headquarterAddress || '',
          hotline: profile.hotline || '',
          contactEmail: profile.contactEmail || '',
          websiteUrl: profile.websiteUrl || '',
          defaultCheckInTime: profile.defaultCheckInTime ? profile.defaultCheckInTime.slice(0, 5) : '14:00',
          defaultCheckOutTime: profile.defaultCheckOutTime ? profile.defaultCheckOutTime.slice(0, 5) : '12:00',
          freeCancellationHours: policy.freeCancellationHours ?? 48,
          partialPenaltyHours: policy.partialPenaltyHours ?? 24,
          partialPenaltyPercent: policy.partialPenaltyPercent ?? 50,
          fullPenaltyPercent: policy.fullPenaltyPercent ?? 100,
        });
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.markForCheck();
      },
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    const val = this.form.value;

    const policyObj = {
      freeCancellationHours: Number(val.freeCancellationHours) || 48,
      partialPenaltyHours: Number(val.partialPenaltyHours) || 24,
      partialPenaltyPercent: Number(val.partialPenaltyPercent) || 50,
      fullPenaltyHours: 0,
      fullPenaltyPercent: Number(val.fullPenaltyPercent) || 100,
    };

    const payload: UpdateTenantOrganizationDto = {
      organizationName: val.organizationName.trim(),
      logoUrl: val.logoUrl?.trim() || undefined,
      headquarterAddress: val.headquarterAddress?.trim() || undefined,
      hotline: val.hotline?.trim() || undefined,
      contactEmail: val.contactEmail?.trim() || undefined,
      websiteUrl: val.websiteUrl?.trim() || undefined,
      defaultCheckInTime: `${val.defaultCheckInTime}:00`,
      defaultCheckOutTime: `${val.defaultCheckOutTime}:00`,
      cancellationPolicyJson: JSON.stringify(policyObj),
    };

    this.orgService.updateProfile(payload).subscribe({
      next: () => {
        this.isSaving = false;
        this.toastr.success(this.localization.instant('TenantOrg:SaveSuccess'));
        this.cdr.markForCheck();
      },
      error: () => {
        this.isSaving = false;
        this.cdr.markForCheck();
      },
    });
  }
}
