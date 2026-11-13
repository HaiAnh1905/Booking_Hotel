import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AuthService, LocalizationPipe } from '@abp/ng.core';
import { BhToastrService } from '../shared/components/toastr/bh-toastr.service';
import { SearchRoomCriteria } from '../shared/models/ui-models';

interface SampleRoom {
  id: string;
  name: string;
  nameKey: string;
  image: string;
  price: number;
  capacity: number;
  rating: number;
  badgeKey: string;
}

interface AmenityItem {
  icon: string;
  titleKey: string;
  descKey: string;
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, LocalizationPipe],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomeComponent {
  private authService = inject(AuthService);
  private toastr = inject(BhToastrService);

  get hasLoggedIn(): boolean {
    return this.authService.isAuthenticated;
  }

  // Tiêu chí tìm kiếm phòng
  searchCriteria: SearchRoomCriteria = {
    destinationOrHotelId: '',
    checkInDate: this.formatDate(new Date()),
    checkOutDate: this.formatDate(new Date(Date.now() + 86400000 * 2)),
    adults: 2,
    children: 0,
    rooms: 1,
  };

  // Danh mục phòng mẫu chuẩn concept
  featuredRooms = signal<SampleRoom[]>([
    {
      id: 'room-deluxe-01',
      name: 'Deluxe Ocean View',
      nameKey: 'Deluxe Ocean View',
      image: 'https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?auto=format&fit=crop&w=800&q=80',
      price: 1850000,
      capacity: 2,
      rating: 4.9,
      badgeKey: 'RoomRack:Available',
    },
    {
      id: 'room-suite-02',
      name: 'Royal Executive Suite',
      nameKey: 'Royal Executive Suite',
      image: 'https://images.unsplash.com/photo-1618773928121-c32242e63f39?auto=format&fit=crop&w=800&q=80',
      price: 3600000,
      capacity: 4,
      rating: 5.0,
      badgeKey: 'RoomRack:Available',
    },
    {
      id: 'room-villa-03',
      name: 'Family Garden Villa',
      nameKey: 'Family Garden Villa',
      image: 'https://images.unsplash.com/photo-1566665797739-1674de7a421a?auto=format&fit=crop&w=800&q=80',
      price: 2750000,
      capacity: 5,
      rating: 4.8,
      badgeKey: 'RoomRack:Available',
    },
  ]);

  // Tiện ích nổi bật
  amenities: AmenityItem[] = [
    {
      icon: 'fa-solid fa-bell-concierge',
      titleKey: 'Landing:AmenityRoomService',
      descKey: 'Landing:AmenityRoomServiceDesc',
    },
    {
      icon: 'fa-solid fa-water-ladder',
      titleKey: 'Landing:AmenityInfinityPool',
      descKey: 'Landing:AmenityInfinityPoolDesc',
    },
    {
      icon: 'fa-solid fa-utensils',
      titleKey: 'Landing:AmenityFineDining',
      descKey: 'Landing:AmenityFineDiningDesc',
    },
    {
      icon: 'fa-solid fa-van-shuttle',
      titleKey: 'Landing:AmenityAirportShuttle',
      descKey: 'Landing:AmenityAirportShuttleDesc',
    },
  ];

  onSearch() {
    this.toastr.info('Common:Search', 'Toastr:Info');
  }

  login() {
    this.authService.navigateToLogin();
  }

  private formatDate(d: Date): string {
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
