import { ChangeDetectionStrategy, Component, OnInit, inject, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalizationPipe } from '@abp/ng.core';
import { PropertyContextService } from '../../services/property-context.service';
import {
  ManagerReportService,
  HotelPerformanceReportDto,
  RevenueBreakdownReportDto,
  CancellationLossReportDto,
} from '../../services/manager/manager-report.service';

@Component({
  selector: 'bh-manager-reports',
  standalone: true,
  imports: [CommonModule, FormsModule, LocalizationPipe],
  templateUrl: './manager-reports.component.html',
  styleUrls: ['./manager-reports.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ManagerReportsComponent implements OnInit {
  readonly propertyService = inject(PropertyContextService);
  private reportService = inject(ManagerReportService);

  readonly performance = signal<HotelPerformanceReportDto | null>(null);
  readonly breakdown = signal<RevenueBreakdownReportDto | null>(null);
  readonly loss = signal<CancellationLossReportDto | null>(null);
  readonly isLoading = signal<boolean>(false);

  // Date range
  fromDate = signal<string>(new Date(Date.now() - 86400000 * 30).toISOString().substring(0, 10));
  toDate = signal<string>(new Date().toISOString().substring(0, 10));

  constructor() {
    effect(() => {
      const hotelId = this.propertyService.currentHotelId();
      if (hotelId) {
        this.loadReports(hotelId);
      }
    });
  }

  ngOnInit(): void {}

  setPreset(days: number): void {
    this.fromDate.set(new Date(Date.now() - 86400000 * days).toISOString().substring(0, 10));
    this.toDate.set(new Date().toISOString().substring(0, 10));
    const hotelId = this.propertyService.currentHotelId();
    if (hotelId) {
      this.loadReports(hotelId);
    }
  }

  loadReports(hotelId: string): void {
    this.isLoading.set(true);

    const from = this.fromDate();
    const to = this.toDate();

    this.reportService.getPerformance(hotelId, from, to).subscribe({
      next: p => this.performance.set(p),
      error: () => this.performance.set(null),
    });

    this.reportService.getRevenueBreakdown(hotelId, from, to).subscribe({
      next: b => this.breakdown.set(b),
      error: () => this.breakdown.set(null),
    });

    this.reportService.getCancellationLoss(hotelId, from, to).subscribe({
      next: l => {
        this.loss.set(l);
        this.isLoading.set(false);
      },
      error: () => {
        this.loss.set(null);
        this.isLoading.set(false);
      },
    });
  }

  exportCsv(): void {
    const p = this.performance();
    const b = this.breakdown();
    const l = this.loss();
    if (!p) return;

    const rows = [
      ['BÁO CÁO HIỆU SUẤT VẬN HÀNH & TÀI CHÍNH (USALI)'],
      ['Cơ sở', this.propertyService.currentHotel()?.name || ''],
      ['Thời gian', `${this.fromDate()} đến ${this.toDate()}`],
      [''],
      ['CHỈ SỐ USALI', 'GIÁ TRỊ', 'ĐƠN VỊ'],
      ['Tỷ lệ lấp đầy (Occupancy Rate)', p.occupancyRate.toString() + '%', '%'],
      ['Giá bình quân ngày (ADR)', p.averageDailyRate.toLocaleString(), 'VNĐ'],
      ['Doanh thu phòng sẵn có (RevPAR)', p.revenuePerAvailableRoom.toLocaleString(), 'VNĐ'],
      ['Doanh thu tiền phòng thuần', p.totalRoomRevenue.toLocaleString(), 'VNĐ'],
      ['Tồn kho phòng sẵn có', p.availableInventoryNights.toString(), 'đêm-phòng'],
      ['Số đêm phòng có khách', p.occupiedRoomNights.toString(), 'đêm-phòng'],
      ['Số đêm ngừng kinh doanh (OutOfService)', p.outOfServiceRoomNights.toString(), 'đêm-phòng'],
      [''],
      ['PHÂN RÃ DÒNG TIỀN', 'SỐ TIỀN (VNĐ)', ''],
      ['Doanh thu phòng thuần', (b?.roomRevenue || 0).toLocaleString(), 'VNĐ'],
      ['Doanh thu dịch vụ phụ trợ', (b?.serviceRevenue || 0).toLocaleString(), 'VNĐ'],
      ['Tổng doanh thu trên hóa đơn', (b?.totalBilledRevenue || 0).toLocaleString(), 'VNĐ'],
      ['Doanh thu thực thu', (b?.collectedRevenue || 0).toLocaleString(), 'VNĐ'],
      ['Tiền đã hoàn trả', (b?.refundAmount || 0).toLocaleString(), 'VNĐ'],
      ['Công nợ chưa thu', (b?.uncollectedBalance || 0).toLocaleString(), 'VNĐ'],
      [''],
      ['HỦY PHÒNG & THẤT THOÁT', 'GIÁ TRỊ', ''],
      ['Tỷ lệ hủy phòng', (l?.cancellationRate || 0).toString() + '%', '%'],
      ['Tỷ lệ khách không đến (No-Show)', (l?.noShowRate || 0).toString() + '%', '%'],
      ['Số lượt miễn phạt No-Show', (l?.waivedNoShowPenaltiesCount || 0).toString(), 'lượt'],
      ['Tiền phạt No-Show đã thu', (l?.totalPenaltiesCollected || 0).toLocaleString(), 'VNĐ'],
      ['Thất thoát công suất do OutOfService', (l?.estimatedRevenueLoss || 0).toLocaleString(), 'VNĐ'],
    ];

    const csvContent = '\uFEFF' + rows.map(e => e.join(',')).join('\n');
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.setAttribute('href', url);
    link.setAttribute('download', `USALI_Report_${this.fromDate()}_${this.toDate()}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }
}
