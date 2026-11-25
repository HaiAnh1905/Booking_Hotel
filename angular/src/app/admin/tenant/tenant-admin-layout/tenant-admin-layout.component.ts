import { ChangeDetectionStrategy, Component, computed, inject, signal, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthService, ConfigStateService, LocalizationPipe } from '@abp/ng.core';
import { OAuthStorage } from 'angular-oauth2-oidc';
import { getUserRoles, getUserTenantId } from '../../guards/role-based-redirect.guard';
import { BhLanguageSwitcherComponent } from '../../../shared/components/language-switcher/bh-language-switcher.component';
import { NavigationItem } from '../../../shared/models/ui-models';

@Component({
  selector: 'bh-tenant-admin-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, LocalizationPipe, BhLanguageSwitcherComponent],
  templateUrl: './tenant-admin-layout.component.html',
  styleUrls: ['./tenant-admin-layout.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TenantAdminLayoutComponent {
  private authService = inject(AuthService);
  private configState = inject(ConfigStateService);
  private router = inject(Router);

  private oAuthStorage = inject(OAuthStorage);

  isCollapsed = signal<boolean>(false);
  isUserMenuOpen = signal<boolean>(false);
  isMobileSidebarOpen = signal<boolean>(false);

  currentUser = computed(() => this.configState.getOne('currentUser'));
  currentTenant = computed(() => this.configState.getOne('currentTenant'));

  isPlatformAdmin = computed(() => {
    const u = this.currentUser();
    const t = this.currentTenant();
    const tenantId = t?.id || u?.tenantId || getUserTenantId(this.oAuthStorage, this.configState);
    const isTenantUser = !!tenantId;
    const roles = getUserRoles(this.oAuthStorage, this.configState);
    return !isTenantUser && (roles.includes('SystemAdmin') || roles.includes('admin'));
  });

  currentTenantName = computed(() => this.currentTenant()?.name || '');
  currentUserName = computed(() => {
    const user = this.currentUser();
    if (!user) return '';
    return user.name ? `${user.surName || ''} ${user.name}`.trim() : (user.userName || '');
  });
  currentUserEmail = computed(() => this.currentUser()?.email || '');

  menuItems = computed<NavigationItem[]>(() => {
    const isPlatform = this.isPlatformAdmin();
    const items: NavigationItem[] = [
      {
        route: '/tenant/dashboard',
        labelKey: 'Menu:TenantDashboard',
        icon: 'fa-solid fa-chart-pie',
      },
      {
        route: '/tenant/hotels',
        labelKey: 'Menu:TenantHotels',
        icon: 'fa-solid fa-hotel',
      },
      {
        route: '/tenant/rate-plans',
        labelKey: 'Menu:TenantRatePlans',
        icon: 'fa-solid fa-tags',
      },
      {
        route: '/tenant/staff',
        labelKey: 'Menu:TenantStaff',
        icon: 'fa-solid fa-users-gear',
      },
    ];

    if (isPlatform) {
      items.push({
        route: '/tenant/roles',
        labelKey: 'Identity:RoleListTitle',
        icon: 'fa-solid fa-shield-halved',
      });
    }

    items.push(
      {
        route: '/tenant/bookings',
        labelKey: 'Menu:TenantBookings',
        icon: 'fa-solid fa-calendar-check',
      },
      {
        route: '/tenant/reports',
        labelKey: 'Menu:TenantReports',
        icon: 'fa-solid fa-chart-line',
      },
      {
        route: '/tenant/organization',
        labelKey: 'Menu:TenantOrganization',
        icon: 'fa-solid fa-building-user',
      }
    );

    return items;
  });

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

