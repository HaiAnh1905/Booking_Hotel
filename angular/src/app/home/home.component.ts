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

  // Danh mục phòng nổi bật (tải từ dữ liệu thực khi tích hợp API công khai)
  featuredRooms = signal<SampleRoom[]>([]);

  // Tiện ích nổi bật
  amenities: AmenityItem[] = [
    {
      icon: 'fa-solid fa-bell-concierge',
      titleKey: '::Landing:AmenityRoomService',
      descKey: '::Landing:AmenityRoomServiceDesc',
    },
    {
      icon: 'fa-solid fa-water-ladder',
      titleKey: '::Landing:AmenityInfinityPool',
      descKey: '::Landing:AmenityInfinityPoolDesc',
    },
    {
      icon: 'fa-solid fa-utensils',
      titleKey: '::Landing:AmenityFineDining',
      descKey: '::Landing:AmenityFineDiningDesc',
    },
    {
      icon: 'fa-solid fa-van-shuttle',
      titleKey: '::Landing:AmenityAirportShuttle',
      descKey: '::Landing:AmenityAirportShuttleDesc',
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
