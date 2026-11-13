import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthService, LocalizationPipe } from '@abp/ng.core';
import { BhLanguageSwitcherComponent } from '../../shared/components/language-switcher/bh-language-switcher.component';
import { NavigationItem } from '../../shared/models/ui-models';

@Component({
  selector: 'bh-management-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, LocalizationPipe, BhLanguageSwitcherComponent],
  templateUrl: './management-layout.component.html',
  styleUrls: ['./management-layout.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ManagementLayoutComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

  isCollapsed = signal<boolean>(false);
  isUserMenuOpen = signal<boolean>(false);
  isMobileSidebarOpen = signal<boolean>(false);

  // Danh mục điều hướng phân hệ quản trị
  readonly menuItems: NavigationItem[] = [
    {
      route: '/admin/ui-showcase',
      labelKey: 'Menu:UiShowcase',
      icon: 'fa-solid fa-shapes',
    },
    {
      route: '/admin/room-rack-preview',
      labelKey: 'Menu:RoomRackPreview',
      icon: 'fa-solid fa-bed',
      badgeCount: 12,
      badgeVariant: 'accent',
    },
    {
      route: '/identity/roles',
      labelKey: 'Menu:StaffAndRoles',
      icon: 'fa-solid fa-users-gear',
    },
    {
      route: '/tenant-management',
      labelKey: 'Permission:Tenants',
      icon: 'fa-solid fa-building-user',
    },
  ];

  get isAuthenticated(): boolean {
    return this.authService.isAuthenticated;
  }

  toggleSidebar() {
    this.isCollapsed.update(v => !v);
  }

  toggleMobileSidebar() {
    this.isMobileSidebarOpen.update(v => !v);
  }

  toggleUserMenu() {
    this.isUserMenuOpen.update(v => !v);
  }

  logout() {
    this.authService.logout().subscribe();
  }

  navigateTo(route: string) {
    this.isMobileSidebarOpen.set(false);
    this.router.navigateByUrl(route);
  }
}
