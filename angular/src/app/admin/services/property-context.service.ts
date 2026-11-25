import { Injectable, computed, signal } from '@angular/core';

export interface HotelItemDto {
  id: string;
  name: string;
  code: string;
  address?: string;
  isPrimary?: boolean;
}

const STORAGE_KEY = 'bh_active_hotel_id';

@Injectable({
  providedIn: 'root',
})
export class PropertyContextService {
  readonly currentHotelId = signal<string | null>(null);
  readonly assignedHotels = signal<HotelItemDto[]>([]);
  readonly isLoading = signal<boolean>(false);

  readonly currentHotel = computed(() => {
    const id = this.currentHotelId();
    if (!id) return this.assignedHotels()[0] || null;
    return (
      this.assignedHotels().find(h => h.id.toLowerCase() === id.toLowerCase()) ||
      this.assignedHotels()[0] ||
      null
    );
  });

  constructor() {
    this.initializeContext();
  }

  private initializeContext(): void {
    // 1. Ưu tiên đọc trực tiếp từ URL query param (?hotelId=...) ngay khi khởi tạo service
    let initialId: string | null = null;
    if (typeof window !== 'undefined' && window.location?.search) {
      const params = new URLSearchParams(window.location.search);
      initialId = params.get('hotelId');
    }

    // 2. Nếu không có trên URL, đọc từ sessionStorage của tab hiện tại
    if (!initialId && typeof sessionStorage !== 'undefined') {
      initialId = sessionStorage.getItem(STORAGE_KEY);
    }

    if (initialId) {
      this.currentHotelId.set(initialId);
      if (typeof sessionStorage !== 'undefined') {
        sessionStorage.setItem(STORAGE_KEY, initialId);
      }
    }
  }

  setAssignedHotels(hotels: HotelItemDto[]): void {
    if (!hotels || hotels.length === 0) {
      this.assignedHotels.set([]);
      this.currentHotelId.set(null);
      if (typeof sessionStorage !== 'undefined') {
        sessionStorage.removeItem(STORAGE_KEY);
      }
      return;
    }

    this.assignedHotels.set(hotels);

    const currentId = this.currentHotelId();
    // Nếu currentId hiện tại đã có và thuộc danh sách cơ sở được phân công, giữ nguyên
    if (currentId && hotels.some(h => h.id.toLowerCase() === currentId.toLowerCase())) {
      return;
    }

    // Nếu chưa có hoặc currentId không thuộc danh sách được phân công, gán cơ sở chính hoặc cơ sở đầu tiên
    const primary = hotels.find(h => h.isPrimary) || hotels[0];
    this.setActiveHotel(primary.id);
  }

  setActiveHotel(hotelId: string): void {
    this.currentHotelId.set(hotelId);
    if (typeof sessionStorage !== 'undefined') {
      sessionStorage.setItem(STORAGE_KEY, hotelId);
    }
  }
}
