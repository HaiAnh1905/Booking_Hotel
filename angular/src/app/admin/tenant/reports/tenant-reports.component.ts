import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalizationPipe } from '@abp/ng.core';
import { GetTenantReportInput, HotelPerformanceReportDto, RevenueReportDto, TenantReportService } from '../../services/tenant-report.service';
import { HotelDto, HotelService } from '../../services/hotel.service';

@Component({
  selector: 'bh-tenant-reports',
  standalone: true,
  imports: [CommonModule, FormsModule, LocalizationPipe],
  templateUrl: './tenant-reports.component.html',
  styleUrls: ['./tenant-reports.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TenantReportsComponent implements OnInit {
  private cdr = inject(ChangeDetectorRef);
  private reportService = inject(TenantReportService);
  private hotelService = inject(HotelService);

  activeTab: 'revenue' | 'occupancy' | 'performance' = 'revenue';
  isLoading = false;

  startDate: string = '';
  endDate: string = '';
  selectedHotelId: string = '';
  hotels: HotelDto[] = [];

  revenueReport: RevenueReportDto | null = null;
  performanceReport: HotelPerformanceReportDto | null = null;

  ngOnInit(): void {
    const now = new Date();
    const firstDay = new Date(now.getFullYear(), now.getMonth(), 1);
    this.startDate = firstDay.toISOString().split('T')[0];
    this.endDate = now.toISOString().split('T')[0];

    this.loadHotels();
    this.loadReport();
  }

  loadHotels(): void {
    this.hotelService.getList({ maxResultCount: 1000 }).subscribe({
      next: (res) => {
        this.hotels = res.items || [];
        this.cdr.markForCheck();
      },
    });
  }

  loadReport(): void {
    this.isLoading = true;
    this.cdr.markForCheck();

    const input: GetTenantReportInput = {
      startDate: this.startDate ? new Date(this.startDate).toISOString() : undefined,
      endDate: this.endDate ? new Date(this.endDate).toISOString() : undefined,
      hotelId: this.selectedHotelId || undefined,
    };

    if (this.activeTab === 'revenue' || this.activeTab === 'occupancy') {
      this.reportService.getRevenueReport(input).subscribe({
        next: (data) => {
          this.revenueReport = data;
          this.isLoading = false;
          this.cdr.markForCheck();
        },
        error: () => {
          this.isLoading = false;
          this.cdr.markForCheck();
        },
      });
    } else {
      this.reportService.getHotelPerformanceReport(input).subscribe({
        next: (data) => {
          this.performanceReport = data;
          this.isLoading = false;
          this.cdr.markForCheck();
        },
        error: () => {
          this.isLoading = false;
          this.cdr.markForCheck();
        },
      });
    }
  }

  switchTab(tab: 'revenue' | 'occupancy' | 'performance'): void {
    this.activeTab = tab;
    this.loadReport();
  }

  exportData(): void {
    const input: GetTenantReportInput = {
      startDate: this.startDate ? new Date(this.startDate).toISOString() : undefined,
      endDate: this.endDate ? new Date(this.endDate).toISOString() : undefined,
      hotelId: this.selectedHotelId || undefined,
    };

    this.reportService.exportReport(this.activeTab, 'csv', input).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `Report_${this.activeTab}_${new Date().toISOString().split('T')[0]}.csv`;
        a.click();
        window.URL.revokeObjectURL(url);
      },
    });
  }
}
