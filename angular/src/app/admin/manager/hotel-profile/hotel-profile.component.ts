import { ChangeDetectionStrategy, Component, OnInit, inject, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { LocalizationPipe } from '@abp/ng.core';
import { PropertyContextService } from '../../services/property-context.service';
import {
  ManagerHotelProfileService,
  ManagerHotelProfileDto,
  UpdateManagerHotelProfileDto,
} from '../../services/manager/manager-hotel-profile.service';

export interface AmenityItem {
  id: string;
  key: string;
  legacyVi: string;
}

export const STANDARD_HOTEL_AMENITIES: AmenityItem[] = [
  { id: 'InfinityPool', key: 'Manager:Amenity:InfinityPool', legacyVi: 'Hồ bơi vô cực' },
  { id: 'SpaMassage', key: 'Manager:Amenity:SpaMassage', legacyVi: 'Trung tâm Spa & Massage' },
  { id: 'FitnessGym', key: 'Manager:Amenity:FitnessGym', legacyVi: 'Phòng tập thể hình Gym' },
  { id: 'Restaurant', key: 'Manager:Amenity:Restaurant', legacyVi: 'Nhà hàng ẩm thực Á-Âu' },
  { id: 'AirportShuttle', key: 'Manager:Amenity:AirportShuttle', legacyVi: 'Dịch vụ đưa đón sân bay' },
  { id: 'RooftopBar', key: 'Manager:Amenity:RooftopBar', legacyVi: 'Quầy bar & Lounge tầng thượng' },
  { id: 'FreeParking', key: 'Manager:Amenity:FreeParking', legacyVi: 'Bãi đậu xe miễn phí' },
  { id: 'ConferenceRoom', key: 'Manager:Amenity:ConferenceRoom', legacyVi: 'Phòng hội nghị & Sự kiện' },
  { id: 'FrontDesk247', key: 'Manager:Amenity:FrontDesk247', legacyVi: 'Lễ tân phục vụ 24/7' },
  { id: 'ExpressLaundry', key: 'Manager:Amenity:ExpressLaundry', legacyVi: 'Dịch vụ giặt là lấy nhanh' },
];

@Component({
  selector: 'bh-hotel-profile',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, LocalizationPipe],
  templateUrl: './hotel-profile.component.html',
  styleUrls: ['./hotel-profile.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HotelProfileComponent implements OnInit {
  readonly propertyService = inject(PropertyContextService);
  private profileService = inject(ManagerHotelProfileService);
  private fb = inject(FormBuilder);

  readonly profile = signal<ManagerHotelProfileDto | null>(null);
  readonly isLoading = signal<boolean>(false);
  readonly isSaving = signal<boolean>(false);
  readonly successMessage = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);

  readonly standardAmenities = STANDARD_HOTEL_AMENITIES;

  selectedAmenities = new Set<string>();

  form = this.fb.group({
    name: [{ value: '', disabled: true }],
    code: [{ value: '', disabled: true }],
    starRating: [{ value: 5, disabled: true }],
    roomCount: [{ value: 0, disabled: true }],
    managerName: [{ value: '', disabled: true }],
    address: ['', [Validators.required, Validators.maxLength(512)]],
    city: ['', [Validators.required, Validators.maxLength(128)]],
    phone: ['', [Validators.required, Validators.maxLength(32)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    checkInTime: ['14:00', [Validators.required]],
    checkOutTime: ['12:00', [Validators.required]],
    description: [''],
    houseRules: [''],
  });

  constructor() {
    effect(() => {
      const hotelId = this.propertyService.currentHotelId();
      if (hotelId) {
        this.loadProfile(hotelId);
      }
    });
  }

  ngOnInit(): void {}

  loadProfile(hotelId: string): void {
    this.isLoading.set(true);
    this.successMessage.set(null);
    this.errorMessage.set(null);

    this.profileService.get(hotelId).subscribe({
      next: p => {
        this.profile.set(p);
        this.selectedAmenities.clear();

        if (p.amenitiesJson) {
          try {
            const list: string[] = JSON.parse(p.amenitiesJson);
            list.forEach(a => {
              const matched = this.standardAmenities.find(item => item.id === a || item.legacyVi === a);
              if (matched) {
                this.selectedAmenities.add(matched.id);
              } else {
                this.selectedAmenities.add(a);
              }
            });
          } catch { }
        }

        const checkIn = p.standardCheckInTime ? p.standardCheckInTime.substring(0, 5) : '14:00';
        const checkOut = p.standardCheckOutTime ? p.standardCheckOutTime.substring(0, 5) : '12:00';

        this.form.patchValue({
          name: p.name,
          code: p.code,
          starRating: p.starRating,
          roomCount: p.roomCount,
          managerName: p.managerName,
          address: p.address,
          city: p.city,
          phone: p.phone,
          email: p.email,
          checkInTime: checkIn,
          checkOutTime: checkOut,
          description: p.description || '',
          houseRules: p.houseRules || '',
        });

        this.isLoading.set(false);
      },
      error: err => {
        this.errorMessage.set(err.error?.error?.message || 'Error loading hotel profile');
        this.isLoading.set(false);
      },
    });
  }

  isAmenitySelected(am: AmenityItem): boolean {
    return this.selectedAmenities.has(am.id) || this.selectedAmenities.has(am.legacyVi);
  }

  toggleAmenity(am: AmenityItem): void {
    if (this.isAmenitySelected(am)) {
      this.selectedAmenities.delete(am.id);
      this.selectedAmenities.delete(am.legacyVi);
    } else {
      this.selectedAmenities.add(am.id);
    }
  }

  saveProfile(): void {
    const hotelId = this.propertyService.currentHotelId();
    if (this.form.invalid || !hotelId) return;

    this.isSaving.set(true);
    this.successMessage.set(null);
    this.errorMessage.set(null);

    const val = this.form.getRawValue();
    const rawCheckIn = val.checkInTime || '14:00';
    const rawCheckOut = val.checkOutTime || '12:00';
    const checkIn = rawCheckIn.length === 5 ? `${rawCheckIn}:00` : rawCheckIn;
    const checkOut = rawCheckOut.length === 5 ? `${rawCheckOut}:00` : rawCheckOut;

    const input: UpdateManagerHotelProfileDto = {
      hotelId,
      address: val.address || '',
      city: val.city || '',
      phone: val.phone || '',
      email: val.email || '',
      standardCheckInTime: checkIn,
      standardCheckOutTime: checkOut,
      description: val.description || undefined,
      amenitiesJson: JSON.stringify(Array.from(this.selectedAmenities)),
      houseRules: val.houseRules || undefined,
    };

    this.profileService.update(input).subscribe({
      next: updated => {
        this.profile.set(updated);
        this.isSaving.set(false);
        this.successMessage.set('Hồ sơ khách sạn đã được cập nhật thành công!');
        setTimeout(() => this.successMessage.set(null), 4000);
      },
      error: err => {
        this.isSaving.set(false);
        this.errorMessage.set(err.error?.error?.message || 'Không thể lưu hồ sơ khách sạn');
      },
    });
  }
}
