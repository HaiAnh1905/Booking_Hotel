import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { forkJoin, of } from 'rxjs';
import { catchError, filter, take } from 'rxjs/operators';
import { AuthService, ConfigStateService, LocalizationPipe, RestService } from '@abp/ng.core';
import { OAuthStorage } from 'angular-oauth2-oidc';
import { BhLanguageSwitcherComponent } from '../../../shared/components/language-switcher/bh-language-switcher.component';
import { BhPortalSwitcherComponent } from '../../../shared/components/portal-switcher/bh-portal-switcher.component';
import { HotelItemDto, PropertyContextService } from '../../services/property-context.service';
import { HotelService } from '../../services/hotel.service';
import { ManagerHotelProfileService } from '../../services/manager/manager-hotel-profile.service';
import { NavigationItem } from '../../../shared/models/ui-models';

@Component({
  selector: 'bh-receptionist-layout',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    LocalizationPipe,
    BhLanguageSwitcherComponent,
    BhPortalSwitcherComponent,
  ],
  templateUrl: './receptionist-layout.component.html',
  styleUrls: ['./receptionist-layout.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReceptionistLayoutComponent implements OnInit {
  private authService = inject(AuthService);
  private configState = inject(ConfigStateService);
  private rest = inject(RestService);
  private oAuthStorage = inject(OAuthStorage);
  private hotelService = inject(HotelService);
  private hotelProfileService = inject(ManagerHotelProfileService);
  private router = inject(Router);
  readonly propertyService = inject(PropertyContextService);

  isCollapsed = signal<boolean>(false);
  isUserMenuOpen = signal<boolean>(false);
  isMobileSidebarOpen = signal<boolean>(false);
  isLoadingHotel = signal<boolean>(false);

  currentUser = toSignal(this.configState.getOne$('currentUser'), {
    initialValue: this.configState.getOne('currentUser'),
  });
  currentUserName = computed(() => {
    const user = this.currentUser();
    if (!user) return '';
    return user.name ? `${user.surName || ''} ${user.name}`.trim() : (user.userName || '');
  });
  currentUserEmail = computed(() => this.currentUser()?.email || '');

  readonly menuItems: NavigationItem[] = [
    {
      route: '/frontdesk/dashboard',
      labelKey: 'Menu:FrontdeskDashboard',
      icon: 'fa-solid fa-gauge-high',
    },
    {
      route: '/frontdesk/room-rack',
      labelKey: 'Menu:FrontdeskRoomRack',
      icon: 'fa-solid fa-bed',
    },
    {
      route: '/frontdesk/reservations',
      labelKey: 'Menu:FrontdeskReservations',
      icon: 'fa-solid fa-calendar-check',
    },
    {
      route: '/frontdesk/guests',
      labelKey: 'Menu:FrontdeskGuests',
      icon: 'fa-solid fa-users',
    },
    {
      route: '/frontdesk/check-in-out',
      labelKey: 'Menu:FrontdeskCheckInOut',
      icon: 'fa-solid fa-door-open',
    },
  ];

  ngOnInit(): void {
    if (this.propertyService.assignedHotels().length > 0) {
      return;
    }

    const immediateUserId = this.getCurrentUserId();
    if (immediateUserId) {
      this.loadAssignedHotels(immediateUserId);
    } else {
      this.isLoadingHotel.set(true);
      this.configState
        .getOne$('currentUser')
        .pipe(
          filter((u: any) => !!u?.id),
          take(1)
        )
        .subscribe((u: any) => {
          this.loadAssignedHotels(u.id);
        });
    }
  }

  private getCurrentUserId(): string | null {
    const fromState = this.configState.getOne('currentUser')?.id;
    if (fromState) return fromState;

    const storageToken = this.oAuthStorage.getItem('access_token');
    const localToken = typeof localStorage !== 'undefined' ? localStorage.getItem('access_token') : null;
    const token = storageToken || localToken;
    if (!token || token === 'undefined' || token === 'null') return null;

    try {
      const base64Url = token.split('.')[1];
      const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
      const payload = JSON.parse(atob(base64));
      return (
        payload['sub'] ||
        payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ||
        null
      );
    } catch {
      return null;
    }
  }

  private loadAssignedHotels(userId: string): void {
    this.isLoadingHotel.set(true);

    // 1. Tải danh sách ID khách sạn được phân công của nhân sự (không yêu cầu quyền Hotels.Default)
    this.rest
      .request<void, string[]>(
        {
          method: 'GET',
          url: `/api/app/user-hotel-assignment/assigned-hotel-ids/${userId}`,
        },
        { skipHandleError: true }
      )
      .subscribe({
        next: hotelIds => {
          if (hotelIds && hotelIds.length > 0) {
            this.loadHotelProfilesByIds(hotelIds);
          } else {
            this.loadHotelsFallback();
          }
        },
        error: () => {
          this.loadHotelsFallback();
        },
      });
  }

  private loadHotelProfilesByIds(hotelIds: string[]): void {
    const requests = hotelIds.map(id =>
      this.hotelProfileService.get(id).pipe(catchError(() => of(null)))
    );

    forkJoin(requests).subscribe({
      next: profiles => {
        const mapped: HotelItemDto[] = profiles
          .filter((p): p is NonNullable<typeof p> => p !== null)
          .map((p, index) => ({
            id: p.id,
            name: p.name,
            code: p.code,
            address: p.address,
            isPrimary: index === 0,
          }));

        if (mapped.length > 0) {
          this.propertyService.setAssignedHotels(mapped);
          this.isLoadingHotel.set(false);
        } else {
          this.loadHotelsFallback();
        }
      },
      error: () => {
        this.loadHotelsFallback();
      },
    });
  }

  private loadHotelsFallback(): void {
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
          this.isLoadingHotel.set(false);
        } else {
          this.loadCurrentHotelProfileIfPresent();
        }
      },
      error: () => {
        this.loadCurrentHotelProfileIfPresent();
      },
    });
  }

  private loadCurrentHotelProfileIfPresent(): void {
    const existingHotelId = this.propertyService.currentHotelId();
    if (!existingHotelId) {
      this.isLoadingHotel.set(false);
      return;
    }

    this.hotelProfileService.get(existingHotelId).subscribe({
      next: p => {
        this.propertyService.setAssignedHotels([
          {
            id: p.id,
            name: p.name,
            code: p.code,
            address: p.address,
            isPrimary: true,
          },
        ]);
        this.isLoadingHotel.set(false);
      },
      error: () => {
        this.isLoadingHotel.set(false);
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

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as Element | null;
    if (!target || !target.closest) return;
    if (!target.closest('.user-dropdown-container')) {
      this.isUserMenuOpen.set(false);
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.isUserMenuOpen.set(false);
  }

  logout(): void {
    this.authService.logout().subscribe();
  }

  navigateTo(route: string): void {
    this.isMobileSidebarOpen.set(false);
    this.router.navigateByUrl(route);
  }
}
