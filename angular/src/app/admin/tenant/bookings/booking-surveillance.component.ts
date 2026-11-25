import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalizationPipe } from '@abp/ng.core';
import { GetBookingSurveillanceInput, TenantBookingSurveillanceDto, TenantDashboardService } from '../../services/tenant-dashboard.service';
import { HotelDto, HotelService } from '../../services/hotel.service';
import { BookingDetailModalComponent } from './booking-detail-modal.component';

@Component({
  selector: 'bh-booking-surveillance',
  standalone: true,
  imports: [CommonModule, FormsModule, LocalizationPipe, BookingDetailModalComponent],
  templateUrl: './booking-surveillance.component.html',
  styleUrls: ['./booking-surveillance.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BookingSurveillanceComponent implements OnInit {
  private cdr = inject(ChangeDetectorRef);
  private dashboardService = inject(TenantDashboardService);
  private hotelService = inject(HotelService);

  bookings: TenantBookingSurveillanceDto[] = [];
  hotels: HotelDto[] = [];
  isLoading = false;

  searchQuery = '';
  selectedHotelId = '';
  selectedStatus = '';
  selectedPaymentStatus = '';

  isDetailModalOpen = false;
  selectedBookingForDetail: TenantBookingSurveillanceDto | null = null;

  ngOnInit(): void {
    this.loadHotels();
    this.loadBookings();
  }

  loadHotels(): void {
    this.hotelService.getList({ maxResultCount: 1000 }).subscribe({
      next: (res) => {
        this.hotels = res.items || [];
        this.cdr.markForCheck();
      },
    });
  }

  loadBookings(): void {
    this.isLoading = true;
    this.cdr.markForCheck();

    const input: GetBookingSurveillanceInput = {
      hotelId: this.selectedHotelId || undefined,
      status: this.selectedStatus || undefined,
      paymentStatus: this.selectedPaymentStatus || undefined,
      filter: this.searchQuery || undefined,
      maxResultCount: 100,
    };

    this.dashboardService.getSurveillanceBookings(input).subscribe({
      next: (res) => {
        this.bookings = res.items || [];
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.markForCheck();
      },
    });
  }

  onFilterChange(): void {
    this.loadBookings();
  }

  openDetail(booking: TenantBookingSurveillanceDto): void {
    this.selectedBookingForDetail = booking;
    this.isDetailModalOpen = true;
  }
}
