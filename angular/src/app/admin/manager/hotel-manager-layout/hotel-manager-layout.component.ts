import { ChangeDetectionStrategy, Component, OnInit, computed, effect, inject, signal, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router, ActivatedRoute } from '@angular/router';
import { AuthService, ConfigStateService, LocalizationPipe } from '@abp/ng.core';
import { BhLanguageSwitcherComponent } from '../../../shared/components/language-switcher/bh-language-switcher.component';
import { BhPortalSwitcherComponent } from '../../../shared/components/portal-switcher/bh-portal-switcher.component';
import { PropertyContextService, HotelItemDto } from '../../services/property-context.service';
import { HotelService } from '../../services/hotel.service';
import {
  OperationalApprovalService,
  ApprovalTicketStatus,
} from '../../services/operational-approval.service';
import { NavigationItem } from '../../../shared/models/ui-models';
import { take } from 'rxjs/operators';

@Component({
  selector: 'bh-hotel-manager-layout',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    LocalizationPipe,
    BhLanguageSwitcherComponent,
    BhPortalSwitcherComponent,
  ],
  templateUrl: './hotel-manager-layout.component.html',
  styleUrls: ['./hotel-manager-layout.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HotelManagerLayoutComponent implements OnInit {
  private authService = inject(AuthService);
  private configState = inject(ConfigStateService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private hotelService = inject(HotelService);
  private approvalService = inject(OperationalApprovalService);
  readonly propertyService = inject(PropertyContextService);

  isCollapsed = signal<boolean>(false);
  isUserMenuOpen = signal<boolean>(false);
  isMobileSidebarOpen = signal<boolean>(false);
  isPropertyDropdownOpen = signal<boolean>(false);

  currentUser = computed(() => this.configState.getOne('currentUser'));
  currentUserName = computed(() => {
    const user = this.currentUser();
    if (!user) return '';
    return user.name ? `${user.surName || ''} ${user.name}`.trim() : (user.userName || '');
  });
  currentUserEmail = computed(() => this.currentUser()?.email || '');

  readonly menuItems = signal<NavigationItem[]>([
    {
      route: '/manager/dashboard',
      labelKey: 'Menu:ManagerDashboard',
      icon: 'fa-solid fa-chart-line',
    },
    {
      route: '/manager/hotel-profile',
      labelKey: 'Menu:ManagerHotelProfile',
      icon: 'fa-solid fa-hotel',
    },
    {
      route: '/manager/rooms',
      labelKey: 'Menu:ManagerRooms',
      icon: 'fa-solid fa-bed',
    },
    {
      route: '/manager/rates',
      labelKey: 'Menu:ManagerRates',
      icon: 'fa-solid fa-tags',
    },
    {
      route: '/manager/bookings',
      labelKey: 'Menu:ManagerBookings',
      icon: 'fa-solid fa-calendar-check',
    },
    {
      route: '/manager/guests',
      labelKey: 'Menu:ManagerGuests',
      icon: 'fa-solid fa-users',
    },
    {
      route: '/manager/reports',
      labelKey: 'Menu:ManagerReports',
      icon: 'fa-solid fa-chart-pie',
    },
    {
      route: '/manager/feedback',
      labelKey: 'Menu:ManagerFeedback',
      icon: 'fa-solid fa-comments',
    },
    {
      route: '/manager/approvals',
      labelKey: 'Menu:ManagerApprovals',
      icon: 'fa-solid fa-clipboard-check',
      badgeVariant: 'warning',
    },
  ]);

  constructor() {
    effect(() => {
      const hotelId = this.propertyService.currentHotelId();
      if (hotelId) {
        this.refreshApprovalsBadge(hotelId);
      } else {
        this.updateApprovalsBadge(0);
      }
    });

    this.approvalService.ticketChanged$.subscribe(() => {
      const hotelId = this.propertyService.currentHotelId();
      if (hotelId) {
        this.refreshApprovalsBadge(hotelId);
      }
    });
  }

  private refreshApprovalsBadge(hotelId: string): void {
    this.approvalService
      .getList({
        hotelId,
        status: ApprovalTicketStatus.Pending,
        maxResultCount: 1,
      })
      .subscribe({
        next: res => this.updateApprovalsBadge(res.totalCount || 0),
        error: () => this.updateApprovalsBadge(0),
      });
  }

  private updateApprovalsBadge(count: number): void {
    this.menuItems.update(items =>
      items.map(item =>
        item.route === '/manager/approvals'
          ? { ...item, badgeCount: count > 0 ? count : undefined }
          : item
      )
    );
  }

  ngOnInit(): void {
    // 1. Đồng bộ ngữ cảnh từ URL Query Param (?hotelId=...)
    this.route.queryParams.pipe(take(1)).subscribe(params => {
      const urlHotelId = params['hotelId'];
      if (urlHotelId) {
        this.propertyService.setActiveHotel(urlHotelId);
      }
    });

    // 2. Tải danh sách chi nhánh khách sạn thực tế từ Backend
    this.hotelService.getList({ maxResultCount: 50 }).subscribe({
      next: res => {
        if (res?.items && res.items.length > 0) {
          const mapped: HotelItemDto[] = res.items.map((h, index) => ({
            id: h.id,
            name: h.name,
            code: h.code,
            address: h.address,
            isPrimary: index === 0,
          }));
          this.propertyService.setAssignedHotels(mapped);

          // Cập nhật URL query parameter nếu chưa có
          const currentId = this.propertyService.currentHotelId();
          if (currentId) {
            this.syncHotelToUrl(currentId);
          }
        } else {
          this.propertyService.setAssignedHotels([]);
        }
      },
      error: () => {
        // Giữ danh sách mặc định nếu chưa có kết nối API
        const currentId = this.propertyService.currentHotelId();
        if (currentId) {
          this.syncHotelToUrl(currentId);
        }
      },
    });
  }

  get isAuthenticated(): boolean {
    return this.authService.isAuthenticated;
  }

  toggleSidebar(): void {
    this.isCollapsed.update(v => !v);
  }

  toggleMobileSidebar(): void {
    this.isMobileSidebarOpen.update(v => !v);
  }

  toggleUserMenu(): void {
    this.isUserMenuOpen.update(v => !v);
  }

  togglePropertyDropdown(): void {
    this.isPropertyDropdownOpen.update(v => !v);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as Element | null;
    if (!target || !target.closest) return;

    if (!target.closest('.property-selector-container')) {
      this.isPropertyDropdownOpen.set(false);
    }
    if (!target.closest('.user-dropdown-container')) {
      this.isUserMenuOpen.set(false);
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.isPropertyDropdownOpen.set(false);
    this.isUserMenuOpen.set(false);
  }

  selectHotel(hotelId: string): void {
    this.propertyService.setActiveHotel(hotelId);
    this.isPropertyDropdownOpen.set(false);
    this.syncHotelToUrl(hotelId);
  }

  private syncHotelToUrl(hotelId: string): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { hotelId },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  logout(): void {
    this.authService.logout().subscribe();
  }

  navigateTo(route: string): void {
    this.isMobileSidebarOpen.set(false);
    const hotelId = this.propertyService.currentHotelId();
    this.router.navigate([route], {
      queryParams: hotelId ? { hotelId } : {},
      queryParamsHandling: 'merge',
    });
  }
}
