import { ChangeDetectionStrategy, Component, OnInit, inject, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalizationPipe } from '@abp/ng.core';
import { PropertyContextService } from '../../../services/property-context.service';
import {
  ManagerGuestService,
  ManagerGuestDto,
  GuestStayHistoryDto,
} from '../../../services/manager/manager-guest.service';
import { GuestNotesModalComponent } from '../guest-notes-modal/guest-notes-modal.component';

@Component({
  selector: 'bh-guest-list',
  standalone: true,
  imports: [CommonModule, FormsModule, LocalizationPipe, GuestNotesModalComponent],
  templateUrl: './guest-list.component.html',
  styleUrls: ['./guest-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GuestListComponent implements OnInit {
  readonly propertyService = inject(PropertyContextService);
  private guestService = inject(ManagerGuestService);

  readonly guests = signal<ManagerGuestDto[]>([]);
  readonly totalCount = signal<number>(0);
  readonly isLoading = signal<boolean>(false);
  searchTerm = signal<string>('');

  // Modals state
  isNotesModalOpen = signal<boolean>(false);
  selectedGuestForNotes = signal<ManagerGuestDto | null>(null);

  // Stay History Drawer
  selectedGuestForHistory = signal<ManagerGuestDto | null>(null);
  stayHistory = signal<GuestStayHistoryDto[]>([]);
  isHistoryLoading = signal<boolean>(false);

  constructor() {
    effect(() => {
      const hotelId = this.propertyService.currentHotelId();
      if (hotelId) {
        this.loadGuests(hotelId);
      }
    });
  }

  ngOnInit(): void {}

  loadGuests(hotelId: string): void {
    this.isLoading.set(true);

    this.guestService.getList(hotelId, this.searchTerm() || undefined).subscribe({
      next: res => {
        this.guests.set(res.items || []);
        this.totalCount.set(res.totalCount || 0);
        this.isLoading.set(false);
      },
      error: () => {
        this.guests.set([]);
        this.totalCount.set(0);
        this.isLoading.set(false);
      },
    });
  }

  onSearchChange(): void {
    const hotelId = this.propertyService.currentHotelId();
    if (hotelId) {
      this.loadGuests(hotelId);
    }
  }

  openNotesModal(g: ManagerGuestDto): void {
    this.selectedGuestForNotes.set(g);
    this.isNotesModalOpen.set(true);
  }

  openHistoryDrawer(g: ManagerGuestDto): void {
    const hotelId = this.propertyService.currentHotelId();
    if (!hotelId) return;

    this.selectedGuestForHistory.set(g);
    this.isHistoryLoading.set(true);

    this.guestService.getStayHistory(g.guestId, hotelId).subscribe({
      next: history => {
        this.stayHistory.set(history);
        this.isHistoryLoading.set(false);
      },
      error: () => {
        this.stayHistory.set([]);
        this.isHistoryLoading.set(false);
      },
    });
  }

  closeHistoryDrawer(): void {
    this.selectedGuestForHistory.set(null);
    this.stayHistory.set([]);
  }
}
