import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, OnInit, OnChanges, SimpleChanges, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { LocalizationPipe } from '@abp/ng.core';
import { ManagerRoomService, RoomTypeDto, CreateUpdateRoomTypeDto } from '../../../services/manager/manager-room.service';

export interface RoomAmenityItem {
  id: string;
  key: string;
  legacyVi: string;
}

export const AVAILABLE_ROOM_AMENITIES: RoomAmenityItem[] = [
  { id: 'HighSpeedWifi', key: 'Manager:RoomAmenity:HighSpeedWifi', legacyVi: 'WiFi tốc độ cao' },
  { id: 'Bathtub', key: 'Manager:RoomAmenity:Bathtub', legacyVi: 'Bồn tắm nằm' },
  { id: 'SmartTv55', key: 'Manager:RoomAmenity:SmartTv55', legacyVi: 'Smart TV 55 inch' },
  { id: 'CoffeeMaker', key: 'Manager:RoomAmenity:CoffeeMaker', legacyVi: 'Máy pha cà phê' },
  { id: 'LakeBalcony', key: 'Manager:RoomAmenity:LakeBalcony', legacyVi: 'Ban công hướng hồ' },
  { id: 'MiniFridge', key: 'Manager:RoomAmenity:MiniFridge', legacyVi: 'Tủ lạnh mini' },
  { id: 'ElectronicSafe', key: 'Manager:RoomAmenity:ElectronicSafe', legacyVi: 'Két an toàn điện tử' },
  { id: 'WorkDesk', key: 'Manager:RoomAmenity:WorkDesk', legacyVi: 'Bàn làm việc' },
  { id: 'Jacuzzi', key: 'Manager:RoomAmenity:Jacuzzi', legacyVi: 'Bồn tắm sục Jacuzzi' },
  { id: 'PrivateLivingRoom', key: 'Manager:RoomAmenity:PrivateLivingRoom', legacyVi: 'Phòng khách riêng' },
];

@Component({
  selector: 'bh-room-type-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, LocalizationPipe],
  template: `
    <div class="modal-backdrop" *ngIf="isOpen">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">
              <i class="fa-solid fa-shapes me-2 text-primary"></i>
              {{ (roomType ? 'Manager:Room:EditRoomType' : 'Manager:Room:CreateRoomType') | abpLocalization }}
            </h5>
            <button type="button" class="btn-close" (click)="close()"></button>
          </div>

          <form [formGroup]="form" (ngSubmit)="submit()">
            <div class="modal-body">
              <div *ngIf="errorMessage()" class="alert alert-danger mb-3">
                {{ errorMessage() }}
              </div>

              <!-- Tên loại phòng -->
              <div class="mb-3">
                <label class="form-label">
                  {{ 'Manager:Room:TypeName' | abpLocalization }}
                  <span class="text-danger">*</span>
                </label>
                <input
                  type="text"
                  class="form-control"
                  formControlName="name"
                  [placeholder]="'Manager:Room:TypeNamePlaceholder' | abpLocalization" />
              </div>

              <div class="row">
                <!-- Giá niêm yết chuẩn -->
                <div class="col-md-6 mb-3">
                  <label class="form-label">
                    {{ 'Manager:Room:BasePrice' | abpLocalization }} (VNĐ)
                    <span class="text-danger">*</span>
                  </label>
                  <input type="number" class="form-control" formControlName="basePrice" min="0" step="10000" />
                </div>

                <!-- Diện tích m2 -->
                <div class="col-md-6 mb-3">
                  <label class="form-label">
                    {{ 'Manager:Room:SizeSquareMeters' | abpLocalization }} (m²)
                    <span class="text-danger">*</span>
                  </label>
                  <input type="number" class="form-control" formControlName="sizeSquareMeters" min="10" />
                </div>
              </div>

              <div class="row">
                <!-- Sức chứa tiêu chuẩn -->
                <div class="col-md-6 mb-3">
                  <label class="form-label">
                    {{ 'Manager:Room:StandardOccupancy' | abpLocalization }}
                    <span class="text-danger">*</span>
                  </label>
                  <input type="number" class="form-control" formControlName="standardOccupancy" min="1" max="20" />
                </div>

                <!-- Sức chứa tối đa -->
                <div class="col-md-6 mb-3">
                  <label class="form-label">
                    {{ 'Manager:Room:MaxOccupancy' | abpLocalization }}
                    <span class="text-danger">*</span>
                  </label>
                  <input type="number" class="form-control" formControlName="maxOccupancy" min="1" max="30" />
                </div>
              </div>

              <!-- Tiện nghi phòng -->
              <div class="mb-3">
                <label class="form-label">{{ 'Manager:Room:Amenities' | abpLocalization }}</label>
                <div class="amenities-selection">
                  <span
                    *ngFor="let am of availableAmenities"
                    class="amenity-tag"
                    [class.selected]="isAmenitySelected(am)"
                    (click)="toggleAmenity(am)">
                    <i class="fa-solid fa-check me-1" *ngIf="isAmenitySelected(am)"></i>
                    {{ am.key | abpLocalization }}
                  </span>
                </div>
              </div>

              <!-- Mô tả -->
              <div class="mb-3">
                <label class="form-label">{{ 'Manager:Room:Description' | abpLocalization }}</label>
                <textarea class="form-control" rows="3" formControlName="description"></textarea>
              </div>
            </div>

            <div class="modal-footer">
              <button type="button" class="btn btn-outline-secondary" (click)="close()" [disabled]="isSubmitting()">
                {{ 'Common:Cancel' | abpLocalization }}
              </button>
              <button type="submit" class="btn btn-primary" [disabled]="form.invalid || isSubmitting()">
                <i *ngIf="isSubmitting()" class="fa-solid fa-spinner fa-spin me-1"></i>
                {{ 'Common:Save' | abpLocalization }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .modal-backdrop {
      position: fixed;
      top: 0; left: 0; right: 0; bottom: 0;
      background: rgba(15, 23, 42, 0.6);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1050;
    }
    .modal-dialog {
      width: 100%;
      max-width: 600px;
      margin: 20px;
    }
    .modal-content {
      background: #ffffff;
      border-radius: 12px;
      box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1);
      overflow: hidden;
      border: 1px solid #e2e8f0;
    }
    .modal-header {
      padding: 16px 20px;
      border-bottom: 1px solid #f1f5f9;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .modal-body {
      padding: 20px;
    }
    .modal-footer {
      padding: 14px 20px;
      border-top: 1px solid #f1f5f9;
      display: flex;
      justify-content: flex-end;
      gap: 10px;
      background: #f8fafc;
    }
    .amenities-selection {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
    }
    .amenity-tag {
      font-size: 0.8125rem;
      padding: 4px 10px;
      border-radius: 20px;
      border: 1px solid #cbd5e1;
      background: #f8fafc;
      color: #334155;
      cursor: pointer;
      user-select: none;
      transition: all 0.15s ease;
      &.selected {
        background: #e0f2fe;
        color: #0369a1;
        border-color: #38bdf8;
        font-weight: 600;
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoomTypeModalComponent implements OnChanges {
  private fb = inject(FormBuilder);
  private roomService = inject(ManagerRoomService);

  @Input() isOpen = false;
  @Input() roomType: RoomTypeDto | null = null;
  @Input() hotelId = '';

  @Output() closed = new EventEmitter<void>();
  @Output() saved = new EventEmitter<RoomTypeDto>();

  readonly isSubmitting = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  readonly availableAmenities = AVAILABLE_ROOM_AMENITIES;

  selectedAmenities = new Set<string>();

  form = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(128)]],
    basePrice: [1500000, [Validators.required, Validators.min(0)]],
    sizeSquareMeters: [32, [Validators.required, Validators.min(5)]],
    standardOccupancy: [2, [Validators.required, Validators.min(1)]],
    maxOccupancy: [3, [Validators.required, Validators.min(1)]],
    description: [''],
  });

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['roomType']) {
      this.selectedAmenities.clear();
      if (this.roomType) {
        this.form.patchValue({
          name: this.roomType.name,
          basePrice: this.roomType.basePrice,
          sizeSquareMeters: this.roomType.sizeSquareMeters,
          standardOccupancy: this.roomType.standardOccupancy,
          maxOccupancy: this.roomType.maxOccupancy,
          description: this.roomType.description,
        });
        (this.roomType.amenities || []).forEach(a => {
          const matched = this.availableAmenities.find(item => item.id === a || item.legacyVi === a);
          if (matched) {
            this.selectedAmenities.add(matched.id);
          } else {
            this.selectedAmenities.add(a);
          }
        });
      } else {
        this.form.reset({
          name: '',
          basePrice: 1500000,
          sizeSquareMeters: 32,
          standardOccupancy: 2,
          maxOccupancy: 3,
          description: '',
        });
      }
    }
  }

  isAmenitySelected(am: RoomAmenityItem): boolean {
    return this.selectedAmenities.has(am.id) || this.selectedAmenities.has(am.legacyVi);
  }

  toggleAmenity(am: RoomAmenityItem): void {
    if (this.isAmenitySelected(am)) {
      this.selectedAmenities.delete(am.id);
      this.selectedAmenities.delete(am.legacyVi);
    } else {
      this.selectedAmenities.add(am.id);
    }
  }

  submit(): void {
    if (this.form.invalid || !this.hotelId) return;

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const input: CreateUpdateRoomTypeDto = {
      hotelId: this.hotelId,
      name: this.form.value.name!,
      basePrice: Number(this.form.value.basePrice),
      sizeSquareMeters: Number(this.form.value.sizeSquareMeters),
      standardOccupancy: Number(this.form.value.standardOccupancy),
      maxOccupancy: Number(this.form.value.maxOccupancy),
      description: this.form.value.description || '',
      amenities: Array.from(this.selectedAmenities),
    };

    const action$ = this.roomType
      ? this.roomService.updateRoomType(this.roomType.id, input)
      : this.roomService.createRoomType(input);

    action$.subscribe({
      next: res => {
        this.isSubmitting.set(false);
        this.saved.emit(res);
        this.close();
      },
      error: err => {
        this.isSubmitting.set(false);
        this.errorMessage.set(err.error?.error?.message || err.message || 'Error saving room type');
      },
    });
  }

  close(): void {
    this.errorMessage.set(null);
    this.closed.emit();
  }
}
